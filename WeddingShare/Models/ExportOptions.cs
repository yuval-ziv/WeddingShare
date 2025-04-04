namespace WeddingShare.Models
{
    public class ExportOptions
    {
        public bool Database { get; set; } = true;
        public bool Uploads { get; set; } = true;
        public bool Thumbnails { get; set; } = true;
        public bool Logos { get; set; } = true;
        public bool Banners { get; set; } = true;
        public bool CustomResources { get; set; } = true;
    }
}