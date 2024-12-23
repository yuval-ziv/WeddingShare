using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Text.Json;
using WeddingShare.Controllers;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Notifications;
using WeddingShare.Models;
using WeddingShare.Models.Database;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class GalleryControllerTests
    {
        private readonly IWebHostEnvironment _env = Substitute.For<IWebHostEnvironment>();
        private readonly IConfigHelper _config = Substitute.For<IConfigHelper>();
        private readonly ISecretKeyHelper _secretKey = Substitute.For<ISecretKeyHelper>();
        private readonly IDatabaseHelper _database = Substitute.For<IDatabaseHelper>();
        private readonly IFileHelper _file = Substitute.For<IFileHelper>();
        private readonly IDeviceDetector _deviceDetector = Substitute.For<IDeviceDetector>();
        private readonly IImageHelper _image = Substitute.For<IImageHelper>();
        private readonly INotificationHelper _notification = Substitute.For<INotificationHelper>();
        private readonly ILogger<GalleryController> _logger = Substitute.For<ILogger<GalleryController>>();
        private readonly IStringLocalizer<GalleryController> _localizer = Substitute.For<IStringLocalizer<GalleryController>>();
        
        public GalleryControllerTests()
        {
        }

        [SetUp]
        public void Setup()
        {
            _env.WebRootPath.Returns("/app/wwwroot");

            _database.GetGallery("default").Returns(Task.FromResult<GalleryModel?>(new GalleryModel() 
            {
                Id = 1,
                Name = "default",
                SecretKey = "password",
                ApprovedItems = 32,
                PendingItems = 50,
                TotalItems = 72
            }));
            _database.GetGallery("missing").Returns(Task.FromResult<GalleryModel?>(null));
            _database.AddGallery(Arg.Any<GalleryModel>()).Returns(Task.FromResult<GalleryModel?>(new GalleryModel()
            {
                Id = 101,
                Name = "missing",
                SecretKey = "123456",
                ApprovedItems = 0,
                PendingItems = 0,
                TotalItems = 0
            }));
			_database.AddGalleryItem(Arg.Any<GalleryItemModel>()).Returns(Task.FromResult<GalleryItemModel?>(MockData.MockGalleryItem()));

			_database.GetAllGalleryItems(Arg.Any<int>(), GalleryItemState.All).Returns(Task.FromResult(MockData.MockGalleryItems(10, 1, GalleryItemState.All)));
            _database.GetAllGalleryItems(Arg.Any<int>(), GalleryItemState.Pending).Returns(Task.FromResult(MockData.MockGalleryItems(10, 1, GalleryItemState.Pending)));
            _database.GetAllGalleryItems(Arg.Any<int>(), GalleryItemState.Approved).Returns(Task.FromResult(MockData.MockGalleryItems(10, 1, GalleryItemState.Approved)));
            
            _secretKey.GetGallerySecretKey(Arg.Any<string>()).Returns("password");
            _secretKey.GetGallerySecretKey("missing").Returns("123456");

            _config.GetOrDefault("Settings", "Allowed_File_Types", Arg.Any<string>()).Returns(".jpg,.jpeg,.png");
            _config.GetOrDefault("Settings", "Default_Gallery_View", Arg.Any<int>()).Returns((int)ViewMode.Default);
			_config.GetOrDefault("Settings", "Require_Review", Arg.Any<bool>()).Returns(true); 
			_config.GetOrDefault("Settings", "Max_File_Size_Mb", Arg.Any<int>()).Returns(10);

			_notification.Send(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));

			_localizer[Arg.Any<string>()].Returns(new LocalizedString("UnitTest", "UnitTest"));
		}

        [TestCase(DeviceType.Desktop, "default", "password", ViewMode.Default, GalleryOrder.None)]
        [TestCase(DeviceType.Mobile, "blaa", "123456", ViewMode.Presentation, GalleryOrder.UploadedAsc)]
        [TestCase(DeviceType.Tablet, "missing", "123456", ViewMode.Slideshow, GalleryOrder.NameAsc)]
        public async Task GalleryController_Index(DeviceType deviceType, string id, string? key, ViewMode? mode, GalleryOrder order)
        {
            _deviceDetector.ParseDeviceType(Arg.Any<string>()).Returns(deviceType);
            _config.GetOrDefault("Settings", "Single_Gallery_Mode", Arg.Any<bool>()).Returns(false);

            var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
            controller.ControllerContext.HttpContext = MockData.MockHttpContext();

            ViewResult actual = (ViewResult)await controller.Index(id, key, mode, order);
            Assert.That(actual, Is.TypeOf<ViewResult>());
            Assert.That(actual?.Model, Is.Not.Null);

            PhotoGallery model = (PhotoGallery)actual.Model;
            Assert.That(model?.GalleryId, Is.EqualTo(id));
            Assert.That(model.GalleryPath, Is.EqualTo($"/uploads/{id}"));
            Assert.That(model.ThumbnailsPath, Is.EqualTo($"/thumbnails"));
            Assert.That(model.ViewMode, Is.EqualTo(mode));
            Assert.That(model?.FileUploader?.GalleryId, Is.EqualTo(id));
            Assert.That(model?.FileUploader?.SecretKey, Is.EqualTo(key));
            Assert.That(model?.FileUploader?.UploadUrl, Is.EqualTo("/Gallery/UploadImage"));
        }

		[TestCase(DeviceType.Desktop, ViewMode.Default, GalleryOrder.None)]
		[TestCase(DeviceType.Mobile, ViewMode.Presentation, GalleryOrder.UploadedAsc)]
		[TestCase(DeviceType.Tablet, ViewMode.Slideshow, GalleryOrder.NameAsc)]
		public async Task GalleryController_Index_SingleGalleryMode(DeviceType deviceType, ViewMode? mode, GalleryOrder order)
		{
			_deviceDetector.ParseDeviceType(Arg.Any<string>()).Returns(deviceType);
			_config.GetOrDefault("Settings", "Single_Gallery_Mode", Arg.Any<bool>()).Returns(true);

			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext();

			ViewResult actual = (ViewResult)await controller.Index("default", "password", mode, order);
			Assert.That(actual, Is.TypeOf<ViewResult>());
			Assert.That(actual?.Model, Is.Not.Null);

			PhotoGallery model = (PhotoGallery)actual.Model;
			Assert.That(model?.GalleryId, Is.EqualTo("default"));
			Assert.That(model.GalleryPath, Is.EqualTo($"/uploads/default"));
			Assert.That(model.ThumbnailsPath, Is.EqualTo($"/thumbnails"));
			Assert.That(model.ViewMode, Is.EqualTo(mode));
			Assert.That(model?.FileUploader?.GalleryId, Is.EqualTo("default"));
			Assert.That(model?.FileUploader?.SecretKey, Is.EqualTo("password"));
			Assert.That(model?.FileUploader?.UploadUrl, Is.EqualTo("/Gallery/UploadImage"));
		}

		[TestCase(true, 1, null)]
		[TestCase(true, 3, "Bob")]
		[TestCase(false, 1, "")]
		[TestCase(false, 3, "Unit Testing")]
		public async Task GalleryController_UploadImage(bool requiresReview, int fileCount, string? uploadedBy)
		{
			_config.GetOrDefault("Settings", "Require_Review", Arg.Any<bool>()).Returns(requiresReview);

			var files = new FormFileCollection();
			for (var i = 0; i < fileCount; i++)
			{
				files.Add(new FormFile(null, 0, 0, "TestFile_001", $"{Guid.NewGuid()}.jpg"));
			}

			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(
				form: new Dictionary<string, StringValues>
				{
					{ "Id", "default" },
					{ "SecretKey", "password" },
					{ "UploadedBy", uploadedBy }
                },
				files: files);

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.True);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(files.Count));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploadedBy", string.Empty), Is.EqualTo(!string.IsNullOrWhiteSpace(uploadedBy) ? uploadedBy : string.Empty));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.EqualTo(0));
		}

		[TestCase(null)]
		[TestCase("")]
		public async Task GalleryController_UploadImage_InvalidGallery(string? id)
		{
			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(form: new Dictionary<string, StringValues>
			{
				{ "Id", id }
			});

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.False);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(0));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.GreaterThan(0));
		}

		[TestCase(null)]
		[TestCase("")]
		public async Task GalleryController_UploadImage_InvalidSecretKey(string? key)
		{
			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(form: new Dictionary<string, StringValues>
			{
				{ "Id", "default" },
				{ "SecretKey", key }
			});

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.False);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(0));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.GreaterThan(0));
		}

		[TestCase()]
		public async Task GalleryController_UploadImage_MissingGallery()
		{
			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(form: new Dictionary<string, StringValues>
			{
				{ "Id", Guid.NewGuid().ToString() }
			});

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.False);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(0));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.GreaterThan(0));
		}

		[TestCase()]
		public async Task GalleryController_UploadImage_NoFiles()
		{
			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(form: new Dictionary<string, StringValues>
			{
				{ "Id", "default" },
				{ "SecretKey", "password" }
			});

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.False);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(0));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.GreaterThan(0));
		}

		[TestCase()]
		public async Task GalleryController_UploadImage_FileTooBig()
		{
			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(
				form: new Dictionary<string, StringValues>
				{
					{ "Id", "default" },
					{ "SecretKey", "password" }
				},
				files: new FormFileCollection() {
					new FormFile(null, 0, int.MaxValue, "TestFile_001", $"{Guid.NewGuid()}.jpg")
				});

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.False);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(0));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.GreaterThan(0));
		}

		[TestCase()]
		public async Task GalleryController_UploadImage_InvalidFileType()
		{
			var controller = new GalleryController(_env, _config, _database, _file, _secretKey, _deviceDetector, _image, _notification, _logger, _localizer);
			controller.ControllerContext.HttpContext = MockData.MockHttpContext(
				form: new Dictionary<string, StringValues>
				{
					{ "Id", "default" },
					{ "SecretKey", "password" }
				},
				files: new FormFileCollection() {
					new FormFile(null, 0, int.MaxValue, "TestFile_001", $"{Guid.NewGuid()}.blaa")
				});

			JsonResult actual = (JsonResult)await controller.UploadImage();
			Assert.That(actual, Is.TypeOf<JsonResult>());
			Assert.That(actual?.Value, Is.Not.Null);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "success", false), Is.False);
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "uploaded", 0), Is.EqualTo(0));
			Assert.That(JsonResponseHelper.GetPropertyValue(actual.Value, "errors", new List<string>()).Count, Is.GreaterThan(0));
		}
	}
}