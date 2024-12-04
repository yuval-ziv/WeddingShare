using Microsoft.Extensions.Logging;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;
using WeddingShare.UnitTests.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class SecretKeyHelperTests
    {
        private readonly IDatabaseHelper _database = Substitute.For<IDatabaseHelper>();

        public SecretKeyHelperTests()
        {
            _database.GetGallery("Gallery1").Returns(new GalleryModel() { SecretKey = "001" });
            _database.GetGallery("Gallery2").Returns(new GalleryModel() { SecretKey = "002" });
        }

        [SetUp]
        public void Setup()
        {
        }

        [TestCase()]
        public async Task SecretKeyHelper_GetGallerySecretKey_DefaultEnvKey()
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("SECRET_KEY").Returns("123");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Secret_Key_Gallery2", "002" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = await new SecretKeyHelper(config, _database).GetGallerySecretKey("Gallery3");
            Assert.That(actual, Is.EqualTo("123"));
        }

        [TestCase()]
        public async Task SecretKeyHelper_GetGallerySecretKey_GalleryEnvKey()
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable("SECRET_KEY").Returns("123");
            environment.GetEnvironmentVariable("SECRET_KEY_GALLERY1").Returns("001");

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>()
            {
                { "Secret_Key_Gallery2", "002" }
            });

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = await new SecretKeyHelper(config, _database).GetGallerySecretKey("Gallery1");
            Assert.That(actual, Is.EqualTo("001"));
        }

        [TestCase("Gallery1", "001")]
        [TestCase("Gallery2", "002")]
        [TestCase("Gallery3", null)]
        public async Task SecretKeyHelper_GetGallerySecretKey_Database(string galleryId, string key)
        {
            var environment = Substitute.For<IEnvironmentWrapper>();
            environment.GetEnvironmentVariable(Arg.Any<string>()).Returns(string.Empty);

            var configuration = ConfigurationHelper.MockConfiguration(new Dictionary<string, string?>());

            var config = new ConfigHelper(environment, configuration, Substitute.For<ILogger<ConfigHelper>>());

            var actual = await new SecretKeyHelper(config, _database).GetGallerySecretKey(galleryId);
            Assert.That(actual, Is.EqualTo(key));
        }
    }
}