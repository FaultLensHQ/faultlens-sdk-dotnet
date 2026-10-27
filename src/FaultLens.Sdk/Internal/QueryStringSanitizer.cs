using System;
using System.Collections.Generic;
using System.Text;

namespace FaultLens.Sdk.Internal
{
    /// <summary>
    /// URL rules (sdk-privacy-v1, section 2.4): userinfo is removed, query and fragment parameters with
    /// sensitive names get the value <c>[Filtered]</c>. A fragment that contains '=' is treated as a query string.
    /// </summary>
    internal static class QueryStringSanitizer
    {
        public const string UserinfoPlaceholder = "[REDACTED]";

        // Names the SDK has always filtered in query strings that the shared fragment list does not cover.
        private static readonly HashSet<string> LegacyQueryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "key", "code", "auth"
        };

        public static string SanitizeUrl(string rawUrl)
        {
            if (rawUrl == null)
                return null;

            string fragment = null;
            var beforeFragment = rawUrl;
            var hashIndex = rawUrl.IndexOf('#');
            if (hashIndex >= 0)
            {
                beforeFragment = rawUrl.Substring(0, hashIndex);
                fragment = rawUrl.Substring(hashIndex + 1);
            }

            string query = null;
            var pathPart = beforeFragment;
            var qIndex = beforeFragment.IndexOf('?');
            if (qIndex >= 0)
            {
                pathPart = beforeFragment.Substring(0, qIndex);
                query = beforeFragment.Substring(qIndex + 1);
            }

            var sb = new StringBuilder(rawUrl.Length);
            sb.Append(StripUserinfo(pathPart));
            if (query != null)
                sb.Append('?').Append(SanitizeQueryString(query));

            if (fragment != null)
            {
                sb.Append('#');
                sb.Append(fragment.IndexOf('=') >= 0 ? SanitizeQueryString(fragment) : fragment);
            }

            return sb.ToString();
        }

        public static string SanitizeQueryString(string rawQuery)
        {
            if (rawQuery == null)
                return null;

            if (string.IsNullOrWhiteSpace(rawQuery))
                return rawQuery;

            var prefix = string.Empty;
            if (rawQuery[0] == '?')
            {
                prefix = "?";
                rawQuery = rawQuery.Substring(1);
            }

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

                var name = part.Substring(0, eqIndex);
                result.Add(IsSensitiveName(name) ? name + "=" + SensitiveKey.Filtered : part);
            }

            return prefix + string.Join("&", result);
        }

        private static bool IsSensitiveName(string rawName)
        {
            var name = rawName.Trim();
            if (SensitiveKey.IsSensitive(name) || LegacyQueryNames.Contains(name))
                return true;

            if (name.IndexOf('%') >= 0 || name.IndexOf('+') >= 0)
            {
                try
                {
                    var decoded = Uri.UnescapeDataString(name.Replace('+', ' ')).Trim();
                    return SensitiveKey.IsSensitive(decoded) || LegacyQueryNames.Contains(decoded);
                }
                catch (UriFormatException)
                {
                    return false;
                }
            }

            return false;
        }

        private static string StripUserinfo(string urlWithoutQuery)
        {
            var schemeEnd = urlWithoutQuery.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd <= 0)
                return urlWithoutQuery;

            for (var i = 0; i < schemeEnd; i++)
            {
                var c = urlWithoutQuery[i];
                if (!(char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.'))
                    return urlWithoutQuery;
            }

            var authorityStart = schemeEnd + 3;
            var authorityEnd = urlWithoutQuery.IndexOf('/', authorityStart);
            if (authorityEnd < 0)
                authorityEnd = urlWithoutQuery.Length;

            var at = urlWithoutQuery.LastIndexOf('@', authorityEnd - 1, authorityEnd - authorityStart);
            if (at < authorityStart)
                return urlWithoutQuery;

            return urlWithoutQuery.Substring(0, authorityStart)
                + UserinfoPlaceholder
                + urlWithoutQuery.Substring(at);
        }
    }
}
