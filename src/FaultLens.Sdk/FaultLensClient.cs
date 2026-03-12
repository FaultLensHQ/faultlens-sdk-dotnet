using FaultLens.Sdk.Builders;
using FaultLens.Sdk.Envelopes;
using FaultLens.Sdk.Internal;
using FaultLens.Sdk.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FaultLens.Sdk
{
    public sealed class FaultLensClient : IFaultLensClient
    {
        private readonly FaultLensOptions _options;
        private readonly ErrorEnvelopeBuilder _envelopeBuilder;
        private readonly IEventTransport _transport;
        private readonly SafeExecutor _executor;
        private readonly BreadcrumbScopeRegistry _scopeRegistry;

        private readonly AsyncLocal<BreadcrumbScope> _breadcrumbScope = new AsyncLocal<BreadcrumbScope>();
        private readonly AsyncLocal<string> _scopeKey = new AsyncLocal<string>();

        private int _disposed;

        public FaultLensClient(FaultLensOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _envelopeBuilder = new ErrorEnvelopeBuilder(_options, new SdkInfo());
            _transport = new HttpEventTransport(_options);
            _executor = new SafeExecutor();
            _scopeRegistry = new BreadcrumbScopeRegistry();
        }

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            if (breadcrumb == null || string.IsNullOrWhiteSpace(breadcrumb.Message) || IsDisposed())
                return;

            var scope = ResolveScopeForWrite();
            var sequence = scope.NextSequence();
            scope.Add(new BreadcrumbEntry
            {
                Timestamp = breadcrumb.Timestamp ?? DateTimeOffset.UtcNow,
                Sequence = sequence,
                Type = Normalize(breadcrumb.Type, "log"),
                Category = (breadcrumb.Category ?? string.Empty).Trim(),
                Level = Normalize(breadcrumb.Level, "info"),
                Message = breadcrumb.Message.Trim(),
                Source = string.IsNullOrWhiteSpace(breadcrumb.Source) ? null : breadcrumb.Source.Trim(),
                Data = BreadcrumbSanitizer.Sanitize(breadcrumb.Data)
            });
        }

        public void CaptureException(Exception exception, string fingerprint = null, Action<DeliveryResult> callback = null)
        {
            if (exception == null || IsDisposed())
                return;

            _executor.Execute(() =>
            {
                var envelope = _envelopeBuilder
                    .WithException(exception)
                    .WithFingerprint(fingerprint)
                    .WithBreadcrumbs(SnapshotBreadcrumbs())
                    .Build();

                _transport.Send(envelope, callback);
            });
        }

        public void CaptureMessage(string message, string fingerprint = null, Action<DeliveryResult> callback = null)
        {
            if (string.IsNullOrWhiteSpace(message) || IsDisposed())
                return;

            _executor.Execute(() =>
            {
                var envelope = _envelopeBuilder
                    .WithMessage(message)
                    .WithFingerprint(fingerprint)
                    .WithBreadcrumbs(SnapshotBreadcrumbs())
                    .Build();

                _transport.Send(envelope, callback);
            });
        }

        public void Flush(TimeSpan timeout)
        {
            if (IsDisposed())
                return;

            _transport.Flush(timeout);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            try
            {
                Flush(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }

        private bool IsDisposed()
        {
            return Volatile.Read(ref _disposed) == 1;
        }

        private BreadcrumbScope ResolveScopeForWrite()
        {
            var scope = _breadcrumbScope.Value;
            if (scope != null)
                return scope;

            var correlationKey = TryGetCorrelationKey();
            if (!string.IsNullOrWhiteSpace(correlationKey))
            {
                scope = _scopeRegistry.GetOrCreate(correlationKey, _options.BreadcrumbCapacity);
                _scopeKey.Value = correlationKey;
                _breadcrumbScope.Value = scope;
                return scope;
            }

            scope = new BreadcrumbScope(_options.BreadcrumbCapacity);
            _scopeKey.Value = null;
            _breadcrumbScope.Value = scope;
            return scope;
        }

        private BreadcrumbScope ResolveScopeForRead(out string correlationKey)
        {
            correlationKey = _scopeKey.Value;

            var scope = _breadcrumbScope.Value;
            if (scope != null)
                return scope;

            var resolvedKey = TryGetCorrelationKey();
            if (string.IsNullOrWhiteSpace(resolvedKey))
                return null;

            if (!_scopeRegistry.TryGet(resolvedKey, out scope))
                return null;

            correlationKey = resolvedKey;
            _scopeKey.Value = resolvedKey;
            _breadcrumbScope.Value = scope;
            return scope;
        }

        private IReadOnlyList<BreadcrumbInfo> SnapshotBreadcrumbs()
        {
            var scope = ResolveScopeForRead(out var correlationKey);
            if (scope == null)
                return null;

            var entries = scope.SnapshotAndClear();

            if (!string.IsNullOrWhiteSpace(correlationKey))
            {
                _scopeRegistry.Remove(correlationKey);
            }

            if (ReferenceEquals(_breadcrumbScope.Value, scope))
            {
                _breadcrumbScope.Value = null;
                _scopeKey.Value = null;
            }

            if (entries.Count == 0)
                return null;

            return entries
                .Select(x => new BreadcrumbInfo(
                    timestamp: x.Timestamp.ToString("O"),
                    sequence: x.Sequence,
                    type: x.Type,
                    category: x.Category,
                    level: x.Level,
                    message: x.Message,
                    source: x.Source,
                    data: x.Data))
                .ToList();
        }

        private static string TryGetCorrelationKey()
        {
            var activity = Activity.Current;
            if (activity == null)
                return null;

            var traceId = activity.TraceId.ToString();
            if (!string.IsNullOrWhiteSpace(traceId) && traceId != "00000000000000000000000000000000")
                return traceId;

            return string.IsNullOrWhiteSpace(activity.Id) ? null : activity.Id;
        }

        private static string Normalize(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim().ToLowerInvariant();
        }
    }
}
