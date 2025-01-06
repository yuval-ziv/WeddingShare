namespace WeddingShare.Helpers.Notifications
{
    public class GotifyHelper : INotificationHelper
    {
        private readonly IConfigHelper _config;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GotifyHelper(IConfigHelper config, IHttpClientFactory clientFactory, ILogger<GotifyHelper> logger)
        {
            _config = config;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            if (string.IsNullOrWhiteSpace(_config.GetOrDefault("Notifications:Gotify:Endpoint", string.Empty)))
            {
                _logger.LogWarning($"Invalid Gotify endpoint specified");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.GetOrDefault("Notifications:Gotify:Token", string.Empty)))
            {
                _logger.LogWarning($"Invalid Gotify token specified");
                return false;
            }

            if (_config.GetOrDefault("Notifications:Gotify:Enabled", false))
            { 
                try
                {
                    var token = _config.GetOrDefault("Notifications:Gotify:Token", string.Empty);
                    if (!string.IsNullOrWhiteSpace(token))
                    { 
                        var priority = _config.GetOrDefault("Notifications:Gotify:Priority", 4);
                        if (priority > 0)
                        {
                            message = !string.IsNullOrWhiteSpace(actionLink) ? $"{message} - Visit - {actionLink}" : message;

                            var client = _clientFactory.CreateClient("GotifyClient");
                            using (var response = await client.PostAsJsonAsync($"/message?token={token}", new { title, message, priority }))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    return true;
                                }
                                else
                                {
                                    var error = await response.Content.ReadAsStringAsync();
                                    _logger.LogError($"Failed to send Gotify message with title '{title}' - {error}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid Gotify priority specified");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid Gotify token specified");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send Gotify message with title '{title}' - {ex?.Message}");
                }
            }

            return false;
        }
    }
}