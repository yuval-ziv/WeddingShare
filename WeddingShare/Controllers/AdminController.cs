using System.IO.Compression;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models;
using WeddingShare.Models.Database;
using WeddingShare.Views.Admin;

namespace WeddingShare.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfigHelper _config;
        private readonly IDatabaseHelper _database;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<AdminController> _localizer;

        private readonly string UploadsDirectory;
        private readonly string ThumbnailsDirectory;

        public AdminController(IWebHostEnvironment hostingEnvironment, IConfigHelper config, IDatabaseHelper database, IImageHelper imageHelper, ILogger<AdminController> logger, IStringLocalizer<AdminController> localizer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config;
            _database = database;
            _imageHelper = imageHelper;
            _logger = logger;
            _localizer = localizer;

            UploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            ThumbnailsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "thumbnails");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                var passkey = _config.Get("Settings", "Admin_Password");
                if (string.IsNullOrWhiteSpace(passkey) || string.Equals(passkey, model?.Password))
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

            return Json(new { success = false });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await this.HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            { 
                return Redirect("/");
            }

            var model = new IndexModel();

            try
            {
                if (!_config.GetOrDefault("Settings", "Single_Gallery_Mode", false))
                {
                    model.Galleries = await _database.GetAllGalleries();
                    model.PendingRequests = await _database.GetPendingGalleryItems();
                }
                else
                {
                    var gallery = await _database.GetGallery("default");
                    if (gallery != null)
                    { 
                        model.Galleries = new List<GalleryModel>() { gallery };
                        model.PendingRequests = await _database.GetPendingGalleryItems(gallery.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Pending_Uploads_Failed"].Value} - {ex?.Message}");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReviewPhoto(int id, ReviewAction action)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var review = await _database.GetPendingGalleryItem(id);
                    if (review != null)
                    { 
                        var galleryDir = Path.Combine(UploadsDirectory, review.GalleryName);
                        var reviewFile = Path.Combine(galleryDir, "Pending", review.Title);
                        if (action == ReviewAction.APPROVED)
                        {
                            if (!Directory.Exists(ThumbnailsDirectory))
                            {
                                Directory.CreateDirectory(ThumbnailsDirectory);
                            }

                            await _imageHelper.GenerateThumbnail(reviewFile, Path.Combine(ThumbnailsDirectory, $"{Path.GetFileNameWithoutExtension(reviewFile)}.webp"), _config.GetOrDefault("Settings", "Thumbnail_Size", 720));

                            if (System.IO.File.Exists(reviewFile))
                            {
                                System.IO.File.Move(reviewFile, Path.Combine(galleryDir, review.Title));
                            }

                            review.State = GalleryItemState.Approved;
                            await _database.EditGalleryItem(review);
                        }
                        else if (action == ReviewAction.REJECTED)
                        {
                            if (System.IO.File.Exists(reviewFile))
                            {
                                System.IO.File.Delete(reviewFile);
                            }

                            await _database.DeleteGalleryItem(review);
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

        [HttpPost]
        public async Task<IActionResult> AddGallery(GalleryModel model)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrWhiteSpace(model?.Name))
                {
                    try
                    {
                        var check = await _database.GetGallery(model.Name);
                        if (check == null)
                        {
                            return Json(new { success = string.Equals(model?.Name, (await _database.AddGallery(model))?.Name, StringComparison.OrdinalIgnoreCase) });
                        }
                        else
                        { 
                            return Json(new { success = false, message = _localizer["Gallery_Name_Already_Exists"].Value });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{_localizer["Failed_Add_Gallery"].Value} - {ex?.Message}");
                    }
                }
                else
                { 
                    return Json(new { success = false, message = _localizer["Name_Cannot_Be_Blank"].Value });
                }
            }

            return Json(new { success = false });
        }

        [HttpPut]
        public async Task<IActionResult> EditGallery(GalleryModel model)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrWhiteSpace(model?.Name))
                {
                    try
                    {
                        var check = await _database.GetGallery(model.Name);
                        if (check == null || model.Id == check.Id)
                        {
                            var gallery = await _database.GetGallery(model.Id);
                            if (gallery != null)
                            {
                                gallery.Name = model.Name;
                                gallery.SecretKey = model.SecretKey;

                                return Json(new { success = string.Equals(model?.Name, (await _database.EditGallery(gallery))?.Name, StringComparison.OrdinalIgnoreCase) });
                            }
                            else
                            {
                                return Json(new { success = false, message = _localizer["Failed_Edit_Gallery"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { success = false, message = _localizer["Gallery_Name_Already_Exists"].Value });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{_localizer["Failed_Edit_Gallery"].Value} - {ex?.Message}");
                    }
                }
                else
                {
                    return Json(new { success = false, message = _localizer["Name_Cannot_Be_Blank"].Value });
                }
            }

            return Json(new { success = false });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGallery(int id)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var gallery = await _database.GetGallery(id);
                    if (gallery != null)
                    {
                        var galleryDir = Path.Combine(UploadsDirectory, gallery.Name);
                        if (Directory.Exists(galleryDir))
                        {
                            Directory.Delete(galleryDir, true);
                        }

                        return Json(new { success = await _database.DeleteGallery(gallery) });
                    }
                    else
                    {
                        return Json(new { success = false, message = _localizer["Failed_Delete_Gallery"].Value });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Delete_Gallery"].Value} - {ex?.Message}");
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> DownloadGallery(int id)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var gallery = await _database.GetGallery(id);
                    if (gallery != null)
                    {
                        var galleryDir = Path.Combine(UploadsDirectory, gallery.Name);
                        if (Directory.Exists(galleryDir))
                        {
                            var tempZipDir = $"Temp";
                            if (!Directory.Exists(tempZipDir))
                            { 
                                Directory.CreateDirectory(tempZipDir);
                            }

                            var tempZipFile = Path.Combine(tempZipDir, $"{gallery.Name}-{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.zip");
                            ZipFile.CreateFromDirectory(galleryDir, tempZipFile, CompressionLevel.Optimal, false);

                            byte[] bytes = System.IO.File.ReadAllBytes(tempZipFile);
                            System.IO.File.Delete(tempZipFile);

                            return Json(new { success = true, filename = Path.GetFileName(tempZipFile), content = Convert.ToBase64String(bytes, 0, bytes.Length) });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = _localizer["Failed_Download_Gallery"].Value });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Download_Gallery"].Value} - {ex?.Message}");
                }
            }

            return Json(new { success = false });
        }
    }
}