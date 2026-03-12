using FaultLens.Sdk.Internal;

namespace FaultLens.Sdk.Tests
{
    public sealed class BreadcrumbScopeTests
    {
        [Fact]
        public void BreadcrumbScope_Should_Drop_Oldest_When_Capacity_Exceeded()
        {
            var scope = new BreadcrumbScope(capacity: 3);

            scope.Add(new BreadcrumbEntry { Sequence = 0, Timestamp = DateTimeOffset.UtcNow, Message = "one" });
            scope.Add(new BreadcrumbEntry { Sequence = 1, Timestamp = DateTimeOffset.UtcNow, Message = "two" });
            scope.Add(new BreadcrumbEntry { Sequence = 2, Timestamp = DateTimeOffset.UtcNow, Message = "three" });
            scope.Add(new BreadcrumbEntry { Sequence = 3, Timestamp = DateTimeOffset.UtcNow, Message = "four" });

            var snapshot = scope.SnapshotAndClear();

            Assert.Equal(3, snapshot.Count);
            Assert.Equal("two", snapshot[0].Message);
            Assert.Equal("three", snapshot[1].Message);
            Assert.Equal("four", snapshot[2].Message);
        }

        [Fact]
        public void BreadcrumbSanitizer_Should_Redact_Sensitive_Keys()
        {
            var data = new Dictionary<string, object>
            {
                ["token"] = "abc",
                ["statusCode"] = 200
            };

            var sanitized = BreadcrumbSanitizer.Sanitize(data);

            Assert.Equal("***redacted***", sanitized["token"]);
            Assert.Equal(200, sanitized["statusCode"]);
        }
    }
}
