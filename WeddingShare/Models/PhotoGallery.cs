using WeddingShare.Enums;

namespace WeddingShare.Models
{
    public class PhotoGallery
    {
        public PhotoGallery()
            : this(ViewMode.Default)
        {
        }

        public PhotoGallery(ViewMode viewMode)
            : this(1, "default", string.Empty, viewMode, new List<PhotoGalleryImage>())
        {
        }

        public PhotoGallery(int id, string name, string secretKey, ViewMode viewMode, List<PhotoGalleryImage> images)
        {
            this.GalleryId = id;
            this.GalleryName = name;
            this.ViewMode = viewMode;
            this.PendingCount = 0;
            this.Images = images;
            this.FileUploader = new FileUploader(name, secretKey, "/Gallery/UploadImage");
        }

        public int? GalleryId { get; set; }
        public string? GalleryName { get; set; }
        public ViewMode ViewMode { get; set; }
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
        public int? GalleryId { get; set; }
        public string? GalleryName { get; set; }
        public string? Name { get; set; }
        public string? UploadedBy { get; set; }
        public string? ImagePath { get; set; }
        public string? ThumbnailPath { get; set; }
        public MediaType MediaType { get; set; }
    }
}