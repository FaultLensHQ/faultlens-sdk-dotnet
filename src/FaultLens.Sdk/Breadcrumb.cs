using System;
using System.Collections.Generic;

namespace FaultLens.Sdk
{
    public sealed class Breadcrumb
    {
        public DateTimeOffset? Timestamp { get; set; }

        public string Type { get; set; } = "log";

        public string Category { get; set; }

        public string Level { get; set; } = "info";

        public string Message { get; set; }

        public string Source { get; set; }

        public IReadOnlyDictionary<string, object> Data { get; set; }
    }
}
