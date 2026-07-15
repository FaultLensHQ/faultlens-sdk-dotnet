using FaultLens.Sdk;
using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FaultLens.Sdk.Tests
{
    public sealed class ReservedTagsTests
    {
        // The FaultLens ingestion contract consumes exactly these three reserved tags.
        // See docs/capability-metadata.md and the backend IngestErrorEnvelopeV1Mapper.
        private static readonly string[] BackendConsumedReservedTags =
        {
            "faultlens.capability",
            "faultlens.criticality",
            "faultlens.operation"
        };

        [Fact]
        public void SupportedReservedTagKeys_HaveExpectedWireNames()
        {
            Assert.Equal("faultlens.capability", FaultLensReservedTags.Capability);
            Assert.Equal("faultlens.criticality", FaultLensReservedTags.Criticality);
            Assert.Equal("faultlens.operation", FaultLensReservedTags.Operation);
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
        public void SetOperation_EmitsOperationTag()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("POST", "/jobs/run");
            scope.SetOperation("nightly-billing-sync");

            client.CaptureMessage("captured");

            Assert.Equal("nightly-billing-sync", transport.LastEnvelope.Tags["faultlens.operation"]);
        }

        [Fact]
        public void SetCapability_OnlyEmitsBackendConsumedReservedKeys()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using var scope = client.BeginRequest("POST", "/checkout");
            scope.SetCapability("checkout", FaultLensCriticality.Critical, "payment-capture");

            client.CaptureMessage("captured");

            var reservedKeys = transport.LastEnvelope.Tags.Keys
                .Where(k => k.StartsWith("faultlens.", StringComparison.Ordinal))
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();

            // The SDK must never emit a faultlens.* reserved key the backend silently discards.
            Assert.All(reservedKeys, k => Assert.Contains(k, BackendConsumedReservedTags));
        }

        [Fact]
        public void DeprecatedHelpers_AreNoOps_AndEmitNoReservedTags()
        {
            var transport = new RecordingTransport();
            var client = MakeClient(transport);

            using (var scope = client.BeginRequest("POST", "/onboard"))
            {
#pragma warning disable CS0618 // Intentionally exercising deprecated no-op helpers.
                scope.SetOperationCriticality(FaultLensCriticality.High);
                scope.SetWorkflow("tenant-onboarding");
                scope.SetJob("nightly-billing-sync");
#pragma warning restore CS0618
            }

            client.CaptureMessage("captured");

            // No supported metadata was set and the deprecated helpers are no-ops,
            // so no tags (and specifically no discarded reserved keys) are emitted.
            Assert.Null(transport.LastEnvelope.Tags);
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
            scope.SetOperation(value);
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
                scope.SetOperation("nightly-billing-sync");
#pragma warning disable CS0618 // Deprecated no-op helpers must also stay safe.
                scope.SetOperationCriticality("high");
                scope.SetWorkflow("tenant-onboarding");
                scope.SetJob("nightly-billing-sync");
#pragma warning restore CS0618
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
