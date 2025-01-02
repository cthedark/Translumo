using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Translumo.Translation.Local
{
    public class LocalRequest
    {
        public LocalRequestBody Body { get; set; }

        public sealed class LocalRequestBody
        {
            [JsonPropertyName("q")]
            public string Query { get; set; }

            [JsonPropertyName("source")]
            public string Source { get; set; }
            [JsonPropertyName("target")]
    
            public string Target { get; set; }
            [JsonPropertyName("format")]

            public string Format { get; set; }
            [JsonPropertyName("alternatives")]

            public int Alternatives { get; set; }
            [JsonPropertyName("api_key")]

            public string ApiKey { get; set; }

            public string ToJsonString()
            {
                return JsonSerializer.Serialize(this);
            }
        }
    }
}
