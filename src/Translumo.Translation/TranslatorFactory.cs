using System;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Dispatching;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Deepl;
using Translumo.Translation.Google;
using Translumo.Translation.Yandex;
using Translumo.Translation.Local;

namespace Translumo.Translation
{
    public class TranslatorFactory
    {
        private readonly LanguageService _languageService;
        private readonly IActionDispatcher _actionDispatcher;
        private readonly ILogger _logger;

        public TranslatorFactory(LanguageService languageService, IActionDispatcher actionDispatcher, ILogger<TranslatorFactory> logger)
        {
            this._languageService = languageService;
            this._actionDispatcher = actionDispatcher;
            this._logger = logger;
        }

        public ITranslator CreateTranslator(TranslationConfiguration translatorConfiguration)
        {
            switch (translatorConfiguration.Translator)
            {
                case Translators.Deepl:
                    return new DeepLTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Yandex:
                    return new YandexTranslator(translatorConfiguration, _languageService, _actionDispatcher, _logger);
                case Translators.Google:
                    return new GoogleTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Local:
                    return new LocalTranslator(translatorConfiguration, _languageService, _logger);
                default:
                    throw new NotSupportedException();
            }
        }

        public LocalDB LoadLocalDB(TranslationConfiguration config)
        {
            if (!config.UseLocalDB || string.IsNullOrEmpty(config.UseLocalDBDir)) {
                return null;
            }
            var ldb = new LocalDB(config.UseLocalDBDir, _logger);

            var success = ldb.SetLanguagePair(
                _languageService.GetLanguageDescriptor(config.TranslateFromLang).IsoCode,
                _languageService.GetLanguageDescriptor(config.TranslateToLang).IsoCode
            );

            if (success) return ldb;
            
            return null;
        }
    }
}
