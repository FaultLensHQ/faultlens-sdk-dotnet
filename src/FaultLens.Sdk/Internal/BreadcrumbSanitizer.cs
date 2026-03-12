using System;
using System.Collections.Generic;

namespace FaultLens.Sdk.Internal
{
    internal static class BreadcrumbSanitizer
    {
        private static readonly HashSet<string> SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "token",
            "password",
            "secret",
            "api_key",
            "apikey",
            "access_token",
            "refresh_token",
            "cookie",
            "set-cookie"
        };

        public static IReadOnlyDictionary<string, object> Sanitize(IReadOnlyDictionary<string, object> data)
        {
            if (data == null || data.Count == 0)
                return data;

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in data)
            {
                result[kvp.Key] = SensitiveKeys.Contains(kvp.Key)
                    ? "***redacted***"
                    : kvp.Value;
            }

            return result;
        }
    }
}
