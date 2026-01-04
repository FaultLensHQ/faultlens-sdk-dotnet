using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class StackFrameInfo
    {
        // Optional
        [JsonPropertyName("file")]
        public string File { get; }

        [JsonPropertyName("method")]
        public string Method { get; }

        [JsonPropertyName("line")]
        public int? Line { get; }

        public StackFrameInfo(string file = null, string method = null, int? line = null)
        {
            File = file;
            Method = method;
            Line = line;
        }
    }
}
