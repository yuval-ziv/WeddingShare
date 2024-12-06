using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeddingShare.Helpers;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IDeviceDetector _deviceDetector;
        private readonly ILogger _logger;

        public HomeController(IDeviceDetector deviceDetector, ILogger<HomeController> logger)
        {
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

            return View();
        }
    }
}