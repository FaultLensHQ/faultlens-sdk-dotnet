using System;

namespace FaultLens.Sdk.Internal
{
    internal sealed class SafeExecutor
    {
        public void Execute(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                // Swallow by design
                // SDK must never affect host application
            }
        }
    }
}
