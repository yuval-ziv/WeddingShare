using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Notifications;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class NtfyHelperTests
    {
        private readonly IConfigHelper _config = Substitute.For<IConfigHelper>();
        private readonly IHttpClientFactory _clientFactory = Substitute.For<IHttpClientFactory>();
        private readonly ILogger<NtfyHelper> _logger = Substitute.For<ILogger<NtfyHelper>>();

        public NtfyHelperTests()
        {
        }

        [SetUp]
        public void Setup()
        {
            var client = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK));
            client.BaseAddress = new Uri("https://unit.test.com/");

            _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            _config.GetOrDefault("Notifications:Ntfy:Enabled", Arg.Any<bool>()).Returns(true);
            _config.GetOrDefault("Notifications:Ntfy:Endpoint", Arg.Any<string>()).Returns("https://unit.test.com/");
            _config.GetOrDefault("Notifications:Ntfy:Token", Arg.Any<string>()).Returns("UnitTest");
            _config.GetOrDefault("Notifications:Ntfy:Topic", Arg.Any<string>()).Returns("UnitTest");
            _config.GetOrDefault("Notifications:Ntfy:Priority", Arg.Any<int>()).Returns(4);
        }

        [TestCase("unit", "test")]
        public async Task NtfyHelper_Success(string title, string message)
        {
            var actual = await new NtfyHelper(_config, _clientFactory, _logger).Send(title, message);
            Assert.That(actual, Is.EqualTo(true));
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public async Task NtfyHelper_Enabled(bool enabled, bool expected)
        {
            _config.GetOrDefault("Notifications:Ntfy:Enabled", Arg.Any<bool>()).Returns(enabled);

            var actual = await new NtfyHelper(_config, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("http://unittest.com", true)]
        [TestCase("https://unittest.com", true)]
        public async Task NtfyHelper_Endpoint(string? endpoint, bool expected)
        {
            var client = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK));
            client.BaseAddress = endpoint != null ? new Uri(endpoint) : null;

            _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            var actual = await new NtfyHelper(_config, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("UnitTest", true)]
        public async Task NtfyHelper_Token(string? token, bool expected)
        {
            _config.GetOrDefault("Notifications:Ntfy:Token", Arg.Any<string>()).Returns(token);

            var actual = await new NtfyHelper(_config, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("UnitTest", true)]
        public async Task NtfyHelper_Topic(string? topic, bool expected)
        {
            _config.GetOrDefault("Notifications:Ntfy:Topic", Arg.Any<string>()).Returns(topic);

            var actual = await new NtfyHelper(_config, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(-100, false)]
        [TestCase(-1, false)]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(100, true)]
        public async Task NtfyHelper_Priority(int priority, bool expected)
        {
            _config.GetOrDefault("Notifications:Ntfy:Priority", Arg.Any<int>()).Returns(priority);

            var actual = await new NtfyHelper(_config, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}