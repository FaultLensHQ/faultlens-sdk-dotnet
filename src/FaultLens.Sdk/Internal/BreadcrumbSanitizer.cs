using System;
using System.Collections.Generic;
using System.Linq;

namespace FaultLens.Sdk.Internal
{
    internal static class BreadcrumbSanitizer
    {
        public const int CategoryMaxLength = 100;
        public const int SourceMaxLength = 100;
        public const int EntityTypeMaxLength = 80;
        public const int EntityIdMaxLength = 120;
        public const int MessageMaxLength = 300;
        public const int MetadataItemLimit = 10;
        public const int MetadataValueMaxLength = 200;
        private const int MetadataDepthLimit = 4;
        private const int MetadataArrayLimit = 10;

        private static readonly HashSet<string> SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "token",
            "password",
            "passwd",
            "secret",
            "api_key",
            "apikey",
            "access_token",
            "refresh_token",
            "cookie",
            "set-cookie"
        };

        private static readonly HashSet<string> AllowedLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "request",
            "application",
            "domain",
            "data",
            "external",
            "system"
        };

        private static readonly HashSet<string> AllowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http",
            "step",
            "decision",
            "log",
            "db",
            "external",
            "error"
        };

        private static readonly HashSet<string> AllowedLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "debug",
            "info",
            "warning",
            "error"
        };

        public static IReadOnlyDictionary<string, object> Sanitize(IReadOnlyDictionary<string, object> data)
        {
            if (data == null || data.Count == 0)
                return null;

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in data.Take(MetadataItemLimit))
            {
                var key = TrimToLength(kvp.Key, CategoryMaxLength);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                result[key] = SanitizeValue(key, kvp.Value, 0);
            }

            return result.Count == 0 ? null : result;
        }

        public static string NormalizeLayer(string value, string type)
        {
            var candidate = NormalizeToken(value);
            if (candidate != null && AllowedLayers.Contains(candidate))
                return candidate;

            switch (NormalizeType(type, "log"))
            {
                case "http":
                    return "request";
                case "db":
                    return "data";
                case "external":
                    return "external";
                case "error":
                    return "system";
                default:
                    return "application";
            }
        }

        public static string NormalizeType(string value, string fallback)
        {
            var candidate = NormalizeToken(value);
            return candidate != null && AllowedTypes.Contains(candidate) ? candidate : fallback;
        }

        public static string NormalizeLevel(string value, string fallback)
        {
            var candidate = NormalizeToken(value);
            return candidate != null && AllowedLevels.Contains(candidate) ? candidate : fallback;
        }

        public static string SanitizeCategory(string value)
        {
            return TrimToLength(value, CategoryMaxLength) ?? string.Empty;
        }

        public static string SanitizeMessage(string value)
        {
            return TrimToLength(value, MessageMaxLength) ?? string.Empty;
        }

        public static string SanitizeSource(string value)
        {
            return TrimToLength(value, SourceMaxLength);
        }

        public static string SanitizeEntityType(string value)
        {
            return TrimToLength(value, EntityTypeMaxLength);
        }

        public static string SanitizeEntityId(string value)
        {
            return TrimToLength(value, EntityIdMaxLength);
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().ToLowerInvariant();
        }

        private static string TrimToLength(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private static object SanitizeValue(string key, object value, int depth)
        {
            if (SensitiveKeys.Contains(key))
                return "***redacted***";

            if (value == null)
                return null;

            if (depth >= MetadataDepthLimit)
                return TruncateString(value.ToString());

            if (value is IReadOnlyDictionary<string, object> readOnlyDictionary)
                return Sanitize(readOnlyDictionary);

            if (value is IDictionary<string, object> dictionary)
                return Sanitize(new Dictionary<string, object>(dictionary, StringComparer.OrdinalIgnoreCase));

            if (value is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                var nested = kvps.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                return Sanitize(nested);
            }

            if (value is string text)
                return TruncateString(text);

            if (value is IEnumerable<object> sequence)
                return sequence.Take(MetadataArrayLimit).Select(item => SanitizeValue(key, item, depth + 1)).ToArray();

            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = new List<object>();
                foreach (var item in enumerable)
                {
                    if (items.Count == MetadataArrayLimit)
                        break;

                    items.Add(SanitizeValue(key, item, depth + 1));
                }

                return items.ToArray();
            }

            return value is ValueType ? value : TruncateString(value.ToString());
        }

        private static string TruncateString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= MetadataValueMaxLength
                ? value
                : value.Substring(0, MetadataValueMaxLength);
        }
    }
}
