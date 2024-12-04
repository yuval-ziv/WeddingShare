using WeddingShare.Helpers.Database;

namespace WeddingShare.Helpers
{
    public interface ISecretKeyHelper
    {
        Task<string?> GetGallerySecretKey(string galleryId);
    }

    public class SecretKeyHelper : ISecretKeyHelper
    {
        private readonly IConfigHelper _config;
        private readonly IDatabaseHelper  _database;

        public SecretKeyHelper(IConfigHelper config, IDatabaseHelper database)
        {
            _config = config;
            _database = database;
        }

        public async Task<string?> GetGallerySecretKey(string galleryId)
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