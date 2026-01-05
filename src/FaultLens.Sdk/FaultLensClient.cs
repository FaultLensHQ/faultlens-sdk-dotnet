using FaultLens.Sdk.Builders;
using FaultLens.Sdk.Internal;
using FaultLens.Sdk.Transport;
using System;
using System.Threading;

namespace FaultLens.Sdk
{
    public sealed class FaultLensClient : IFaultLensClient
    {
        private readonly FaultLensOptions _options;
        private readonly ErrorEnvelopeBuilder _envelopeBuilder;
        private readonly IEventTransport _transport;
        private readonly SafeExecutor _executor;

        private int _disposed;

        public FaultLensClient(FaultLensOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _envelopeBuilder = new ErrorEnvelopeBuilder(_options, new SdkInfo());

            _transport = new HttpEventTransport(_options);
            _executor = new SafeExecutor();
        }

        // ----------------------------
        // Capture Exception
        // ----------------------------
        public void CaptureException(Exception exception, string fingerprint = null, Action<DeliveryResult> callback = null)
        {
            if (exception == null || IsDisposed())
                return;

            _executor.Execute(() =>
            {
                var envelope = _envelopeBuilder
                    .WithException(exception)
                    .WithFingerprint(fingerprint)
                    .Build();

                _transport.Send(envelope, callback);
            });
        }

        // ----------------------------
        // Capture Message
        // ----------------------------
        public void CaptureMessage(string message, string fingerprint = null, Action<DeliveryResult> callback = null)
        {
            if (string.IsNullOrWhiteSpace(message) || IsDisposed())
                return;

            _executor.Execute(() =>
            {
                var envelope = _envelopeBuilder
                    .WithMessage(message)
                    .WithFingerprint(fingerprint)
                    .Build();

                _transport.Send(envelope, callback);
            });
        }

        // ----------------------------
        // Flush (best-effort)
        // ----------------------------
        public void Flush(TimeSpan timeout)
        {
            if (IsDisposed())
                return;

            _transport.Flush(timeout);
        }

        // ----------------------------
        // Dispose (safe, idempotent)
        // ----------------------------
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            try
            {
                Flush(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // NEVER throw during Dispose
            }
        }

        private bool IsDisposed()
        {
            return Volatile.Read(ref _disposed) == 1;
        }
    }
}
