using System;
using System.Collections.Generic;

namespace FaultLens.Sdk
{
    public interface IFaultLensClient : IDisposable
    {
        void AddBreadcrumb(Breadcrumb breadcrumb);

        void AddStep(
            string category,
            string message,
            BreadcrumbLayer layer = BreadcrumbLayer.Application,
            BreadcrumbLevel level = BreadcrumbLevel.Info,
            string source = null,
            string entityType = null,
            string entityId = null,
            IReadOnlyDictionary<string, object> data = null);

        void AddDecision(
            string category,
            string message,
            BreadcrumbLayer layer = BreadcrumbLayer.Application,
            BreadcrumbLevel level = BreadcrumbLevel.Info,
            string source = null,
            string entityType = null,
            string entityId = null,
            IReadOnlyDictionary<string, object> data = null);

        IFaultLensRequestScope BeginRequest(
            string method,
            string route,
            string source = null,
            IReadOnlyDictionary<string, object> data = null);

        void CaptureException(Exception exception, string fingerprint = null, Action<DeliveryResult> callback = null);

        void CaptureMessage(string message, string fingerprint = null, Action<DeliveryResult> callback = null);
    }
}
