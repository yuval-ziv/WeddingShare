using System.Net.Mail;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Notifications;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class EmailHelperTests
    {
        private readonly IConfigHelper _config = Substitute.For<IConfigHelper>();
        private readonly ISmtpClientWrapper _smtp = Substitute.For<ISmtpClientWrapper>();
        private readonly ILogger<EmailHelper> _logger = Substitute.For<ILogger<EmailHelper>>();

        public EmailHelperTests()
        {
        }

        [SetUp]
        public void Setup()
        {
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
        }

        [TestCase("unit", "test")]
        public async Task EmailHelper_Success(string title, string message)
        {
            var actual = await new EmailHelper(_config, _smtp, _logger).Send(title, message);
            Assert.That(actual, Is.EqualTo(true));
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public async Task EmailHelper_Enabled(bool enabled, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:Enabled", Arg.Any<bool>()).Returns(enabled);

            var actual = await new EmailHelper(_config, _smtp, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("blaa@blaa.com", true)]
        public async Task EmailHelper_Recipient(string recipient, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:Recipient", Arg.Any<string>()).Returns(recipient);

            var actual = await new EmailHelper(_config, _smtp, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("https://unit.test.com/", true)]
        public async Task EmailHelper_Host(string host, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:Host", Arg.Any<string>()).Returns(host);

            var actual = await new EmailHelper(_config, _smtp, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(-100, false)]
        [TestCase(-1, false)]
        [TestCase(0, false)]
        [TestCase(1, true)]
        public async Task EmailHelper_Port(int port, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:Port", Arg.Any<int>()).Returns(port);

            var actual = await new EmailHelper(_config, _smtp, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("blaa@blaa.com", true)]
        public async Task EmailHelper_From(string from, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:From", Arg.Any<string>()).Returns(from);

            var actual = await new EmailHelper(_config, _smtp, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null, true)]
        [TestCase("", true)]
        [TestCase("UnitTest", true)]
        public async Task EmailHelper_DisplayName(string displayName, bool expected)
        {
            _config.GetOrDefault("Notifications:Smtp:DisplayName", Arg.Any<string>()).Returns(displayName);

            var actual = await new EmailHelper(_config, _smtp, _logger).Send("unit", "test");
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}