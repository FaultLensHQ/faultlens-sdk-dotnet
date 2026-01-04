using System;

namespace FaultLens.Sdk
{
    public sealed class FaultLensOptions
    {
        public string ApiKey { get; }
        public Uri Endpoint { get; }
        public string Environment { get; }
        public string Release { get; }

        public FaultLensOptions(string apiKey, string environment = "production", string release = null, Uri endpoint = null)
        {
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            Environment = environment;
            Release = release;

            Endpoint = endpoint ?? new Uri("https://api.faultlens.io");
        }
    }
}
