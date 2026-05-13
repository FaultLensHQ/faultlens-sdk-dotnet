using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class RequestContextInfo
    {
        [JsonPropertyName("requestedUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RequestedUrl { get; }

        [JsonPropertyName("method")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Method { get; }

        [JsonPropertyName("route")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Route { get; }

        [JsonPropertyName("queryString")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string QueryString { get; }

        [JsonPropertyName("referrer")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Referrer { get; }

        public RequestContextInfo(
            string requestedUrl = null,
            string method = null,
            string route = null,
            string queryString = null,
            string referrer = null)
        {
            RequestedUrl = requestedUrl;
            Method = method;
            Route = route;
            QueryString = queryString;
            Referrer = referrer;
        }
    }
}
