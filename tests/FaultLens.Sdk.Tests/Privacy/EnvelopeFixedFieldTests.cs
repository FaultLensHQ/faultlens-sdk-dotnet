using FaultLens.Sdk.Builders;
using FaultLens.Sdk.Envelopes;
using FluentAssertions;

namespace FaultLens.Sdk.Tests.Privacy
{
    // All values in this file are synthetic. Fixed-vocabulary fields are exempt only for exact allow-listed
    // or SDK-owned values; everything else is scanned.
    public sealed class EnvelopeFixedFieldTests
    {
        public static IEnumerable<object[]> Secrets() => new[]
        {
            new object[] { "4111111111111111" },
            new object[] { "AKIAABCDEFGHIJKLMNOP" },
            new object[] { "ghp_ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" },
            new object[] { "2345-6789-0124" },
            new object[] { Synthetic.Jwt },
        };

        private static void AssertScanned(string json, string secret)
        {
            json.Should().NotContain(secret);
            json.Should().Contain("[REDACTED:");
        }

        [Theory]
        [MemberData(nameof(Secrets))]
        public void Customer_set_environment_is_scanned(string secret)
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(new FaultLensOptions(apiKey: "test-key", environment: "env-" + secret), transport);

            client.CaptureMessage("m");

            AssertScanned(Synthetic.Json(transport.Sent.Single()), secret);
        }

        [Theory]
        [MemberData(nameof(Secrets))]
        public void Custom_sdk_info_is_scanned(string secret)
        {
            var envelope = new ErrorEnvelopeBuilder(Synthetic.Options(), new SdkInfo("sdk-" + secret, "1.0-" + secret))
                .WithMessage("m").Build();

            AssertScanned(Synthetic.Json(envelope), secret);
        }

        [Theory]
        [MemberData(nameof(Secrets))]
        public void Custom_breadcrumb_fixed_fields_are_scanned(string secret)
        {
            foreach (var field in new[] { "timestamp", "layer", "type", "level" })
            {
                var crumb = new BreadcrumbInfo(
                    field == "timestamp" ? "ts-" + secret : "2024-01-01T00:00:00.0000000+00:00",
                    1,
                    field == "layer" ? "layer-" + secret : "request",
                    field == "type" ? "type-" + secret : "http",
                    "c",
                    field == "level" ? "level-" + secret : "info",
                    "m");
                var envelope = Synthetic.Builder().WithMessage("m").WithBreadcrumbs(new[] { crumb }).Build();

                AssertScanned(Synthetic.Json(envelope), secret);
            }
        }

        [Fact]
        public void Allow_listed_and_sdk_owned_values_are_unchanged()
        {
            var stamp = DateTimeOffset.UtcNow.ToString("O");
            var crumb = new BreadcrumbInfo(stamp, 1, "request", "http", "c", "info", "m");
            var envelope = new ErrorEnvelopeBuilder(
                    new FaultLensOptions(apiKey: "test-key", environment: "production"), new SdkInfo())
                .WithMessage("m").WithBreadcrumbs(new[] { crumb }).Build();

            envelope.Environment.Should().Be("production");
            envelope.Sdk.Name.Should().Be("faultlens-dotnet");
            envelope.Sdk.Version.Should().Be(new SdkInfo().Version);
            var b = envelope.Breadcrumbs![0];
            b.Timestamp.Should().Be(stamp);
            b.Layer.Should().Be("request");
            b.Type.Should().Be("http");
            b.Level.Should().Be("info");
            Synthetic.Json(envelope).Should().NotContain("[REDACTED:").And.NotContain("[Filtered]");
        }

        [Fact]
        public void Case_variants_of_allow_listed_values_are_not_exempt()
        {
            var crumb = new BreadcrumbInfo("2024-01-01T00:00:00.0000000+00:00", 1, "Request", "HTTP", "c", "Info", "m");
            var envelope = Synthetic.Builder().WithMessage("m").WithBreadcrumbs(new[] { crumb }).Build();

            // Scanned, and clean text is kept as-is.
            envelope.Breadcrumbs![0].Layer.Should().Be("Request");
        }
    }
}
