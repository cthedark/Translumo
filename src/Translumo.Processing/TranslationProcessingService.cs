using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Translumo.Infrastructure;
using Translumo.OCR;
using Translumo.OCR.Configuration;
using Translumo.Processing.Configuration;
using Translumo.Processing.Exceptions;
using Translumo.Processing.Interfaces;
using Translumo.Processing.TextProcessing;
using Translumo.Translation;
using Translumo.Translation.Configuration;
using Translumo.Translation.Exceptions;
using Translumo.TTS;
using Translumo.TTS.Engines;

namespace Translumo.Processing
{

    public class TranslationProcessingService : IProcessingService, IDisposable
    {
        public bool IsStarted => !_ctSource?.IsCancellationRequested ?? false;

        private readonly ICapturerFactory _capturerFactory;
        private readonly IChatTextMediator _chatTextMediator;
        private readonly OcrEnginesFactory _enginesFactory;
        private readonly TranslatorFactory _translatorFactory;
        private readonly TtsFactory _ttsFactory;
        private readonly TtsConfiguration _ttsConfiguration;
        private readonly TextDetectionProvider _textProvider;
        private readonly TextResultCacheService _textResultCacheService;
        private readonly ILogger _logger;
        private static readonly object _obj = new object();

        private ITTSEngine _ttsEngine;
        private IEnumerable<IOCREngine> _engines;
        private ITranslator _translator;
        private LocalDB _localDB;
        private TranslationConfiguration _translationConfiguration;
        private OcrGeneralConfiguration _ocrGeneralConfiguration;
        private TextProcessingConfiguration _textProcessingConfiguration;

        private CancellationTokenSource _ctSource;
        private IScreenCapturer _capturer;
        private IScreenCapturer _onceTimeCapturer;

        private long _lastTranslatedTextTicks;

        private const float MIN_SCORE_THRESHOLD = 2.1f;

        private const int CHAR_LENGTH_NOTICE = 5000; // send a text telling the user how many characters were translated in this session every N chars.

        private int _totalCharSentForTranslation = 0;

        private string _lastDetectedText = "";
        
        public TranslationProcessingService(ICapturerFactory capturerFactory, IChatTextMediator chatTextMediator, OcrEnginesFactory ocrEnginesFactory,
            TranslatorFactory translationFactory, TtsFactory ttsFactory, TtsConfiguration ttsConfiguration,
            TextDetectionProvider textProvider, TranslationConfiguration translationConfiguration, OcrGeneralConfiguration ocrConfiguration, 
            TextResultCacheService textResultCacheService, TextProcessingConfiguration textConfiguration, ILogger<TranslationProcessingService> logger)
        {
            _logger = logger;
            _chatTextMediator = chatTextMediator;
            _capturerFactory = capturerFactory;
            _translationConfiguration = translationConfiguration;
            _ocrGeneralConfiguration = ocrConfiguration;
            _enginesFactory = ocrEnginesFactory;
            _textProvider = textProvider;
            _textResultCacheService = textResultCacheService;
            _translatorFactory = translationFactory;
            _ttsFactory = ttsFactory;
            _ttsConfiguration = ttsConfiguration;
            _ttsEngine = ttsFactory.CreateTtsEngine(ttsConfiguration);
            _textProcessingConfiguration = textConfiguration;
            _engines = InitializeEngines();
            _translator = _translatorFactory.CreateTranslator(_translationConfiguration);
            InitLocalDB();
            _textProvider.Language = translationConfiguration.TranslateFromLang;

            _translationConfiguration.PropertyChanged += TranslationConfigurationOnPropertyChanged;
            _ocrGeneralConfiguration.PropertyChanged += OcrGeneralConfigurationOnPropertyChanged;
            _ttsConfiguration.PropertyChanged += TtsConfigurationOnPropertyChanged;
        }

        public void StartProcessing()
        {
            if (IsStarted)
            {
                return;
            }

            if (!_engines.Any())
            {
                _chatTextMediator.SendText("No OCR engine is selected!", false);
                return;
            }

            _lastTranslatedTextTicks = DateTime.UtcNow.Ticks;
            _ctSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => TranslateInternal(_ctSource.Token));

            _chatTextMediator.SendText("Translation started", TextTypes.Info);
        }

        public void ProcessOnce(RectangleF captureArea)
        {
            if (!_engines.Any())
            {
                _chatTextMediator.SendText("No OCR engine is selected!", false);
                return;
            }

            Task.Factory.StartNew(() => TranslateOnceInternal(captureArea));
        }

        public void StopProcessing()
        {
            _ctSource.Cancel();

            _chatTextMediator.SendText("Translation finished", TextTypes.Info);

            FlushLocalDB();
        }

        // This creates the repeating task of translating.
        private void TranslateInternal(CancellationToken cancellationToken)
        {
            const int MAX_TRANSLATE_TASK_POOL = 2;
            const int SEQUENTIAL_DIFF_LETTERS = 3;

            IOCREngine primaryOcr = _engines.OrderByDescending(e => e.PrimaryPriority).First();
            IOCREngine[] otherOcr = _engines.Except(new[] { primaryOcr }).ToArray();

            var detectedResults = new Task<TextDetectionResult>[otherOcr.Length + 1];
            var activeTranslationTasks = new List<Task>();
            Mat cachedImg = null;
            Guid iterationId;
            IterationType lastIterationType = IterationType.None;
            bool sequentialText = false;
            int clearTextDelayMs = _textProcessingConfiguration.AutoClearTexts
                ? (int)_textProcessingConfiguration.AutoClearTextsDelayMs * -1
                : int.MaxValue * -1;

            TextDetectionResult GetSecondaryCheckText(byte[] screen)
            {
                Mat grayScaleScreen = ImageHelper.ToGrayScale(screen);
                if (cachedImg != null)
                {
                    var unitedScreen = ImageHelper.UnionImages(cachedImg, grayScaleScreen);

                    cachedImg?.Dispose();
                    cachedImg = grayScaleScreen;

                    return _textProvider.GetText(primaryOcr, unitedScreen);
                }

                cachedImg = grayScaleScreen;

                return null;
            }

            void CapturerEnsureInitialized()
            {
                lock (_obj)
                {
                    if (_capturer == null)
                    {
                        _capturer = _capturerFactory.CreateCapturer(false);
                        if (_capturer == null)
                        {
                            _chatTextMediator.SendText("Failed to initialize capturer. Please check logs for details", false);
                            _ctSource.Cancel();
                        }
                    }
                }
            }

            CapturerEnsureInitialized();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Thread.Sleep(GetIterationDelayMs(lastIterationType, sequentialText));
                    lock (_obj)
                    {
                        if (Interlocked.Read(ref _lastTranslatedTextTicks) < DateTime.UtcNow.AddMilliseconds(clearTextDelayMs).Ticks
                            && !cancellationToken.IsCancellationRequested)
                        {
                            _chatTextMediator.ClearTexts();
                        }

                        _textResultCacheService.EndIteration();

                        var faultedTask = activeTranslationTasks.FirstOrDefault(t => t.IsFaulted);
                        activeTranslationTasks.RemoveAll(task => task.IsCompleted);
                        if (faultedTask != null)
                        {
                            throw faultedTask.Exception.InnerException;
                        }

                        if (activeTranslationTasks.Count >= MAX_TRANSLATE_TASK_POOL)
                        {
                            _logger.LogTrace($"Max task count reached - no-op");
                            continue;
                        }

                        byte[] screenshot = _capturer.CaptureScreen();
                        var primaryDetected = _textProvider.GetText(primaryOcr, screenshot);
                        lastIterationType = IterationType.Short;
                        if (primaryDetected.ValidityScore == 0 || _textResultCacheService.IsCached(primaryDetected.Text, sequentialText))
                        {
                            _logger.LogTrace($"PrimaryCheck no-op;");
                            continue;
                        }

                        if (primaryOcr.SecondaryPrimaryCheck)
                        {
                            var res = GetSecondaryCheckText(screenshot);
                            if (res != null && _textResultCacheService.IsCached(res.Text, false))
                            {
                                if (primaryDetected.Text.Length - res.Text.Length > SEQUENTIAL_DIFF_LETTERS)
                                {
                                    sequentialText = true;
                                }
                                _logger.LogTrace($"SecondaryCheck no-op");
                                continue;
                            }
                        }

                        for (var i = 0; i < otherOcr.Length; i++)
                        {
                            detectedResults[i] = _textProvider.GetTextAsync(otherOcr[i], screenshot);
                        }

                        detectedResults[^1] = Task.FromResult(primaryDetected);
                        lastIterationType = IterationType.Full;
                        Task.WaitAll(detectedResults);

                        TextDetectionResult bestDetected = GetBestDetectionResult(detectedResults, 3);
                        if (bestDetected.ValidityScore <= MIN_SCORE_THRESHOLD)
                        {
                            sequentialText = false;
                            _logger.LogTrace($"Detection score threshold not met '{bestDetected.ValidityScore}';");
                            continue;
                        }

                        if (_textResultCacheService.IsCached(bestDetected.Text, bestDetected.ValidityScore, sequentialText, 
                                bestDetected.Language.Asian, out iterationId))
                        {
                            sequentialText = false;
                            _logger.LogTrace($"Cache Hit");
                            continue;
                        }

                        sequentialText = false;

                        // Remove the portion that is already translated
                        var filteredResult = FilterAndLogResult(bestDetected.Text);
                        if (!string.IsNullOrWhiteSpace(filteredResult)){
                            activeTranslationTasks.Add(TranslateTextAsync(filteredResult, iterationId));
                        }
                    }
                }
                catch (CaptureException ex)
                {
                    if (lastIterationType == IterationType.None)
                    {
                        _chatTextMediator.SendText($"Failed to capture screen ({ex.Message})", false);
                        lastIterationType = IterationType.Short;
                    }

                    _logger.LogError(ex, $"Screen capture failed (code: {ex.ErrorCode})");
                    
                    _capturer.Dispose();
                    _capturer = null;
                    CapturerEnsureInitialized();
                }
                catch (TranslationException)
                {
                    _chatTextMediator.SendText("Text translation is failed", false);
                }
                catch (AggregateException ex) when (ex.InnerException is TextDetectionException innerEx)
                {
                    _chatTextMediator.SendText($"Text detection is failed ({innerEx.SourceOCREngineType.Name})", false);
                    _logger.LogError(ex, $"Unexpected error during text detection ({innerEx.SourceOCREngineType})");
                }
                catch (Exception ex)
                {
                    _chatTextMediator.SendText($"Unknown error: {ex.Message}", false);
                    _logger.LogError(ex, $"Processing iteration failed due to unknown error");
                }
            }
            _textResultCacheService.Reset();
            _logger.LogTrace("Translation finished");
        }

        private void TranslateOnceInternal(RectangleF captureArea)
        {
            const int TRANSLATION_TIMEOUT_MS = 10000;
            _logger.LogTrace($"Translate once with defined capturedArea {captureArea}");

            if (_onceTimeCapturer == null)
            {
                _onceTimeCapturer = _capturerFactory.CreateCapturer(true);
                if (_onceTimeCapturer == null)
                {
                    _chatTextMediator.SendText("Failed to initialize capturer. Please check logs for details", false);

                    return;
                }
            }

            try
            {
                Task translationTask = null;
                lock (_obj)
                {
                    byte[] screenshot = _onceTimeCapturer.CaptureScreen(captureArea);
                    var taskResults = _engines.Select(engine => _textProvider.GetTextAsync(engine, screenshot)).ToArray();
                    // TODO: sometimes one of task (win tts) is not complete long time and translation is not working
                    Task.WaitAll(taskResults);
                    TextDetectionResult bestDetected = GetBestDetectionResult(taskResults, 3);
                    var filteredResult = FilterAndLogResult(bestDetected.Text);
                    if (!string.IsNullOrWhiteSpace(filteredResult)) {
                        translationTask = TranslateTextAsync(filteredResult, Guid.NewGuid());
                    }
                }

                if (translationTask != null) {
                    translationTask.Wait(TRANSLATION_TIMEOUT_MS);
                }
            }
            catch (CaptureException ex)
            {
                _chatTextMediator.SendText($"Failed to capture screen ({ex.Message})", false);
                _logger.LogError(ex, $"Screen capture failed (code: {ex.ErrorCode})");
            }
            catch (AggregateException ex) when (ex.InnerException is TextDetectionException innerEx)
            {
                _chatTextMediator.SendText($"Text detection is failed ({innerEx.SourceOCREngineType.Name})", false);
                _logger.LogError(ex, $"Unexpected error during text detection ({innerEx.SourceOCREngineType})");
            }
            catch (Exception ex)
            {
                _chatTextMediator.SendText($"Unknown error: {ex.Message}", false);
                _logger.LogError(ex, $"Processing iteration failed due to unknown error");
            }
        }

        private async Task TranslateTextAsync(string text, Guid iterationId)
        {
            string translation = "";
            // get translation from local db if it's provided
            if (_localDB != null) {
                translation =_localDB.GetTranslation(text);
            }

            // get translation from external translation service
            if (string.IsNullOrWhiteSpace(translation)) {
                int roundedTotalCharSentBefore = _totalCharSentForTranslation / CHAR_LENGTH_NOTICE;
                _totalCharSentForTranslation += text.Length;
                int roundedTotalCharSentAfter = _totalCharSentForTranslation / CHAR_LENGTH_NOTICE;
                
                _logger.LogTrace($"Adding translation request for '{text}'. Total characters sent to translation: {_totalCharSentForTranslation}.");
                if (roundedTotalCharSentAfter > roundedTotalCharSentBefore) {
                    _chatTextMediator.SendText($"({_totalCharSentForTranslation} characters sent to translation so far)", true);
                }
                translation = await _translator.TranslateTextAsync(text);

                if (_translationConfiguration.AppendToLocalDB && !string.IsNullOrWhiteSpace(translation) && _localDB != null) {
                    // user wants to record all the newly translated strings into local db.
                    // The new translated string is not empty, so we append this result.
                    _localDB.UpdateTranslation(text, translation);
                }
            }

            // process translation result
            if (!string.IsNullOrWhiteSpace(translation) && !_textResultCacheService.IsTranslatedCached(translation, iterationId))
            {
                Interlocked.Exchange(ref _lastTranslatedTextTicks, DateTime.UtcNow.Ticks);
                _chatTextMediator.SendText(translation, true);
                _ttsEngine.SpeechText(translation);
            }
        }

        // Returns the amount of delay before the next translation request is sent.
        private int GetIterationDelayMs(IterationType lastIterationType, bool withSequentialText)
        {
            if (withSequentialText)
            {
                return 1600;
            }

            switch (lastIterationType)
            {
                case IterationType.Full:
                    return 1200;
                case IterationType.Short:
                    return 800;
                default:
                    return 0;
            }
        }

        private TextDetectionResult GetBestDetectionResult(Task<TextDetectionResult>[] results, int minCountSameResults)
        {
            var maxScoreIndex = 0;
            for (var i = 0; i < results.Length; i++)
            {
                maxScoreIndex = results[maxScoreIndex].Result.CompareTo(results[i].Result) > 0 ? maxScoreIndex : i;
                if (i > results.Length - minCountSameResults || results[i].Result.ValidityScore == 0)
                {
                    continue;
                }

                var intRowCount = 1;
                var inRowIndex = i;
                for (var j = i + 1; j < results.Length; j++)
                {
                    if (results[i].Result.ValidatedText == results[j].Result.ValidatedText)
                    {
                        intRowCount++;
                        inRowIndex = results[inRowIndex].Result.SourceEngine.Confidence > results[j].Result.SourceEngine.Confidence
                            ? inRowIndex
                            : j;
                    }
                }
                //If array contains multiple (=minCountSameResults) same results, consider it as the best
                if (intRowCount >= minCountSameResults)
                {
                    results[inRowIndex].Result.ValidityScore = float.MaxValue;
                    return results[inRowIndex].Result;
                }
            }

            return results[maxScoreIndex].Result;
        }

        private void TranslationConfigurationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_translationConfiguration.TranslateFromLang))
            {
                _engines = InitializeEngines();
                _textProvider.Language = _translationConfiguration.TranslateFromLang;
            }

            _translator = _translatorFactory.CreateTranslator(_translationConfiguration);
            InitLocalDB();
        }

        private void TtsConfigurationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_ttsConfiguration.TtsLanguage)
                || e.PropertyName == nameof(_ttsConfiguration.TtsSystem))
            {
                _ttsEngine.Dispose();
                _ttsEngine = _ttsFactory.CreateTtsEngine(_ttsConfiguration);
            }
        }

        private void OcrGeneralConfigurationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _engines = InitializeEngines();
        }

        private void InitLocalDB() {
            FlushLocalDB();
            _localDB = _translatorFactory.LoadLocalDB(_translationConfiguration);
        }

        private void FlushLocalDB() {
            if (_localDB != null && _translationConfiguration.AppendToLocalDB) {
                _localDB.FlushUpdatedTranslations();
            }
        }

        private string FilterAndLogResult(string resultText) {
            _logger.LogTrace($"FilterAndLogResult {resultText}");
            var textToBeTranslated = resultText; // textToBeTranslated is mutated whle resultText is not (important)

            // Remove the portion that is already translated from the last request (useful for "appending" text scroller common in games)
            if (!string.IsNullOrWhiteSpace(_lastDetectedText)) {
                textToBeTranslated = textToBeTranslated.Replace(_lastDetectedText, "");
            }
            _lastDetectedText = resultText;

            // Nothing to translate (complete duplicate from the last query)
            if (string.IsNullOrWhiteSpace(textToBeTranslated)) {
                _logger.LogTrace($"empty string to translate - skipping;");
                return "";
            }

            return textToBeTranslated;
        }

        private IEnumerable<IOCREngine> InitializeEngines()
        {
            return _enginesFactory
                .GetEngines(_ocrGeneralConfiguration.OcrConfigurations, _translationConfiguration.TranslateFromLang)
                .ToArray();
        }

        public void Dispose()
        {
            _logger.LogTrace("TranslationProcessingService Dispose");
            FlushLocalDB();
            _ttsEngine.Dispose();
            _textProvider.Dispose();
            _capturer?.Dispose();
            _onceTimeCapturer?.Dispose();
        }

        public void OnExit()
        {
            Dispose();
        }

        private enum IterationType : byte
        {
            None = 0,
            Full = 1,
            Short = 2
        }
    }
}
