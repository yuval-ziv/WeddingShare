using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WeddingShare.Views.Admin
{
    public class IndexModel : PageModel
    {
        public IndexModel() 
        {
        }

        public List<KeyValuePair<string, string>>? Galleries { get; set; }
        public List<string>? PendingRequests { get; set; }

        public void OnGet()
        {
        }
    }
}