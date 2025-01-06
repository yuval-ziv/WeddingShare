namespace WeddingShare.Helpers.Notifications
{
    public class NtfyHelper : INotificationHelper
    {
        private readonly IConfigHelper _config;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger _logger;

        public NtfyHelper(IConfigHelper config, IHttpClientFactory clientFactory, ILogger<NtfyHelper> logger)
        {
            _config = config;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            if (string.IsNullOrWhiteSpace(_config.GetOrDefault("Notifications:Ntfy:Endpoint", string.Empty)))
            {
                _logger.LogWarning($"Invalid Ntfy endpoint specified");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.GetOrDefault("Notifications:Ntfy:Token", string.Empty)))
            {
                _logger.LogWarning($"Invalid Ntfy token specified");
                return false;
            }

            if (_config.GetOrDefault("Notifications:Ntfy:Enabled", false))
            { 
                try
                {
                    var topic = _config.GetOrDefault("Notifications:Ntfy:Topic", "WeddingShare");
                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        var priority = _config.GetOrDefault("Notifications:Ntfy:Priority", 4);
                        if (priority > 0)
                        {
                            var defaultIcon = "https://github.com/Cirx08/WeddingShare/blob/main/WeddingShare/wwwroot/images/logo.png?raw=true";
                            var icon = _config.GetOrDefault("Settings:Logo", defaultIcon);
                            icon = !icon.StartsWith('.') && !icon.StartsWith('/') ? icon : defaultIcon;

                            var client = _clientFactory.CreateClient("NtfyClient");
                            using (var response = await client.PostAsJsonAsync("/", new { icon, topic, title, message, priority, click = actionLink }))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    return true;
                                }
                                else
                                {
                                    var error = await response.Content.ReadAsStringAsync();
                                    _logger.LogError($"Failed to send Ntfy message with title '{title}' - {error}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid Ntfy priority specified");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid Ntfy topic specified");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send Ntfy message with title '{title}' - {ex?.Message}");
                }
            }

            return false;
        }
    }
}