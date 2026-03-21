using System;
using System.Collections.Generic;

namespace FaultLens.Sdk
{
    public interface IFaultLensRequestScope : IDisposable
    {
        void Complete(int? statusCode = null, IReadOnlyDictionary<string, object> data = null);

        void Fail(int? statusCode = null, IReadOnlyDictionary<string, object> data = null);
    }
}
