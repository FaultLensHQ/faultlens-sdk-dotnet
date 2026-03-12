using FaultLens.Sdk.Internal;

namespace FaultLens.Sdk.Tests;

public sealed class BreadcrumbScopeRegistryTests
{
    [Fact]
    public void GetOrCreate_Should_ReturnSameScope_ForSameKey()
    {
        var registry = new BreadcrumbScopeRegistry();

        var first = registry.GetOrCreate("trace-1", 50);
        var second = registry.GetOrCreate("trace-1", 50);

        Assert.Same(first, second);
    }

    [Fact]
    public void Remove_Should_EvictScope()
    {
        var registry = new BreadcrumbScopeRegistry();

        var first = registry.GetOrCreate("trace-1", 50);
        registry.Remove("trace-1");
        var second = registry.GetOrCreate("trace-1", 50);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_WhenKeyMissing()
    {
        var registry = new BreadcrumbScopeRegistry();

        var found = registry.TryGet("missing", out _);

        Assert.False(found);
    }
}
