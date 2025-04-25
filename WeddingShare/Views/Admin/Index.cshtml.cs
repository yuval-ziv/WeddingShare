using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Admin
{
    public class IndexModel : PageModel
    {
        public IndexModel() 
        {
        }

        public List<GalleryItemModel>? PendingRequests { get; set; }
        public List<UserModel>? Users { get; set; }
        public List<GalleryModel>? Galleries { get; set; }
        public IDictionary<string, string>? Settings { get; set; }

        public void OnGet()
        {
        }
    }
}