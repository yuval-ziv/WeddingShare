namespace WeddingShare.Models
{
    public class PhotoGallery
    {
        public PhotoGallery()
            : this(3)
        {
        }

        public PhotoGallery(int columnCount)
            : this("default", columnCount, string.Empty, new List<string>())
        {
        }

        public PhotoGallery(string id, int columnCount, string path, List<string> images)
        {
            this.GalleryId = id;
            this.GalleryPath = path;
            this.ColumnCount = columnCount;
            this.Images = images;
            this.FileUploader = new FileUploader(id, "/Gallery/UploadImage");
        }

        public string? GalleryId { get; set; }
        public string? GalleryPath { get; set; }
        public int ColumnCount { get; set; }
        public List<string>? Images { get; set; }
        public FileUploader? FileUploader { get; set; }
    }
}