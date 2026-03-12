using System;
using System.Collections.Generic;

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
            string message = null,
            IReadOnlyList<BreadcrumbInfo> breadcrumbs = null)
            : base(
                  eventId: eventId,
                  timestamp: timestamp,
                  environment: environment,
                  sdk: sdk,
                  release: null,
                  fingerprint: fingerprint,
                  exception: exception,
                  message: message,
                  breadcrumbs: breadcrumbs)
        {
        }
    }
}
