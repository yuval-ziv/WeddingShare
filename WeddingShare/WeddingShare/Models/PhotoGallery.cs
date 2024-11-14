namespace WeddingShare.Models
{
    public class PhotoGallery
    {
        public PhotoGallery()
            : this(3)
        {
        }

        public PhotoGallery(int columnCount)
            : this(columnCount, string.Empty, new List<string>())
        {
        }

        public PhotoGallery(int columnCount, string path, List<string> images)
        {
            this.ColumnCount = columnCount;
            this.GalleryPath = path;
            this.Images = images;
        }

        public int ColumnCount { get; set; }
        
        public string? GalleryPath { get; set; }

        public List<string>? Images { get; set; }
    }
}