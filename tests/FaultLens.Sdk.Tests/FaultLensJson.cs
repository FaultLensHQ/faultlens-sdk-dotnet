using System.Text.Json;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Tests
{
    internal static class FaultLensJson
    {
        public static readonly JsonSerializerOptions Options = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false
            };

#if NETSTANDARD2_1
        options.IgnoreNullValues = true;
#else
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
#endif
            return options;
        }
    }
}
