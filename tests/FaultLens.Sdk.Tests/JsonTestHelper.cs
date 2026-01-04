using System.Text.Json;

namespace FaultLens.Sdk.Tests
{
    internal static class JsonTestHelper
    {
        public static JsonDocument Serialize(object obj)
        {
            var json = JsonSerializer.Serialize(obj, FaultLensJson.Options);
            return JsonDocument.Parse(json);
        }
    }
}
