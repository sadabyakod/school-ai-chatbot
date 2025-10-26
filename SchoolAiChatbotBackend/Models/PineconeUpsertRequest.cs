using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SchoolAiChatbotBackend.Models
{
    public class PineconeUpsertRequest
    {  [JsonPropertyName("vectors")]
        public List<PineconeVector> Vectors { get; set; }
    }

    public class PineconeVector
    {   [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("values")]
        public List<float> Values { get; set; }
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; }
    }
}
