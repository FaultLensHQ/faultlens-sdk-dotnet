namespace FaultLens.Sdk.Tests.Serialization
{
    public sealed class SdkInfoSerializationTests
    {
        [Fact]
        public void SdkInfo_Should_Have_Default_Values()
        {
            var sdk = new SdkInfo();

            var json = JsonTestHelper.Serialize(sdk);
            var root = json.RootElement;

            Assert.Equal("faultlens-dotnet", root.GetProperty("name").GetString());
            Assert.Equal("1.0.0", root.GetProperty("version").GetString());
        }
    }
}
