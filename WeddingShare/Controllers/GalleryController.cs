using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Attributes;
using WeddingShare.Enums;
using WeddingShare.Extensions;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models;
using WeddingShare.Models.Database;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class GalleryController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfigHelper _config;
        private readonly IDatabaseHelper _database;
        private readonly ISecretKeyHelper _secretKey;
        private readonly IDeviceDetector _deviceDetector;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<GalleryController> _localizer;

        private readonly string UploadsDirectory;
        private readonly string ThumbnailsDirectory;

        public GalleryController(IWebHostEnvironment hostingEnvironment, IConfigHelper config, IDatabaseHelper database, ISecretKeyHelper secretKey, IDeviceDetector deviceDetector, IImageHelper imageHelper, ILogger<GalleryController> logger, IStringLocalizer<GalleryController> localizer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config;
            _database = database;
            _secretKey = secretKey;
            _deviceDetector = deviceDetector;
            _imageHelper = imageHelper;
            _logger = logger;
            _localizer = localizer;

            UploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            ThumbnailsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "thumbnails");
        }

        [HttpGet]
        [RequiresSecretKey]
        [AllowGuestCreate]
        public async Task<IActionResult> Index(string id = "default", string? key = null, ViewMode? mode = null, GalleryOrder order = GalleryOrder.None)
        {
            id = id.ToLower();
            if (string.IsNullOrWhiteSpace(id) || _config.GetOrDefault("Settings", "Single_Gallery_Mode", false))
            {
                id = "default";
            }

            try
            {
                ViewBag.ViewMode = mode ?? (ViewMode)_config.GetOrDefault("Settings", "Default_Gallery_View", (int)ViewMode.Default);
            }
            catch
            {
                ViewBag.ViewMode = ViewMode.Default;
            }

            var deviceType = HttpContext.Session.GetString("DeviceType");
            if (string.IsNullOrWhiteSpace(deviceType))
            {
                deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                HttpContext.Session.SetString("DeviceType", deviceType ?? "Desktop");
            }

            ViewBag.IsMobile = !string.Equals("Desktop", deviceType, StringComparison.OrdinalIgnoreCase);

            var galleryPath = Path.Combine(UploadsDirectory, id);
            if (!Directory.Exists(galleryPath))
            {
                Directory.CreateDirectory(galleryPath);
                Directory.CreateDirectory(Path.Combine(galleryPath, "Pending"));
            }

            GalleryModel? gallery = await _database.GetGallery(id);
            if (gallery == null)
            {
                gallery = await _database.AddGallery(new GalleryModel()
                {
                    Name = id.ToLower(),
                    SecretKey = key
                });
            }

            if (gallery != null)
            { 
                ViewBag.SecretKey = gallery.SecretKey;

                var allowedFileTypes = _config.GetOrDefault("Settings", "Allowed_File_Types", ".jpg,.jpeg,.png").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var images = (await _database.GetAllGalleryItems(gallery.Id, GalleryItemState.Approved))?.Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x.Title).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase)));
                switch (order) 
                {
                    case GalleryOrder.UploadedAsc:
                        images = images?.OrderBy(x => x.Id);
                        break;
                    case GalleryOrder.UploadedDesc:
                        images = images?.OrderByDescending(x => x.Id);
                        break;
                    case GalleryOrder.NameAsc:
                        images = images?.OrderByDescending(x => x.Title);
                        break;
                    case GalleryOrder.NameDesc:
                        images = images?.OrderBy(x => x.Title);
                        break;
                    default: 
                        break;
                }

                var model = new PhotoGallery(_config.GetOrDefault("Settings", "Gallery_Columns", 4), (ViewMode)ViewBag.ViewMode)
                {
                    GalleryId = id,
                    GalleryPath = $"/{galleryPath.Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}",
                    ThumbnailsPath = $"/{ThumbnailsDirectory.Remove(_hostingEnvironment.WebRootPath).Replace('\\', '/').TrimStart('/')}",
                    Images = images?.Select(x => new PhotoGalleryImage() { Id = x.Id, Name = Path.GetFileName(x.Title), Path = x.Title })?.ToList(),
                    PendingCount = gallery?.PendingItems ?? 0,
                    FileUploader = !_config.GetOrDefault("Settings", "Disable_Upload", false) || (User?.Identity != null && User.Identity.IsAuthenticated) ? new FileUploader(id, "/Gallery/UploadImage") : null
                };
            
                return View(model);
            }

            return View(new PhotoGallery());
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage()
        {
            try
            {
                string galleryId = (Request?.Form?.FirstOrDefault(x => string.Equals("Id", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString()?.ToLower() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(galleryId))
                {
                    return Json(new { success = true, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Gallery_Id"].Value } });
                }

                var gallery = await _database.GetGallery(galleryId);
                if (gallery != null)
                {
                    var secretKey = await _secretKey.GetGallerySecretKey(galleryId);
                    string key = (Request?.Form?.FirstOrDefault(x => string.Equals("SecretKey", x.Key, StringComparison.OrdinalIgnoreCase)).Value)?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(secretKey) && !string.Equals(secretKey, key))
                    {
                        return Json(new { success = true, uploaded = 0, errors = new List<string>() { _localizer["Invalid_Secret_Key_Warning"].Value } });
                    }

                    var files = Request?.Form?.Files;
                    if (files != null && files.Count > 0)
                    {
                        var requiresReview = _config.GetOrDefault("Settings", "Require_Review", true);

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
                                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                                    var galleryPath = requiresReview ? Path.Combine(UploadsDirectory, gallery.Name, "Pending") : Path.Combine(UploadsDirectory, gallery.Name);
                                    if (!Directory.Exists(galleryPath))
                                    {
                                        Directory.CreateDirectory(galleryPath);
                                    }

                                    var filePath = Path.Combine(galleryPath, fileName);
                                    if (!string.IsNullOrWhiteSpace(filePath))
                                    {
                                        using (var fs = new FileStream(filePath, FileMode.Create))
                                        {
                                            await file.CopyToAsync(fs);
                                        }

                                        if (!requiresReview)
                                        { 
                                            if (!Directory.Exists(ThumbnailsDirectory))
                                            {
                                                Directory.CreateDirectory(ThumbnailsDirectory);
                                            }

                                            await _imageHelper.GenerateThumbnail(filePath, Path.Combine(ThumbnailsDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}.webp"), _config.GetOrDefault("Settings", "Thumbnail_Size", 720));
                                        }

                                        var item = await _database.AddGalleryItem(new GalleryItemModel()
                                        {
                                            GalleryId = gallery.Id,
                                            Title = fileName,
                                            State = requiresReview ? GalleryItemState.Pending : GalleryItemState.Approved
                                        });

                                        if (item?.Id > 0)
                                        { 
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
                    else
                    {
                        return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["No_Files_For_Upload"].Value } });
                    }
                }
                else
                {
                    return Json(new { success = false, uploaded = 0, errors = new List<string>() { _localizer["Gallery_Does_Not_Exist"].Value } });
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