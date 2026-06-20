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
    }
}
