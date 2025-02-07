using WeddingShare.Helpers.Database;

namespace WeddingShare.Helpers
{
    public interface IGalleryHelper
    {
        string GetConfig(string? galleryId, string key, string defaultValue);
        int GetConfig(string? galleryId, string key, int defaultValue);
        long GetConfig(string? galleryId, string key, long defaultValue);
        decimal GetConfig(string? galleryId, string key, decimal defaultValue);
        double GetConfig(string? galleryId, string key, double defaultValue);
        bool GetConfig(string? galleryId, string key, bool defaultValue);
        DateTime? GetConfig(string section, string key, DateTime? defaultValue);
        Task<string?> GetSecretKey(string galleryId);
    }

    public class GalleryHelper : IGalleryHelper
    {
        private readonly IConfigHelper _config;
        private readonly IDatabaseHelper _database;

        public GalleryHelper(IConfigHelper config, IDatabaseHelper database)
        {
            _config = config;
            _database = database;
        }

        public string GetConfig(string? galleryId, string key, string defaultValue = "")
        {
            string? value = null;

            try
            {
                value = _config.Get($"Settings:{key}", galleryId ?? "default");
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = _config.Get($"Settings:{key}");
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = defaultValue;
                    }
                }
            }
            catch
            {
            }

            return !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
        }

        public int GetConfig(string? galleryId, string key, int defaultValue)
        {
            try
            {
                var value = this.GetConfig(galleryId, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToInt32(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public long GetConfig(string? galleryId, string key, long defaultValue)
        {
            try
            {
                var value = this.GetConfig(galleryId, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToInt64(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public decimal GetConfig(string? galleryId, string key, decimal defaultValue)
        {
            try
            {
                var value = this.GetConfig(galleryId, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDecimal(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public double GetConfig(string? galleryId, string key, double defaultValue)
        {
            try
            {
                var value = this.GetConfig(galleryId, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDouble(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public bool GetConfig(string? galleryId, string key, bool defaultValue)
        {
            try
            {
                var value = this.GetConfig(galleryId, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToBoolean(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public DateTime? GetConfig(string? galleryId, string key, DateTime? defaultValue)
        {
            try
            {
                var value = this.GetConfig(galleryId, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Convert.ToDateTime(value);
                }
            }
            catch { }

            return defaultValue;
        }

        public async Task<string?> GetSecretKey(string galleryId)
        {
            try
            {
                var secretKey = _config.Get($"Settings:Gallery:Secret_Key", galleryId ?? "default");
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    secretKey = (await _database.GetGallery(galleryId ?? "default"))?.SecretKey;
                    if (string.IsNullOrWhiteSpace(secretKey))
                    {
                        secretKey = _config.Get("Settings:Gallery:Secret_Key");
                    }
                }

                return secretKey;
            }
            catch
            {
                return null;
            }
        }
    }
}