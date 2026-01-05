using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FaultLens.Sdk.Transport
{
    internal sealed class HttpEventTransport : IEventTransport
    {
        private static readonly HttpClient Client = new HttpClient();

        private readonly FaultLensOptions _options;
        private readonly RetryPolicy _retryPolicy;
        private int _inFlight;

        public HttpEventTransport(FaultLensOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retryPolicy = new RetryPolicy(maxRetries: 3, baseDelay: TimeSpan.FromMilliseconds(500));
        }

        public async void Send(ErrorEnvelope envelope, Action<DeliveryResult> callback = null)
        {
            if (envelope == null)
                return;

            Interlocked.Increment(ref _inFlight);
            _ = SendWithRetryAsync(envelope, callback);
        }

        private async Task SendWithRetryAsync(ErrorEnvelope envelope, Action<DeliveryResult> callback = null)
        {
            try
            {
                for (var attempt = 0; attempt <= _retryPolicy.MaxRetries; attempt++)
                {
                    TransportResult transportResult;

                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events/ingest")
                        {
                            Content = new StringContent(JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json")
                        };

                        request.Headers.Add("X-API-Key", _options.ApiKey);

                        var response = await Client.SendAsync(request).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            callback?.Invoke(DeliveryResult.Delivered());
                            return;
                        }

                        transportResult = MapHttpFailure(response.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        transportResult = TransportResult.TransientFailure("network_error", ex.Message);
                    }

                    if (!transportResult.IsTransient || attempt == _retryPolicy.MaxRetries)
                    {
                        callback?.Invoke(DeliveryResult.Failed(transportResult.ErrorCode, transportResult.ErrorMessage));
                        return;
                    }

                    await Task.Delay(_retryPolicy.GetDelay(attempt)).ConfigureAwait(false);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        private static TransportResult MapHttpFailure(HttpStatusCode status)
        {
            if ((int)status >= 500 || status == HttpStatusCode.TooManyRequests)
            {
                return TransportResult.TransientFailure(status.ToString(), "Transient ingest failure");
            }

            return TransportResult.PermanentFailure(status.ToString(), "Permanent ingest failure");
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
