namespace WeddingShare.Helpers
{
    public interface IConfigHelper
    {
        string? GetEnvironmentVariable(string key);
        string? GetConfigValue(string key);
        string? GetOrDefault(string key, string? defaultValue);
        int GetOrDefault(string key, int defaultValue);
        long GetOrDefault(string key, long defaultValue);
        decimal GetOrDefault(string key, decimal defaultValue);
        double GetOrDefault(string key, double defaultValue);
        bool GetOrDefault(string key, bool defaultValue);
        DateTime? GetOrDefault(string key, DateTime? defaultValue);
    }

    public class ConfigHelper : IConfigHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigHelper> _logger;

        public ConfigHelper(IConfiguration config, ILogger<ConfigHelper> logger)
        { 
            _configuration = config;
            _logger = logger;
        }

        public string? GetEnvironmentVariable(string key)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(key.Replace(":", "_").ToUpper());
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get environment variable '{key}'");
            }

            return null;
        }

        public string? GetConfigValue(string key)
        {
            try
            {
                var value = _configuration.GetValue<string>(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get config value '{key}'");
            }

            return null;
        }

        public string? GetOrDefault(string key, string? defaultValue)
        {
            try
            {
                var value = this.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                value = this.GetConfigValue(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to find key '{key}' in either environment variables or appsettings");
            }

            return defaultValue;
        }

        public int GetOrDefault(string key, int defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(key, string.Empty);
                if (!string.IsNullOrEmpty(value))
                {
                    return Convert.ToInt32(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public long GetOrDefault(string key, long defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(key, string.Empty);
                if (!string.IsNullOrEmpty(value))
                {
                    return Convert.ToInt64(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public decimal GetOrDefault(string key, decimal defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(key, string.Empty);
                if (!string.IsNullOrEmpty(value))
                {
                    return Convert.ToDecimal(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public double GetOrDefault(string key, double defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(key, string.Empty);
                if (!string.IsNullOrEmpty(value))
                {
                    return Convert.ToDouble(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public bool GetOrDefault(string key, bool defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(key, string.Empty);
                if (!string.IsNullOrEmpty(value))
                {
                    return Convert.ToBoolean(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public DateTime? GetOrDefault(string key, DateTime? defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(key, string.Empty);
                if (!string.IsNullOrEmpty(value))
                {
                    return Convert.ToDateTime(value);
                }
            }
            catch { }

            return defaultValue;
        }
    }
}