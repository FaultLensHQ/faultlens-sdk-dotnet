using FaultLens.Sdk.Envelopes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FaultLens.Sdk.Tests.Serialization
{
    public sealed class ErrorEnvelopeSnapshotTests
    {
        [Fact]
        public void Envelope_Snapshot_Should_Not_Change()
        {
            // Arrange
            var envelope = BuildKnownEnvelope();

            // Act
            JsonElement actualJson = JsonTestHelper.Serialize(envelope).RootElement.Clone();
            var expectedJson = ExpectedEnvelopeJson;

            var actualNode = JsonNode.Parse(actualJson.GetRawText());
            var expectedNode = JsonNode.Parse(expectedJson);

            // Assert
            Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), "Serialized ErrorEnvelope JSON contract has changed.");
        }

        private static ErrorEnvelope BuildKnownEnvelope()
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

            return envelope;
        }

        private const string ExpectedEnvelopeJson = @"
        {
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
          }
        }";
    }
}
