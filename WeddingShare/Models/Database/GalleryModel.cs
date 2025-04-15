using System.Xml.Linq;

namespace WeddingShare.Models.Database
{
    public class GalleryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? SecretKey { get; set; }
        public int TotalItems { get; set; }
        public int ApprovedItems { get; set; }
        public int PendingItems { get; set; }
        public long TotalGallerySize { get; set; }  
        
        public string CalculateUsage(long maxSizeMB = long.MaxValue)
        {
            return ((double)(TotalGallerySize / (double)(maxSizeMB * 1000000L))).ToString("0.00%");
        }
    }
}