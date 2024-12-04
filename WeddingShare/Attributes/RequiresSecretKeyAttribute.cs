using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WeddingShare.Helpers;

namespace WeddingShare.Attributes
{
    public class RequiresSecretKeyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var request = filterContext.HttpContext.Request;

                var secretKeyHelper = filterContext.HttpContext.RequestServices.GetService<ISecretKeyHelper>();
                if (secretKeyHelper != null)
                {
                    var galleryId = (request.Query.ContainsKey("id") && !string.IsNullOrWhiteSpace(request.Query["id"])) ? request.Query["id"].ToString().ToLower() : "default";
                    var secretKey = secretKeyHelper.GetGallerySecretKey(galleryId).Result;

                    var key = request.Query.ContainsKey("key") ? request.Query["key"].ToString() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(secretKey) && !string.Equals(secretKey, key))
                    {
                        var logger = filterContext.HttpContext.RequestServices.GetService<ILogger<RequiresSecretKeyAttribute>>();
                        if (logger != null)
                        {
                            logger.LogWarning($"A request was made to an endpoint with an invalid secure key");
                        }

                        filterContext.Result = new RedirectToActionResult("Index", "Error", new { Reason = ErrorCode.InvalidSecretKey }, false);
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = filterContext.HttpContext.RequestServices.GetService<ILogger<RequiresSecretKeyAttribute>>();
                if (logger != null)
                {
                    logger.LogError(ex, $"Failed to validate secure key - {ex?.Message}");
                }
            }
        }
    }
}