using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Transport;
using System;
using System.Collections.Generic;

namespace FaultLens.Sdk.Tests
{
    public sealed class FaultLensClientBreadcrumbTests
    {
        [Fact]
        public void CaptureMessage_Should_Send_AddStep_And_AddDecision_Breadcrumbs()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(
                new FaultLensOptions(apiKey: "test-key", environment: "production"),
                transport);

            client.AddStep(
                category: "project.mapping",
                message: "Mapping Project entity to ProjectDetailsDto",
                layer: BreadcrumbLayer.Application,
                source: "ProjectsService",
                entityType: "Project",
                entityId: "proj_42");

            client.AddDecision(
                category: "platform.resolve",
                message: "Using project primary platform as fallback",
                layer: BreadcrumbLayer.Domain,
                level: BreadcrumbLevel.Warning,
                data: new Dictionary<string, object> { ["platform"] = "dotnet" });

            client.CaptureMessage("failed");

            Assert.NotNull(transport.LastEnvelope);
            Assert.Equal(2, transport.LastEnvelope.Breadcrumbs.Count);
            Assert.Equal("step", transport.LastEnvelope.Breadcrumbs[0].Type);
            Assert.Equal("application", transport.LastEnvelope.Breadcrumbs[0].Layer);
            Assert.Equal("decision", transport.LastEnvelope.Breadcrumbs[1].Type);
            Assert.Equal("domain", transport.LastEnvelope.Breadcrumbs[1].Layer);
            Assert.Equal("warning", transport.LastEnvelope.Breadcrumbs[1].Level);
        }

        [Fact]
        public void BeginRequest_And_CaptureException_Should_Add_Request_Boundary_Breadcrumbs()
        {
            var transport = new RecordingTransport();
            var client = new FaultLensClient(
                new FaultLensOptions(apiKey: "test-key", environment: "production"),
                transport);

            using (client.BeginRequest("GET", "/api/projects/{id}", "AspNetCore"))
            {
                client.CaptureException(new InvalidOperationException("boom"));
            }

            Assert.NotNull(transport.LastEnvelope);
            Assert.Equal("request.started", transport.LastEnvelope.Breadcrumbs[0].Category);
            Assert.Equal("request.failed", transport.LastEnvelope.Breadcrumbs[1].Category);
            Assert.Equal("error", transport.LastEnvelope.Breadcrumbs[1].Level);
        }

        private sealed class RecordingTransport : IEventTransport
        {
            public ErrorEnvelopeV1 LastEnvelope { get; private set; }

            public void Dispose()
            {
            }

            public void Flush(TimeSpan timeout)
            {
            }

            public void Send(ErrorEnvelopeV1 envelope, Action<DeliveryResult> callback = null)
            {
                LastEnvelope = envelope;
                callback?.Invoke(DeliveryResult.Delivered());
            }
        }
    }
}
