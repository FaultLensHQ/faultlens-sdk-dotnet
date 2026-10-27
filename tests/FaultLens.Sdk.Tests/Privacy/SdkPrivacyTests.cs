using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FaultLens.Sdk.Builders;
using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using FaultLens.Sdk.Transport;
using FluentAssertions;

namespace FaultLens.Sdk.Tests.Privacy
{
    // All values in this file are synthetic.
    internal static class Synthetic
    {
        public const string Email = "alice.synthetic@example.test";
        public const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJzeW50aGV0aWMifQ.c3ludGhldGljc2ln";
        public const string Password = "Sup3rSynthetic!";
        public const string Card = "4111 1111 1111 1111";
        public const string Aws = "AKIAABCDEFGHIJKLMNOP";

        public static readonly string[] All = { Email, Jwt, Password, Card, Aws };

        public static FaultLensOptions Options() => new FaultLensOptions(apiKey: "test-key", environment: "test");

        public static ErrorEnvelopeBuilder Builder() => new ErrorEnvelopeBuilder(Options(), new SdkInfo("faultlens-dotnet-tests", "1.0.0"));

        public static string Json(ErrorEnvelopeV1 envelope) => JsonSerializer.Serialize(envelope);

        public static void AssertNoSecrets(string json)
        {
            foreach (var secret in All)
                json.Should().NotContain(secret);
        }
    }

    internal sealed class RecordingTransport : IEventTransport
    {
        public List<ErrorEnvelopeV1> Sent { get; } = new List<ErrorEnvelopeV1>();

        public void Dispose() { }

        public void Flush(TimeSpan timeout) { }

        public void Send(ErrorEnvelopeV1 envelope, Action<DeliveryResult>? callback = null)
        {
            Sent.Add(envelope);
            callback?.Invoke(DeliveryResult.Delivered());
        }
    }

    public sealed class PrivacyTextVectorTests
    {
        private sealed class VectorFile
        {
            public List<Vector> Vectors { get; set; } = new List<Vector>();
        }

        private sealed class Vector
        {
            public string Id { get; set; } = "";
            public string Input { get; set; } = "";
            public string Expected { get; set; } = "";
        }

        private static List<Vector> Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "privacy-text-vectors.v1.json");
            var file = JsonSerializer.Deserialize<VectorFile>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            return file.Vectors;
        }

        public static IEnumerable<object[]> VectorIds() => Load().Select(v => new object[] { v.Id });

        [Fact]
        public void Vector_file_is_not_empty()
        {
            Load().Count.Should().BeGreaterThan(20);
        }

        [Theory]
        [MemberData(nameof(VectorIds))]
        public void Shared_vector_is_redacted_exactly_and_idempotently(string id)
        {
            var vector = Load().Single(v => v.Id == id);

            var actual = PrivacyRules.Redact(vector.Input);
            actual.Should().Be(vector.Expected, "vector {0}", id);
            PrivacyRules.Redact(actual).Should().Be(actual, "rules must be idempotent for {0}", id);
        }

        [Theory]
        [InlineData("Authorization", true)]
        [InlineData(" X-CSRF-Token ", true)]
        [InlineData("user_email", true)]
        [InlineData("PWD", true)]
        [InlineData("pwdx", false)]
        [InlineData("userId", false)]
        [InlineData("faultlens.operation", false)]
        [InlineData("api-key", true)]
        public void Sensitive_key_semantics(string key, bool expected)
        {
            SensitiveKey.IsSensitive(key).Should().Be(expected);
        }
    }

    public sealed class EnvelopeRedactionTests
    {
        [Fact]
        public void Exception_message_and_stack_frames_are_redacted()
        {
            var ex = new InvalidOperationException("failed for " + Synthetic.Email + " password=" + Synthetic.Password);

            var envelope = Synthetic.Builder().WithException(ex).Build();

            envelope.Exception!.Message.Should().Contain("[REDACTED:email]").And.Contain("password=[REDACTED:kv_secret]");
            Synthetic.AssertNoSecrets(Synthetic.Json(envelope));
        }

        [Fact]
        public void Stack_frame_text_and_file_are_redacted_and_bounded()
        {
            var frames = new List<StackFrameInfo>();
            frames.Add(new StackFrameInfo(file: "https://u:p@host.test/a.js?token=abc123#x", method: "at M(" + Synthetic.Email + ")", line: 3));
            for (var i = 0; i < 300; i++)
                frames.Add(new StackFrameInfo(method: new string('m', 3000)));
            var exception = new ExceptionInfo("T", "message", frames);
            var raw = new ErrorEnvelopeV1("e1", DateTimeOffset.UtcNow, "test", new SdkInfo(), exception: exception);

            var result = EnvelopePrivacyPolicy.Apply(raw);

            result.Exception!.Stacktrace.Should().HaveCount(200);
            result.Exception.Stacktrace[0].Method.Should().Contain("[REDACTED:email]");
            result.Exception.Stacktrace[0].File.Should().Be("https://[REDACTED]@host.test/a.js?token=[Filtered]#x");
            result.Exception.Stacktrace[1].Method!.Length.Should().Be(1024);
            result.Exception.Stacktrace[0].Line.Should().Be(3);
        }

        [Fact]
        public void Message_tags_fingerprint_and_correlation_id_are_redacted()
        {
            var tags = new Dictionary<string, string>
            {
                ["faultlens.operation"] = "checkout",
                ["note"] = "mail " + Synthetic.Email,
                ["session_id"] = "abc",
                ["bob.synthetic@example.test"] = "x"
            };

            var envelope = Synthetic.Builder()
                .WithMessage("card " + Synthetic.Card + " " + Synthetic.Jwt)
                .WithFingerprint("fp-" + Synthetic.Aws)
                .WithContext(correlationId: "corr:" + Synthetic.Email)
                .WithTags(tags)
                .Build();

            envelope.Message.Should().Be("card [REDACTED:card] [REDACTED:jwt]");
            envelope.Fingerprint.Should().Be("fp-[REDACTED:aws_key]");
            envelope.CorrelationId.Should().Be("corr:[REDACTED:email]");
            envelope.Tags!["faultlens.operation"].Should().Be("checkout");
            envelope.Tags["note"].Should().Be("mail [REDACTED:email]");
            envelope.Tags["session_id"].Should().Be("[Filtered]");
            envelope.Tags["[REDACTED:email]"].Should().Be("[Filtered]");
            Synthetic.AssertNoSecrets(Synthetic.Json(envelope));
        }

        [Fact]
        public void Breadcrumb_text_and_nested_data_are_redacted_with_key_collision_suffix()
        {
            var data = new Dictionary<string, object>
            {
                ["token"] = "abc",
                ["nested"] = new Dictionary<string, object>
                {
                    ["password"] = Synthetic.Password,
                    ["ok"] = "fine " + Synthetic.Email,
                    ["list"] = new object[] { "a " + Synthetic.Card, 5 }
                },
                ["a.synthetic@example.test"] = "v1",
                ["b.synthetic@example.test"] = "v2",
                ["[REDACTED:email]"] = "v0"
            };
            var crumb = new BreadcrumbInfo("2024-01-01T00:00:00Z", 1, "application", "log", "cat " + Synthetic.Email,
                "info", "msg " + Synthetic.Jwt, source: "src", entityType: "t", entityId: Synthetic.Card, data: data);

            var envelope = Synthetic.Builder().WithMessage("m").WithBreadcrumbs(new[] { crumb }).Build();

            var b = envelope.Breadcrumbs![0];
            b.Category.Should().Be("cat [REDACTED:email]");
            b.Message.Should().Be("msg [REDACTED:jwt]");
            b.EntityId.Should().Be("[REDACTED:card]");
            b.Data!["token"].Should().Be("[Filtered]");
            var nested = (Dictionary<string, object>)b.Data["nested"];
            nested["password"].Should().Be("[Filtered]");
            nested["ok"].Should().Be("fine [REDACTED:email]");
            ((List<object>)nested["list"])[0].Should().Be("a [REDACTED:card]");
            ((List<object>)nested["list"])[1].Should().Be(5);
            // The redacted form of an e-mail-shaped key is itself a sensitive name, so every value is filtered,
            // and colliding keys are kept apart with a numeric suffix.
            b.Data["[REDACTED:email]"].Should().Be("[Filtered]");
            b.Data["[REDACTED:email]#2"].Should().Be("[Filtered]");
            b.Data["[REDACTED:email]#3"].Should().Be("[Filtered]");
            b.Data.Keys.Should().NotContain(k => k.Contains("@"));
            Synthetic.AssertNoSecrets(Synthetic.Json(envelope));
        }

        [Fact]
        public void Email_shaped_key_with_sensitive_name_is_filtered()
        {
            var data = new Dictionary<string, object> { ["user@example.test"] = "value" };
            var crumb = new BreadcrumbInfo("t", 1, "application", "log", "c", "info", "m", data: data);

            var envelope = Synthetic.Builder().WithMessage("m").WithBreadcrumbs(new[] { crumb }).Build();

            envelope.Breadcrumbs![0].Data!["[REDACTED:email]"].Should().Be("[Filtered]");
        }

        [Fact]
        public void Url_userinfo_query_fragment_referrer_and_user_agent_are_handled()
        {
            var request = new RequestContextInfo(
                requestedUrl: "https://user:pa55@api.example.test/v1?code=1&q=dotnet&password=x#access_token=abc&view=2",
                queryString: "?token=abc&q=1&email=" + Synthetic.Email,
                referrer: "https://ref.example.test/p?session=zzz&reset=" + Synthetic.Email);
            var client = new ClientContextInfo(userAgent: new string('u', 2000));

            var envelope = Synthetic.Builder().WithMessage("m").WithRequestContext(request).WithClientContext(client).Build();

            envelope.Request!.RequestedUrl.Should().Be("https://[REDACTED]@api.example.test/v1?code=[Filtered]&q=dotnet&password=[Filtered]#access_token=[Filtered]&view=2");
            envelope.Request.QueryString.Should().Be("?token=[Filtered]&q=1&email=[Filtered]");
            envelope.Request.Referrer.Should().Be("https://ref.example.test/p?session=[Filtered]&reset=[REDACTED:email]");
            envelope.Client!.UserAgent!.Length.Should().Be(512);
            Synthetic.AssertNoSecrets(Synthetic.Json(envelope));
        }

        [Fact]
        public void Policy_output_is_a_fixed_point()
        {
            var envelope = Synthetic.Builder()
                .WithMessage("x " + Synthetic.Email + " " + Synthetic.Jwt)
                .WithTags(new Dictionary<string, string> { ["a@b.example"] = "v " + Synthetic.Card })
                .Build();

            Synthetic.Json(EnvelopePrivacyPolicy.Apply(envelope)).Should().Be(Synthetic.Json(envelope));
        }

        [Fact]
        public void Identity_values_pass_through_unchanged_and_are_not_copied_elsewhere()
        {
            var builder = Synthetic.Builder()
                .WithMessage("hello")
                .WithUserId("user.synthetic@example.test")
                .WithContext(tenantId: "tenant-1", accountId: "acct-" + Synthetic.Card, useConfiguredIdentity: false);

            var envelope = builder.Build();

            envelope.UserId.Should().Be("user.synthetic@example.test");
            envelope.AccountId.Should().Be("acct-" + Synthetic.Card);
            envelope.TenantId.Should().Be("tenant-1");
            envelope.Message.Should().Be("hello");
            envelope.Request.Should().BeNull();
            envelope.Tags.Should().BeNull();
            envelope.Breadcrumbs.Should().BeNull();
        }
    }

    public sealed class EnvelopeBoundsTests
    {
        private sealed class ThrowingToString
        {
            public override string ToString() => throw new InvalidOperationException("boom");
        }

        private static BreadcrumbInfo Crumb(object? value) =>
            new BreadcrumbInfo("t", 1, "application", "log", "c", "info", "m",
                data: new Dictionary<string, object> { ["k"] = value! });

        [Fact]
        public void Oversized_strings_are_bounded()
        {
            var envelope = Synthetic.Builder().WithMessage(new string('x', 20000)).Build();

            envelope.Message!.Length.Should().Be(8192);
        }

        [Fact]
        public void Deep_and_wide_dictionaries_are_bounded()
        {
            object deep = "leaf";
            for (var i = 0; i < 30; i++)
                deep = new Dictionary<string, object> { ["n"] = deep };
            var wide = Enumerable.Range(0, 500).ToDictionary(i => "k" + i, i => (object)i);

            var envelope = Synthetic.Builder().WithMessage("m")
                .WithBreadcrumbs(new[] { Crumb(deep), Crumb(wide) }).Build();

            var json = Synthetic.Json(envelope);
            json.Should().Contain("[Truncated]");
            var wideResult = (Dictionary<string, object>)envelope.Breadcrumbs![1].Data!["k"];
            wideResult.Count.Should().Be(100);
        }

        [Fact]
        public void More_than_100_breadcrumbs_and_200_frames_are_capped()
        {
            var crumbs = Enumerable.Range(1, 250)
                .Select(i => new BreadcrumbInfo("t", i, "application", "log", "c", "info", "m" + i)).ToList();

            var envelope = Synthetic.Builder().WithMessage("m").WithBreadcrumbs(crumbs).Build();

            envelope.Breadcrumbs.Should().HaveCount(100);
            envelope.Breadcrumbs![99].Sequence.Should().Be(250);
        }

        [Fact]
        public void Infinite_and_self_referencing_enumerables_terminate()
        {
            IEnumerable<object> Infinite() { while (true) yield return "x"; }
            var self = new List<object>();
            self.Add(self);

            var envelope = Synthetic.Builder().WithMessage("m")
                .WithBreadcrumbs(new[] { Crumb(Infinite()), Crumb(self) }).Build();

            ((List<object>)envelope.Breadcrumbs![0].Data!["k"]).Should().HaveCount(100);
            Synthetic.Json(envelope).Should().Contain("[Truncated]");
        }

        [Fact]
        public void Value_whose_ToString_throws_fails_the_builder_closed()
        {
            var builder = Synthetic.Builder().WithMessage("m").WithBreadcrumbs(new[] { Crumb(new ThrowingToString()) });

            Action act = () => builder.Build();

            act.Should().Throw<EnvelopePrivacyException>();
        }

        [Fact]
        public void Client_fails_closed_when_policy_throws_and_reports_reason_code()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(Synthetic.Options(), transport, _ => throw new InvalidOperationException(Synthetic.Email));
            DeliveryResult? result = null;

            client.CaptureException(new Exception("x " + Synthetic.Email), callback: r => result = r);
            client.CaptureMessage("y", callback: r => result = r);

            transport.Sent.Should().BeEmpty();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("sdk_privacy_policy_failed");
            result.ReasonCode.Should().Be("sdk_privacy_policy_failed");
            result.Retryable.Should().BeFalse();
            result.Reason.Should().Be(DeliveryFailureKind.Unknown);
            result.ErrorMessage.Should().NotContain("synthetic");
        }

        [Fact]
        public void Client_fail_closed_survives_throwing_callback()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(Synthetic.Options(), transport, _ => throw new InvalidOperationException());

            Action act = () => client.CaptureMessage("y", callback: _ => throw new InvalidOperationException());

            act.Should().NotThrow();
            transport.Sent.Should().BeEmpty();
        }

        [Fact]
        public void AddBreadcrumb_with_throwing_data_does_not_throw_into_host()
        {
            var client = new FaultLensClient(Synthetic.Options(), new RecordingTransport());

            Action act = () => client.AddStep("c", "m", data: new Dictionary<string, object> { ["k"] = new ThrowingToString() });

            act.Should().NotThrow();
        }
    }

    public sealed class ClientPrivacyContractTests
    {
        private static string[] Names(JsonElement element) => element.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        [Fact]
        public void Default_collection_has_exactly_the_documented_properties()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(Synthetic.Options(), transport);

            using (var scope = client.BeginRequest("GET", "/api/items/{id}"))
            {
                scope.SetRequestContext("https://api.example.test/api/items/1?q=1", "https://app.example.test/", "UA/1.0", "q=1");
                scope.SetCorrelationId("corr-1");
                client.CaptureException(new InvalidOperationException("boom"));
            }

            var json = Synthetic.Json(transport.Sent.Single());
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Names(root).Should().Equal("breadcrumbs", "client", "correlationId", "environment", "eventId", "exception", "request", "sdk", "timestamp");
            Names(root.GetProperty("request")).Should().Equal("correlationId", "durationMs", "method", "queryString", "referrer", "requestedUrl", "route");
            Names(root.GetProperty("client")).Should().Equal("runtimeName", "runtimeVersion", "userAgent");
            Names(root.GetProperty("exception")).Should().Equal("message", "stacktrace", "type");
        }

        [Fact]
        public void Captured_request_scope_never_exposes_secrets_or_identity_copies()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(Synthetic.Options(), transport);

            using (var scope = client.BeginRequest("POST", "/pay", data: new Dictionary<string, object> { ["authorization"] = "Bearer " + Synthetic.Jwt }))
            {
                scope.SetRequestContext("https://u:" + Synthetic.Password + "@api.example.test/pay?token=" + Synthetic.Jwt, referrer: "https://r.example.test/?e=" + Synthetic.Email);
                scope.SetUser("user-opaque-1");
                scope.SetTag("customer_email", Synthetic.Email);
                client.AddStep("pay", "card " + Synthetic.Card, data: new Dictionary<string, object> { ["note"] = Synthetic.Email });
                client.CaptureException(new Exception("failed " + Synthetic.Aws));
            }

            var envelope = transport.Sent.Single();
            var json = Synthetic.Json(envelope);
            Synthetic.AssertNoSecrets(json);
            envelope.UserId.Should().Be("user-opaque-1");
            json.Split("user-opaque-1").Length.Should().Be(2, "identity appears only in userId");
            envelope.Tags!["customer_email"].Should().Be("[Filtered]");
        }
    }

    public sealed class RetrySafetyAndPerformanceTests
    {
        private sealed class CapturingHandler : HttpMessageHandler
        {
            private readonly Queue<HttpStatusCode> _statuses;
            public List<string> Bodies { get; } = new List<string>();

            public CapturingHandler(params HttpStatusCode[] statuses) => _statuses = new Queue<HttpStatusCode>(statuses);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Bodies.Add(await request.Content!.ReadAsStringAsync());
                return new HttpResponseMessage(_statuses.Dequeue()) { Content = new StringContent("{}") };
            }
        }

        private sealed class NoDelay : IAsyncDelay
        {
            public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task Every_retry_attempt_carries_only_the_redacted_payload()
        {
            var handler = new CapturingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
            var options = new FaultLensOptions(apiKey: "test-key", environment: "test", endpoint: new Uri("https://tenant.faultlens.in"));
            var transport = new HttpEventTransport(options, handler, new NoDelay());
            var envelope = new ErrorEnvelopeBuilder(options, new SdkInfo("t", "1"))
                .WithException(new Exception("x " + Synthetic.Email + " " + Synthetic.Jwt))
                .WithMessage("password=" + Synthetic.Password)
                .Build();

            await transport.SendWithRetryAsync(envelope, null, CancellationToken.None);

            handler.Bodies.Should().HaveCount(3);
            foreach (var body in handler.Bodies)
                Synthetic.AssertNoSecrets(body);
            handler.Bodies.Distinct().Should().HaveCount(1);
        }

        public static IEnumerable<object[]> Adversarial()
        {
            yield return new object[] { new string('a', 8192) };
            yield return new object[] { string.Concat(Enumerable.Repeat("a.", 4096)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("1-", 4096)) };
            yield return new object[] { new string('1', 8192) };
            yield return new object[] { string.Concat(Enumerable.Repeat("1 ", 4096)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("eyJ", 2730)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("password=", 910)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("password=\"", 800)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("@", 8192)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("a@", 4096)) };
            yield return new object[] { "x@" + string.Concat(Enumerable.Repeat("a.", 4000)) };
            yield return new object[] { string.Concat(Enumerable.Repeat("Bearer ", 1170)) };
            yield return new object[] { "Bearer" + new string(' ', 8000) };
            yield return new object[] { string.Concat(Enumerable.Repeat("http://", 1170)) };
            yield return new object[] { "-----BEGIN PRIVATE KEY-----" + string.Concat(Enumerable.Repeat("-----BEGIN PRIVATE KEY-----", 280)) };
        }

        [Theory]
        [MemberData(nameof(Adversarial))]
        public void Adversarial_input_is_redacted_quickly(string input)
        {
            PrivacyRules.RedactOrFilter(input); // warm up regex construction
            var sw = Stopwatch.StartNew();
            var result = PrivacyRules.RedactOrFilter(input);
            sw.Stop();

            sw.ElapsedMilliseconds.Should().BeLessThan(200);
            result.Should().NotBeNull();
        }
    }
}
