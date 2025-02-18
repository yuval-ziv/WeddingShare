using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using WeddingShare.Helpers;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class LanguageController : Controller
    {
        private readonly IConfigHelper _config;
        private readonly ILanguageHelper _languageHelper;

        public LanguageController(IConfigHelper config, ILanguageHelper languageHelper)
        {
            _config = config;
            _languageHelper = languageHelper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var options = new List<SupportedLanguage>();

            try
            {
                var defaultLang = HttpContext.Session.GetString(SessionKey.SelectedLanguage);
                if (string.IsNullOrWhiteSpace(defaultLang))
                { 
                    defaultLang = _config.GetOrDefault("Settings:Languages:Default", "en-GB");
                }

                options = (await _languageHelper.DetectSupportedCulturesAsync()).Select(x => new SupportedLanguage() { Key = x.Name, Value = $"{x.EnglishName} ({x.Name})", Selected = string.Equals(defaultLang, x.Name, StringComparison.OrdinalIgnoreCase) }).OrderBy(x => x.Value.ToLower()).ToList();
            }
            catch { }

            return Json(new { supported = options });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeDisplayLanguage(string culture)
        {
            try
            {
                HttpContext.Session.SetString(SessionKey.SelectedLanguage, culture.ToLower());
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

                return Json(new { success = true });
            }
            catch { }

            return Json(new { success = false });
        }
    }
}