using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using FaultLens.Sdk.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FaultLens.Sdk.Tests
{
    public sealed class RequestContextCaptureTests
    {
        // -------------------------
        // Backward compat
        // -------------------------

        [Fact]
        public void CaptureException_Without_RequestScope_Still_Sends_Envelope()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            client.CaptureException(new InvalidOperationException("no scope"));

            Assert.NotNull(transport.LastEnvelope);
            Assert.NotNull(transport.LastEnvelope.Client);
            Assert.Equal(".NET", transport.LastEnvelope.Client.RuntimeName);
            Assert.Null(transport.LastEnvelope.Request);
            Assert.Null(transport.LastEnvelope.UserId);
            Assert.Null(transport.LastEnvelope.Tags);
        }

        [Fact]
        public void CaptureMessage_Without_RequestScope_Still_Sends_Envelope()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            client.CaptureMessage("hello");

            Assert.NotNull(transport.LastEnvelope);
            Assert.NotNull(transport.LastEnvelope.Client);
        }

        // -------------------------
        // Request context
        // -------------------------

        [Fact]
        public void SetRequestContext_Url_Appears_In_Envelope_Request()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/api/orders/{id}", "AspNetCore");
            scope.SetRequestContext("https://api.example.com/api/orders/42");

            client.CaptureMessage("captured");

            var req = transport.LastEnvelope.Request;
            Assert.NotNull(req);
            Assert.Equal("https://api.example.com/api/orders/42", req.RequestedUrl);
            Assert.Equal("GET", req.Method);
            Assert.Equal("/api/orders/{id}", req.Route);
        }

        [Fact]
        public void SetRequestContext_Referrer_Captured()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("POST", "/checkout");
            scope.SetRequestContext(
                url: "https://app.example.com/checkout",
                referrer: "https://app.example.com/cart");

            client.CaptureMessage("order");

            Assert.Equal("https://app.example.com/cart", transport.LastEnvelope.Request.Referrer);
        }

        [Fact]
        public void SetRequestContext_UserAgent_Appears_In_Client()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/page");
            scope.SetRequestContext(
                url: "https://app.example.com/page",
                userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            client.CaptureMessage("test");

            Assert.Equal("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", transport.LastEnvelope.Client.UserAgent);
        }

        [Fact]
        public void SetRequestContext_SensitiveToken_RedactedFromUrl()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/callback");
            scope.SetRequestContext("https://app.example.com/callback?token=secret123&user=42");

            client.CaptureMessage("callback");

            var url = transport.LastEnvelope.Request.RequestedUrl;
            Assert.Contains("token=[REDACTED]", url);
            Assert.Contains("user=42", url);
            Assert.DoesNotContain("secret123", url);
        }

        [Fact]
        public void SetRequestContext_SensitiveQueryString_Redacted()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/search");
            scope.SetRequestContext(
                url: "https://app.example.com/search",
                queryString: "api_key=my-key&q=dotnet");

            client.CaptureMessage("search");

            var qs = transport.LastEnvelope.Request.QueryString;
            Assert.Contains("api_key=[REDACTED]", qs);
            Assert.Contains("q=dotnet", qs);
        }

        [Fact]
        public void SetRequestContext_Authorization_NotInHeaders_NoHeaderCapture()
        {
            // The SDK does not capture headers at all — this test documents the absence.
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/secure");
            scope.SetRequestContext("https://app.example.com/secure");

            client.CaptureMessage("test");

            var json = SerializeEnvelope(transport.LastEnvelope);
            Assert.DoesNotContain("authorization", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cookie", json, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------
        // Client / runtime context
        // -------------------------

        [Fact]
        public void Client_RuntimeName_AlwaysPopulated()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            client.CaptureMessage("test");

            Assert.Equal(".NET", transport.LastEnvelope.Client.RuntimeName);
        }

        [Fact]
        public void Client_RuntimeVersion_AlwaysPopulated()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            client.CaptureMessage("test");

            Assert.False(string.IsNullOrWhiteSpace(transport.LastEnvelope.Client.RuntimeVersion));
        }

        [Fact]
        public void Client_UserAgent_Null_When_Not_Set()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/page");
            client.CaptureMessage("test");

            Assert.Null(transport.LastEnvelope.Client.UserAgent);
        }

        // -------------------------
        // userId
        // -------------------------

        [Fact]
        public void SetUserId_AppearsInEnvelope()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/profile");
            scope.SetUserId("user-abc-123");

            client.CaptureMessage("profile");

            Assert.Equal("user-abc-123", transport.LastEnvelope.UserId);
        }

        [Fact]
        public void SetUserId_WhitespaceOrEmpty_IsNull()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/anon");
            scope.SetUserId("   ");

            client.CaptureMessage("anon");

            Assert.Null(transport.LastEnvelope.UserId);
        }

        [Fact]
        public void UserId_Null_When_Not_Set()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            client.CaptureMessage("no user");

            Assert.Null(transport.LastEnvelope.UserId);
        }

        // -------------------------
        // Tags
        // -------------------------

        [Fact]
        public void SetTag_AppearsInEnvelope()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/data");
            scope.SetTag("region", "us-east-1");
            scope.SetTag("tier", "premium");

            client.CaptureMessage("data");

            var tags = transport.LastEnvelope.Tags;
            Assert.NotNull(tags);
            Assert.Equal("us-east-1", tags["region"]);
            Assert.Equal("premium", tags["tier"]);
        }

        [Fact]
        public void SetTag_EmptyKey_Ignored()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/data");
            scope.SetTag("   ", "value");
            scope.SetTag("valid", "ok");

            client.CaptureMessage("data");

            var tags = transport.LastEnvelope.Tags;
            Assert.False(tags.ContainsKey("   "));
            Assert.True(tags.ContainsKey("valid"));
        }

        [Fact]
        public void Tags_Null_When_Not_Set()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            client.CaptureMessage("no tags");

            Assert.Null(transport.LastEnvelope.Tags);
        }

        // -------------------------
        // Failure isolation
        // -------------------------

        [Fact]
        public void SetRequestContext_NullUrl_DoesNotThrow()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/safe");
            var ex = Record.Exception(() => scope.SetRequestContext(null));

            Assert.Null(ex);
            client.CaptureMessage("safe");
            Assert.NotNull(transport.LastEnvelope);
        }

        [Fact]
        public void NoopScope_NewMethods_DoNotThrow()
        {
            // NoopScope is returned when the client is disposed
            var transport = new RecordingTransport();
            var client = MakeClient(transport);
            client.Dispose();

            var scope = client.BeginRequest("GET", "/any");
            var ex = Record.Exception(() =>
            {
                scope.SetRequestContext("https://example.com");
                scope.SetUserId("u1");
                scope.SetTag("k", "v");
            });

            Assert.Null(ex);
        }

        // -------------------------
        // Payload property names (backend #50 contract)
        // -------------------------

        [Fact]
        public void Payload_PropertyNames_MatchBackend50Contract()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/api/items");
            scope.SetRequestContext(
                url: "https://api.example.com/api/items",
                referrer: "https://app.example.com/",
                userAgent: "TestAgent/1.0");
            scope.SetUserId("u-1");
            scope.SetTag("env", "test");

            client.CaptureMessage("contract check");

            var json = SerializeEnvelope(transport.LastEnvelope);

            Assert.Contains("\"request\"", json);
            Assert.Contains("\"requestedUrl\"", json);
            Assert.Contains("\"client\"", json);
            Assert.Contains("\"runtimeName\"", json);
            Assert.Contains("\"runtimeVersion\"", json);
            Assert.Contains("\"userAgent\"", json);
            Assert.Contains("\"userId\"", json);
            Assert.Contains("\"tags\"", json);
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static FaultLensClient MakeClient(IEventTransport transport)
        {
            return new FaultLensClient(
                new FaultLensOptions(apiKey: "test-key", environment: "test"),
                transport);
        }

        private static string SerializeEnvelope(ErrorEnvelopeV1 envelope)
        {
            return JsonSerializer.Serialize(envelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private sealed class RecordingTransport : IEventTransport
        {
            public ErrorEnvelopeV1 LastEnvelope { get; private set; }

            public void Dispose() { }

            public void Flush(TimeSpan timeout) { }

            public void Send(ErrorEnvelopeV1 envelope, Action<DeliveryResult> callback = null)
            {
                LastEnvelope = envelope;
                callback?.Invoke(DeliveryResult.Delivered());
            }
        }
    }
}
