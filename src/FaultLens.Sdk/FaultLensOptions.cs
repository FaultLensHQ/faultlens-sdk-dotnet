using System;
using System.Collections.Generic;

namespace FaultLens.Sdk
{
    public sealed class FaultLensOptions
    {
        public string ApiKey { get; }
        public Uri Endpoint { get; }
        public string Environment { get; }
        public string Release { get; }
        public int BreadcrumbCapacity { get; }

        public FaultLensOptions(
            string apiKey,
            string environment = "production",
            string release = null,
            Uri endpoint = null,
            int breadcrumbCapacity = 40,
            string serviceName = null,
            string serviceVersion = null,
            string tenantId = null,
            string customerId = null,
            string accountId = null,
            string anonymousId = null,
            string correlationId = null)
        {
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            Environment = environment;
            Release = release;
            ServiceName = Normalize(serviceName);
            ServiceVersion = Normalize(serviceVersion);
            TenantId = Normalize(tenantId);
            CustomerId = Normalize(customerId);
            AccountId = Normalize(accountId);
            AnonymousId = Normalize(anonymousId);
            CorrelationId = Normalize(correlationId);

            Endpoint = endpoint ?? new Uri("https://api.faultlens.io");

            if (breadcrumbCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(breadcrumbCapacity), "Breadcrumb capacity must be greater than 0.");

            BreadcrumbCapacity = breadcrumbCapacity;
        }

        public string ServiceName { get; }

        public string ServiceVersion { get; }

        public string TenantId { get; }

        public string CustomerId { get; }

        public string AccountId { get; }

        public string AnonymousId { get; }

        public string CorrelationId { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
