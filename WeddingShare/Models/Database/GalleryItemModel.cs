using WeddingShare.Enums;

namespace WeddingShare.Models.Database
{
    public class GalleryItemModel
    {
        public GalleryItemModel()
            : this(0, 0, string.Empty, string.Empty, null, MediaType.Unknown, GalleryItemState.Pending)
        {
        }

        public GalleryItemModel(int id, int galleryId, string galleryName, string title, string? uploadedBy, MediaType mediaType, GalleryItemState state)
        {
            Id = id;
            GalleryId = galleryId;
            GalleryName = galleryName;
            Title = title;
            UploadedBy = uploadedBy;
            MediaType = mediaType;
            State = state;
        }

        public int Id { get; set; }
        public int GalleryId { get; set; }
        public string GalleryName { get; set; }
        public string Title { get; set; }
        public string? UploadedBy { get; set; }
        public MediaType MediaType { get; set; }
        public GalleryItemState State { get; set; }
    }
}