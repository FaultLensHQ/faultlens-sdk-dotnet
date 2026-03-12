using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class BreadcrumbInfo
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; }

        [JsonPropertyName("sequence")]
        public int Sequence { get; }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("category")]
        public string Category { get; }

        [JsonPropertyName("level")]
        public string Level { get; }

        [JsonPropertyName("message")]
        public string Message { get; }

        [JsonPropertyName("source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Source { get; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> Data { get; }

        public BreadcrumbInfo(
            string timestamp,
            int sequence,
            string type,
            string category,
            string level,
            string message,
            string source = null,
            IReadOnlyDictionary<string, object> data = null)
        {
            Timestamp = timestamp;
            Sequence = sequence;
            Type = type;
            Category = category;
            Level = level;
            Message = message;
            Source = source;
            Data = data;
        }
    }
}
