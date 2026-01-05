using System;

namespace FaultLens.Sdk
{
    public interface IFaultLensClient : IDisposable
    {
        void CaptureException(Exception exception, string fingerprint = null, Action<DeliveryResult> callback = null);

        void CaptureMessage(string message, string fingerprint = null, Action<DeliveryResult> callback = null);
    }
}
