using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using WeddingShare.Controllers;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class HomeControllerTests
    {
        private readonly IConfigHelper _config = Substitute.For<IConfigHelper>();
        private readonly IGalleryHelper _gallery = Substitute.For<IGalleryHelper>();
        private readonly IDeviceDetector _deviceDetector = Substitute.For<IDeviceDetector>();
        private readonly ILogger<HomeController> _logger = Substitute.For<ILogger<HomeController>>();
        private readonly IStringLocalizer<Lang.Translations> _localizer = Substitute.For<IStringLocalizer<Lang.Translations>>();
        
        public HomeControllerTests()
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [TestCase(DeviceType.Desktop, true, "", true)]
        [TestCase(DeviceType.Desktop, false, "", false)]
        [TestCase(DeviceType.Mobile, true, "", true)]
        [TestCase(DeviceType.Mobile, false, "", false)]
        [TestCase(DeviceType.Desktop, true, "123456", false)]
        [TestCase(DeviceType.Desktop, false, "Abc123!", false)]
        [TestCase(DeviceType.Mobile, true, "abc123!", false)]
        [TestCase(DeviceType.Mobile, false, "adsbsds", false)]
        public async Task HomeController_Index(DeviceType deviceType, bool singleGalleryMode, string secretKey, bool isRedirect)
        {
            _deviceDetector.ParseDeviceType(Arg.Any<string>()).Returns(deviceType);
            _config.GetOrDefault("Settings:Single_Gallery_Mode", Arg.Any<bool>()).Returns(singleGalleryMode);
            _gallery.GetSecretKey(Arg.Any<string>()).Returns(secretKey);

            var controller = new HomeController(_config, _gallery, _deviceDetector, _logger, _localizer);
            controller.ControllerContext.HttpContext = new DefaultHttpContext()
            {
                Session = new MockSession()
            };

            if (!isRedirect)
            {
                ViewResult actual = (ViewResult)await controller.Index();
                Assert.That(actual, Is.TypeOf<ViewResult>());
            }
            else
            { 
                RedirectToActionResult actual = (RedirectToActionResult)await controller.Index();
                Assert.That(actual, Is.TypeOf<RedirectToActionResult>());
                Assert.That(actual.Permanent, Is.EqualTo(false));
                Assert.That(actual.ControllerName, Is.EqualTo("Gallery"));
                Assert.That(actual.ActionName, Is.EqualTo("Index"));
                Assert.That(actual.RouteValues, Is.Null);
                Assert.That(actual.Fragment, Is.Null);
            }
        }
    }
}