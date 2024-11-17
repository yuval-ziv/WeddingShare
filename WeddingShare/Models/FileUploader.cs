namespace WeddingShare.Models
{
    public class FileUploader
    {
        public FileUploader(string id, string url)
        {
            this.GalleryId = id;
            this.UploadUrl = url;
        }

        public string? GalleryId { get; set; }
        public string? UploadUrl { get; set; }
    }
}