using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Exceptions;
using Translumo.Utils.Http;
using System.Text.RegularExpressions;

namespace Translumo.Translation.Local
{
    public class LocalTranslator : BaseTranslator<LocalContainer>
    {
        private const string _placeholder_text = "[content]";
        private readonly ILogger _logger;

        private readonly string _localServerURL;

        private readonly string _localServerPayload;

        private readonly string _localServerResponsePath;
        
        public LocalTranslator(TranslationConfiguration translationConfiguration, LanguageService languageService, ILogger logger) 
            : base(translationConfiguration, languageService, logger)
        {
            _logger = logger;
            _localServerURL = translationConfiguration.LocalServerURL;
            _localServerPayload = translationConfiguration.LocalServerPayload;
            _localServerResponsePath = translationConfiguration.LocalServerResponsePath;
        }

        public override Task<string> TranslateTextAsync(string sourceText)
        {
            return base.TranslateTextAsync(sourceText);
        }

        protected override async Task<string> TranslateTextInternal(LocalContainer container, string sourceText)
        {
            var dataIn = _localServerPayload.Replace(_placeholder_text, sourceText);
            _logger.LogTrace($"requesting translation from locally run server with body '{dataIn}' and endpoint '{_localServerURL}'");

            var response = await container.Reader.RequestWebDataAsync(_localServerURL, HttpMethods.POST, dataIn, true).ConfigureAwait(false);

            if (response.IsSuccessful)
            {
                _logger.LogTrace("response is received successfully - parsing the value");
                var foundVal = GetValueByPath(response.Body, _localServerResponsePath);
                if (foundVal != null)
                {
                    return foundVal.Value.GetString();
                } 
                else
                {
                    _logger.LogTrace("failed to parse any non-zero string value");
                    return "";
                }
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
        
        /// <summary>
        /// Finds a JSON element from a JSON string using a simplified jq-like path.
        /// </summary>
        /// <param name="jsonString">The raw JSON string to search.</param>
        /// <param name="path">The dot-notation path (e.g., "choices[0].message.content").</param>
        /// <returns>A nullable JsonElement. HasValue will be false if the path is not found or an error occurs.
        /// You can check the .ValueKind and use .GetString(), .GetInt32(), etc. on the result.</returns>
        public JsonElement? GetValueByPath(string jsonString, string path)
        {
            if (string.IsNullOrEmpty(jsonString) || string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                // Parse the entire JSON string into a read-only document
                using (JsonDocument document = JsonDocument.Parse(jsonString))
                {
                    JsonElement currentElement = document.RootElement;
                    
                    // Split the path by the '.' separator
                    string[] segments = path.Split('.');

                    foreach (string segment in segments)
                    {
                        if (string.IsNullOrEmpty(segment))
                        {
                            return null; // Invalid path
                        }

                        // Check if this segment has array accessors like [0]
                        MatchCollection indexMatches = Regex.Matches(segment, @"\[(\d+)\]");

                        // The property name is the part *before* the first array accessor
                        string propName = indexMatches.Count > 0 ? segment.Substring(0, segment.IndexOf('[')) : segment;

                        // 1. Access the property (if the current element is an object)
                        if (currentElement.ValueKind == JsonValueKind.Object)
                        {
                            if (!currentElement.TryGetProperty(propName, out currentElement))
                            {
                                _logger.LogWarning($"Error: Property '{propName}' not found.");
                                return null; // Property not found
                            }
                        }
                        else if (indexMatches.Count == 0)
                        {
                            // Path asks for a property, but current element is not an object
                            _logger.LogWarning($"Error: Cannot access property '{propName}' on a non-object.");
                            return null; 
                        }

                        // 2. Handle array indices, if any
                        foreach (Match match in indexMatches)
                        {
                            if (currentElement.ValueKind != JsonValueKind.Array)
                            {
                                _logger.LogWarning($"Error: Trying to index into a non-array element with path segment '{segment}'.");
                                return null; // Trying to index into a non-array
                            }

                            if (int.TryParse(match.Groups[1].Value, out int index))
                            {
                                if (index < 0 || index >= currentElement.GetArrayLength())
                                {
                                    _logger.LogWarning($"Error: Index {index} out of bounds for path segment '{segment}'.");
                                    return null; // Index out of bounds
                                }
                                // Move to the element at the specified index
                                currentElement = currentElement[index];
                            }
                            else
                            {
                                return null; // Should not happen with our regex
                            }
                        }
                    }
                    
                    // After processing all segments, currentElement is our result
                    return currentElement.Clone();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"JSON Parsing Error: {ex.Message}");
                // Invalid JSON string
                return null;
            }
        }
    }
}
