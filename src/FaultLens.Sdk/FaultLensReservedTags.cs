using System;

namespace FaultLens.Sdk
{
    /// <summary>
    /// Reserved FaultLens tag names. Values sent under these keys are treated by the FaultLens
    /// backend as explicit, trusted business metadata for severity classification.
    /// FaultLens never infers business criticality from routes, URLs, or stack traces — these
    /// tags are the only way to mark an event as belonging to a business-critical capability.
    /// <para>
    /// The FaultLens ingestion contract consumes exactly three reserved tags:
    /// <see cref="Capability"/>, <see cref="Criticality"/>, and <see cref="Operation"/>.
    /// The <c>Operation</c> value may name a route, workflow, job, command, or any other
    /// business operation. Other keys in this type are deprecated and ignored by the backend.
    /// </para>
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

        /// <summary>
        /// Business operation the event belongs to. This is a single, general-purpose field: it may
        /// name a service operation, route, workflow, job, command, or background operation, e.g.
        /// "payment-capture", "GET /api/orders/{id}", "tenant-onboarding", or "nightly-billing-sync".
        /// Max 128 chars.
        /// </summary>
        public const string Operation = "faultlens.operation";

        /// <summary>
        /// Deprecated. The FaultLens backend does not consume a separate operation-criticality tag;
        /// use <see cref="Criticality"/> for the event's criticality. Retained only for source
        /// compatibility with 1.1.0.
        /// </summary>
        [Obsolete("Not consumed by the FaultLens backend. Use FaultLensReservedTags.Criticality. This member is ignored end-to-end and will be removed in a future major version.")]
        public const string OperationCriticality = "faultlens.operation.criticality";

        /// <summary>
        /// Deprecated. The FaultLens backend does not consume a separate workflow tag; model the
        /// workflow as the <see cref="Operation"/> value instead. Retained only for source
        /// compatibility with 1.1.0.
        /// </summary>
        [Obsolete("Not consumed by the FaultLens backend. Use FaultLensReservedTags.Operation to name the workflow. This member is ignored end-to-end and will be removed in a future major version.")]
        public const string Workflow = "faultlens.workflow";

        /// <summary>
        /// Deprecated. The FaultLens backend does not consume a separate job tag; model the job as
        /// the <see cref="Operation"/> value instead. Retained only for source compatibility with 1.1.0.
        /// </summary>
        [Obsolete("Not consumed by the FaultLens backend. Use FaultLensReservedTags.Operation to name the job. This member is ignored end-to-end and will be removed in a future major version.")]
        public const string Job = "faultlens.job";
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
