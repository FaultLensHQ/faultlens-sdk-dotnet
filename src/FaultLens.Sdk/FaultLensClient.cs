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
        private readonly AsyncLocal<RequestScopeState> _requestScope = new AsyncLocal<RequestScopeState>();

        private int _disposed;

        public FaultLensClient(FaultLensOptions options)
            : this(options, null)
        {
        }

        internal FaultLensClient(FaultLensOptions options, IEventTransport transport)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _envelopeBuilder = new ErrorEnvelopeBuilder(_options, new SdkInfo());
            _transport = transport ?? new HttpEventTransport(_options);
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
                Layer = BreadcrumbSanitizer.NormalizeLayer(breadcrumb.Layer, breadcrumb.Type),
                Type = BreadcrumbSanitizer.NormalizeType(breadcrumb.Type, "log"),
                Category = BreadcrumbSanitizer.SanitizeCategory(breadcrumb.Category),
                Level = BreadcrumbSanitizer.NormalizeLevel(breadcrumb.Level, "info"),
                Message = BreadcrumbSanitizer.SanitizeMessage(breadcrumb.Message),
                Source = BreadcrumbSanitizer.SanitizeSource(breadcrumb.Source),
                EntityType = BreadcrumbSanitizer.SanitizeEntityType(breadcrumb.EntityType),
                EntityId = BreadcrumbSanitizer.SanitizeEntityId(breadcrumb.EntityId),
                Data = BreadcrumbSanitizer.Sanitize(breadcrumb.Data)
            });
        }

        public void AddStep(
            string category,
            string message,
            BreadcrumbLayer layer = BreadcrumbLayer.Application,
            BreadcrumbLevel level = BreadcrumbLevel.Info,
            string source = null,
            string entityType = null,
            string entityId = null,
            IReadOnlyDictionary<string, object> data = null)
        {
            AddBreadcrumb(new Breadcrumb
            {
                Layer = ToLayer(layer),
                Type = "step",
                Category = category,
                Level = ToLevel(level),
                Message = message,
                Source = source,
                EntityType = entityType,
                EntityId = entityId,
                Data = data
            });
        }

        public void AddDecision(
            string category,
            string message,
            BreadcrumbLayer layer = BreadcrumbLayer.Application,
            BreadcrumbLevel level = BreadcrumbLevel.Info,
            string source = null,
            string entityType = null,
            string entityId = null,
            IReadOnlyDictionary<string, object> data = null)
        {
            AddBreadcrumb(new Breadcrumb
            {
                Layer = ToLayer(layer),
                Type = "decision",
                Category = category,
                Level = ToLevel(level),
                Message = message,
                Source = source,
                EntityType = entityType,
                EntityId = entityId,
                Data = data
            });
        }

        public IFaultLensRequestScope BeginRequest(
            string method,
            string route,
            string source = null,
            IReadOnlyDictionary<string, object> data = null)
        {
            if (IsDisposed())
                return NoopRequestScope.Instance;

            var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();
            var normalizedRoute = BreadcrumbSanitizer.SanitizeMessage(route);
            if (string.IsNullOrWhiteSpace(normalizedRoute))
                normalizedRoute = "/";

            var state = new RequestScopeState(normalizedMethod, normalizedRoute, BreadcrumbSanitizer.SanitizeSource(source));
            _requestScope.Value = state;

            AddBreadcrumb(new Breadcrumb
            {
                Layer = "request",
                Type = "http",
                Category = "request.started",
                Level = "info",
                Message = normalizedMethod + " " + normalizedRoute,
                Source = state.Source,
                Data = MergeRequestData(
                    data,
                    new Dictionary<string, object>
                    {
                        ["method"] = normalizedMethod,
                        ["route"] = normalizedRoute,
                        ["traceId"] = TryGetCorrelationKey()
                    })
            });

            return new ActiveRequestScope(this, state);
        }

        public void CaptureException(Exception exception, string fingerprint = null, Action<DeliveryResult> callback = null)
        {
            if (exception == null || IsDisposed())
                return;

            _executor.Execute(() =>
            {
                EnsureRequestFailedBreadcrumb(exception);

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
                    layer: x.Layer,
                    type: x.Type,
                    category: x.Category,
                    level: x.Level,
                    message: x.Message,
                    source: x.Source,
                    entityType: x.EntityType,
                    entityId: x.EntityId,
                    data: x.Data))
                .ToList();
        }

        private void CompleteRequest(RequestScopeState state, int? statusCode, IReadOnlyDictionary<string, object> data)
        {
            if (state == null || state.IsCompleted || state.IsFailed)
                return;

            state.IsCompleted = true;
            AddBreadcrumb(new Breadcrumb
            {
                Layer = "request",
                Type = "http",
                Category = "request.completed",
                Level = statusCode.HasValue && statusCode.Value >= 500 ? "warning" : "info",
                Message = state.Method + " " + state.Route + " completed",
                Source = state.Source,
                Data = MergeRequestData(
                    data,
                    new Dictionary<string, object>
                    {
                        ["method"] = state.Method,
                        ["route"] = state.Route,
                        ["statusCode"] = statusCode,
                        ["durationMs"] = state.Stopwatch.ElapsedMilliseconds,
                        ["traceId"] = TryGetCorrelationKey()
                    })
            });

            if (ReferenceEquals(_requestScope.Value, state))
                _requestScope.Value = null;
        }

        private void FailRequest(RequestScopeState state, int? statusCode, IReadOnlyDictionary<string, object> data, Exception exception)
        {
            if (state == null || state.IsFailed)
                return;

            state.IsFailed = true;
            AddBreadcrumb(new Breadcrumb
            {
                Layer = "system",
                Type = "error",
                Category = "request.failed",
                Level = "error",
                Message = state.Method + " " + state.Route + " failed",
                Source = state.Source,
                Data = MergeRequestData(
                    data,
                    new Dictionary<string, object>
                    {
                        ["method"] = state.Method,
                        ["route"] = state.Route,
                        ["statusCode"] = statusCode,
                        ["durationMs"] = state.Stopwatch.ElapsedMilliseconds,
                        ["traceId"] = TryGetCorrelationKey(),
                        ["exceptionType"] = exception == null ? null : exception.GetType().FullName
                    })
            });

            if (ReferenceEquals(_requestScope.Value, state))
                _requestScope.Value = null;
        }

        private void EnsureRequestFailedBreadcrumb(Exception exception)
        {
            var state = _requestScope.Value;
            if (state == null || state.IsFailed)
                return;

            FailRequest(state, null, null, exception);
        }

        private static IReadOnlyDictionary<string, object> MergeRequestData(
            IReadOnlyDictionary<string, object> original,
            IReadOnlyDictionary<string, object> additions)
        {
            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (original != null)
            {
                foreach (var item in original)
                {
                    merged[item.Key] = item.Value;
                }
            }

            if (additions != null)
            {
                foreach (var item in additions)
                {
                    if (item.Value != null)
                    {
                        merged[item.Key] = item.Value;
                    }
                }
            }

            return merged;
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

        private static string ToLayer(BreadcrumbLayer layer)
        {
            switch (layer)
            {
                case BreadcrumbLayer.Request:
                    return "request";
                case BreadcrumbLayer.Domain:
                    return "domain";
                case BreadcrumbLayer.Data:
                    return "data";
                case BreadcrumbLayer.External:
                    return "external";
                case BreadcrumbLayer.System:
                    return "system";
                default:
                    return "application";
            }
        }

        private static string ToLevel(BreadcrumbLevel level)
        {
            switch (level)
            {
                case BreadcrumbLevel.Debug:
                    return "debug";
                case BreadcrumbLevel.Warning:
                    return "warning";
                case BreadcrumbLevel.Error:
                    return "error";
                default:
                    return "info";
            }
        }

        private sealed class ActiveRequestScope : IFaultLensRequestScope
        {
            private readonly FaultLensClient _client;
            private readonly RequestScopeState _state;
            private int _disposed;

            public ActiveRequestScope(FaultLensClient client, RequestScopeState state)
            {
                _client = client;
                _state = state;
            }

            public void Complete(int? statusCode = null, IReadOnlyDictionary<string, object> data = null)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                    return;

                _client.CompleteRequest(_state, statusCode, data);
            }

            public void Fail(int? statusCode = null, IReadOnlyDictionary<string, object> data = null)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                    return;

                _client.FailRequest(_state, statusCode, data, null);
            }

            public void Dispose()
            {
                Complete();
            }
        }

        private sealed class NoopRequestScope : IFaultLensRequestScope
        {
            public static readonly NoopRequestScope Instance = new NoopRequestScope();

            public void Complete(int? statusCode = null, IReadOnlyDictionary<string, object> data = null)
            {
            }

            public void Fail(int? statusCode = null, IReadOnlyDictionary<string, object> data = null)
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class RequestScopeState
        {
            public RequestScopeState(string method, string route, string source)
            {
                Method = method;
                Route = route;
                Source = source;
                Stopwatch = Stopwatch.StartNew();
            }

            public string Method { get; }

            public string Route { get; }

            public string Source { get; }

            public Stopwatch Stopwatch { get; }

            public bool IsCompleted { get; set; }

            public bool IsFailed { get; set; }
        }
    }
}
