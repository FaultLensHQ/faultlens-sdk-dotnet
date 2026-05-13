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

        void SetTag(string key, string value) { }
    }
}
