using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeddingShare.Helpers;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IConfigHelper _config;
        private readonly ISecretKeyHelper _secretKey;
        private readonly IDeviceDetector _deviceDetector;
        private readonly ILogger _logger;

        public HomeController(IConfigHelper config, ISecretKeyHelper secretKey, IDeviceDetector deviceDetector, ILogger<HomeController> logger)
        {
            _config = config;
            _secretKey = secretKey;
            _deviceDetector = deviceDetector;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var deviceType = HttpContext.Session.GetString("DeviceType");
            if (string.IsNullOrWhiteSpace(deviceType))
            {
                deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                HttpContext.Session.SetString("DeviceType", deviceType ?? "Desktop");
            }

            if (_config.GetOrDefault("Settings", "Single_Gallery_Mode", false))
            { 
                var key = await _secretKey.GetGallerySecretKey("default");
                if (string.IsNullOrEmpty(key))
                {
                    return RedirectToAction("Index", "Gallery");
                }
            }

            return View();
        }
    }
}