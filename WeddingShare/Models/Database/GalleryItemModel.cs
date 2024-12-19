using WeddingShare.Enums;

namespace WeddingShare.Models.Database
{
    public class GalleryItemModel
    {
        public GalleryItemModel()
            : this(0, 0, string.Empty, null, GalleryItemState.Pending)
        {
        }

        public GalleryItemModel(int id, int galleryId, string title, string? uploadedBy, GalleryItemState state)
        {
            Id = id;
            GalleryId = galleryId;
            Title = title;
            UploadedBy = uploadedBy;
            State = state;
        }

        public int Id { get; set; }
        public int GalleryId { get; set; }
        public string Title { get; set; }
        public string? UploadedBy { get; set; }
        public GalleryItemState State { get; set; }
    }
}