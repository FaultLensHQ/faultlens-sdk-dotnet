namespace FaultLens.Sdk
{
    /// <summary>
    /// Classifies why an ingest delivery failed. Distinguishes terminal outcomes (retrying will not
    /// help) from transient ones (a later attempt could succeed), and names the two ingest-specific
    /// terminal cases the backend contract calls out: identity conflict and monthly capacity
    /// exhaustion. See docs/ingestion-api.md in faultlens-backend for the authoritative contract.
    /// </summary>
    public enum DeliveryFailureKind
    {
        /// <summary>No specific classification is available (e.g. an unrecognized HTTP failure).</summary>
        Unknown = 0,

        /// <summary>HTTP 409: the same eventId was already accepted for a different event. Terminal.</summary>
        IdentityConflict,

        /// <summary>HTTP 429 with reasonCode monthly_event_capacity_exhausted: the workspace's monthly
        /// accepted-event allowance is used up. Terminal until the next usage period.</summary>
        CapacityExhausted,

        /// <summary>HTTP 503: the monthly capacity authority could not answer. Transient.</summary>
        ServiceUnavailable,

        /// <summary>HTTP 429 without evidence of monthly capacity exhaustion (e.g. ordinary ingestion
        /// rate limiting). Transient.</summary>
        Throttled,

        /// <summary>A network-level failure (DNS, connection, timeout) rather than an HTTP response.
        /// Transient.</summary>
        NetworkError,

        /// <summary>Any other non-2xx HTTP response not covered above. Treated as terminal.</summary>
        Http
    }
}
