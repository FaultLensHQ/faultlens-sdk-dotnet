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
            _fingerprint = fingerprint;
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

        // -------------------------
        // Final build
        // -------------------------

        public ErrorEnvelope Build()
        {
            return new ErrorEnvelope(
                eventId: Guid.NewGuid().ToString("N"),
                timestamp: DateTimeOffset.UtcNow,
                environment: _options.Environment,
                sdk: _sdk,
                fingerprint: _fingerprint,
                exception: _exception,
                message: _message
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
    }
}
