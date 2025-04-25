using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Constants;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ISettingsHelper _settings;
        private readonly IDatabaseHelper _database;
        private readonly IDeviceDetector _deviceDetector;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<Lang.Translations> _localizer;

        public HomeController(ISettingsHelper settings, IDatabaseHelper database, IDeviceDetector deviceDetector, ILogger<HomeController> logger, IStringLocalizer<Lang.Translations> localizer)
        {
            _settings = settings;
            _database = database;
            _deviceDetector = deviceDetector;
            _logger = logger;
            _localizer = localizer;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
        {
            var model = new Views.Home.IndexModel();

            try
            {
                var deviceType = HttpContext.Session.GetString(SessionKey.DeviceType);
                if (string.IsNullOrWhiteSpace(deviceType))
                {
                    deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                    HttpContext.Session.SetString(SessionKey.DeviceType, deviceType ?? "Desktop");
                }

                if (await _settings.GetOrDefault(Settings.Basic.SingleGalleryMode, false))
                {
                    var key = await _settings.GetOrDefault(Settings.Gallery.SecretKey, string.Empty, "default");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        return RedirectToAction("Index", "Gallery");
                    }
                }

                model.GalleryNames = await _settings.GetOrDefault(Settings.GallerySelector.Dropdown, false) ? await _database.GetGalleryNames() : new List<string>() { "default" };
                if (await _settings.GetOrDefault(Settings.GallerySelector.HideDefaultOption, false))
                {
                    model.GalleryNames = model.GalleryNames.Where(x => !x.Equals("default", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Homepage_Load_Error"].Value} - {ex?.Message}");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult SetIdentity(string name) 
        {
            try
            {
                if (Regex.IsMatch(name, @"^[a-zA-Z-\s\-\']+$", RegexOptions.Compiled))
                {
                    HttpContext.Session.SetString(SessionKey.ViewerIdentity, name);

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Identity_Session_Error"].Value}: '{name}'");
            }

            return Json(new { success = false });
        }
    }
}