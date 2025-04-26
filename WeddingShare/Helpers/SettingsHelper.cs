using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers
{
    public interface ISettingsHelper
    {
        Task<SettingModel?> Get(string key, string? gallery = "");
        Task<string> GetOrDefault(string key, string defaultValue, string? gallery = "");
        Task<int> GetOrDefault(string key, int defaultValue, string? gallery = "");
        Task<long> GetOrDefault(string key, long defaultValue, string? gallery = "");
        Task<decimal> GetOrDefault(string key, decimal defaultValue, string? gallery = "");
        Task<double> GetOrDefault(string key, double defaultValue, string? gallery = "");
        Task<bool> GetOrDefault(string key, bool defaultValue, string? gallery = "");
        Task<DateTime?> GetOrDefault(string key, DateTime? defaultValue, string? gallery = "");
        Task<SettingModel?> SetSetting(string key, string value, string? gallery = "");
        Task<bool> DeleteSetting(string key, string? gallery = "");
    }

    public class SettingsHelper : ISettingsHelper
    {
        private readonly IDatabaseHelper _databaseHelper;
        private readonly IConfigHelper _configHelper;
        private readonly ILogger _logger;

        public SettingsHelper(IDatabaseHelper databaseHelper, IConfigHelper configHelper, ILogger<SettingsHelper> logger)
        { 
            _databaseHelper = databaseHelper;
            _configHelper = configHelper;
            _logger = logger;
        }

        public async Task<SettingModel?> Get(string key, string? gallery = "")
        {
            if (!string.IsNullOrWhiteSpace(key))
            { 
                try
                {
                    var dbValue = await _databaseHelper.GetSetting(key, gallery);
                    if (dbValue != null)
                    {
                        return dbValue;
                    }

                    var configValue = _configHelper.Get(key);
                    if (configValue != null)
                    { 
                        return new SettingModel()
                        {
                            Id = key.ToUpper(),
                            Value = configValue
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to find key '{key}' in either database or config");
                }
            }

            return null;
        }

        public async Task<string> GetOrDefault(string key, string defaultValue, string? gallery = "")
        {
            try
            {
                var value = (await this.Get(key, gallery))?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<int> GetOrDefault(string key, int defaultValue, string? gallery = "")
        {
            try
            {
                var value = await this.GetOrDefault(key, string.Empty, gallery);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToInt32(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<long> GetOrDefault(string key, long defaultValue, string? gallery = "")
        {
            try
            {
                var value = await this.GetOrDefault(key, string.Empty, gallery);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToInt64(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<decimal> GetOrDefault(string key, decimal defaultValue, string? gallery = "")
        {
            try
            {
                var value = await this.GetOrDefault(key, string.Empty, gallery);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDecimal(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<double> GetOrDefault(string key, double defaultValue, string? gallery = "")
        {
            try
            {
                var value = await this.GetOrDefault(key, string.Empty, gallery);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDouble(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<bool> GetOrDefault(string key, bool defaultValue, string? gallery = "")
        {
            try
            {
                var value = await this.GetOrDefault(key, string.Empty, gallery);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToBoolean(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<DateTime?> GetOrDefault(string key, DateTime? defaultValue, string? gallery = "")
        {
            try
            {
                var value = await this.GetOrDefault(key, string.Empty, gallery);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDateTime(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<SettingModel?> SetSetting(string key, string value, string? gallery = "")
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                return await _databaseHelper.SetSetting(new SettingModel() 
                {
                    Id = key,
                    Value = value
                }, gallery);
            }

            return null;
        }

        public async Task<bool> DeleteSetting(string key, string? gallery = "")
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                return await _databaseHelper.DeleteSetting(new SettingModel()
                {
                    Id = key.ToUpper()
                }, gallery);
            }

            return false;
        }

        public async Task<bool> DeleteAllSettings(string? gallery = "")
        {
            return await _databaseHelper.DeleteAllSettings(gallery);
        }
    }
}