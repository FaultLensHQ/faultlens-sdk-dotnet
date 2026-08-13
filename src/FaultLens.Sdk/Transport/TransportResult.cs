using System;

namespace FaultLens.Sdk.Transport
{
    internal sealed class TransportResult
    {
        public bool Success { get; }
        public bool IsTransient { get; }
        public IngestFailureKind Kind { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public string ReasonCode { get; }
        public DateTimeOffset? PeriodEndUtc { get; }

        private TransportResult(
            bool success,
            bool isTransient,
            IngestFailureKind kind,
            string errorCode,
            string errorMessage,
            string reasonCode,
            DateTimeOffset? periodEndUtc)
        {
            Success = success;
            IsTransient = isTransient;
            Kind = kind;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            ReasonCode = reasonCode;
            PeriodEndUtc = periodEndUtc;
        }

        public static TransportResult Delivered() =>
            new TransportResult(true, false, IngestFailureKind.Unknown, null, null, null, null);

        public static TransportResult Transient(IngestFailureKind kind, string code, string message, string reasonCode = null) =>
            new TransportResult(false, true, kind, code, message, reasonCode, null);

        public static TransportResult Permanent(IngestFailureKind kind, string code, string message, string reasonCode = null, DateTimeOffset? periodEndUtc = null) =>
            new TransportResult(false, false, kind, code, message, reasonCode, periodEndUtc);
    }
}
