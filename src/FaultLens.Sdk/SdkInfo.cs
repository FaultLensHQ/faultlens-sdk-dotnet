using System;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk
{
    public sealed class SdkInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("version")]
        public string Version { get; }

        public SdkInfo(string name = "faultlens-dotnet", string version = "1.0.0")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SDK name is required.", nameof(name));

            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("SDK version is required.", nameof(version));

            Name = name;
            Version = version;
        }
    }
}
