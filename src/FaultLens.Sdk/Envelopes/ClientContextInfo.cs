using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class ClientContextInfo
    {
        [JsonPropertyName("userAgent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string UserAgent { get; }

        [JsonPropertyName("runtimeName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RuntimeName { get; }

        [JsonPropertyName("runtimeVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RuntimeVersion { get; }

        public ClientContextInfo(
            string userAgent = null,
            string runtimeName = null,
            string runtimeVersion = null)
        {
            UserAgent = userAgent;
            RuntimeName = runtimeName;
            RuntimeVersion = runtimeVersion;
        }
    }
}
