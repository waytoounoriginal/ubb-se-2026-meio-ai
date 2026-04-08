namespace UnitTests.TrailerScraping
{
    using System.IO;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Features.TrailerScraping.Services;

    /// <summary>
    /// Unit tests for the <see cref="VideoDownloadService"/> class.
    /// </summary>
    [TestFixture]
    public class VideoDownloadServiceTests
    {
        private string testDownloadFolder;
        private VideoDownloadService service;

        /// <summary>
        /// Sets up the test environment before each test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Use a temporary folder for tests so we don't write to the real app data
            this.testDownloadFolder = Path.Combine(Path.GetTempPath(), "MeioAITests", "VideoDownloadService");

            // Clean up any old test runs
            if (Directory.Exists(this.testDownloadFolder))
            {
                Directory.Delete(this.testDownloadFolder, true);
            }

            this.service = new VideoDownloadService(this.testDownloadFolder);
        }

        /// <summary>
        /// Cleans up the test environment after each test finishes.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            // Clean up the temp folder after the test finishes
            if (Directory.Exists(this.testDownloadFolder))
            {
                Directory.Delete(this.testDownloadFolder, true);
            }
        }

        /// <summary>
        /// Tests that the constructor creates the specified download directory.
        /// </summary>
        [Test]
        public void Constructor_CreatesDownloadDirectory()
        {
            Assert.That(Directory.Exists(this.testDownloadFolder), Is.True);
        }

        /// <summary>
        /// Tests that GetExpectedFilePath returns the correctly formatted file path for a given video ID.
        /// </summary>
        [Test]
        public void GetExpectedFilePath_ReturnsCorrectlyFormattedPath()
        {
            string videoId = "dQw4w9WgXcQ"; // Never gonna give you up
            string expectedPath = Path.Combine(this.testDownloadFolder, $"{videoId}.mp4");

            string result = this.service.GetExpectedFilePath(videoId);

            Assert.That(result, Is.EqualTo(expectedPath));
        }

        /// <summary>
        /// Tests that the LastError property is initially null.
        /// </summary>
        [Test]
        public void LastError_InitiallyNull()
        {
            Assert.That(this.service.LastError, Is.Null);
        }
    }
}