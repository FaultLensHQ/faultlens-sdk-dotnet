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
        /// </summary>
        void SetCapability(string capability, string criticality = null, string operation = null)
        {
            if (!string.IsNullOrWhiteSpace(capability)) SetTag(FaultLensReservedTags.Capability, capability);
            if (!string.IsNullOrWhiteSpace(criticality)) SetTag(FaultLensReservedTags.Criticality, criticality);
            if (!string.IsNullOrWhiteSpace(operation)) SetTag(FaultLensReservedTags.Operation, operation);
        }

        /// <summary>
        /// Sets the criticality of the operation or route (distinct from the capability criticality set
        /// by <see cref="SetCapability"/>). Convenience over <see cref="SetTag"/> with
        /// <see cref="FaultLensReservedTags.OperationCriticality"/>; criticality should be one of
        /// <see cref="FaultLensCriticality"/> — other values are ignored by the backend.
        /// </summary>
        void SetOperationCriticality(string criticality)
        {
            if (!string.IsNullOrWhiteSpace(criticality)) SetTag(FaultLensReservedTags.OperationCriticality, criticality);
        }

        /// <summary>
        /// Marks this request as belonging to an explicit business workflow so FaultLens can use it as a
        /// trusted severity signal. Convenience over <see cref="SetTag"/> with
        /// <see cref="FaultLensReservedTags.Workflow"/>.
        /// </summary>
        void SetWorkflow(string workflow)
        {
            if (!string.IsNullOrWhiteSpace(workflow)) SetTag(FaultLensReservedTags.Workflow, workflow);
        }

        /// <summary>
        /// Marks this request as belonging to an explicit background job or scheduled task. Convenience
        /// over <see cref="SetTag"/> with <see cref="FaultLensReservedTags.Job"/>.
        /// </summary>
        void SetJob(string job)
        {
            if (!string.IsNullOrWhiteSpace(job)) SetTag(FaultLensReservedTags.Job, job);
        }
    }
}
