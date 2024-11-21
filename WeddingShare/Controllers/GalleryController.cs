using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Extensions;
using WeddingShare.Helpers;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class GalleryController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfigHelper _config;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<GalleryController> _localizer;

        private readonly string UploadsDirectory;

        public GalleryController(IWebHostEnvironment hostingEnvironment, IConfigHelper config, ILogger<GalleryController> logger, IStringLocalizer<GalleryController> localizer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config;
            _logger = logger;
            _localizer = localizer;

            UploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
        }

        [HttpGet]
        public IActionResult Index(string id, string? key)
        {
            if (_config.GetOrDefault("Settings", "Single_Gallery_Mode", false))
            {
                id = "default";
            }

            id = id.ToLower();

            var secretKey = _config.Get("Settings", $"Secret_Key_{id}");
            if (string.IsNullOrEmpty(secretKey))
            {
                secretKey = _config.Get("Settings", "Secret_Key");
            }

            if (!string.IsNullOrEmpty(secretKey) && !string.Equals(secretKey, key))
            {
                _logger.LogWarning(_localizer["Invalid_Security_Key_Warning"].Value);
                ViewBag.ErrorMessage = _localizer["Invalid_Gallery_Key"].Value;

                return View("~/Views/Home/Index.cshtml");
            }
            else if (string.IsNullOrEmpty(id))
            {
                ViewBag.ErrorMessage = _localizer["Invalid_Gallery_Id"].Value;

                return View("~/Views/Home/Index.cshtml");
            }

            ViewBag.SecretKey = key;

            var galleryPath = Path.Combine(UploadsDirectory, id);
            var allowedFileTypes = _config.GetOrDefault("Settings", "Allowed_File_Types", ".jpg,.jpeg,.png").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var files = Directory.Exists(galleryPath) ? Directory.GetFiles(galleryPath, "*.*", SearchOption.TopDirectoryOnly)?.Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase))) : null;
            var pendingPath = Path.Combine(galleryPath, "Pending");
            var images = new PhotoGallery(_config.GetOrDefault("Settings", "Gallery_Columns", 4))
            {
                GalleryId = id,
                GalleryPath = $"/{galleryPath.Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}",
                Images = files?.OrderByDescending(x => new FileInfo(x).CreationTimeUtc)?.Select(x => Path.GetFileName(x))?.ToList(),
                PendingCount = Directory.Exists(pendingPath) ? Directory.GetFiles(pendingPath, "*.*", SearchOption.TopDirectoryOnly).Length : 0,
                FileUploader = !_config.GetOrDefault("Settings", "Disable_Upload", false) ? new FileUploader(id, "/Gallery/UploadImage") : null
            };

            return View(images);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage()
        {
            try
            {
                var secretKey = _config.Get("Settings", "Secret_Key");
                var key = Request?.Form?.FirstOrDefault(x => string.Equals("SecretKey", x.Key, StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(secretKey) && !string.Equals(secretKey, key))
                {
                    _logger.LogWarning(_localizer["Invalid_Security_Key_Warning"].Value);
                    throw new UnauthorizedAccessException(_localizer["Invalid_Access_Token"].Value);
                }

                string galleryId = Request?.Form?.FirstOrDefault(x => string.Equals("GalleryId", x.Key, StringComparison.OrdinalIgnoreCase)).Value ?? string.Empty;
                if (string.IsNullOrEmpty(galleryId))
                {
                    return Json(new { success = true, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Gallery_Id"].Value } });
                }

                galleryId = galleryId.ToLower();

                var galleryPath = Path.Combine(UploadsDirectory, galleryId);
                var files = Request?.Form?.Files;
                if (files != null && files.Count > 0)
                {
                    if (!Directory.Exists(galleryPath))
                    {
                        Directory.CreateDirectory(galleryPath);
                    }

                    var requiresReview = _config.GetOrDefault("Settings", "Require_Review", true);
                    if (requiresReview)
                    { 
                        galleryPath = Path.Combine(galleryPath, "Pending");
                        if (!Directory.Exists(galleryPath))
                        {
                            Directory.CreateDirectory(galleryPath);
                        }
                    }

                    var uploaded = 0;
                    var errors = new List<string>();
                    foreach (IFormFile file in files)
                    {
                        try
                        {
                            var extension = Path.GetExtension(file.FileName);
                            var maxFilesSize = _config.GetOrDefault("Settings", "Max_File_Size_Mb", 10) * 1000000;

                            var allowedFileTypes = _config.GetOrDefault("Settings", "Allowed_File_Types", ".jpg,.jpeg,.png").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                            if (!allowedFileTypes.Any(x => string.Equals(x.Trim('.'), extension.Trim('.'), StringComparison.OrdinalIgnoreCase)))
                            {
                                errors.Add($"{_localizer["File_Upload_Failed"].Value} '{Path.GetFileName(file.FileName)}'. {_localizer["Invalid_File_Type"].Value}");
                            }
                            else if (file.Length > maxFilesSize)
                            {
                                errors.Add($"{_localizer["File_Upload_Failed"].Value} '{Path.GetFileName(file.FileName)}'. {_localizer["Max_File_Size"].Value} {maxFilesSize} bytes");
                            }
                            else
                            {
                                var filePath = Path.Combine(galleryPath, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    using (var fs = new FileStream(filePath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(fs);
                                        uploaded++;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"{_localizer["Save_To_Gallery_Failed"].Value} - {ex?.Message}");
                        }
                    }

                    return Json(new { success = true, uploaded, requiresReview, errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Image_Upload_Failed"].Value} - {ex?.Message}");
            }

            return Json(new { success = false, uploaded = 0 });
        }
    }
}