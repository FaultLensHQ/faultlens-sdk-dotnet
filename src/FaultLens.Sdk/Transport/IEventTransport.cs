using FaultLens.Sdk.Envelopes;
using System;

namespace FaultLens.Sdk.Transport
{
    public interface IEventTransport : IDisposable
    {
        void Send(ErrorEnvelopeV1 envelope, Action<DeliveryResult> callback = null);

        void Flush(TimeSpan timeout);
    }
}
