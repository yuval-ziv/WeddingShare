using WeddingShare.Helpers.Database;

namespace WeddingShare.Helpers
{
    public interface IGalleryHelper
    {
        string? GetConfig(string? galleryId, string key);
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

        public string? GetConfig(string? galleryId, string key)
        {
            try
            {
                var value = _config.Get("Settings", $"{key}_{galleryId ?? "default"}");
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = _config.Get("Settings", key);
                }
            
                return value;
            }
            catch 
            {
                return null;
            }
        }

        public async Task<string?> GetSecretKey(string galleryId)
        {
            try
            {
                var secretKey = _config.Get("Settings", $"Secret_Key_{galleryId}");
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    secretKey = (await _database.GetGallery(galleryId))?.SecretKey;
                    if (string.IsNullOrWhiteSpace(secretKey))
                    {
                        secretKey = _config.Get("Settings", "Secret_Key");
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