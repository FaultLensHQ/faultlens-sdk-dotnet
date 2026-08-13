using System;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk
{
    public sealed class DeliveryResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; }

        /// <summary>
        /// True when a later delivery attempt of the same event could plausibly succeed (e.g. HTTP 503,
        /// network failure, ordinary rate limiting). False for terminal outcomes such as identity
        /// conflict or monthly capacity exhaustion, where retrying will not help.
        /// </summary>
        [JsonPropertyName("retryable")]
        public bool Retryable { get; }

        /// <summary>Typed classification of the failure. See <see cref="DeliveryFailureKind"/>.</summary>
        [JsonPropertyName("reason")]
        public DeliveryFailureKind Reason { get; }

        /// <summary>The backend's machine-readable reasonCode from the ingest response body, when the
        /// body was present and parsed (e.g. "monthly_event_capacity_exhausted"). Null when no ingest
        /// response body was available to read (network failure) or it did not carry a reasonCode.</summary>
        [JsonPropertyName("reasonCode")]
        public string ReasonCode { get; }

        /// <summary>The end of the current monthly usage period, when the backend response supplies it.
        /// The current ingestion contract does not include this in the response body, so this is null
        /// today; the property exists so the SDK can surface it without a breaking change if the
        /// backend adds it later.</summary>
        [JsonPropertyName("periodEndUtc")]
        public DateTimeOffset? PeriodEndUtc { get; }

        private DeliveryResult(
            bool success,
            string errorCode,
            string errorMessage,
            bool retryable,
            DeliveryFailureKind reason,
            string reasonCode,
            DateTimeOffset? periodEndUtc)
        {
            Success = success;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Retryable = retryable;
            Reason = reason;
            ReasonCode = reasonCode;
            PeriodEndUtc = periodEndUtc;
        }

        public static DeliveryResult Delivered() =>
            new DeliveryResult(true, null, null, false, DeliveryFailureKind.Unknown, null, null);

        /// <summary>
        /// Reports a failed delivery with only the original error code/message. Retained exactly as-is
        /// for source and binary compatibility with existing callers; prefer the overload below for new
        /// code, which also carries retryability and the typed failure reason.
        /// </summary>
        public static DeliveryResult Failed(string errorCode, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                throw new ArgumentException("errorCode is required", nameof(errorCode));

            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("errorMessage is required", nameof(errorMessage));

            return new DeliveryResult(
                success: false,
                errorCode: errorCode,
                errorMessage: errorMessage,
                retryable: false,
                reason: DeliveryFailureKind.Unknown,
                reasonCode: null,
                periodEndUtc: null);
        }

        public static DeliveryResult Failed(
            string errorCode,
            string errorMessage,
            DeliveryFailureKind reason,
            bool retryable,
            string reasonCode = null,
            DateTimeOffset? periodEndUtc = null)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                throw new ArgumentException("errorCode is required", nameof(errorCode));

            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("errorMessage is required", nameof(errorMessage));

            return new DeliveryResult(
                success: false,
                errorCode: errorCode,
                errorMessage: errorMessage,
                retryable: retryable,
                reason: reason,
                reasonCode: reasonCode,
                periodEndUtc: periodEndUtc);
        }
    }
}
