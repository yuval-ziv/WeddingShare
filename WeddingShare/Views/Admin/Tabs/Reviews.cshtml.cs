using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Admin.Tabs
{
    public class ReviewsModel : PageModel
    {
        public ReviewsModel() 
        {
        }

        public List<GalleryItemModel>? PendingRequests { get; set; }

        public void OnGet()
        {
        }
    }
}