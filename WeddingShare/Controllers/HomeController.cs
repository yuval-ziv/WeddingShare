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

        public HomeController(IConfigHelper config, ILogger<HomeController> logger)
        {
            _config = config;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var galleryPath = "gallery";
            var workingDirectory = Path.Combine(Environment.CurrentDirectory, "wwwroot", galleryPath.Replace('/', '\\'));

            var images = new PhotoGallery(_config.GetOrDefault("Settings:GalleryColumns", 4))
            { 
                GalleryPath = $"/{galleryPath.Replace('\\', '/').TrimStart('/')}",
                Images = Directory.Exists(workingDirectory) ? Directory.GetFiles(workingDirectory, "*.*", SearchOption.TopDirectoryOnly)?.OrderByDescending(x => new FileInfo(x).CreationTimeUtc)?.Select(x => Path.GetFileName(x))?.ToList() : null
            };

            return View(images);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}