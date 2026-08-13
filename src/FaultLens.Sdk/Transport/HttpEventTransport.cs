using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FaultLens.Sdk.Transport
{
    internal sealed class HttpEventTransport : IEventTransport
    {
        private static readonly HttpClient SharedClient = new HttpClient();

        private readonly FaultLensOptions _options;
        private readonly RetryPolicy _retryPolicy;
        private readonly HttpClient _client;
        private readonly IAsyncDelay _delay;
        private int _inFlight;

        public HttpEventTransport(FaultLensOptions options)
            : this(options, null, null)
        {
        }

        internal HttpEventTransport(FaultLensOptions options, HttpMessageHandler handler, IAsyncDelay delay)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retryPolicy = new RetryPolicy(maxRetries: 3, baseDelay: TimeSpan.FromMilliseconds(500));
            _client = handler != null ? new HttpClient(handler) : SharedClient;
            _delay = delay ?? SystemAsyncDelay.Instance;
        }

        public async void Send(ErrorEnvelopeV1 envelope, Action<DeliveryResult> callback = null)
        {
            if (envelope == null)
                return;

            Interlocked.Increment(ref _inFlight);
            _ = SendWithRetryAsync(envelope, callback, CancellationToken.None);
        }

        /// <summary>Internal, test-visible entry point: awaitable and cancellable, so retry timing and
        /// bounded-retry behaviour can be verified deterministically without wall-clock sleeps.</summary>
        internal async Task SendWithRetryAsync(ErrorEnvelopeV1 envelope, Action<DeliveryResult> callback, CancellationToken cancellationToken)
        {
            try
            {
                for (var attempt = 0; attempt <= _retryPolicy.MaxRetries; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TransportResult transportResult;

                    try
                    {
                        var ingestUri = new Uri(_options.Endpoint, "/api/events/ingest");
                        var request = new HttpRequestMessage(HttpMethod.Post, ingestUri)
                        {
                            Content = new StringContent(JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json")
                        };

                        request.Headers.Add("X-API-Key", _options.ApiKey);

                        var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            callback?.Invoke(DeliveryResult.Delivered());
                            return;
                        }

                        transportResult = await IngestResponseClassifier.ClassifyAsync(response).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        transportResult = TransportResult.Transient(IngestFailureKind.NetworkError, "network_error", ex.Message);
                    }

                    if (!transportResult.IsTransient || attempt == _retryPolicy.MaxRetries)
                    {
                        callback?.Invoke(DeliveryResult.Failed(
                            transportResult.ErrorCode,
                            transportResult.ErrorMessage,
                            MapKind(transportResult.Kind),
                            transportResult.IsTransient,
                            transportResult.ReasonCode,
                            transportResult.PeriodEndUtc));
                        return;
                    }

                    await _delay.DelayAsync(_retryPolicy.GetDelay(attempt), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Caller cancelled; no further callback. Matches the pre-existing fire-and-forget
                // contract of Send(), which never invoked the callback on Dispose either.
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        private static DeliveryFailureKind MapKind(IngestFailureKind kind)
        {
            switch (kind)
            {
                case IngestFailureKind.IdentityConflict:
                    return DeliveryFailureKind.IdentityConflict;
                case IngestFailureKind.CapacityExhausted:
                    return DeliveryFailureKind.CapacityExhausted;
                case IngestFailureKind.ServiceUnavailable:
                    return DeliveryFailureKind.ServiceUnavailable;
                case IngestFailureKind.Throttled:
                    return DeliveryFailureKind.Throttled;
                case IngestFailureKind.NetworkError:
                    return DeliveryFailureKind.NetworkError;
                case IngestFailureKind.Http:
                    return DeliveryFailureKind.Http;
                default:
                    return DeliveryFailureKind.Unknown;
            }
        }

        public void Flush(TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (Volatile.Read(ref _inFlight) > 0 && sw.Elapsed < timeout)
            {
                Thread.Sleep(10);
            }
        }

        public void Dispose()
        {
            Flush(TimeSpan.FromSeconds(1));
        }
    }
}
