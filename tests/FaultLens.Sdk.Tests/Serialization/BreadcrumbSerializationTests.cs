using FaultLens.Sdk.Envelopes;

namespace FaultLens.Sdk.Tests.Serialization
{
    public sealed class BreadcrumbSerializationTests
    {
        [Fact]
        public void Breadcrumbs_Should_Serialize_Correctly()
        {
            var crumb = new BreadcrumbInfo(
                timestamp: "2026-01-01T12:00:00.0000000Z",
                sequence: 2,
                type: "http",
                category: "request",
                level: "info",
                message: "GET /api/projects",
                source: "HttpClient",
                data: new Dictionary<string, object> { ["statusCode"] = 200 });

            var json = JsonTestHelper.Serialize(crumb);
            var root = json.RootElement;

            Assert.Equal("http", root.GetProperty("type").GetString());
            Assert.Equal("request", root.GetProperty("category").GetString());
            Assert.Equal("info", root.GetProperty("level").GetString());
            Assert.Equal("GET /api/projects", root.GetProperty("message").GetString());
            Assert.Equal(2, root.GetProperty("sequence").GetInt32());
        }
    }
}
