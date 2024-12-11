using System.Composition;
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
        private readonly IDeviceDetector _deviceDetector;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<AdminController> _localizer;

        private readonly string UploadsDirectory;
        private readonly string ThumbnailsDirectory;

        public AdminController(IWebHostEnvironment hostingEnvironment, IConfigHelper config, IDatabaseHelper database, IDeviceDetector deviceDetector, IImageHelper imageHelper, ILogger<AdminController> logger, IStringLocalizer<AdminController> localizer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config;
            _database = database;
            _deviceDetector = deviceDetector;
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

            var deviceType = HttpContext.Session.GetString("DeviceType");
            if (string.IsNullOrWhiteSpace(deviceType))
            {
                deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                HttpContext.Session.SetString("DeviceType", deviceType ?? "Desktop");
            }

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
        public async Task<IActionResult> WipeGallery(int id)
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
                            foreach (var photo in Directory.GetFiles(galleryDir, "*.*", SearchOption.AllDirectories))
                            {
                                var thumbnail = Path.Combine(ThumbnailsDirectory, $"{Path.GetFileNameWithoutExtension(photo)}.webp");
                                if (System.IO.File.Exists(thumbnail))
                                {
                                    System.IO.File.Delete(thumbnail);
                                }
                            }

                            Directory.Delete(galleryDir, true);
                            Directory.CreateDirectory(galleryDir);
                        }

                        return Json(new { success = await _database.WipeGallery(gallery) });
                    }
                    else
                    {
                        return Json(new { success = false, message = _localizer["Failed_Wipe_Gallery"].Value });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Wipe_Gallery"].Value} - {ex?.Message}");
                }
            }

            return Json(new { success = false });
        }

        [HttpDelete]
        public async Task<IActionResult> WipeAllGalleries()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    if (Directory.Exists(UploadsDirectory))
                    {
                        foreach (var gallery in Directory.GetDirectories(UploadsDirectory, "*", SearchOption.TopDirectoryOnly))
                        {
                            Directory.Delete(gallery, true);
                        }

                        foreach (var thumbnail in Directory.GetFiles(ThumbnailsDirectory, "*.*", SearchOption.AllDirectories))
                        {
                            System.IO.File.Delete(thumbnail);
                        }

                        Directory.CreateDirectory(Path.Combine(UploadsDirectory, "default"));
                    }

                    return Json(new { success = await _database.WipeAllGalleries() });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Wipe_Galleries"].Value} - {ex?.Message}");
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

        [HttpDelete]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var photo = await _database.GetGalleryItem(id);
                    if (photo != null)
                    {
                        var gallery = await _database.GetGallery(photo.GalleryId);
                        if (gallery != null)
                        { 
                            var photoPath = Path.Combine(UploadsDirectory, gallery.Name, photo.Title);
                            if (System.IO.File.Exists(photoPath))
                            {
                                System.IO.File.Delete(photoPath);
                            }

                            return Json(new { success = await _database.DeleteGalleryItem(photo) });
                        }
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

        [HttpGet]
        public async Task<IActionResult> ExportBackup()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var tempDir = $"Temp";
                var exportDir = Path.Combine(tempDir, "Export");

                try
                {
                    if (Directory.Exists(UploadsDirectory))
                    {
                        if (!Directory.Exists(tempDir))
                        {
                            Directory.CreateDirectory(tempDir);
                        }

                        if (Directory.Exists(exportDir))
                        {
                            Directory.Delete(exportDir, true);
                        }
                            
                        Directory.CreateDirectory(exportDir);

                        var dbExport = Path.Combine(exportDir, $"WeddingShare.bak");
                        var exported = await _database.Export($"Data Source={dbExport}");
                        if (exported)
                        {
                            var uploadsZip = Path.Combine(exportDir, $"Uploads.bak");
                            ZipFile.CreateFromDirectory(UploadsDirectory, uploadsZip, CompressionLevel.Optimal, false);

                            var thumbnailsZip = Path.Combine(exportDir, $"Thumbnails.bak");
                            ZipFile.CreateFromDirectory(ThumbnailsDirectory, thumbnailsZip, CompressionLevel.Optimal, false);

                            var exportZipFile = Path.Combine(tempDir, $"WeddingShare-{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.zip");
                            if (System.IO.File.Exists(exportZipFile))
                            {
                                System.IO.File.Delete(exportZipFile);
                            }

                            ZipFile.CreateFromDirectory(exportDir, exportZipFile, CompressionLevel.Optimal, false);
                            System.IO.File.Delete(dbExport);
                            System.IO.File.Delete(uploadsZip);
                            System.IO.File.Delete(thumbnailsZip);

                            byte[] bytes = System.IO.File.ReadAllBytes(exportZipFile);
                            System.IO.File.Delete(exportZipFile);

                            return Json(new { success = true, filename = Path.GetFileName(exportZipFile), content = Convert.ToBase64String(bytes, 0, bytes.Length) });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Export"].Value} - {ex?.Message}");
                }
                finally
                {
                    Directory.Delete(exportDir, true);
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> ImportBackup()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var tempDir = $"Temp";
                var importDir = Path.Combine(tempDir, "Import");

                try
                {
                    var files = Request?.Form?.Files;
                    if (files != null && files.Count > 0)
                    {
                        foreach (IFormFile file in files)
                        {
                            var extension = Path.GetExtension(file.FileName)?.Trim('.');
                            if (string.Equals("zip", extension, StringComparison.OrdinalIgnoreCase))
                            {
                                if (!Directory.Exists(tempDir))
                                {
                                    Directory.CreateDirectory(tempDir);
                                }

                                var filePath = Path.Combine(tempDir, "Import.zip");
                                if (!string.IsNullOrWhiteSpace(filePath))
                                {
                                    using (var fs = new FileStream(filePath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(fs);
                                    }

                                    if (Directory.Exists(importDir))
                                    {
                                        Directory.Delete(importDir, true);
                                    }

                                    Directory.CreateDirectory(importDir);

                                    ZipFile.ExtractToDirectory(filePath, importDir, true);
                                    System.IO.File.Delete(filePath);

                                    var uploadsZip = Path.Combine(importDir, "Uploads.bak");
                                    ZipFile.ExtractToDirectory(uploadsZip, UploadsDirectory, true);

                                    var thumbnailsZip = Path.Combine(importDir, "Thumbnails.bak");
                                    ZipFile.ExtractToDirectory(thumbnailsZip, ThumbnailsDirectory, true);

                                    var dbImport = Path.Combine(importDir, "WeddingShare.bak");
                                    var imported = await _database.Import($"Data Source={dbImport}");

                                    return Json(new { success = imported });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Import_Failed"].Value} - {ex?.Message}");
                }
                finally
                {
                    Directory.Delete(importDir, true);
                }
            }

            return Json(new { success = false });
        }
    }
}