using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FaultLens.Sdk.Transport
{
    internal sealed class HttpEventTransport : IEventTransport
    {
        private static readonly HttpClient Client = new HttpClient();

        private readonly FaultLensOptions _options;
        private readonly RetryPolicy _retryPolicy;

        public HttpEventTransport(FaultLensOptions options, RetryPolicy retryPolicy)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retryPolicy = retryPolicy;
        }

        public async void Send(ErrorEnvelope envelope, Action<DeliveryResult> callback)
        {
            if (envelope == null)
                return;

            _ = SendWithRetryAsync(envelope, callback);
        }

        private async Task SendWithRetryAsync(ErrorEnvelope envelope, Action<DeliveryResult> callback)
        {
            var url = new Uri($"{_options.Endpoint}/api/events/ingest");

            for (var attempt = 0; attempt <= _retryPolicy.MaxRetries; attempt++)
            {
                TransportResult result;

                try
                {
                    var json = JsonSerializer.Serialize(envelope);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = content
                    };

                    request.Headers.Add("X-API-Key", _options.ApiKey);

                    // Fire-and-forget by design
                    var response = await Client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        callback?.Invoke(DeliveryResult.Delivered());
                        return;
                    }

                    result = MapHttpFailure(response.StatusCode);
                }
                catch (Exception ex)
                {
                    result = TransportResult.TransientFailure("network_error", ex.Message);
                }

                if (!result.IsTransient || attempt == _retryPolicy.MaxRetries)
                {
                    callback?.Invoke(DeliveryResult.Failed(result.ErrorCode, result.ErrorMessage));
                    return;
                }

                await Task.Delay(_retryPolicy.GetDelay(attempt)).ConfigureAwait(false);
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

        public void Dispose() 
        {
            Client.Dispose();
        }
    }
}
