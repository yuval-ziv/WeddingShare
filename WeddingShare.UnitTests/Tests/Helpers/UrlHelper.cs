using Microsoft.AspNetCore.Http;
using WeddingShare.Helpers;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class UrlHelperTests
    {
        private readonly IConfigHelper _config;

        public UrlHelperTests()
        {
            _config = Substitute.For<IConfigHelper>();
        }

        [SetUp]
        public void Setup()
        {
            _config.GetOrDefault("Settings:Force_Https", Arg.Any<bool>()).Returns(false);
        }

        [TestCase("http", "unittest.com", null, "http://unittest.com/")]
        [TestCase("https", "unittest.org", null, "https://unittest.org/")]
        [TestCase("http", "www.unittest.com", null, "http://www.unittest.com/")]
        [TestCase("https", "mobile.unittest.org", null, "https://mobile.unittest.org/")]
        [TestCase("http", "unittest.com", "", "http://unittest.com/")]
        [TestCase("https", "unittest.org", "", "https://unittest.org/")]
        [TestCase("http", "www.unittest.com", "", "http://www.unittest.com/")]
        [TestCase("https", "mobile.unittest.org", "", "https://mobile.unittest.org/")]
        [TestCase("http", "unittest.com", "/unittest", "http://unittest.com/unittest")]
        [TestCase("https", "unittest.org", "?unit=test", "https://unittest.org/?unit=test")]
        [TestCase("http", "www.unittest.com", "?unit=test&blaa=test", "http://www.unittest.com/?unit=test&blaa=test")]
        [TestCase("https", "mobile.unittest.org", "/unittest?unit=test&blaa=test", "https://mobile.unittest.org/unittest?unit=test&blaa=test")]
        public async Task UrlHelper_Success(string scheme, string host, string? querystring, string expected)
        {
            _config.GetOrDefault("Settings:Base_Url", Arg.Any<string>()).Returns(host);

            var mockContext = MockData.MockHttpContext();
            mockContext.Request.Scheme = scheme;
            mockContext.Request.Host = new HostString(host);

            var actual = UrlHelper.Generate(mockContext, _config, querystring);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}