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

        [JsonPropertyName("layer")]
        public string Layer { get; }

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

        [JsonPropertyName("entityType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string EntityType { get; }

        [JsonPropertyName("entityId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string EntityId { get; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> Data { get; }

        public BreadcrumbInfo(
            string timestamp,
            int sequence,
            string layer,
            string type,
            string category,
            string level,
            string message,
            string source = null,
            string entityType = null,
            string entityId = null,
            IReadOnlyDictionary<string, object> data = null)
        {
            Timestamp = timestamp;
            Sequence = sequence;
            Layer = layer;
            Type = type;
            Category = category;
            Level = level;
            Message = message;
            Source = source;
            EntityType = entityType;
            EntityId = entityId;
            Data = data;
        }
    }
}
