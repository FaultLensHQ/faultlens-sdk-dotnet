using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk
{
    public sealed class SdkInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("version")]
        public string Version { get; }

        public SdkInfo(string name = "faultlens-dotnet", string version = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SDK name is required.", nameof(name));

            version = string.IsNullOrWhiteSpace(version) ? ResolveSdkVersion() : version;

            Name = name;
            Version = version;
        }

        private static string ResolveSdkVersion()
        {
            var informationalVersion = typeof(SdkInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
                return informationalVersion;

            var assemblyVersion = typeof(SdkInfo).Assembly.GetName().Version?.ToString();
            if (!string.IsNullOrWhiteSpace(assemblyVersion))
                return assemblyVersion;

            return "0.0.0";
        }
    }
}
