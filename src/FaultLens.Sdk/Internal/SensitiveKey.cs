using System;

namespace FaultLens.Sdk.Internal
{
    /// <summary>
    /// Single definition of a "sensitive key name" (sdk-privacy-v1, section 2.1). Same semantics as the
    /// backend <c>IsSensitiveKey</c>: after trimming, lower-casing and replacing '-' with '_', the name equals
    /// <c>pwd</c> or contains one of the fragments below.
    /// </summary>
    internal static class SensitiveKey
    {
        /// <summary>The placeholder that replaces the value stored under a sensitive key name.</summary>
        public const string Filtered = "[Filtered]";

        private static readonly string[] Fragments =
        {
            "authorization", "bearer", "cookie", "password", "passwd", "token", "secret", "apikey", "api_key",
            "session", "csrf", "xsrf", "credential", "private_key", "privatekey", "card", "cvv", "otp", "email", "jwt",
            "aadhaar", "signature"
        };

        public static bool IsSensitive(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var normalized = key.Trim().Replace('-', '_').ToLowerInvariant();
            if (normalized == "pwd")
                return true;

            foreach (var fragment in Fragments)
            {
                if (normalized.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }
    }
}
