using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.Views.Admin;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfigHelper _config;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<AdminController> _localizer;

        private readonly string UploadsDirectory;

        public AdminController(IWebHostEnvironment hostingEnvironment, IConfigHelper config, ILogger<AdminController> logger, IStringLocalizer<AdminController> localizer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config;
            _logger = logger;
            _localizer = localizer;

            UploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                var passkey = _config.Get("Settings", "Admin_Password");
                if (string.IsNullOrEmpty(passkey) || string.Equals(passkey, model?.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "Admin")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await this.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Login_Failed"].Value} - {ex?.Message}");
            }

            return Json(new { success = false, message = "Invalid password" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await this.HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            { 
                return Redirect("/");
            }

            var model = new IndexModel();

            try
            {
                if (Directory.Exists(UploadsDirectory))
                {
                    model.Galleries = Directory.GetDirectories(UploadsDirectory)?.Select(x => new KeyValuePair<string, string>(Path.GetFileName(x), x))?.ToList();
                    model.PendingRequests = model.Galleries?.SelectMany(x => Directory.GetFiles(Path.Combine(x.Value, "Pending"), "*.*", SearchOption.TopDirectoryOnly))?.Select(x => x.Replace(UploadsDirectory, string.Empty))?.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Pending_Uploads_Failed"].Value} - {ex?.Message}");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult ReviewPhoto(string galleryId, string photoId, ReviewAction action)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var galleryDir = Path.Combine(UploadsDirectory, galleryId);
                    var reviewFile = Path.Combine(galleryDir, "Pending", photoId);
                    if (System.IO.File.Exists(reviewFile))
                    {
                        if (action == ReviewAction.APPROVED)
                        {
                            System.IO.File.Move(reviewFile, Path.Combine(galleryDir, Path.GetFileName(reviewFile)));
                        }
                        else if (action == ReviewAction.REJECTED)
                        {
                            System.IO.File.Delete(reviewFile);
                        }
                        else if (action == ReviewAction.UNKNOWN)
                        {
                            throw new Exception(_localizer["Unknown_Review_Action"].Value);
                        }

                        return Json(new { success = true, action });
                    }
                    else
                    {
                        return Json(new { success = false, message = _localizer["Failed_Finding_File"].Value });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Reviewing_Photo"].Value} - {ex?.Message}");
                }
            }

            return Json(new { success = false });
        }
    }
}