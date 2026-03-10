using System;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class ErrorEnvelope : ErrorEnvelopeV1
    {
        public ErrorEnvelope(
            string eventId,
            DateTimeOffset timestamp,
            string environment,
            SdkInfo sdk,
            string fingerprint = null,
            ExceptionInfo exception = null,
            string message = null)
            : base(
                  eventId: eventId,
                  timestamp: timestamp,
                  environment: environment,
                  sdk: sdk,
                  release: null,
                  fingerprint: fingerprint,
                  exception: exception,
                  message: message)
        {
        }
    }
}
