namespace WeddingShare.Helpers.Notifications
{
    public class NotificationBroker : INotificationHelper
    {
        private readonly IConfigHelper _config;
        private readonly ISmtpClientWrapper _smtp;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILoggerFactory _logger;

        public NotificationBroker(IConfigHelper config, ISmtpClientWrapper smtp, IHttpClientFactory clientFactory, ILoggerFactory logger)
        {
            _config = config;
            _smtp = smtp;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            var emailSent = true;
            var ntfySent = true;
            var gotifySent = true;

            if (_config.GetOrDefault("Notifications:Smtp:Enabled", false))
            {
                emailSent = await new EmailHelper(_config, _smtp, _logger.CreateLogger<EmailHelper>()).Send(title, message, actionLink);
            }

            if (_config.GetOrDefault("Notifications:Ntfy:Enabled", false))
            {
                ntfySent = await new NtfyHelper(_config, _clientFactory, _logger.CreateLogger<NtfyHelper>()).Send(title, message, actionLink);
            }

            if (_config.GetOrDefault("Notifications:Gotify:Enabled", false))
            {
                gotifySent = await new GotifyHelper(_config, _clientFactory, _logger.CreateLogger<GotifyHelper>()).Send(title, message, actionLink);
            }

            return emailSent && ntfySent && gotifySent;
        }
    }
}