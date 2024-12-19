namespace WeddingShare.Models
{
    public class FileUploader
    {
        public FileUploader(string id, string? key, string url)
        {
            this.GalleryId = id;
            this.SecretKey = key;
            this.UploadUrl = url;
        }

        public string? GalleryId { get; set; }
        public string? SecretKey { get; set; }
        public string? UploadUrl { get; set; }
    }
}