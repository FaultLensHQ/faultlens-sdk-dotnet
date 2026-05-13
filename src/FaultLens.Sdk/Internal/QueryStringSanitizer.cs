using System;
using System.Collections.Generic;

namespace FaultLens.Sdk.Internal
{
    internal static class QueryStringSanitizer
    {
        private static readonly HashSet<string> SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "token", "access_token", "refresh_token", "id_token",
            "password", "secret", "api_key", "apikey", "key",
            "code", "session", "sessionid", "auth", "authorization"
        };

        public static string SanitizeUrl(string rawUrl)
        {
            if (rawUrl == null)
                return null;

            var qIndex = rawUrl.IndexOf('?');
            if (qIndex < 0)
                return rawUrl;

            var pathPart = rawUrl.Substring(0, qIndex);
            var rest = rawUrl.Substring(qIndex + 1);

            var hashIndex = rest.IndexOf('#');
            string fragment;
            string query;
            if (hashIndex >= 0)
            {
                query = rest.Substring(0, hashIndex);
                fragment = rest.Substring(hashIndex);
            }
            else
            {
                query = rest;
                fragment = string.Empty;
            }

            return pathPart + "?" + SanitizeQueryString(query) + fragment;
        }

        public static string SanitizeQueryString(string rawQuery)
        {
            if (rawQuery == null)
                return null;

            if (string.IsNullOrWhiteSpace(rawQuery))
                return rawQuery;

            var parts = rawQuery.Split('&');
            var result = new List<string>(parts.Length);

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                var eqIndex = part.IndexOf('=');
                if (eqIndex < 0)
                {
                    result.Add(part);
                    continue;
                }

                var name = part.Substring(0, eqIndex).Trim();
                result.Add(SensitiveKeys.Contains(name) ? name + "=[REDACTED]" : part);
            }

            return string.Join("&", result);
        }
    }
}
