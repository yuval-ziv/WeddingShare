using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeddingShare.Helpers;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IConfigHelper _config;
        private readonly IGalleryHelper _gallery;
        private readonly IDeviceDetector _deviceDetector;
        private readonly ILogger _logger;

        public HomeController(IConfigHelper config, IGalleryHelper gallery, IDeviceDetector deviceDetector, ILogger<HomeController> logger)
        {
            _config = config;
            _gallery = gallery;
            _deviceDetector = deviceDetector;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var deviceType = HttpContext.Session.GetString("DeviceType");
                if (string.IsNullOrWhiteSpace(deviceType))
                {
                    deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                    HttpContext.Session.SetString("DeviceType", deviceType ?? "Desktop");
                }

                if (_config.GetOrDefault("Settings", "Single_Gallery_Mode", false))
                {
                    var key = await _gallery.GetSecretKey("default");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        return RedirectToAction("Index", "Gallery");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred loading the homepage - {ex?.Message}");
            }

            return View();
        }
    }
}