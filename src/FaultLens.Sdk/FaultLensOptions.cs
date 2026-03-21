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
            int breadcrumbCapacity = 40)
        {
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            Environment = environment;
            Release = release;

            Endpoint = endpoint ?? new Uri("https://api.faultlens.io");

            if (breadcrumbCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(breadcrumbCapacity), "Breadcrumb capacity must be greater than 0.");

            BreadcrumbCapacity = breadcrumbCapacity;
        }
    }
}
