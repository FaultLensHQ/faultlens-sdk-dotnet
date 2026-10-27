using FaultLens.Sdk.Envelopes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FaultLens.Sdk.Internal
{
    /// <summary>Raised by the builder when the mandatory privacy pass fails; the event must not be sent.</summary>
    internal sealed class EnvelopePrivacyException : Exception
    {
        public const string ReasonCode = "sdk_privacy_policy_failed";

        // Deliberately carries no inner exception and no payload content.
        public EnvelopePrivacyException()
            : base("The mandatory SDK privacy pass failed; the event was not sent.")
        {
        }
    }

    /// <summary>
    /// The mandatory sdk-privacy-v1 pass over the final outgoing envelope. Returns a new envelope built only
    /// from bounded, redacted copies; the input is never mutated and never retained. Identity fields are
    /// passed through unchanged (ADR-0006); key fields (fingerprint, correlation id) are redacted only when a
    /// text rule would change them.
    /// </summary>
    internal static class EnvelopePrivacyPolicy
    {
        public const int MaxLeafLength = 8192;
        public const int MaxStackFrames = 200;
        public const int MaxFrameFieldLength = 1024;
        public const int MaxUserAgentLength = 512;
        public const int MaxBreadcrumbs = 100;
        public const int MaxDepth = 10;
        public const int MaxProperties = 100;
        public const int MaxArrayItems = 100;
        public const int MaxNodes = 10000;
        public const int MaxKeyLength = 256;
        public const string TruncatedMarker = "[Truncated]";

        public static ErrorEnvelopeV1 Apply(ErrorEnvelopeV1 envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            return new Walker().Envelope(envelope);
        }

        private sealed class Walker
        {
            private int _nodes;

            public ErrorEnvelopeV1 Envelope(ErrorEnvelopeV1 e)
            {
                return new ErrorEnvelopeV1(
                    eventId: Key(e.EventId),
                    timestamp: e.Timestamp,
                    environment: e.Environment,
                    sdk: e.Sdk,
                    release: Text(e.Release),
                    fingerprint: Key(e.Fingerprint),
                    exception: Exception(e.Exception),
                    message: Text(e.Message),
                    breadcrumbs: Breadcrumbs(e.Breadcrumbs),
                    request: Request(e.Request),
                    client: Client(e.Client),
                    userId: e.UserId,
                    serviceName: Text(e.ServiceName),
                    serviceVersion: Text(e.ServiceVersion),
                    tenantId: e.TenantId,
                    customerId: e.CustomerId,
                    accountId: e.AccountId,
                    anonymousId: e.AnonymousId,
                    correlationId: Key(e.CorrelationId),
                    tags: Tags(e.Tags));
            }

            // ---- string leaves ----

            private static string Bound(string value, int max)
            {
                if (value == null || value.Length <= max)
                    return value;

                var length = max;
                if (char.IsHighSurrogate(value[length - 1]))
                    length--;

                return value.Substring(0, length);
            }

            private string Text(string value, int max = MaxLeafLength)
            {
                if (value == null)
                    return null;

                _nodes++;
                return PrivacyRules.RedactOrFilter(Bound(value, max));
            }

            private string Key(string value)
            {
                if (value == null)
                    return null;

                var redacted = PrivacyRules.RedactOrFilter(Bound(value, MaxLeafLength));
                return redacted;
            }

            private string Url(string value)
            {
                if (value == null)
                    return null;

                _nodes++;
                return PrivacyRules.RedactOrFilter(QueryStringSanitizer.SanitizeUrl(Bound(value, MaxLeafLength)));
            }

            private string QueryString(string value)
            {
                if (value == null)
                    return null;

                _nodes++;
                return PrivacyRules.RedactOrFilter(QueryStringSanitizer.SanitizeQueryString(Bound(value, MaxLeafLength)));
            }

            // ---- envelope members ----

            private ExceptionInfo Exception(ExceptionInfo info)
            {
                if (info == null)
                    return null;

                var frames = new List<StackFrameInfo>();
                if (info.Stacktrace != null)
                {
                    foreach (var frame in info.Stacktrace)
                    {
                        if (frames.Count >= MaxStackFrames)
                            break;

                        if (frame == null)
                            continue;

                        frames.Add(new StackFrameInfo(
                            file: Url(Bound(frame.File, MaxFrameFieldLength)),
                            method: Text(frame.Method, MaxFrameFieldLength),
                            line: frame.Line));
                    }
                }

                return new ExceptionInfo(Text(info.Type), Text(info.Message), frames);
            }

            private IReadOnlyList<BreadcrumbInfo> Breadcrumbs(IReadOnlyList<BreadcrumbInfo> source)
            {
                if (source == null)
                    return null;

                // Keep the most recent breadcrumbs: they are closest to the failure.
                var skip = Math.Max(0, source.Count - MaxBreadcrumbs);
                var result = new List<BreadcrumbInfo>(Math.Min(source.Count, MaxBreadcrumbs));
                for (var i = skip; i < source.Count; i++)
                {
                    var b = source[i];
                    if (b == null)
                        continue;

                    result.Add(new BreadcrumbInfo(
                        timestamp: b.Timestamp,
                        sequence: b.Sequence,
                        layer: b.Layer,
                        type: b.Type,
                        category: Text(b.Category),
                        level: b.Level,
                        message: Text(b.Message),
                        source: Text(b.Source),
                        entityType: Text(b.EntityType),
                        entityId: Text(b.EntityId),
                        data: b.Data == null ? null : Entries(ReadEntries(b.Data), 0)));
                }

                return result;
            }

            private RequestContextInfo Request(RequestContextInfo r)
            {
                if (r == null)
                    return null;

                return new RequestContextInfo(
                    requestedUrl: Url(r.RequestedUrl),
                    method: Text(r.Method),
                    route: Text(r.Route),
                    queryString: QueryString(r.QueryString),
                    referrer: Url(r.Referrer),
                    statusCode: r.StatusCode,
                    durationMs: r.DurationMs,
                    requestId: Text(r.RequestId),
                    correlationId: Key(r.CorrelationId),
                    traceId: Text(r.TraceId));
            }

            private ClientContextInfo Client(ClientContextInfo c)
            {
                if (c == null)
                    return null;

                return new ClientContextInfo(
                    userAgent: Text(c.UserAgent, MaxUserAgentLength),
                    runtimeName: Text(c.RuntimeName),
                    runtimeVersion: Text(c.RuntimeVersion));
            }

            private IReadOnlyDictionary<string, string> Tags(IReadOnlyDictionary<string, string> tags)
            {
                if (tags == null)
                    return null;

                var entries = new List<KeyValuePair<string, object>>();
                foreach (var tag in tags)
                {
                    if (entries.Count >= MaxProperties)
                        break;

                    entries.Add(new KeyValuePair<string, object>(tag.Key, tag.Value));
                }

                var processed = Entries(entries, 0);
                var result = new Dictionary<string, string>(processed.Count, StringComparer.Ordinal);
                foreach (var item in processed)
                    result[item.Key] = item.Value as string;

                return result;
            }

            // ---- generic values (breadcrumb data, tags) ----

            private static List<KeyValuePair<string, object>> ReadEntries(IEnumerable<KeyValuePair<string, object>> source)
            {
                var list = new List<KeyValuePair<string, object>>();
                foreach (var item in source)
                {
                    if (list.Count >= MaxProperties)
                        break;

                    list.Add(item);
                }

                return list;
            }

            private Dictionary<string, object> Entries(List<KeyValuePair<string, object>> entries, int depth)
            {
                var originals = new List<string>(entries.Count);
                var finals = new List<string>(entries.Count);
                var values = new List<object>(entries.Count);
                var changed = new List<bool>(entries.Count);

                foreach (var entry in entries)
                {
                    _nodes++;
                    var key = entry.Key ?? string.Empty;
                    var bounded = Bound(key, MaxKeyLength);
                    var redacted = PrivacyRules.RedactOrFilter(bounded);
                    var timedOut = redacted == SensitiveKey.Filtered && bounded != SensitiveKey.Filtered;

                    object value;
                    if (entry.Value != null && (timedOut || SensitiveKey.IsSensitive(key) || SensitiveKey.IsSensitive(redacted)))
                        value = SensitiveKey.Filtered;
                    else
                        value = Value(entry.Value, depth + 1);

                    originals.Add(bounded);
                    finals.Add(redacted);
                    values.Add(value);
                    changed.Add(!string.Equals(bounded, redacted, StringComparison.Ordinal));
                }

                // Unchanged keys keep their names, so a renamed key can never take one of them.
                var used = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < originals.Count; i++)
                {
                    if (!changed[i])
                        used.Add(originals[i]);
                }

                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                for (var i = 0; i < originals.Count; i++)
                {
                    var finalKey = finals[i];
                    if (changed[i])
                    {
                        for (var n = 2; used.Contains(finalKey); n++)
                            finalKey = finals[i] + "#" + n;

                        used.Add(finalKey);
                    }
                    else if (result.ContainsKey(finalKey))
                    {
                        // Distinct originals that only differ beyond the key cap: keep both.
                        var n = 2;
                        while (result.ContainsKey(finalKey))
                            finalKey = finals[i] + "#" + n++;
                    }

                    result[finalKey] = values[i];
                }

                return result;
            }

            private object Value(object value, int depth)
            {
                if (value == null)
                    return null;

                if (++_nodes > MaxNodes || depth > MaxDepth)
                    return TruncatedMarker;

                var text = value as string;
                if (text != null)
                    return PrivacyRules.RedactOrFilter(Bound(text, MaxLeafLength));

                if (value is char)
                    return PrivacyRules.RedactOrFilter(value.ToString());

                if (value is bool || value is double || value is float || value is decimal
                    || value is DateTime || value is DateTimeOffset || value is TimeSpan || value is Guid
                    || value is Enum)
                {
                    return value;
                }

                if (value is long || value is ulong || value is int || value is uint
                    || value is short || value is ushort || value is byte || value is sbyte)
                {
                    var digits = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                    if (digits.Length >= 12 && digits[0] != '-')
                    {
                        var redacted = PrivacyRules.RedactNumeric(digits);
                        if (!string.Equals(redacted, digits, StringComparison.Ordinal))
                            return redacted;
                    }

                    return value;
                }

                var kvps = value as IEnumerable<KeyValuePair<string, object>>;
                if (kvps != null)
                    return Entries(ReadEntries(kvps), depth);

                var dictionary = value as IDictionary;
                if (dictionary != null)
                {
                    var list = new List<KeyValuePair<string, object>>();
                    var enumerator = dictionary.GetEnumerator();
                    while (list.Count < MaxProperties && enumerator.MoveNext())
                        list.Add(new KeyValuePair<string, object>(enumerator.Key?.ToString(), enumerator.Value));

                    return Entries(list, depth);
                }

                var sequence = value as IEnumerable;
                if (sequence != null)
                {
                    var items = new List<object>();
                    foreach (var item in sequence)
                    {
                        if (items.Count >= MaxArrayItems)
                            break;

                        items.Add(Value(item, depth + 1));
                    }

                    return items;
                }

                return PrivacyRules.RedactOrFilter(Bound(value.ToString(), MaxLeafLength));
            }
        }
    }
}
