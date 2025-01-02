using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Exceptions;
using Translumo.Utils.Http;

namespace Translumo.Translation.Local
{
    public class LocalTranslator : BaseTranslator<LocalContainer>
    {
        private const string TRANSLATE_URL = "http://127.0.0.1:5000/translate";
        private ILogger _logger;
        
        public LocalTranslator(TranslationConfiguration translationConfiguration, LanguageService languageService, ILogger logger) 
            : base(translationConfiguration, languageService, logger)
        {
            _logger = logger;
        }

        public override Task<string> TranslateTextAsync(string sourceText)
        {
            return base.TranslateTextAsync(sourceText);
        }

        protected override async Task<string> TranslateTextInternal(LocalContainer container, string sourceText)
        {

            var request = LocalRequestFactory.CreateRequest(container, sourceText, SourceLangDescriptor.IsoCode,
                TargetLangDescriptor.IsoCode);

            var dataIn = request.Body.ToJsonString();
            _logger.LogTrace($"requesting translation from locally run LibreTranslate server with body '{dataIn}' and headers ");

            var response = await container.Reader.RequestWebDataAsync(TRANSLATE_URL, HttpMethods.POST, dataIn, true).ConfigureAwait(false);

            if (response.IsSuccessful)
            {
                var localResponse = JsonSerializer.Deserialize<LocalResponse>(response.Body);
                if (localResponse == null)
                {
                    throw new TranslationException($"Unexpected response: '{response.Body}'");
                }

                return localResponse.TranslatedText;
            }
            else
            {
                throw new TranslationException($"Bad response by translator: '{response.Body}'");
            }
            
        }
        
        protected override IList<LocalContainer> CreateContainers(TranslationConfiguration configuration)
        {
            var result = configuration.ProxySettings.Select(proxy => new LocalContainer(proxy)).ToList();
            result.Add(new LocalContainer(isPrimary: true));

            return result;
        }
    }
}
