using FaultLens.Sdk.Builders;

namespace FaultLens.Sdk.Tests.Serialization
{
    public sealed class ErrorEnvelopeSerializationTests
    {
        [Fact]
        public void ErrorEnvelope_Should_Serialize_With_Required_Fields()
        {
            var options = new FaultLensOptions(apiKey: "test-key",
                environment: "production");

            var builder = new ErrorEnvelopeBuilder(
                options,
                new SdkInfo("faultlens-dotnet", "1.0.0"));

            var envelope = builder
                .WithMessage("Something went wrong")
                .Build();

            var json = JsonTestHelper.Serialize(envelope);
            var root = json.RootElement;

            Assert.True(root.TryGetProperty("eventId", out _));
            Assert.True(root.TryGetProperty("timestamp", out _));
            Assert.True(root.TryGetProperty("environment", out _));
            Assert.True(root.TryGetProperty("sdk", out _));
        }

        [Fact]
        public void ErrorEnvelope_Should_Serialize_Exception_Correctly()
        {
            var options = new FaultLensOptions(
                apiKey: "test-key",
                environment: "production");

            var builder = new ErrorEnvelopeBuilder(options, new SdkInfo());

            Exception captured;

            try
            {
                ThrowBoom();
                throw new Exception("Should not reach here");
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            var envelope = builder.WithException(captured).Build();

            var json = JsonTestHelper.Serialize(envelope);
            var exception = json.RootElement.GetProperty("exception");

            Assert.Equal("System.InvalidOperationException", exception.GetProperty("type").GetString());

            Assert.Equal("Boom", exception.GetProperty("message").GetString());

            var stacktrace = exception.GetProperty("stacktrace");
            Assert.True(stacktrace.GetArrayLength() > 0);
        }

        [Fact]
        public void ErrorEnvelope_Should_Omit_Null_Optional_Fields()
        {
            var options = new FaultLensOptions(
                apiKey: "test-key",
                environment: "production");

            var envelope = new ErrorEnvelopeBuilder(options, new SdkInfo())
                .Build();

            var json = JsonTestHelper.Serialize(envelope);
            var root = json.RootElement;

            Assert.False(root.TryGetProperty("fingerprint", out _));
            Assert.False(root.TryGetProperty("message", out _));
            Assert.False(root.TryGetProperty("breadcrumbs", out _));
        }

        #region Privat methods

        private static void ThrowBoom()
        {
            throw new InvalidOperationException("Boom");
        }

        #endregion
    }
}
