using System;

namespace FaultLens.Sdk.Internal
{
    internal sealed class RetryPolicy
    {
        public int MaxRetries { get; }
        public TimeSpan BaseDelay { get; }

        public RetryPolicy(int maxRetries, TimeSpan baseDelay)
        {
            MaxRetries = maxRetries;
            BaseDelay = baseDelay;
        }

        public TimeSpan GetDelay(int attempt)
        {
            var jitter = new Random().Next(-100, 100);
            var delayMs = (int)(BaseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            return TimeSpan.FromMilliseconds(delayMs + jitter);
        }
    }
}
