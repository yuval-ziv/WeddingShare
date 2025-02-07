using System.IO.Compression;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TwoFactorAuthNet;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Notifications;
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
        private readonly IFileHelper _fileHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly Helpers.IUrlHelper _url;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<Lang.Translations> _localizer;

        private readonly string TempDirectory;
        private readonly string UploadsDirectory;
        private readonly string ThumbnailsDirectory;

        public AdminController(IWebHostEnvironment hostingEnvironment, IConfigHelper config, IDatabaseHelper database, IDeviceDetector deviceDetector, IFileHelper fileHelper, INotificationHelper notificationHelper, Helpers.IUrlHelper url, ILogger<AdminController> logger, IStringLocalizer<Lang.Translations> localizer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config;
            _database = database;
            _deviceDetector = deviceDetector;
            _fileHelper = fileHelper;
            _notificationHelper = notificationHelper;
            _url = url;
            _logger = logger;
            _localizer = localizer;

            TempDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            UploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            ThumbnailsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "thumbnails");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Admin");
            }

            return View();
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                var user = await _database.GetUser(model.Username);
                if (user != null && !user.IsLockedOut)
                {
                    if (await _database.ValidateCredentials(user.Username, model.Password))
                    {
                        if (user.FailedLogins > 0)
                        {
                            await _database.ResetLockoutCount(user.Id);
                        }

                        var mfaSet = !string.IsNullOrEmpty(user.MultiFactorToken);
                        HttpContext.Session.SetString(SessionKey.MultiFactorTokenSet, mfaSet.ToString().ToLower());

                        if (mfaSet)
                        {
                            return Json(new { success = true, mfa = true });
                        }
                        else
                        {
                            return Json(new { success = await this.SetUserClaims(this.HttpContext, user), mfa = false });
                        }
                    }
                    else
                    {
                        await this.FailedLoginDetected(model, user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Login_Failed"].Value} - {ex?.Message}");
            }

            return Json(new { success = false });
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ValidateMultifactorAuth(LoginModel model)
        {
            if (!string.IsNullOrWhiteSpace(model?.Code))
            { 
                try
                {
                    var user = await _database.GetUser(model.Username);
                    if (user != null && !user.IsLockedOut)
                    {
                        if (await _database.ValidateCredentials(user.Username, model.Password))
                        {
                            if (user.FailedLogins > 0)
                            {
                                await _database.ResetLockoutCount(user.Id);
                            }

                            var mfaSet = !string.IsNullOrWhiteSpace(user.MultiFactorToken);
                            HttpContext.Session.SetString(SessionKey.MultiFactorTokenSet, (!string.IsNullOrEmpty(user.MultiFactorToken)).ToString().ToLower());

                            if (mfaSet)
                            {
                                var tfa = new TwoFactorAuth(_config.GetOrDefault("Settings:Title", "WeddingShare"));
                                if (tfa.VerifyCode(user.MultiFactorToken, model.Code))
                                {
                                    return Json(new { success = await this.SetUserClaims(this.HttpContext, user) });
                                }
                            }
                            else
                            {
                                return Json(new { success = await this.SetUserClaims(this.HttpContext, user) });
                            }
                        }
                        else
                        {
                            await this.FailedLoginDetected(model, user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Login_Failed"].Value} - {ex?.Message}");
                }
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

            var deviceType = HttpContext.Session.GetString(SessionKey.DeviceType);
            if (string.IsNullOrWhiteSpace(deviceType))
            {
                deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                HttpContext.Session.SetString(SessionKey.DeviceType, deviceType ?? "Desktop");
            }

            try
            {
                var user = await _database.GetUser(int.Parse(((ClaimsIdentity)User.Identity).Claims.FirstOrDefault(x => string.Equals(ClaimTypes.Sid, x.Type, StringComparison.OrdinalIgnoreCase))?.Value ?? "-1"));
                if (user != null)
                { 
                    if (!_config.GetOrDefault("Settings:Single_Gallery_Mode", false))
                    {
                        model.Galleries = await _database.GetAllGalleries();
                        if (model.Galleries != null)
                        { 
                            var all = await _database.GetGallery(0);
                            if (all != null)
                            { 
                                model.Galleries.Add(all);
                            }
                        }

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
                            _fileHelper.MoveFileIfExists(reviewFile, Path.Combine(galleryDir, review.Title));

                            review.State = GalleryItemState.Approved;
                            await _database.EditGalleryItem(review);
                        }
                        else if (action == ReviewAction.REJECTED)
                        {
                            var retain = _config.GetOrDefault("Settings:Gallery:Retain_Rejected_Items", false);
                            if (retain)
                            {
                                var rejectedDir = Path.Combine(galleryDir, "Rejected");
                                _fileHelper.CreateDirectoryIfNotExists(rejectedDir);
                                _fileHelper.MoveFileIfExists(reviewFile, Path.Combine(rejectedDir, review.Title));
                            }
                            else
                            {
                                _fileHelper.DeleteFileIfExists(reviewFile);
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
                    _logger.LogError(ex, $"{_localizer["Failed_Reviewing_Media"].Value} - {ex?.Message}");
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> BulkReview(ReviewAction action)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var items = await _database.GetPendingGalleryItems();
                    if (items != null && items.Any())
                    {
                        foreach (var review in items)
                        { 
                            var galleryDir = Path.Combine(UploadsDirectory, review.GalleryName);
                            var reviewFile = Path.Combine(galleryDir, "Pending", review.Title);
                            if (action == ReviewAction.APPROVED)
                            {
                                _fileHelper.MoveFileIfExists(reviewFile, Path.Combine(galleryDir, review.Title));

                                review.State = GalleryItemState.Approved;
                                await _database.EditGalleryItem(review);
                            }
                            else if (action == ReviewAction.REJECTED)
                            {
                                var retain = _config.GetOrDefault("Settings:Gallery:Retain_Rejected_Items", false);
                                if (retain)
                                {
                                    var rejectedDir = Path.Combine(galleryDir, "Rejected");
                                    _fileHelper.CreateDirectoryIfNotExists(rejectedDir);
                                    _fileHelper.MoveFileIfExists(reviewFile, Path.Combine(rejectedDir, review.Title));
                                }
                                else
                                {
                                    _fileHelper.DeleteFileIfExists(reviewFile);
                                }

                                await _database.DeleteGalleryItem(review);
                            }
                            else if (action == ReviewAction.UNKNOWN)
                            {
                                throw new Exception(_localizer["Unknown_Review_Action"].Value);
                            }
                        }
                    }
                     
                    return Json(new { success = true, action });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Reviewing_Media"].Value} - {ex?.Message}");
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
                        if (_fileHelper.DirectoryExists(galleryDir))
                        {
                            foreach (var photo in _fileHelper.GetFiles(galleryDir, "*.*", SearchOption.AllDirectories))
                            {
                                var thumbnail = Path.Combine(ThumbnailsDirectory, $"{Path.GetFileNameWithoutExtension(photo)}.webp");
                                _fileHelper.DeleteFileIfExists(thumbnail);
                            }

                            _fileHelper.DeleteDirectoryIfExists(galleryDir);
                            _fileHelper.CreateDirectoryIfNotExists(galleryDir);

                            if (_config.GetOrDefault("Notifications:Alerts:Destructive_Action", true))
                            { 
                                await _notificationHelper.Send("Destructive Action Performed", $"The destructive action 'Wipe' was performed on gallery '{gallery.Name}'.", _url.GenerateBaseUrl(HttpContext?.Request, "/Admin"));
                            }
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
                    if (_fileHelper.DirectoryExists(UploadsDirectory))
                    {
                        foreach (var gallery in _fileHelper.GetDirectories(UploadsDirectory, "*", SearchOption.TopDirectoryOnly))
                        {
                            _fileHelper.DeleteDirectoryIfExists(gallery);
                        }

                        foreach (var thumbnail in _fileHelper.GetFiles(ThumbnailsDirectory, "*.*", SearchOption.AllDirectories))
                        {
                            _fileHelper.DeleteFileIfExists(thumbnail);
                        }

                        _fileHelper.CreateDirectoryIfNotExists(Path.Combine(UploadsDirectory, "default"));

                        if (_config.GetOrDefault("Notifications:Alerts:Destructive_Action", true))
                        {
                            await _notificationHelper.Send("Destructive Action Performed", $"The destructive action 'Wipe' was performed on all galleries'.", _url.GenerateBaseUrl(HttpContext?.Request, "/Admin"));
                        }
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
                        _fileHelper.DeleteDirectoryIfExists(galleryDir);

                        if (_config.GetOrDefault("Notifications:Alerts:Destructive_Action", true))
                        {
                            await _notificationHelper.Send("Destructive Action Performed", $"The destructive action 'Delete' was performed on gallery '{gallery.Name}'.", _url.GenerateBaseUrl(HttpContext?.Request, "/Admin"));
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
                            _fileHelper.DeleteFileIfExists(photoPath);

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

        [HttpGet]
        public async Task<IActionResult> ExportBackup()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var exportDir = Path.Combine(TempDirectory, "Export");

                try
                {
                    if (_fileHelper.DirectoryExists(UploadsDirectory))
                    {
                        _fileHelper.CreateDirectoryIfNotExists(TempDirectory);
                        _fileHelper.DeleteDirectoryIfExists(exportDir);
                        _fileHelper.CreateDirectoryIfNotExists(exportDir);

                        var dbExport = Path.Combine(exportDir, $"WeddingShare.bak");
                        var exported = await _database.Export($"Data Source={dbExport}");
                        if (exported)
                        {
                            var uploadsZip = Path.Combine(exportDir, $"Uploads.bak");
                            ZipFile.CreateFromDirectory(UploadsDirectory, uploadsZip, CompressionLevel.Optimal, false);

                            var thumbnailsZip = Path.Combine(exportDir, $"Thumbnails.bak");
                            ZipFile.CreateFromDirectory(ThumbnailsDirectory, thumbnailsZip, CompressionLevel.Optimal, false);

                            var exportZipFile = Path.Combine(TempDirectory, $"WeddingShare-{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.zip");
                            _fileHelper.DeleteFileIfExists(exportZipFile);

                            ZipFile.CreateFromDirectory(exportDir, exportZipFile, CompressionLevel.Optimal, false);
                            _fileHelper.DeleteFileIfExists(dbExport);
                            _fileHelper.DeleteFileIfExists(uploadsZip);
                            _fileHelper.DeleteFileIfExists(thumbnailsZip);

                            return Json(new { success = true, filename = $"/temp/{Path.GetFileName(exportZipFile)}" });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["Failed_Export"].Value} - {ex?.Message}");
                }
                finally
                {
                    _fileHelper.DeleteDirectoryIfExists(exportDir);
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> ImportBackup()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var importDir = Path.Combine(TempDirectory, "Import");

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
                                _fileHelper.CreateDirectoryIfNotExists(TempDirectory);

                                var filePath = Path.Combine(TempDirectory, "Import.zip");
                                if (!string.IsNullOrWhiteSpace(filePath))
                                {
									await _fileHelper.SaveFile(file, filePath, FileMode.Create);

									_fileHelper.DeleteDirectoryIfExists(importDir);
                                    _fileHelper.CreateDirectoryIfNotExists(importDir);

                                    ZipFile.ExtractToDirectory(filePath, importDir, true);
                                    _fileHelper.DeleteFileIfExists(filePath);

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
                    _fileHelper.DeleteDirectoryIfExists(importDir);
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterMultifactorAuth(string secret, string code)
        {
            if (!string.IsNullOrWhiteSpace(secret) && !string.IsNullOrWhiteSpace(code))
            {
                if (User?.Identity != null && User.Identity.IsAuthenticated)
                {
                    try
                    {
                        var tfa = new TwoFactorAuth(_config.GetOrDefault("Settings:Title", "WeddingShare"));
                        if (tfa.VerifyCode(secret, code))
                        {
                            var userId = int.Parse(((ClaimsIdentity)User.Identity).Claims.FirstOrDefault(x => string.Equals(ClaimTypes.Sid, x.Type, StringComparison.OrdinalIgnoreCase))?.Value ?? "-1");
                            if (userId > 0)
                            {
                                var set = await _database.SetMultiFactorToken(userId, secret);
                                if (set)
                                { 
                                    HttpContext.Session.SetString(SessionKey.MultiFactorTokenSet, "true");
                                    return Json(new { success = true });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{_localizer["MultiFactor_Token_Set_Failed"].Value} - {ex?.Message}");
                    }
                }
            }

            return Json(new { success = false });
        }

        [HttpDelete]
        public async Task<IActionResult> ResetMultifactorAuth()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                try
                {
                    var userId = int.Parse(((ClaimsIdentity)User.Identity).Claims.FirstOrDefault(x => string.Equals(ClaimTypes.Sid, x.Type, StringComparison.OrdinalIgnoreCase))?.Value ?? "-1");
                    if (userId > 0)
                    {
                        var cleared = await _database.SetMultiFactorToken(userId, string.Empty);
                        if (cleared)
                        {
                            HttpContext.Session.SetString(SessionKey.MultiFactorTokenSet, "false");
                            return Json(new { success = true });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_localizer["MultiFactor_Token_Set_Failed"].Value} - {ex?.Message}");
                }
            }

            return Json(new { success = false });
        }

        private async Task<bool> SetUserClaims(HttpContext ctx, UserModel user)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Sid, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username.ToLower())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> FailedLoginDetected(LoginModel model, UserModel user)
        {
            try
            {
                if (_config.GetOrDefault("Notifications:Alerts:Failed_Login", true))
                {
                    await _notificationHelper.Send("Invalid Login Detected", $"An invalid login attempt was made for account '{model?.Username}'.", _url.GenerateBaseUrl(HttpContext?.Request, "/Admin"));
                }

                var failedAttempts = await _database.IncrementLockoutCount(user.Id);
                if (failedAttempts >= _config.GetOrDefault("Settings:Account:Lockout_Attempts", 5))
                {
                    var timeout = _config.GetOrDefault("Settings:Account:Lockout_Mins", 60);
                    await _database.SetLockout(user.Id, DateTime.UtcNow.AddMinutes(timeout));

                    if (_config.GetOrDefault("Notifications:Alerts:Account_Lockout", true))
                    {
                        await _notificationHelper.Send("Account Lockout", $"Account '{model?.Username}' has been locked out for {timeout} minutes due to too many failed login attempts.", _url.GenerateBaseUrl(HttpContext?.Request, "/Admin"));
                    }
                }

                return true;
            }
            catch 
            {
                return false;
            }
        }
    }
}