using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Notifications;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class NotificationBrokerTests
    {
        private readonly IConfigHelper _config = Substitute.For<IConfigHelper>();
        private readonly IHttpClientFactory _clientFactory = Substitute.For<IHttpClientFactory>();
        private readonly ISmtpClientWrapper _smtp = Substitute.For<ISmtpClientWrapper>();
        private readonly ILoggerFactory _logger = Substitute.For<ILoggerFactory>();

        public NotificationBrokerTests()
        {
        }

        [SetUp]
        public void Setup()
        {
            var client = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK));
            client.BaseAddress = new Uri("https://unit.test.com/");

            _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            _smtp.SendMailAsync(Arg.Any<SmtpClient>(), Arg.Any<MailMessage>()).Returns(Task.FromResult(true));

            _config.GetOrDefault("Notifications:Smtp:Enabled", Arg.Any<bool>()).Returns(true);
            _config.GetOrDefault("Notifications:Smtp:Recipient", Arg.Any<string>()).Returns("unit@test.com");
            _config.GetOrDefault("Notifications:Smtp:Host", Arg.Any<string>()).Returns("https://unit.test.com/");
            _config.GetOrDefault("Notifications:Smtp:Port", Arg.Any<int>()).Returns(999);
            _config.GetOrDefault("Notifications:Smtp:Username", Arg.Any<string>()).Returns("Unit");
            _config.GetOrDefault("Notifications:Smtp:Password", Arg.Any<string>()).Returns("Test");
            _config.GetOrDefault("Notifications:Smtp:From", Arg.Any<string>()).Returns("unittest@test.com");
            _config.GetOrDefault("Notifications:Smtp:DisplayName", Arg.Any<string>()).Returns("UnitTest");
            _config.GetOrDefault("Notifications:Smtp:UseSSL", Arg.Any<bool>()).Returns(true);

            _config.GetOrDefault("Notifications:Ntfy:Enabled", Arg.Any<bool>()).Returns(true);
            _config.GetOrDefault("Notifications:Ntfy:Endpoint", Arg.Any<string>()).Returns("https://unit.test.com/");
            _config.GetOrDefault("Notifications:Ntfy:Token", Arg.Any<string>()).Returns("UnitTest");
            _config.GetOrDefault("Notifications:Ntfy:Topic", Arg.Any<string>()).Returns("UnitTest");
            _config.GetOrDefault("Notifications:Ntfy:Priority", Arg.Any<int>()).Returns(4);

            _config.GetOrDefault("Notifications:Gotify:Enabled", Arg.Any<bool>()).Returns(true);
            _config.GetOrDefault("Notifications:Gotify:Endpoint", Arg.Any<string>()).Returns("https://unit.test.com/");
            _config.GetOrDefault("Notifications:Gotify:Token", Arg.Any<string>()).Returns("UnitTest");
            _config.GetOrDefault("Notifications:Gotify:Priority", Arg.Any<int>()).Returns(4);
        }

        [TestCase(false, false, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(false, true, false, true)]
        [TestCase(false, false, true, true)]
        [TestCase(true, true, true, true)]
        public async Task NotificationBroker_Success(bool smtp, bool ntfy, bool gotify, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:Enabled", Arg.Any<bool>()).Returns(smtp);
            _config.GetOrDefault("Notifications:Ntfy:Enabled", Arg.Any<bool>()).Returns(ntfy);
            _config.GetOrDefault("Notifications:Gotify:Enabled", Arg.Any<bool>()).Returns(gotify);

            var actual = await new NotificationBroker(_config, _smtp, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase()]
        public async Task NotificationBroker_Issue_Smtp()
        {
            _config.GetOrDefault("Notifications:Smtp:Host", Arg.Any<string>()).Returns(string.Empty);

            var actual = await new NotificationBroker(_config, _smtp, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(false));
        }

        [TestCase()]
        public async Task NotificationBroker_Issue_Ntfy()
        {
            _config.GetOrDefault("Notifications:Ntfy:Endpoint", Arg.Any<string>()).Returns(string.Empty);

            var actual = await new NotificationBroker(_config, _smtp, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(false));
        }

        [TestCase()]
        public async Task NotificationBroker_Issue_Gotify()
        {
            _config.GetOrDefault("Notifications:Gotify:Endpoint", Arg.Any<string>()).Returns(string.Empty);

            var actual = await new NotificationBroker(_config, _smtp, _clientFactory, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(false));
        }
    }
}