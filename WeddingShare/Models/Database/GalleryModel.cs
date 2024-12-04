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
    }
}