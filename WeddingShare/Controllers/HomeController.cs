using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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

        public HomeController(IConfigHelper config, ILogger<HomeController> logger)
        {
            _config = config;
            _logger = logger;

            WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            GalleryPath = "gallery";
        }

        public IActionResult Index()
        {
            var uploadPath = Path.Combine(WorkingDirectory, GalleryPath);
            var images = new PhotoGallery(_config.GetOrDefault("Settings:GalleryColumns", 4))
            { 
                GalleryPath = $"/{GalleryPath}",
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
                    var uploadPath = Path.Combine(WorkingDirectory, GalleryPath);
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var uploaded = 0;
                    foreach (IFormFile file in files)
                    {
                        try
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
                        catch { }
                    }

                    return Json(new { success = true, uploaded });
                }
            }
            catch (Exception ex)
            { 
                return Json(new { success = false, uploaded = 0, message = ex?.Message });
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