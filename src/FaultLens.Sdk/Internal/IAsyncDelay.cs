using System;
using System.Threading;
using System.Threading.Tasks;

namespace FaultLens.Sdk.Internal
{
    /// <summary>Delay seam so retry timing is deterministically testable without wall-clock sleeps.</summary>
    internal interface IAsyncDelay
    {
        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }

    internal sealed class SystemAsyncDelay : IAsyncDelay
    {
        public static readonly SystemAsyncDelay Instance = new SystemAsyncDelay();

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            Task.Delay(delay, cancellationToken);
    }
}
