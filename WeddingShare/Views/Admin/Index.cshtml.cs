using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Admin
{
    public class IndexModel : PageModel
    {
        public IndexModel() 
        {
        }

        public List<GalleryModel>? Galleries { get; set; }
        public List<GalleryItemModel>? PendingRequests { get; set; }

        public void OnGet()
        {
        }
    }
}