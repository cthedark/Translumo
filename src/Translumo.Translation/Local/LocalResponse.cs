using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Translumo.Translation.Local
{
    internal class LocalResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}
