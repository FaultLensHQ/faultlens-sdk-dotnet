using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FaultLens.Sdk.Builders;
using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using FaultLens.Sdk.Transport;
using FluentAssertions;
using Xunit;

namespace FaultLens.Sdk.Tests.Transport
{
    /// <summary>
    /// Pins the SDK's classification of the /api/events/ingest response contract (see
    /// docs/ingestion-api.md in faultlens-backend): 409 and 429-with-capacity-exhaustion are
    /// terminal and must never be retried; 503 and ordinary 429 are transient and retried with
    /// bounded backoff. All timing is driven by a fake delay so no test sleeps on the wall clock.
    /// </summary>
    public sealed class HttpEventTransportTests
    {
        private static readonly Uri Endpoint = new Uri("https://tenant.faultlens.in");

        [Fact]
        public async Task Accepted_200_Delivers_WithoutRetry()
        {
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonBody(new { status = 1, id = Guid.NewGuid() })
            });
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(1);
            result.Success.Should().BeTrue();
            delay.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task OrdinaryDropped_200_Delivers_WithoutTransportRetry()
        {
            // Ordinary Dropped/Rejected outcomes inside a 200 are an application-level result, not a
            // transport failure; the transport layer only reports HTTP-level delivery.
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonBody(new { status = 2, reasonCode = "some_other_drop_reason" })
            });
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(1);
            result.Success.Should().BeTrue();
            delay.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task IdentityConflict_409_NeverRetries()
        {
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonBody(new
                {
                    status = 3,
                    reasonCode = "event_identity_conflict",
                    message = "The event identity conflicts with an already accepted event."
                })
            });
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(1, "identity conflict is terminal and must not be retried");
            result.Success.Should().BeFalse();
            result.Retryable.Should().BeFalse();
            result.Reason.Should().Be(DeliveryFailureKind.IdentityConflict);
            result.ReasonCode.Should().Be("event_identity_conflict");
            delay.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task CapacityExhausted_429_NeverRetries()
        {
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = JsonBody(new
                {
                    status = 2,
                    reasonCode = "monthly_event_capacity_exhausted",
                    message = "Monthly accepted-event capacity is exhausted."
                })
            });
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(1, "monthly capacity exhaustion is terminal for the period and must not be retried");
            result.Success.Should().BeFalse();
            result.Retryable.Should().BeFalse();
            result.Reason.Should().Be(DeliveryFailureKind.CapacityExhausted);
            result.ReasonCode.Should().Be("monthly_event_capacity_exhausted");
            delay.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task CapacityExhausted_429_SurfacesPeriodEndUtc_WhenBackendSuppliesIt()
        {
            // The current backend response body does not include periodEndUtc, but the SDK must
            // surface it without a breaking change if the backend adds it later.
            var periodEnd = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = JsonBody(new
                {
                    status = 2,
                    reasonCode = "monthly_event_capacity_exhausted",
                    message = "Monthly accepted-event capacity is exhausted.",
                    periodEndUtc = periodEnd
                })
            });
            var (transport, _) = CreateTransport(handler);

            var result = await SendAsync(transport);

            result.PeriodEndUtc.Should().Be(periodEnd);
        }

        [Fact]
        public async Task OrdinaryThrottling_429_IsRetried()
        {
            // A generic ingestion-rate-limit 429 (a different body shape entirely, no
            // monthly_event_capacity_exhausted evidence) must not be treated as capacity exhaustion.
            var handler = new FakeHandler(
                _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = JsonBody(new { code = "rate_limited", message = "Too many requests. Please try again later." })
                },
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonBody(new { status = 1, id = Guid.NewGuid() })
                });
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(2);
            result.Success.Should().BeTrue();
            delay.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task ServiceUnavailable_503_IsRetried_AndSucceedsExactlyOnce()
        {
            var handler = new FakeHandler(
                _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = JsonBody(new { status = 3, reasonCode = "monthly_event_capacity_resolution_failed" })
                },
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonBody(new { status = 1, id = Guid.NewGuid() })
                });
            var (transport, delay) = CreateTransport(handler);

            var results = new List<DeliveryResult>();
            await transport.SendWithRetryAsync(BuildEnvelope(), results.Add, CancellationToken.None);

            handler.Requests.Should().HaveCount(2);
            results.Should().HaveCount(1, "exactly one logical completion must be reported per event");
            results[0].Success.Should().BeTrue();
            delay.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task ServiceUnavailable_503_ExhaustsRetryBudget_SurfacesTransientFailure()
        {
            var handler = new FakeHandler(
                _ => Response503(),
                _ => Response503(),
                _ => Response503(),
                _ => Response503());
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(4, "3 retries + the initial attempt");
            result.Success.Should().BeFalse();
            result.Retryable.Should().BeTrue("503 remains transient in nature even once the retry budget is spent");
            result.Reason.Should().Be(DeliveryFailureKind.ServiceUnavailable);
            delay.CallCount.Should().Be(3);

            static HttpResponseMessage Response503() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = JsonBody(new { status = 3, reasonCode = "monthly_event_accounting_not_authoritative" })
            };
        }

        [Fact]
        public async Task UnknownClientError_4xx_KeepsExistingTerminalBehaviour()
        {
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(1);
            result.Retryable.Should().BeFalse();
            delay.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task MalformedCapacityBody_429_FailsClosed_RetriesBounded_DoesNotClaimCapacityExhaustion()
        {
            var handler = new FakeHandler(
                _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("not json", Encoding.UTF8, "application/json") },
                _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("not json", Encoding.UTF8, "application/json") },
                _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("not json", Encoding.UTF8, "application/json") },
                _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("not json", Encoding.UTF8, "application/json") });
            var (transport, delay) = CreateTransport(handler);

            var result = await SendAsync(transport);

            handler.Requests.Should().HaveCount(4, "an unparseable 429 body must not be assumed to be capacity exhaustion, so it retries like an ordinary 429");
            result.Reason.Should().Be(DeliveryFailureKind.Throttled);
            result.Retryable.Should().BeTrue();
            delay.CallCount.Should().Be(3);
        }

        [Fact]
        public async Task Cancellation_StopsRetryLoop_WithoutInvokingCallback()
        {
            var handler = new FakeHandler(
                _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = JsonBody(new { status = 3, reasonCode = "monthly_event_capacity_resolution_failed" }) },
                _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonBody(new { status = 1 }) });
            using var cts = new CancellationTokenSource();
            var delay = new CancelingAsyncDelay(cts);
            var transport = new HttpEventTransport(TestOptions(), handler, delay);

            var results = new List<DeliveryResult>();
            await transport.SendWithRetryAsync(BuildEnvelope(), results.Add, cts.Token);

            handler.Requests.Should().HaveCount(1, "cancellation during the backoff delay must stop further attempts");
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task TerminalEvent_DoesNotBlockFlush()
        {
            var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonBody(new { status = 3, reasonCode = "event_identity_conflict" })
            });
            var transport = new HttpEventTransport(TestOptions(), handler, new ImmediateAsyncDelay());

            transport.Send(BuildEnvelope());

            var sw = System.Diagnostics.Stopwatch.StartNew();
            transport.Flush(TimeSpan.FromSeconds(5));
            sw.Stop();

            handler.Requests.Should().HaveCount(1);
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1), "a terminal delivery must complete promptly, not hold Flush for the full timeout");
        }

        private static async Task<DeliveryResult> SendAsync(HttpEventTransport transport)
        {
            var results = new List<DeliveryResult>();
            await transport.SendWithRetryAsync(BuildEnvelope(), results.Add, CancellationToken.None);
            results.Should().HaveCount(1);
            return results[0];
        }

        private static (HttpEventTransport transport, ImmediateAsyncDelay delay) CreateTransport(FakeHandler handler)
        {
            var delay = new ImmediateAsyncDelay();
            var transport = new HttpEventTransport(TestOptions(), handler, delay);
            return (transport, delay);
        }

        private static FaultLensOptions TestOptions() =>
            new FaultLensOptions(apiKey: "test-key", environment: "production", endpoint: Endpoint);

        private static ErrorEnvelopeV1 BuildEnvelope() =>
            new ErrorEnvelopeBuilder(TestOptions(), new SdkInfo("faultlens-dotnet-tests", "1.0.0"))
                .WithMessage("boom")
                .Build();

        private static StringContent JsonBody(object value) =>
            new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;
            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

            public FakeHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
            {
                _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                if (_responses.Count == 0)
                    throw new InvalidOperationException("No more fake responses queued; the transport retried more than the test expected.");

                return Task.FromResult(_responses.Dequeue()(request));
            }
        }

        private sealed class ImmediateAsyncDelay : IAsyncDelay
        {
            public int CallCount { get; private set; }

            public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                CallCount++;
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }

        private sealed class CancelingAsyncDelay : IAsyncDelay
        {
            private readonly CancellationTokenSource _cts;

            public CancelingAsyncDelay(CancellationTokenSource cts)
            {
                _cts = cts;
            }

            public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                _cts.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }
    }
}
