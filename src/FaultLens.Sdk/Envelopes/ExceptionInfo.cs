using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FaultLens.Sdk.Envelopes
{
    public sealed class ExceptionInfo
    {
        // Required
        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("message")]
        public string Message { get; }

        [JsonPropertyName("stacktrace")]
        public IReadOnlyList<StackFrameInfo> Stacktrace { get; }

        public ExceptionInfo(
            string type,
            string message,
            IReadOnlyList<StackFrameInfo> stacktrace)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Exception type is required.", nameof(type));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Exception message is required.", nameof(message));

            if (stacktrace == null)
                throw new ArgumentNullException(nameof(stacktrace));

            Type = type;
            Message = message;
            Stacktrace = stacktrace;
        }
    }
}

