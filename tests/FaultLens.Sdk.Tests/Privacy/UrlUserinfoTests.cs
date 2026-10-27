using System.Text.Json;
using FaultLens.Sdk.Internal;
using FluentAssertions;

namespace FaultLens.Sdk.Tests.Privacy
{
    // All values in this file are synthetic.
    public sealed class UrlUserinfoTests
    {
        private const string User = "alice";
        private const string Pw = "S3cretPw";

        private static string CaptureJson(string url, string? referrer = null, Action<FaultLensClient>? extra = null)
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(Synthetic.Options(), transport);
            using (var scope = client.BeginRequest("GET", "/x"))
            {
                scope.SetRequestContext(url: url, referrer: referrer);
                extra?.Invoke(client);
                client.CaptureException(new InvalidOperationException("boom"));
            }

            return Synthetic.Json(transport.Sent.Single());
        }

        private static void AssertNoCredentials(string text)
        {
            text.Should().NotContain(User + ":");
            text.Should().NotContain(Pw);
        }

        [Theory]
        [InlineData("https://alice:S3cretPw@api.example.test?token=abc123", "https://[REDACTED]@api.example.test?token=[Filtered]")]
        [InlineData("https://alice:S3cretPw@api.example.test#access_token=abc123", "https://[REDACTED]@api.example.test#access_token=[Filtered]")]
        [InlineData("https://alice:S3cretPw@api.example.test?%74oken=abc123&x=1", "https://[REDACTED]@api.example.test?%74oken=[Filtered]&x=1")]
        [InlineData("https://alice:S3cretPw@api.example.test#%61ccess%5Ftoken=abc123", "https://[REDACTED]@api.example.test#%61ccess%5Ftoken=[Filtered]")]
        [InlineData("https://alice:S3cretPw@api.example.test/path?q=1#f", "https://[REDACTED]@api.example.test/path?q=1#f")]
        public void Userinfo_is_removed_and_sensitive_params_filtered(string input, string expected)
        {
            var sanitized = QueryStringSanitizer.SanitizeUrl(input);
            sanitized.Should().Be(expected);
            AssertNoCredentials(sanitized);
            sanitized.Should().NotContain("abc123");

            var asUrl = CaptureJson(input);
            AssertNoCredentials(asUrl);
            asUrl.Should().NotContain("abc123");

            var asReferrer = CaptureJson("https://api.example.test/", referrer: input);
            AssertNoCredentials(asReferrer);
            asReferrer.Should().NotContain("abc123");
        }

        [Theory]
        [InlineData("https://api.example.test?next=/a@b")]
        [InlineData("https://api.example.test#x@y")]
        [InlineData("https://api.example.test/p?next=https://a:b@x")]
        public void At_sign_outside_authority_is_not_treated_as_userinfo(string input)
        {
            var sanitized = QueryStringSanitizer.SanitizeUrl(input);
            sanitized.Should().NotStartWith("https://[REDACTED]@");
            sanitized.Should().StartWith("https://api.example.test");
        }

        [Fact]
        public void No_credentials_urls_with_at_in_query_or_fragment_stay_intact()
        {
            QueryStringSanitizer.SanitizeUrl("https://api.example.test?next=/a@b").Should().Be("https://api.example.test?next=/a@b");
            QueryStringSanitizer.SanitizeUrl("https://api.example.test#x@y").Should().Be("https://api.example.test#x@y");
        }

        [Fact]
        public void Private_key_name_is_sensitive_in_all_casings()
        {
            SensitiveKey.IsSensitive("privateKey").Should().BeTrue();
            SensitiveKey.IsSensitive("PrivateKey").Should().BeTrue();
            SensitiveKey.IsSensitive("private_key").Should().BeTrue();
        }

        [Fact]
        public void Breadcrumb_data_key_private_key_is_filtered_in_final_json()
        {
            var json = CaptureJson("https://api.example.test/", extra: c =>
                c.AddStep("c", "m", data: new Dictionary<string, object> { ["privateKey"] = "synthetic-pk-value" }));

            json.Should().NotContain("synthetic-pk-value");
            using var doc = JsonDocument.Parse(json);
            json.Should().Contain("\"privateKey\":\"[Filtered]\"");
        }
    }
}
