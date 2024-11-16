using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WeddingShare.Extensions;
using WeddingShare.Helpers;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfigHelper _config;
        private readonly ILogger _logger;
     
        private readonly string WorkingDirectory;
        private readonly string GalleryPath;
        private readonly string PathSeparator;

        public HomeController(IConfigHelper config, ILogger<HomeController> logger)
        {
            _config = config;
            _logger = logger;

            WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            GalleryPath = "gallery";
            PathSeparator = _config.GetOrDefault("Settings:PathSeparator", "\\");
        }

        public IActionResult Index()
        {
            var uploadPath = Path.Combine(WorkingDirectory, GalleryPath).ReplaceSeparator(PathSeparator);
            var images = new PhotoGallery(_config.GetOrDefault("Settings:GalleryColumns", 4))
            { 
                GalleryPath = $"/{GalleryPath}".Replace('\\', '/'),
                Images = Directory.Exists(uploadPath) ? Directory.GetFiles(uploadPath, "*.*", SearchOption.TopDirectoryOnly)?.OrderByDescending(x => new FileInfo(x).CreationTimeUtc)?.Select(x => Path.GetFileName(x))?.ToList() : null
            };

            return View(images);
        }

        public async Task<IActionResult> UploadImage()
        {
            try
            {
                var files = Request?.Form?.Files;
                if (files != null && files.Count > 0)
                {
                    var uploadPath = Path.Combine(WorkingDirectory, GalleryPath).ReplaceSeparator(PathSeparator);
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var uploaded = 0;
                    var errors = new List<string>();
                    foreach (IFormFile file in files)
                    {
                        try
                        {
                            var extension = Path.GetExtension(file.FileName);
                            var maxFilesSize = _config.GetOrDefault("Settings:MaxFileSizeBytes", 20000000);

                            if (!string.Equals(".png", extension) && !string.Equals(".jgp", extension))
                            {
                                errors.Add($"Failed to upload file '{Path.GetFileName(file.FileName)}'. Only JPG and PNG images are allowed");
                            }
                            else if (file.Length > maxFilesSize)
                            {
                                errors.Add($"Failed to upload file '{Path.GetFileName(file.FileName)}'. Max file size is {maxFilesSize} bytes");
                            }
                            else
                            {
                                var filePath = Path.Combine(uploadPath, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
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
                            _logger.LogWarning(ex, $"Failed to save image to gallery - {ex?.Message}");
                        }
                    }

                    return Json(new { success = true, uploaded, errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload images - {ex?.Message}");
            }

            return Json(new { success = false, uploaded = 0 });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}