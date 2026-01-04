using FaultLens.Sdk.Envelopes;

namespace FaultLens.Sdk.Tests.Serialization
{
    public sealed class ExceptionInfoSerializationTests
    {
        [Fact]
        public void ExceptionInfo_Should_Serialize_All_Fields()
        {
            var exception = new ExceptionInfo(
                type: "System.Exception",
                message: "Test error",
                stacktrace: new List<StackFrameInfo>
                {
                    new StackFrameInfo("file.cs", "Method()", 42)
                });

            var json = JsonTestHelper.Serialize(exception);
            var root = json.RootElement;

            Assert.Equal("System.Exception", root.GetProperty("type").GetString());
            Assert.Equal("Test error", root.GetProperty("message").GetString());

            var frames = root.GetProperty("stacktrace");
            Assert.Single(frames.EnumerateArray());
        }
    }
}
