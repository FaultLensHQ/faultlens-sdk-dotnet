using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FaultLens.Sdk.Transport
{
    /// <summary>
    /// Single authority for turning a non-2xx /api/events/ingest response into a <see cref="TransportResult"/>.
    /// Mirrors the backend contract in docs/ingestion-api.md (faultlens-backend):
    ///   409 event_identity_conflict            -> terminal
    ///   429 monthly_event_capacity_exhausted    -> terminal
    ///   429 (anything else, incl. no body)      -> transient (ordinary ingestion rate limiting)
    ///   503 monthly_event_* authority failure   -> transient
    /// A 429/503 body is read only to look for evidence of the specific reasonCode; if the body is
    /// missing or does not parse, the SDK fails closed and falls back to the generic classification for
    /// that status code rather than guessing.
    /// </summary>
    internal static class IngestResponseClassifier
    {
        private const string CapacityExhaustedReasonCode = "monthly_event_capacity_exhausted";
        private const string IdentityConflictReasonCode = "event_identity_conflict";

        public static async Task<TransportResult> ClassifyAsync(HttpResponseMessage response)
        {
            var status = response.StatusCode;

            if (status == HttpStatusCode.Conflict)
            {
                var body = await TryReadBodyAsync(response).ConfigureAwait(false);
                return TransportResult.Permanent(
                    IngestFailureKind.IdentityConflict,
                    body.ReasonCode ?? IdentityConflictReasonCode,
                    body.Message ?? "The event identity conflicts with an already accepted event.",
                    body.ReasonCode ?? IdentityConflictReasonCode);
            }

            if (status == HttpStatusCode.TooManyRequests)
            {
                var body = await TryReadBodyAsync(response).ConfigureAwait(false);
                if (body.ReasonCode == CapacityExhaustedReasonCode)
                {
                    return TransportResult.Permanent(
                        IngestFailureKind.CapacityExhausted,
                        CapacityExhaustedReasonCode,
                        body.Message ?? "Monthly accepted-event capacity is exhausted.",
                        CapacityExhaustedReasonCode,
                        body.PeriodEndUtc);
                }

                // No evidence this 429 is monthly-capacity exhaustion (missing/malformed body, or a
                // different reasonCode e.g. ordinary ingestion rate limiting) -> fail closed, treat as
                // the existing generic/retryable 429 behaviour rather than assuming a terminal outcome.
                return TransportResult.Transient(
                    IngestFailureKind.Throttled,
                    "rate_limited",
                    "FaultLens ingestion rate limit exceeded; retry with backoff.",
                    body.ReasonCode);
            }

            if (status == HttpStatusCode.ServiceUnavailable)
            {
                var body = await TryReadBodyAsync(response).ConfigureAwait(false);
                return TransportResult.Transient(
                    IngestFailureKind.ServiceUnavailable,
                    body.ReasonCode ?? "ServiceUnavailable",
                    body.Message ?? "FaultLens ingestion capacity authority is temporarily unavailable.",
                    body.ReasonCode);
            }

            if ((int)status >= 500)
            {
                return TransportResult.Transient(IngestFailureKind.NetworkError, status.ToString(), "Transient ingest failure");
            }

            return TransportResult.Permanent(IngestFailureKind.Http, status.ToString(), "Permanent ingest failure");
        }

        private static async Task<ParsedBody> TryReadBodyAsync(HttpResponseMessage response)
        {
            try
            {
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(text))
                    return ParsedBody.Empty;

                using (var document = JsonDocument.Parse(text))
                {
                    var root = document.RootElement;
                    string reasonCode = TryGetString(root, "reasonCode");
                    string message = TryGetString(root, "message");
                    var periodEndUtc = TryGetDateTimeOffset(root, "periodEndUtc");
                    return new ParsedBody(reasonCode, message, periodEndUtc);
                }
            }
            catch
            {
                // Malformed/unreadable body -> fail closed: no evidence, caller falls back to the
                // generic classification for the status code.
                return ParsedBody.Empty;
            }
        }

        private static string TryGetString(JsonElement root, string propertyName)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        private static System.DateTimeOffset? TryGetDateTimeOffset(JsonElement root, string propertyName)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                System.DateTimeOffset.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private readonly struct ParsedBody
        {
            public static readonly ParsedBody Empty = new ParsedBody(null, null, null);

            public ParsedBody(string reasonCode, string message, System.DateTimeOffset? periodEndUtc)
            {
                ReasonCode = reasonCode;
                Message = message;
                PeriodEndUtc = periodEndUtc;
            }

            public string ReasonCode { get; }
            public string Message { get; }
            public System.DateTimeOffset? PeriodEndUtc { get; }
        }
    }
}
