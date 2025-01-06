using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;

namespace WeddingShare.Attributes
{
    public class AllowGuestCreateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var request = filterContext.HttpContext.Request;

                var galleryId = (request.Query.ContainsKey("id") && !string.IsNullOrWhiteSpace(request.Query["id"])) ? request.Query["id"].ToString().ToLower() : "default";

                if (!string.Equals("default", galleryId, StringComparison.OrdinalIgnoreCase))
                { 
                    var user = filterContext?.HttpContext?.User;
                    if (user?.Identity == null || !user.Identity.IsAuthenticated)
                    {
                        var configHelper = filterContext.HttpContext.RequestServices.GetService<IConfigHelper>();
                        if (configHelper != null)
                        { 
                            if (configHelper.GetOrDefault("Settings:Disable_Guest_Gallery_Creation", true))
                            {
                                var databaseHelper = filterContext.HttpContext.RequestServices.GetService<IDatabaseHelper>();
                                if (databaseHelper != null)
                                { 
                                    var gallery = databaseHelper.GetGallery(galleryId).Result;
                                    if (gallery == null)
                                    { 
                                        filterContext.Result = new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.GalleryCreationNotAllowed }, false);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = filterContext.HttpContext.RequestServices.GetService<ILogger<RequiresSecretKeyAttribute>>();
                if (logger != null)
                {
                    logger.LogError(ex, $"Failed to check guest creation - {ex?.Message}");
                }
            }
        }
    }
}