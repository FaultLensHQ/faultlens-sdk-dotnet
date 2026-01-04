using FaultLens.Sdk.Envelopes;
using System;

namespace FaultLens.Sdk.Transport
{
    public interface IEventTransport : IDisposable
    {
        void Send(ErrorEnvelope envelope, Action<DeliveryResult> callback);
    }
}
