using System;

namespace FaultLens.Sdk
{
    public interface IFaultLensClient : IDisposable
    {
        void CaptureException(Exception exception, Action<DeliveryResult> callback = null);

        void CaptureMessage(string message, Action<DeliveryResult> callback = null);
    }
}
