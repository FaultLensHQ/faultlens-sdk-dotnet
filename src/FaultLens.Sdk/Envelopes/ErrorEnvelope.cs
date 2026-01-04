using System;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class ErrorEnvelope
    {
        // Required
        [JsonPropertyName("eventId")]
        public string EventId { get; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; }

        [JsonPropertyName("environment")]
        public string Environment { get; }

        [JsonPropertyName("sdk")]
        public SdkInfo Sdk { get; }

        // Optional
        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; }

        [JsonPropertyName("exception")]
        public ExceptionInfo Exception { get; }

        [JsonPropertyName("message")]
        public string Message { get; }

        public ErrorEnvelope(
            string eventId,
            DateTimeOffset timestamp,
            string environment,
            SdkInfo sdk,
            string fingerprint = null,
            ExceptionInfo exception = null,
            string message = null)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("EventId is required.", nameof(eventId));

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentException("Environment is required.", nameof(environment));

            EventId = eventId;
            Timestamp = timestamp;
            Environment = environment;
            Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));

            Fingerprint = fingerprint;
            Exception = exception;
            Message = message;
        }
    }
}

