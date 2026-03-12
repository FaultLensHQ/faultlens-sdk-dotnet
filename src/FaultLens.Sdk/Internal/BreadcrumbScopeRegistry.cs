using System.Collections.Concurrent;

namespace FaultLens.Sdk.Internal
{
    internal sealed class BreadcrumbScopeRegistry
    {
        private readonly ConcurrentDictionary<string, BreadcrumbScope> _scopes = new ConcurrentDictionary<string, BreadcrumbScope>();

        public BreadcrumbScope GetOrCreate(string key, int capacity)
        {
            return _scopes.GetOrAdd(key, _ => new BreadcrumbScope(capacity));
        }

        public bool TryGet(string key, out BreadcrumbScope scope)
        {
            return _scopes.TryGetValue(key, out scope);
        }

        public void Remove(string key)
        {
            _scopes.TryRemove(key, out _);
        }
    }
}
