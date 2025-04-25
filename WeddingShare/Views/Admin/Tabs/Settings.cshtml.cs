using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WeddingShare.Views.Admin.Tabs
{
    public class SettingsModel : PageModel
    {
        public SettingsModel() 
        {
        }

        public IDictionary<string, string>? Settings { get; set; }
        
        public void OnGet()
        {
        }
    }
}