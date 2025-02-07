using Microsoft.Extensions.Logging;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class GalleryHelperTests
    {
        private readonly IDatabaseHelper _database = Substitute.For<IDatabaseHelper>();

        public GalleryHelperTests()
        {
            _database.GetGallery("Gallery1").Returns(new GalleryModel() { SecretKey = "001" });
            _database.GetGallery("Gallery2").Returns(new GalleryModel() { SecretKey = "002" });
        }

        [SetUp]
        public void Setup()
        {
        }

        [TestCase()]
        public async Task GalleryHelper_GetSecretKey_DefaultEnvKey()
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("GALLERY_SECRET_KEY").Returns("123");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Gallery:Secret_Key_Gallery2", "002" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = await new GalleryHelper(config, _database).GetSecretKey("Gallery3");
            Assert.That(actual, Is.EqualTo("123"));
        }

        [TestCase()]
        public async Task GalleryHelper_GetSecretKey_GalleryEnvKey()
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("GALLERY_SECRET_KEY").Returns("123");
            environment.GetEnvironmentVariable("GALLERY_SECRET_KEY_GALLERY1").Returns("001");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Gallery:Secret_Key_Gallery2", "002" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = await new GalleryHelper(config, _database).GetSecretKey("Gallery1");
            Assert.That(actual, Is.EqualTo("001"));
        }

        [TestCase("Gallery1", "001")]
        [TestCase("Gallery2", "002")]
        [TestCase("Gallery3", null)]
        public async Task GalleryHelper_GetSecretKey_Database(string galleryId, string key)
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable(Arg.Any<string>()).Returns(string.Empty);

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>());

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = await new GalleryHelper(config, _database).GetSecretKey(galleryId);
            Assert.That(actual, Is.EqualTo(key));
        }

        [TestCase()]
        public void GalleryHelper_GetConfig_DefaultEnvKey()
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("GALLERY_SECRET_KEY").Returns("123");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Gallery:Secret_Key_Gallery2", "002" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = new GalleryHelper(config, _database).GetConfig("Gallery3", "Gallery:Secret_Key");
            Assert.That(actual, Is.EqualTo("123"));
        }

        [TestCase("Gallery:Secret_Key", "001")]
        [TestCase("Gallery:Columns", "001")]
        public void GalleryHelper_GetConfig_GalleryEnvKey(string key, string expected)
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("SECRET_KEY").Returns("123");
            environment.GetEnvironmentVariable("SECRET_KEY_GALLERY1").Returns("001");
            environment.GetEnvironmentVariable("GALLERY_COLUMNS").Returns("123");
            environment.GetEnvironmentVariable("GALLERY_COLUMNS_GALLERY1").Returns("001");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Gallery:Secret_Key_Gallery2", "002" },
                { "Gallery:Columns_Gallery2", "002" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = new GalleryHelper(config, _database).GetConfig("Gallery1", key);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("Disable_QR_Code", "true")]
        [TestCase("Gallery:QR_Code", "false")]
        public void GalleryHelper_GetConfig_Migrated_GalleryEnvKey(string key, string expected)
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("DISABLE_QR_CODE").Returns("false");
            environment.GetEnvironmentVariable("DISABLE_QR_CODE_GALLERY1").Returns("true");
            environment.GetEnvironmentVariable("GALLERY_QR_CODE").Returns("true");
            environment.GetEnvironmentVariable("GALLERY_QR_CODE_GALLERY1").Returns("false");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Disable_QR_Code_Gallery2", "false" },
                { "Gallery:QR_Code_Gallery2", "true" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = new GalleryHelper(config, _database).GetConfig("Gallery1", key);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}