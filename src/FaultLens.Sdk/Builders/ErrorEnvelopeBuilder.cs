using FaultLens.Sdk.Envelopes;
using System;
using System.Collections.Generic;

namespace FaultLens.Sdk.Builders
{
    public sealed class ErrorEnvelopeBuilder
    {
        private readonly FaultLensOptions _options;
        private readonly SdkInfo _sdk;

        private string _fingerprint;
        private ExceptionInfo _exception;
        private string _message;
        private IReadOnlyList<BreadcrumbInfo> _breadcrumbs;
        private RequestContextInfo _requestContext;
        private ClientContextInfo _clientContext;
        private string _userId;
        private string _serviceName;
        private string _serviceVersion;
        private string _tenantId;
        private string _customerId;
        private string _accountId;
        private string _anonymousId;
        private string _correlationId;
        private bool _useConfiguredIdentity = true;
        private IReadOnlyDictionary<string, string> _tags;

        public ErrorEnvelopeBuilder(FaultLensOptions options, SdkInfo sdk)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        // -------------------------
        // Optional enrichments
        // -------------------------

        public ErrorEnvelopeBuilder WithFingerprint(string fingerprint)
        {
            _fingerprint = fingerprint ?? null;
            return this;
        }

        public ErrorEnvelopeBuilder WithMessage(string message)
        {
            _message = message;
            return this;
        }

        public ErrorEnvelopeBuilder WithException(Exception exception)
        {
            if (exception == null)
                return this;

            _exception = BuildExceptionInfo(exception);
            return this;
        }

        public ErrorEnvelopeBuilder WithBreadcrumbs(IReadOnlyList<BreadcrumbInfo> breadcrumbs)
        {
            _breadcrumbs = breadcrumbs;
            return this;
        }

        public ErrorEnvelopeBuilder WithRequestContext(RequestContextInfo context)
        {
            _requestContext = context;
            return this;
        }

        public ErrorEnvelopeBuilder WithClientContext(ClientContextInfo context)
        {
            _clientContext = context;
            return this;
        }

        public ErrorEnvelopeBuilder WithUserId(string userId)
        {
            _userId = userId;
            return this;
        }

        public ErrorEnvelopeBuilder WithContext(
            string serviceName = null,
            string serviceVersion = null,
            string tenantId = null,
            string customerId = null,
            string accountId = null,
            string anonymousId = null,
            string correlationId = null,
            bool useConfiguredIdentity = true)
        {
            _serviceName = serviceName;
            _serviceVersion = serviceVersion;
            _tenantId = tenantId;
            _customerId = customerId;
            _accountId = accountId;
            _anonymousId = anonymousId;
            _correlationId = correlationId;
            _useConfiguredIdentity = useConfiguredIdentity;
            return this;
        }

        public ErrorEnvelopeBuilder WithTags(IReadOnlyDictionary<string, string> tags)
        {
            _tags = tags;
            return this;
        }

        // -------------------------
        // Final build
        // -------------------------

        public ErrorEnvelopeV1 Build()
        {
            var tenantId = _useConfiguredIdentity
                ? FirstNonBlank(_tenantId, _options.TenantId)
                : FirstNonBlank(_tenantId);
            var customerId = _useConfiguredIdentity
                ? FirstNonBlank(_customerId, _options.CustomerId)
                : FirstNonBlank(_customerId);
            var accountId = _useConfiguredIdentity
                ? FirstNonBlank(_accountId, _options.AccountId, _tenantId, _options.TenantId)
                : FirstNonBlank(_accountId, _tenantId);
            var userId = FirstNonBlank(_userId);
            var anonymousId = _useConfiguredIdentity
                ? FirstNonBlank(_anonymousId, _options.AnonymousId)
                : FirstNonBlank(_anonymousId);

            if (FirstNonBlank(userId, tenantId, customerId, accountId) != null)
                anonymousId = null;

            return new ErrorEnvelopeV1(
                eventId: Guid.NewGuid().ToString("N"),
                timestamp: DateTimeOffset.UtcNow,
                environment: _options.Environment,
                sdk: _sdk,
                release: _options.Release,
                fingerprint: _fingerprint,
                exception: _exception,
                message: _message,
                breadcrumbs: _breadcrumbs,
                request: _requestContext,
                client: _clientContext,
                userId: userId,
                serviceName: FirstNonBlank(_serviceName, _options.ServiceName),
                serviceVersion: FirstNonBlank(_serviceVersion, _options.ServiceVersion),
                tenantId: tenantId,
                customerId: customerId,
                accountId: accountId,
                anonymousId: anonymousId,
                correlationId: FirstNonBlank(_correlationId, _options.CorrelationId),
                tags: _tags
            );
        }

        // -------------------------
        // Internal helpers
        // -------------------------

        private static ExceptionInfo BuildExceptionInfo(Exception exception)
        {
            var frames = new List<StackFrameInfo>();

            var stackTrace = exception.StackTrace;
            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                var lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    frames.Add(new StackFrameInfo(
                        file: null,
                        method: line.Trim(),
                        line: null
                    ));
                }
            }

            return new ExceptionInfo(
                type: exception.GetType().FullName,
                message: exception.Message,
                stacktrace: frames
            );
        }

        private static string FirstNonBlank(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }
    }
}
