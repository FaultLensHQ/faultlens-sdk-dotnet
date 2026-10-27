using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FaultLens.Sdk.Internal
{
    /// <summary>
    /// The sdk-privacy-v1 text rules (section 2.2): the backend <c>ingest-privacy-v1</c> high-precision rule
    /// set, in the same order and with the same placeholder format. Patterns are compiled once, use bounded
    /// quantifiers where they scan runs, and run with a match timeout. Callers must bound the input first.
    /// </summary>
    internal static class PrivacyRules
    {
        public const int MinScannedLength = 6;

        private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

        private const RegexOptions Opts = RegexOptions.CultureInvariant | RegexOptions.Compiled;
        private const RegexOptions OptsI = Opts | RegexOptions.IgnoreCase;

        private sealed class Rule
        {
            public Rule(string id, Regex regex, Func<string, Match, string> evaluate)
            {
                Id = id;
                Regex = regex;
                Evaluate = evaluate;
            }

            public string Id { get; }
            public Regex Regex { get; }
            public Func<string, Match, string> Evaluate { get; }
        }

        private static string Token(string ruleId)
        {
            return "[REDACTED:" + ruleId + "]";
        }

        private static bool IsAlreadyMasked(string value)
        {
            return value.StartsWith("[REDACTED", StringComparison.Ordinal)
                || value.StartsWith("[Filtered]", StringComparison.Ordinal)
                || value.StartsWith("[Truncated]", StringComparison.Ordinal);
        }

        private static Regex Rx(string pattern, RegexOptions options)
        {
            return new Regex(pattern, options, MatchTimeout);
        }

        private static readonly Rule[] All =
        {
            new Rule("private_key",
                Rx(@"-----BEGIN [A-Z0-9 ]{0,64}PRIVATE KEY-----[\s\S]*?(?:-----END [A-Z0-9 ]{0,64}PRIVATE KEY-----|\z)", Opts),
                (_, __) => Token("private_key")),

            new Rule("jwt",
                Rx(@"eyJ(?>[A-Za-z0-9_-]{5,})\.(?>[A-Za-z0-9_-]{5,})\.[A-Za-z0-9_-]*", Opts),
                (_, __) => Token("jwt")),

            new Rule("auth_scheme",
                Rx(@"\b(Bearer|Basic|Digest|Negotiate|Token)([ \t]{1,32})([A-Za-z0-9._~+/=-]{8,})", OptsI),
                EvaluateAuthScheme),

            new Rule("kv_secret",
                Rx(@"\b([A-Za-z0-9_.-]{0,64}(?:password|passwd|pwd|secret|api[_-]?key|private[_-]?key|access[_-]?key|token|authorization|credentials?|cookie|session[_-]?id|jwt))" +
                   @"([\\""']{0,2}[ \t]{0,32}[:=][ \t]{0,32})" +
                   @"(\\?""[^""\\\r\n]*\\?""|'[^'\r\n]*'|[^\s""',;&<>\\]+)", OptsI),
                EvaluateKeyValueSecret),

            new Rule("aws_key",
                Rx(@"\b(?:AKIA|ASIA|AGPA|AIDA|AROA|ANPA|ANVA|AIPA)[A-Z0-9]{16}\b", Opts),
                (_, __) => Token("aws_key")),

            new Rule("github_token",
                Rx(@"\b(?:gh[pousr]_[A-Za-z0-9]{36,255}|github_pat_[A-Za-z0-9_]{22,255})", Opts),
                (_, __) => Token("github_token")),

            new Rule("slack_token",
                Rx(@"\b(?:xox[abprs]-[A-Za-z0-9-]{10,}|xapp-[0-9]-[A-Za-z0-9-]{10,})", Opts),
                (_, __) => Token("slack_token")),

            new Rule("provider_secret_key",
                Rx(@"\b(?:(?:sk|rk)_(?:live|test)_[A-Za-z0-9]{16,}|rzp_(?:live|test)_[A-Za-z0-9]{10,})", Opts),
                (_, __) => Token("provider_secret_key")),

            new Rule("google_api_key",
                Rx(@"\bAIza[0-9A-Za-z_-]{35}", Opts),
                (_, __) => Token("google_api_key")),

            new Rule("url_userinfo",
                Rx(@"\b([A-Za-z][A-Za-z0-9+.-]{0,31}://)([^\s/?#""'<>]{1,512})@", Opts),
                EvaluateUrlUserinfo),

            new Rule("email",
                Rx(@"[A-Za-z0-9._%+-]{1,64}@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}", Opts),
                EvaluateEmail),

            new Rule("card",
                Rx(@"\b(?:[0-9][ -]?){12,18}[0-9]\b", Opts),
                EvaluateCard),

            new Rule("aadhaar",
                Rx(@"\b[2-9][0-9]{3}[ -]?[0-9]{4}[ -]?[0-9]{4}\b", Opts),
                EvaluateAadhaar),
        };

        private static readonly Rule[] NumericRules = { All[11], All[12] };

        /// <summary>
        /// Applies all rules in order. Throws <see cref="RegexMatchTimeoutException"/> when a rule exceeds the
        /// match timeout; <see cref="RedactOrFilter"/> converts that into <c>[Filtered]</c>.
        /// </summary>
        public static string Redact(string input)
        {
            return Apply(input, All);
        }

        /// <summary>Redacts a decimal integer rendered as text (card and aadhaar only).</summary>
        public static string RedactNumeric(string digits)
        {
            return Apply(digits, NumericRules);
        }

        public static string RedactOrFilter(string input)
        {
            try
            {
                return Redact(input);
            }
            catch (RegexMatchTimeoutException)
            {
                return SensitiveKey.Filtered;
            }
        }

        private static string Apply(string input, Rule[] rules)
        {
            if (input == null || input.Length < MinScannedLength)
                return input;

            var current = input;
            foreach (var rule in rules)
                current = ApplyRule(rule, current);

            return current;
        }

        private static string ApplyRule(Rule rule, string input)
        {
            StringBuilder sb = null;
            var last = 0;
            var match = rule.Regex.Match(input);
            while (match.Success)
            {
                var replacement = rule.Evaluate(input, match);
                if (replacement != null && !string.Equals(replacement, match.Value, StringComparison.Ordinal))
                {
                    if (sb == null)
                        sb = new StringBuilder(input.Length);

                    sb.Append(input, last, match.Index - last).Append(replacement);
                    last = match.Index + match.Length;
                }

                match = match.NextMatch();
            }

            if (sb == null)
                return input;

            sb.Append(input, last, input.Length - last);
            return sb.ToString();
        }

        // ---- evaluators ----

        private static string EvaluateAuthScheme(string input, Match m)
        {
            var scheme = m.Groups[1].Value;
            var value = m.Groups[3].Value;
            if (!scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) && !LooksLikeCredential(value))
                return null;

            return scheme + m.Groups[2].Value + Token("auth_scheme");
        }

        private static bool LooksLikeCredential(string value)
        {
            var hasLower = false;
            var upperAfterFirst = false;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsDigit(c) || c == '=' || c == '+' || c == '/')
                    return true;

                if (char.IsLower(c))
                    hasLower = true;
                else if (char.IsUpper(c) && i > 0)
                    upperAfterFirst = true;
            }

            return hasLower && upperAfterFirst;
        }

        private static string EvaluateKeyValueSecret(string input, Match m)
        {
            var value = m.Groups[3].Value;
            string open = string.Empty, close = string.Empty, inner = value;
            if (value.StartsWith("\\\"", StringComparison.Ordinal) && value.Length >= 4 && value.EndsWith("\\\"", StringComparison.Ordinal))
            {
                open = close = "\\\"";
                inner = value.Substring(2, value.Length - 4);
            }
            else if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[value.Length - 1] == value[0])
            {
                open = close = value.Substring(0, 1);
                inner = value.Substring(1, value.Length - 2);
            }

            if (IsAlreadyMasked(inner) || inner.Length == 0
                || inner.Equals("null", StringComparison.OrdinalIgnoreCase)
                || inner.Equals("undefined", StringComparison.OrdinalIgnoreCase)
                || inner.Equals("true", StringComparison.OrdinalIgnoreCase)
                || inner.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return m.Groups[1].Value + m.Groups[2].Value + open + Token("kv_secret") + close;
        }

        private static string EvaluateUrlUserinfo(string input, Match m)
        {
            return IsAlreadyMasked(m.Groups[2].Value)
                ? null
                : m.Groups[1].Value + "[REDACTED:url_userinfo]@";
        }

        private static string EvaluateEmail(string input, Match m)
        {
            // "app@1.0.0.dll" is a package or file reference: require a letter in a label before the TLD.
            var domain = m.Value.Substring(m.Value.IndexOf('@') + 1);
            var labels = domain.Split('.');
            for (var i = 0; i < labels.Length - 1; i++)
            {
                foreach (var c in labels[i])
                {
                    if (char.IsLetter(c))
                        return Token("email");
                }
            }

            return null;
        }

        private static bool IsAsciiDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static string EvaluateCard(string input, Match m)
        {
            // The regex matches maximal digit groups separated by single spaces/dashes. Validate every contiguous
            // run of whole groups (widest first from each start), so a preceding number cannot hide a real card.
            var groupStarts = new List<int>();
            var groupLengths = new List<int>();
            var gStart = -1;
            for (var i = 0; i <= m.Length; i++)
            {
                var isDigit = i < m.Length && IsAsciiDigit(input[m.Index + i]);
                if (isDigit && gStart < 0)
                {
                    gStart = i;
                }
                else if (!isDigit && gStart >= 0)
                {
                    groupStarts.Add(gStart);
                    groupLengths.Add(i - gStart);
                    gStart = -1;
                }
            }

            for (var first = 0; first < groupStarts.Count; first++)
            {
                var digits = 0;
                for (var last = first; last < groupStarts.Count; last++)
                {
                    digits += groupLengths[last];
                    if (digits > 19)
                        break;

                    if (digits < 13)
                        continue;

                    var spanStart = groupStarts[first];
                    var spanEnd = groupStarts[last] + groupLengths[last];
                    var span = input.Substring(m.Index + spanStart, spanEnd - spanStart);
                    if (IsPlausibleCard(DigitsOnly(span)))
                    {
                        return input.Substring(m.Index, spanStart)
                            + Token("card")
                            + input.Substring(m.Index + spanEnd, m.Length - spanEnd);
                    }
                }
            }

            return null;
        }

        private static string DigitsOnly(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (IsAsciiDigit(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private static bool IsPlausibleCard(string digits)
        {
            return digits.Length >= 13 && digits.Length <= 19 && PlausibleIssuerPrefix(digits) && PassesLuhn(digits);
        }

        private static bool PlausibleIssuerPrefix(string d)
        {
            var p2 = (d[0] - '0') * 10 + (d[1] - '0');
            switch (d[0])
            {
                case '4':
                case '6':
                    return true;
                case '5':
                    return p2 >= 50 && p2 <= 59;
                case '3':
                    return p2 == 30 || (p2 >= 34 && p2 <= 39);
                case '2':
                    var p4 = p2 * 100 + (d[2] - '0') * 10 + (d[3] - '0');
                    return p4 >= 2221 && p4 <= 2720;
                case '8':
                    return p2 == 81 || p2 == 82;
                default:
                    return false;
            }
        }

        private static bool PassesLuhn(string digits)
        {
            var sum = 0;
            var doubleIt = false;
            for (var i = digits.Length - 1; i >= 0; i--)
            {
                var n = digits[i] - '0';
                if (doubleIt)
                {
                    n *= 2;
                    if (n > 9)
                        n -= 9;
                }

                sum += n;
                doubleIt = !doubleIt;
            }

            return sum % 10 == 0;
        }

        private static string EvaluateAadhaar(string input, Match m)
        {
            var digits = DigitsOnly(m.Value);
            return digits.Length == 12 && PassesVerhoeff(digits) ? Token("aadhaar") : null;
        }

        private static readonly int[,] VerhoeffD =
        {
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 }, { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 },
            { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 }, { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 }, { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
            { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 }, { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 }, { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 },
            { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }
        };

        private static readonly int[,] VerhoeffP =
        {
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 5, 7, 6, 2, 8, 3, 0, 9, 4 }, { 5, 8, 0, 3, 7, 9, 6, 1, 4, 2 },
            { 8, 9, 1, 6, 0, 4, 3, 5, 2, 7 }, { 9, 4, 5, 3, 1, 2, 6, 8, 7, 0 }, { 4, 2, 8, 6, 5, 7, 3, 9, 0, 1 },
            { 2, 7, 9, 3, 8, 0, 6, 4, 1, 5 }, { 7, 0, 4, 6, 9, 1, 3, 2, 5, 8 }
        };

        private static bool PassesVerhoeff(string digits)
        {
            var c = 0;
            for (var i = 0; i < digits.Length; i++)
                c = VerhoeffD[c, VerhoeffP[i % 8, digits[digits.Length - 1 - i] - '0']];

            return c == 0;
        }
    }
}
