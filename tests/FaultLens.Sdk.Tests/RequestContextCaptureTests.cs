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
            Assert.Null(transport.LastEnvelope.ServiceName);
            Assert.Null(transport.LastEnvelope.TenantId);
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
        public void SetUser_AppearsInEnvelope()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/profile");
            scope.SetUser("user-9482");

            client.CaptureMessage("profile");

            Assert.Equal("user-9482", transport.LastEnvelope.UserId);
        }

        [Fact]
        public void Options_Context_AppearsAsDirectEnvelopeFields()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(
                new FaultLensOptions(
                    apiKey: "test-key",
                    environment: "production",
                    release: "v1.8.4",
                    serviceName: "checkout-api",
                    serviceVersion: "2026.06.19",
                    tenantId: "tenant_demo_retail",
                    customerId: "customer_demo_042",
                    accountId: "account_demo_standard",
                    correlationId: "corr-global"),
                transport);

            client.CaptureMessage("context");

            Assert.Equal("checkout-api", transport.LastEnvelope.ServiceName);
            Assert.Equal("2026.06.19", transport.LastEnvelope.ServiceVersion);
            Assert.Equal("tenant_demo_retail", transport.LastEnvelope.TenantId);
            Assert.Equal("customer_demo_042", transport.LastEnvelope.CustomerId);
            Assert.Equal("account_demo_standard", transport.LastEnvelope.AccountId);
            Assert.Null(transport.LastEnvelope.AnonymousId);
            Assert.Equal("corr-global", transport.LastEnvelope.CorrelationId);
        }

        [Fact]
        public void Options_AnonymousId_Appears_When_KnownIdentity_Is_Not_Configured()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(
                new FaultLensOptions(
                    apiKey: "test-key",
                    environment: "production",
                    anonymousId: "anon_demo_456"),
                transport);

            client.CaptureMessage("anonymous");

            Assert.Equal("anon_demo_456", transport.LastEnvelope.AnonymousId);
            Assert.Null(transport.LastEnvelope.TenantId);
            Assert.Null(transport.LastEnvelope.AccountId);
            Assert.Null(transport.LastEnvelope.UserId);
        }

        [Fact]
        public void Scope_AccountContext_OverridesOptionsContext()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(
                new FaultLensOptions(
                    apiKey: "test-key",
                    environment: "production",
                    serviceName: "checkout-api",
                    tenantId: "tenant_global",
                    accountId: "account_global",
                    anonymousId: "anon_global"),
                transport);

            using var scope = client.BeginRequest("GET", "/orders");
            scope.SetAccount(
                accountId: "account_request",
                tenantId: "tenant_request");

            client.CaptureMessage("context");

            Assert.Equal("checkout-api", transport.LastEnvelope.ServiceName);
            Assert.Equal("tenant_request", transport.LastEnvelope.TenantId);
            Assert.Null(transport.LastEnvelope.CustomerId);
            Assert.Equal("account_request", transport.LastEnvelope.AccountId);
            Assert.Null(transport.LastEnvelope.AnonymousId);
        }

        [Fact]
        public void SetAnonymousId_AppearsInEnvelope()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/anonymous");
            scope.SetAnonymousId("anon_abc123");

            client.CaptureMessage("anonymous");

            Assert.Equal("anon_abc123", transport.LastEnvelope.AnonymousId);
            Assert.Null(transport.LastEnvelope.TenantId);
            Assert.Null(transport.LastEnvelope.AccountId);
            Assert.Null(transport.LastEnvelope.UserId);
        }

        [Fact]
        public void SetAccount_EmitsAccountIdAndTenantId()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/account");
            scope.SetAccount(accountId: "acct_1318", tenantId: "tenant_42");

            client.CaptureMessage("account");

            Assert.Equal("tenant_42", transport.LastEnvelope.TenantId);
            Assert.Equal("acct_1318", transport.LastEnvelope.AccountId);
            Assert.Null(transport.LastEnvelope.AnonymousId);
        }

        [Fact]
        public void Identify_EmitsKnownIdentity()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/account");
            scope.Identify(
                userId: "user_9482",
                accountId: "acct_1318",
                tenantId: "tenant_42");

            client.CaptureMessage("account");

            Assert.Equal("tenant_42", transport.LastEnvelope.TenantId);
            Assert.Equal("acct_1318", transport.LastEnvelope.AccountId);
            Assert.Equal("user_9482", transport.LastEnvelope.UserId);
            Assert.Null(transport.LastEnvelope.AnonymousId);
        }

        [Fact]
        public void SettingAccountAndUserAfterAnonymous_ClearsAnonymousId()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/login");
            scope.SetAnonymousId("anon_abc123");
            scope.SetAccount(accountId: "acct_1318", tenantId: "tenant_42");
            scope.SetUser("user_9482");

            client.CaptureMessage("login");

            Assert.Equal("tenant_42", transport.LastEnvelope.TenantId);
            Assert.Equal("acct_1318", transport.LastEnvelope.AccountId);
            Assert.Equal("user_9482", transport.LastEnvelope.UserId);
            Assert.Null(transport.LastEnvelope.AnonymousId);
        }

        [Fact]
        public void SettingAnonymousAfterKnownIdentity_ClearsKnownIdentity()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/logout");
            scope.SetAccount(accountId: "acct_1318", tenantId: "tenant_42");
            scope.SetUser("user_9482");
            scope.SetAnonymousId("anon_after_logout");

            client.CaptureMessage("logout");

            Assert.Equal("anon_after_logout", transport.LastEnvelope.AnonymousId);
            Assert.Null(transport.LastEnvelope.TenantId);
            Assert.Null(transport.LastEnvelope.AccountId);
            Assert.Null(transport.LastEnvelope.UserId);
        }

        [Fact]
        public void Scope_TenantId_IsUsedAsAccountIdFallback()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/orders");
            scope.SetAccount(accountId: null, tenantId: "tenant_only");

            client.CaptureMessage("context");

            Assert.Equal("tenant_only", transport.LastEnvelope.TenantId);
            Assert.Equal("tenant_only", transport.LastEnvelope.AccountId);
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
        public void BeginRequest_CorrelationHeaderData_AppearsInEnvelopeAndRequest()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using (client.BeginRequest(
                "GET",
                "/orders/{id}",
                data: new Dictionary<string, object>
                {
                    ["X-Correlation-ID"] = "corr-header-123",
                    ["requestId"] = "req-123"
                }))
            {
                client.CaptureMessage("request");
            }

            Assert.Equal("corr-header-123", transport.LastEnvelope.CorrelationId);
            Assert.NotNull(transport.LastEnvelope.Request);
            Assert.Equal("corr-header-123", transport.LastEnvelope.Request.CorrelationId);
            Assert.Equal("req-123", transport.LastEnvelope.Request.RequestId);
            Assert.Equal("GET", transport.LastEnvelope.Request.Method);
            Assert.Equal("/orders/{id}", transport.LastEnvelope.Request.Route);
        }

        [Fact]
        public void SetCorrelationId_OverridesBeginRequestFallback()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/orders");
            scope.SetCorrelationId("corr-explicit");

            client.CaptureMessage("request");

            Assert.Equal("corr-explicit", transport.LastEnvelope.CorrelationId);
            Assert.Equal("corr-explicit", transport.LastEnvelope.Request.CorrelationId);
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
                scope.SetUser("u2");
                scope.SetAnonymousId("anon1");
                scope.SetAccount("account1", "tenant1");
                scope.Identify("u3", "account2", "tenant2");
                scope.SetCorrelationId("corr1");
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
            scope.SetCorrelationId("corr-contract");
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
            Assert.Contains("\"correlationId\"", json);
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
            public ErrorEnvelopeV1 LastEnvelope { get; private set; } = null!;

            public void Dispose() { }

            public void Flush(TimeSpan timeout) { }

            public void Send(ErrorEnvelopeV1 envelope, Action<DeliveryResult> callback = null!)
            {
                LastEnvelope = envelope;
                callback?.Invoke(DeliveryResult.Delivered());
            }
        }
    }
}
