namespace WeddingShare.Helpers
{
    public interface IConfigHelper
    {
        string? GetEnvironmentVariable(string key);
        string? GetConfigValue(string section, string? subsection, string key);
        string? Get(string section, string key);
        string? Get(string section, string? subsection, string key);
        string GetOrDefault(string section, string key, string defaultValue);
        string GetOrDefault(string section, string? subsection, string key, string defaultValue);
        int GetOrDefault(string section, string key, int defaultValue);
        int GetOrDefault(string section, string? subsection, string key, int defaultValue);
        long GetOrDefault(string section, string key, long defaultValue);
        long GetOrDefault(string section, string? subsection, string key, long defaultValue);
        decimal GetOrDefault(string section, string key, decimal defaultValue);
        decimal GetOrDefault(string section, string? subsection, string key, decimal defaultValue);
        double GetOrDefault(string section, string key, double defaultValue);
        double GetOrDefault(string section, string? subsection, string key, double defaultValue);
        bool GetOrDefault(string section, string key, bool defaultValue);
        bool GetOrDefault(string section, string? subsection, string key, bool defaultValue);
        DateTime? GetOrDefault(string section, string key, DateTime? defaultValue);
        DateTime? GetOrDefault(string section, string? subsection, string key, DateTime? defaultValue);
    }

    public class ConfigHelper : IConfigHelper
    {
        private readonly IEnvironmentWrapper _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public ConfigHelper(IEnvironmentWrapper environment, IConfiguration config, ILogger<ConfigHelper> logger)
        {
            _environment = environment;
            _configuration = config;
            _logger = logger;
        }

        public string? GetEnvironmentVariable(string key)
        {
            try
            {
                var value = _environment.GetEnvironmentVariable(key.Replace(":", "_").Trim('_').ToUpper());
                if (!string.IsNullOrWhiteSpace(value))
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

        public string? GetConfigValue(string section, string? subsection, string key)
        {
            try
            {
                section = !string.IsNullOrWhiteSpace(subsection) ? $"{section}:{subsection}" : section;

                var configKey = !string.IsNullOrWhiteSpace(section) ? $"{section}:{key}" : key;
                var value = _configuration.GetValue<string>(configKey);
                if (!string.IsNullOrWhiteSpace(value))
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

        public string? Get(string section, string key)
        {
            return this.Get(section, null, key);
        }

        public string? Get(string section, string? subsection, string key)
        {
            try
            {
                var value = !IsProtectedVariable($"{CombineSections(section, subsection)}_{key}") ? this.GetEnvironmentVariable($"{subsection}_{key}") : string.Empty;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                value = this.GetConfigValue(section, subsection, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to find key '{key}' in either environment variables or appsettings");
            }

            return null;
        }

        public string GetOrDefault(string section, string key, string defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public string GetOrDefault(string section, string? subsection, string key, string defaultValue)
        {
            try
            {
                var value = this.Get(section, subsection, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch { }

            return defaultValue;
        }

        public int GetOrDefault(string section, string key, int defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public int GetOrDefault(string section, string? subsection, string key, int defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(section, subsection, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToInt32(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public long GetOrDefault(string section, string key, long defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public long GetOrDefault(string section, string? subsection, string key, long defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(section, subsection, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToInt64(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public decimal GetOrDefault(string section, string key, decimal defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public decimal GetOrDefault(string section, string? subsection, string key, decimal defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(section, subsection, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDecimal(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public double GetOrDefault(string section, string key, double defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public double GetOrDefault(string section, string? subsection, string key, double defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(section, subsection, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDouble(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public bool GetOrDefault(string section, string key, bool defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public bool GetOrDefault(string section, string? subsection, string key, bool defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(section, subsection, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToBoolean(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public DateTime? GetOrDefault(string section, string key, DateTime? defaultValue)
        {
            return this.GetOrDefault(section, null, key, defaultValue);
        }

        public DateTime? GetOrDefault(string section, string? subsection, string key, DateTime? defaultValue)
        {
            try
            {
                var value = this.GetOrDefault(section, subsection, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDateTime(value);
                }
            }
            catch { }

            return defaultValue;
        }

        private string CombineSections(string section, string? subsection)
        {
            return $"{section}_{subsection}".Trim('_');
        }

        private bool IsProtectedVariable(string key)
        {
            switch (key.Replace(":", "_").Trim('_').ToUpper())
            {
                case "RELEASE_VERSION":
                    return true;
                default:
                    return false;
            }
        }
    }
}