using FaultLens.Sdk;
using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Transport;
using System;
using Xunit;

namespace FaultLens.Sdk.Tests
{
    public sealed class ReservedTagsTests
    {
        [Fact]
        public void ReservedTagKeys_HaveExpectedWireNames()
        {
            Assert.Equal("faultlens.capability", FaultLensReservedTags.Capability);
            Assert.Equal("faultlens.criticality", FaultLensReservedTags.Criticality);
            Assert.Equal("faultlens.operation", FaultLensReservedTags.Operation);
            Assert.Equal("faultlens.operation.criticality", FaultLensReservedTags.OperationCriticality);
            Assert.Equal("faultlens.workflow", FaultLensReservedTags.Workflow);
            Assert.Equal("faultlens.job", FaultLensReservedTags.Job);
        }

        [Fact]
        public void SetCapability_EmitsCapabilityCriticalityAndOperationTags()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("POST", "/checkout");
            scope.SetCapability("checkout", FaultLensCriticality.Critical, "payment-capture");

            client.CaptureMessage("captured");

            var tags = transport.LastEnvelope.Tags;
            Assert.NotNull(tags);
            Assert.Equal("checkout", tags["faultlens.capability"]);
            Assert.Equal("critical", tags["faultlens.criticality"]);
            Assert.Equal("payment-capture", tags["faultlens.operation"]);
        }

        [Fact]
        public void SetOperationCriticality_EmitsOperationCriticalityTag()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/api/orders/{id}");
            scope.SetOperationCriticality(FaultLensCriticality.High);

            client.CaptureMessage("captured");

            Assert.Equal("high", transport.LastEnvelope.Tags["faultlens.operation.criticality"]);
        }

        [Fact]
        public void SetWorkflow_EmitsWorkflowTag()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("POST", "/onboard");
            scope.SetWorkflow("tenant-onboarding");

            client.CaptureMessage("captured");

            Assert.Equal("tenant-onboarding", transport.LastEnvelope.Tags["faultlens.workflow"]);
        }

        [Fact]
        public void SetJob_EmitsJobTag()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("POST", "/jobs/run");
            scope.SetJob("nightly-billing-sync");

            client.CaptureMessage("captured");

            Assert.Equal("nightly-billing-sync", transport.LastEnvelope.Tags["faultlens.job"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ReservedTagHelpers_IgnoreNullOrWhitespace(string? value)
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("GET", "/data");
            scope.SetWorkflow(value);
            scope.SetJob(value);
            scope.SetOperationCriticality(value);
            scope.SetCapability(value, value, value);

            client.CaptureMessage("data");

            // No reserved tags were set, so the tag bag stays null (matches SetTag empty-key behaviour).
            Assert.Null(transport.LastEnvelope.Tags);
        }

        [Fact]
        public void ReservedTagHelpers_OnDisposedClientScope_DoNotThrow()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);
            client.Dispose();

            var scope = client.BeginRequest("GET", "/any");
            var ex = Record.Exception(() =>
            {
                scope.SetCapability("checkout", "critical", "payment-capture");
                scope.SetOperationCriticality("high");
                scope.SetWorkflow("tenant-onboarding");
                scope.SetJob("nightly-billing-sync");
            });

            Assert.Null(ex);
        }

        private static FaultLensClient MakeClient(IEventTransport transport)
        {
            return new FaultLensClient(
                new FaultLensOptions(apiKey: "test-key", environment: "test"),
                transport);
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
