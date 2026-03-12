using System;

namespace FaultLens.Sdk
{
    public interface IFaultLensClient : IDisposable
    {
        void AddBreadcrumb(Breadcrumb breadcrumb);

        void CaptureException(Exception exception, string fingerprint = null, Action<DeliveryResult> callback = null);

        void CaptureMessage(string message, string fingerprint = null, Action<DeliveryResult> callback = null);
    }
}
