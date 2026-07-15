using System;
using System.Collections.Generic;

namespace FaultLens.Sdk
{
    public interface IFaultLensRequestScope : IDisposable
    {
        void Complete(int? statusCode = null, IReadOnlyDictionary<string, object> data = null);

        void Fail(int? statusCode = null, IReadOnlyDictionary<string, object> data = null);

        void SetRequestContext(
            string url,
            string referrer = null,
            string userAgent = null,
            string queryString = null)
        { }

        void SetUserId(string userId) { }

        void SetUser(string userId) { SetUserId(userId); }

        void SetAnonymousId(string anonymousId) { }

        void SetAccount(string accountId, string tenantId = null) { }

        void Identify(string userId = null, string accountId = null, string tenantId = null)
        {
            SetAccount(accountId: accountId, tenantId: tenantId);
            SetUser(userId);
        }

        [Obsolete("Use SetAccount(accountId, tenantId) and SetUser(userId). CustomerId remains payload-compatible but is not the recommended public identity API.")]
        void SetCustomer(string tenantId = null, string customerId = null, string accountId = null) { }

        void SetCorrelationId(string correlationId) { }

        void SetTag(string key, string value) { }

        /// <summary>
        /// Marks this request as belonging to an explicit business capability so FaultLens can use
        /// it as a trusted severity signal. Convenience over <see cref="SetTag"/> with the
        /// <see cref="FaultLensReservedTags"/> keys; criticality should be one of
        /// <see cref="FaultLensCriticality"/> — other values are ignored by the backend.
        /// <para>
        /// <paramref name="operation"/> is a single, general-purpose field: it may name a route,
        /// workflow, job, command, or any other business operation.
        /// </para>
        /// </summary>
        void SetCapability(string capability, string criticality = null, string operation = null)
        {
            if (!string.IsNullOrWhiteSpace(capability)) SetTag(FaultLensReservedTags.Capability, capability);
            if (!string.IsNullOrWhiteSpace(criticality)) SetTag(FaultLensReservedTags.Criticality, criticality);
            if (!string.IsNullOrWhiteSpace(operation)) SetTag(FaultLensReservedTags.Operation, operation);
        }

        /// <summary>
        /// Sets the business operation for this request (route, workflow, job, command, or any other
        /// operation). Convenience over <see cref="SetTag"/> with <see cref="FaultLensReservedTags.Operation"/>.
        /// </summary>
        void SetOperation(string operation)
        {
            if (!string.IsNullOrWhiteSpace(operation)) SetTag(FaultLensReservedTags.Operation, operation);
        }

        /// <summary>
        /// Deprecated. The FaultLens backend does not consume a separate operation-criticality signal;
        /// pass the criticality through <see cref="SetCapability"/> instead. This helper is a client-side
        /// no-op and is retained only for source compatibility with 1.1.0.
        /// </summary>
        [Obsolete("Not consumed by the FaultLens backend. Pass criticality via SetCapability(capability, criticality, operation). This helper is a no-op and will be removed in a future major version.")]
        void SetOperationCriticality(string criticality)
        {
        }

        /// <summary>
        /// Deprecated. The FaultLens backend does not consume a separate workflow signal; name the
        /// workflow through <see cref="SetCapability"/>'s <c>operation</c> parameter or
        /// <see cref="SetOperation"/> instead. This helper is a client-side no-op and is retained only
        /// for source compatibility with 1.1.0.
        /// </summary>
        [Obsolete("Not consumed by the FaultLens backend. Name the workflow via SetOperation(...) or SetCapability(..., operation). This helper is a no-op and will be removed in a future major version.")]
        void SetWorkflow(string workflow)
        {
        }

        /// <summary>
        /// Deprecated. The FaultLens backend does not consume a separate job signal; name the job
        /// through <see cref="SetCapability"/>'s <c>operation</c> parameter or <see cref="SetOperation"/>
        /// instead. This helper is a client-side no-op and is retained only for source compatibility
        /// with 1.1.0.
        /// </summary>
        [Obsolete("Not consumed by the FaultLens backend. Name the job via SetOperation(...) or SetCapability(..., operation). This helper is a no-op and will be removed in a future major version.")]
        void SetJob(string job)
        {
        }
    }
}
