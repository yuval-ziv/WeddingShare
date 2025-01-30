using WeddingShare.Enums;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers.Database
{
    public interface IDatabaseHelper
    {
        Task<List<GalleryModel>> GetAllGalleries();
        Task<GalleryModel?> GetGallery(int id);
        Task<GalleryModel?> GetGallery(string name);
        Task<GalleryModel?> AddGallery(GalleryModel model);
        Task<GalleryModel?> EditGallery(GalleryModel model);
        Task<bool> WipeGallery(GalleryModel model);
        Task<bool> WipeAllGalleries();
        Task<bool> DeleteGallery(GalleryModel model);
        Task<List<GalleryItemModel>> GetAllGalleryItems(int? galleryId, GalleryItemState state = GalleryItemState.All, MediaType type = MediaType.All, GalleryOrder order = GalleryOrder.UploadedDesc, int limit = int.MaxValue, int page = 1);
        Task<int> GetPendingGalleryItemCount(int? galleryId = null);
        Task<List<GalleryItemModel>> GetPendingGalleryItems(int? galleryId = null);
        Task<GalleryItemModel?> GetPendingGalleryItem(int id);
        Task<GalleryItemModel?> GetGalleryItem(int id);
        Task<GalleryItemModel?> GetGalleryItemByChecksum(int galleryId, string checksum);
        Task<GalleryItemModel?> AddGalleryItem(GalleryItemModel model);
        Task<GalleryItemModel?> EditGalleryItem(GalleryItemModel model);
        Task<bool> DeleteGalleryItem(GalleryItemModel model);
        Task<bool> InitAdminAccount(UserModel model);
        Task<bool> ValidateCredentials(string username, string password);
        Task<UserModel?> GetUser(int id);
        Task<UserModel?> GetUser(string name);
        Task<UserModel?> AddUser(UserModel model);
        Task<UserModel?> EditUser(UserModel model);
        Task<bool> DeleteUser(UserModel model);
        Task<bool> ChangePassword(UserModel model);
        Task<int> IncrementLockoutCount(int id);
        Task<bool> SetLockout(int id, DateTime? datetime);
        Task<bool> ResetLockoutCount(int id);
        Task<bool> SetMultiFactorToken(int id, string token);
        Task<bool> ResetMultiFactorToDefault();
        Task<bool> Import(string path);
        Task<bool> Export(string path);
    }
}