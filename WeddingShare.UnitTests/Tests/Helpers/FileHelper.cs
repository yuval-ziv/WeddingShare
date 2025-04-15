using Microsoft.Extensions.Logging;
using WeddingShare.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class FileHelperTests
    {
        private readonly ILogger<FileHelper> _logger = Substitute.For<ILogger<FileHelper>>();

        public FileHelperTests()
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [TestCase(-1, "0.00 B")]
        [TestCase(0, "0.00 B")]
        [TestCase(1, "1.00 B")]
        [TestCase(10, "10.00 B")]
        [TestCase(100, "100.00 B")]
        [TestCase(1000, "1.00 KB")]
        [TestCase(1000000, "1.00 MB")]
        [TestCase(1000000000, "1.00 GB")]
        [TestCase(1000000000000, "1.00 TB")]
        [TestCase(1000000000000000, "1.00 PB")]
        [TestCase(1000000000000000000, "1.00 EB")]
        public void FileHelper_BytesToHumanReadable(long bytes, string expected)
        {
            var actual = new FileHelper(_logger).BytesToHumanReadable(bytes);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}