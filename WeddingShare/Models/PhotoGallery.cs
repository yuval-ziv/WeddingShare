using WeddingShare.Enums;

namespace WeddingShare.Models
{
    public class PhotoGallery
    {
        public PhotoGallery()
            : this(3, ViewMode.Default)
        {
        }

        public PhotoGallery(int columnCount, ViewMode viewMode)
            : this("default", columnCount, string.Empty, string.Empty, viewMode, new List<PhotoGalleryImage>())
        {
        }

        public PhotoGallery(string id, int columnCount, string galleryPath, string thumbnailPath, ViewMode viewMode, List<PhotoGalleryImage> images)
        {
            this.GalleryId = id;
            this.GalleryPath = galleryPath;
            this.ThumbnailsPath = thumbnailPath;
            this.ViewMode = viewMode;
            this.ColumnCount = columnCount;
            this.PendingCount = 0;
            this.Images = images;
            this.FileUploader = new FileUploader(id, "/Gallery/UploadImage");
        }

        public string? GalleryId { get; set; }
        public string? GalleryPath { get; set; }
        public string? ThumbnailsPath { get; set; }
        public ViewMode ViewMode { get; set; }
        public int ColumnCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount
        {
            get
            {
                return this.Images?.Count ?? 0;
            }
        }
        public int TotalCount
        {
            get
            {
                return this.ApprovedCount + this.PendingCount;
            }
        }
        public List<PhotoGalleryImage>? Images { get; set; }
        public FileUploader? FileUploader { get; set; }
    }

    public class PhotoGalleryImage
    {
        public PhotoGalleryImage()
        { 
        }

        public int Id { get; set; }
        public string? Path { get; set; }
        public string? Name { get; set; }
    }
}