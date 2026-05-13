using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public class ErrorEnvelopeV1
    {
        [JsonPropertyName("eventId")]
        public string EventId { get; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; }

        [JsonPropertyName("environment")]
        public string Environment { get; }

        [JsonPropertyName("sdk")]
        public SdkInfo Sdk { get; }

        [JsonPropertyName("release")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Release { get; }

        [JsonPropertyName("fingerprint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Fingerprint { get; }

        [JsonPropertyName("exception")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ExceptionInfo Exception { get; }

        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Message { get; }

        [JsonPropertyName("breadcrumbs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<BreadcrumbInfo> Breadcrumbs { get; }

        [JsonPropertyName("request")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RequestContextInfo Request { get; }

        [JsonPropertyName("client")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ClientContextInfo Client { get; }

        [JsonPropertyName("userId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string UserId { get; }

        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, string> Tags { get; }

        public ErrorEnvelopeV1(
            string eventId,
            DateTimeOffset timestamp,
            string environment,
            SdkInfo sdk,
            string release = null,
            string fingerprint = null,
            ExceptionInfo exception = null,
            string message = null,
            IReadOnlyList<BreadcrumbInfo> breadcrumbs = null,
            RequestContextInfo request = null,
            ClientContextInfo client = null,
            string userId = null,
            IReadOnlyDictionary<string, string> tags = null)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("EventId is required.", nameof(eventId));

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentException("Environment is required.", nameof(environment));

            EventId = eventId;
            Timestamp = timestamp;
            Environment = environment;
            Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));

            Release = release;
            Fingerprint = fingerprint;
            Exception = exception;
            Message = message;
            Breadcrumbs = breadcrumbs;
            Request = request;
            Client = client;
            UserId = userId;
            Tags = tags;
        }
    }
}
