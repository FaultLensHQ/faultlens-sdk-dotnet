using FaultLens.Sdk.Envelopes;
using FluentAssertions;
using System.Text.Json;

namespace FaultLens.Sdk.Tests.Serialization
{
    public sealed class ErrorEnvelopeSnapshotTests
    {
        [Fact]
        public void Envelope_Snapshot_Should_Not_Change()
        {
            // Arrange
            var sdk = new SdkInfo(name: "faultlens-dotnet", version: "1.0.0");

            var stackFrames = new List<StackFrameInfo>
            {
                new StackFrameInfo(
                    file: "Program.cs",
                    method: "Main",
                    line: 42
                )
            };

            var exception = new ExceptionInfo(
                type: "System.InvalidOperationException",
                message: "Something went wrong",
                stacktrace: stackFrames
            );

            var envelope = new ErrorEnvelope(
                eventId: "evt_123",
                timestamp: new DateTimeOffset(2024, 01, 01, 10, 00, 00, TimeSpan.Zero),
                environment: "production",
                sdk: sdk,
                fingerprint: "fp_abc",
                exception: exception,
                message: null
            );

            // Act
            var json = JsonSerializer.Serialize(
                envelope,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            // Assert
            const string expectedJson = @"{
  ""eventId"": ""evt_123"",
  ""timestamp"": ""2024-01-01T10:00:00+00:00"",
  ""environment"": ""production"",
  ""sdk"": {
    ""name"": ""faultlens-dotnet"",
    ""version"": ""1.0.0""
  },
  ""fingerprint"": ""fp_abc"",
  ""exception"": {
    ""type"": ""System.InvalidOperationException"",
    ""message"": ""Something went wrong"",
    ""stacktrace"": [
      {
        ""file"": ""Program.cs"",
        ""method"": ""Main"",
        ""line"": 42
      }
    ]
  },
  ""message"": null
}";

            json.Should().Be(expectedJson);
        }
    }
}
