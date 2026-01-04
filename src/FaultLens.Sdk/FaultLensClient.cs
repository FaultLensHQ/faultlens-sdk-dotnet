using FaultLens.Sdk.Builders;
using FaultLens.Sdk.Internal;
using FaultLens.Sdk.Transport;
using System;

namespace FaultLens.Sdk
{
    public sealed class FaultLensClient : IFaultLensClient
    {
        private readonly ErrorEnvelopeBuilder _builder;
        private readonly IEventTransport _transport;
        private readonly SafeExecutor _executor;

        public FaultLensClient(FaultLensOptions options, IEventTransport transport, SdkInfo sdkInfo)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _builder = new ErrorEnvelopeBuilder(options, sdkInfo);
            _transport = transport;
            _executor = new SafeExecutor();
        }

        public void CaptureException(Exception exception, Action<DeliveryResult> callback = null)
        {
            if (exception == null)
                return;

            _executor.Execute(() =>
            {
                var envelope = _builder.WithException(exception).Build();
                _transport.Send(envelope, callback);
            });
        }

        public void CaptureMessage(string message, Action<DeliveryResult> callback = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _executor.Execute(() =>
            {
                var envelope = _builder.WithMessage(message).Build();
                _transport.Send(envelope, callback);
            });
        }

        public void Dispose()
        {
           _transport.Dispose();    
        }
    }
}

