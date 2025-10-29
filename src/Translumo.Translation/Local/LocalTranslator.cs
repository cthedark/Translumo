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
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;

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
            string dataIn = _localServerPayload.Replace(_placeholder_text, sourceText);
            string sanitizedDataIn = SanitizeJSON(dataIn);

            // As a way to sanitize this, we will serialize and deserialize it.


            _logger.LogTrace($"requesting translation from locally run server with body '{sanitizedDataIn}' and endpoint '{_localServerURL}'");

            var response = await container.Reader.RequestWebDataAsync(_localServerURL, HttpMethods.POST, sanitizedDataIn, true).ConfigureAwait(false);

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
                throw new TranslationException($"Bad response by translator: '{response.Body}' inner exception? '{response.InnerException}'");
            }
        }

        protected override IList<LocalContainer> CreateContainers(TranslationConfiguration configuration)
        {
            var result = configuration.ProxySettings.Select(proxy => new LocalContainer(proxy)).ToList();
            result.Add(new LocalContainer(isPrimary: true));

            return result;
        }

        private static readonly JsonSerializerOptions SanitizeOptions = new JsonSerializerOptions
        {
            // Setting the Encoder to JavaScriptEncoder.Default forces all non-ASCII
            // characters (including '、', 'é', 'ñ', etc.) to be written as \uXXXX
            // Unicode escape sequences, which is highly compatible with strict servers.
            Encoder = JavaScriptEncoder.Default,

            // Optional: Keep the output JSON compact (no extra spaces)
            WriteIndented = false
        };

        public string SanitizeJSON(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                // Handle null or empty input gracefully
                return "{}";
            }

            try
            {
                // Optional Safety Step: Remove carriage returns and newlines from the input
                // string just in case the parser is overly strict. This is often unnecessary
                // if the JSON is correctly formatted, but provides extra robustness.
                string compactInput = jsonString
                    .Replace("\r\n", "") // Standard Windows line ending
                    .Replace("\n", "")   // Standard Unix/Linux line ending
                    .Replace("\r", "");   // Standard Mac line ending (less common)
                    
                // 1. Parse the input string into a generic JSON object (JsonNode).
                // This step will throw an exception if the input is not valid JSON.
                var jsonNode = JsonNode.Parse(compactInput);

                // 2. Re-serialize the object using the specified options.
                // This step applies the JavaScriptEncoder.Default setting,
                // which escapes characters like '、' to '\u3001'.
                string sanitizedJson = jsonNode.ToJsonString(SanitizeOptions);

                return sanitizedJson;
            }
            catch (JsonException ex)
            {
                // Log the parsing error if needed, but return an error indicator.
                _logger.LogWarning($"Error parsing JSON input: {ex.Message}");
                // Return an empty or error-indicating JSON object as a fallback
                return "{\"error\": \"Invalid JSON input\"}";
            }
    
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
