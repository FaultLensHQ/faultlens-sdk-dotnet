namespace FaultLens.Sdk
{
    /// <summary>
    /// Reserved FaultLens tag names. Values sent under these keys are treated by the FaultLens
    /// backend as explicit, trusted capability/criticality metadata for severity classification.
    /// FaultLens never infers business criticality from routes, URLs, or stack traces — these
    /// tags are the only way to mark an event as belonging to a business-critical capability.
    /// </summary>
    public static class FaultLensReservedTags
    {
        /// <summary>Business capability the event belongs to, e.g. "checkout" or "billing-sync". Max 128 chars.</summary>
        public const string Capability = "faultlens.capability";

        /// <summary>
        /// Criticality of the capability. Allowed values: "critical", "high", "normal", "low"
        /// (see <see cref="FaultLensCriticality"/>). Any other value is ignored by the backend.
        /// </summary>
        public const string Criticality = "faultlens.criticality";

        /// <summary>Operation, workflow, or job name, e.g. "payment-capture" or "nightly-billing-sync". Max 128 chars.</summary>
        public const string Operation = "faultlens.operation";
    }

    /// <summary>Allowed values for the <see cref="FaultLensReservedTags.Criticality"/> tag.</summary>
    public static class FaultLensCriticality
    {
        public const string Critical = "critical";
        public const string High = "high";
        public const string Normal = "normal";
        public const string Low = "low";
    }
}
