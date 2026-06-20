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

        [JsonPropertyName("statusCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StatusCode { get; }

        [JsonPropertyName("durationMs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? DurationMs { get; }

        [JsonPropertyName("requestId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RequestId { get; }

        [JsonPropertyName("correlationId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CorrelationId { get; }

        [JsonPropertyName("traceId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TraceId { get; }

        public RequestContextInfo(
            string requestedUrl = null,
            string method = null,
            string route = null,
            string queryString = null,
            string referrer = null,
            int? statusCode = null,
            double? durationMs = null,
            string requestId = null,
            string correlationId = null,
            string traceId = null)
        {
            RequestedUrl = requestedUrl;
            Method = method;
            Route = route;
            QueryString = queryString;
            Referrer = referrer;
            StatusCode = statusCode;
            DurationMs = durationMs;
            RequestId = requestId;
            CorrelationId = correlationId;
            TraceId = traceId;
        }
    }
}
