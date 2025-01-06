using System.Net.Http.Headers;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Notifications;

namespace WeddingShare.Configurations
{
    internal static class NotificationConfiguration
    {
        private const int CLIENT_DEFAULT_TIMEOUT = 10;

        public static void AddNotificationConfiguration(this IServiceCollection services, ConfigHelper config)
        {
            services.AddSingleton<INotificationHelper, NotificationBroker>();
            services.AddNtfyConfiguration(config);
            services.AddGotifyConfiguration(config);
        }

        public static void AddNtfyConfiguration(this IServiceCollection services, ConfigHelper config)
        {
            services.AddHttpClient("NtfyClient", (serviceProvider, httpClient) =>
            {
                var endpoint = config.GetOrDefault("Notifications:Ntfy:Endpoint", string.Empty);
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    var token = config.GetOrDefault("Notifications:Ntfy:Token", string.Empty);
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    httpClient.BaseAddress = new Uri(endpoint);
                    httpClient.Timeout = TimeSpan.FromSeconds(CLIENT_DEFAULT_TIMEOUT);
                }
            });
        }

        public static void AddGotifyConfiguration(this IServiceCollection services, ConfigHelper config)
        {
            services.AddHttpClient("GotifyClient", (serviceProvider, httpClient) =>
            {
                var endpoint = config.GetOrDefault("Notifications:Gotify:Endpoint", string.Empty);
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    httpClient.BaseAddress = new Uri(endpoint);
                    httpClient.Timeout = TimeSpan.FromSeconds(CLIENT_DEFAULT_TIMEOUT);
                }
            });
        }
    }
}