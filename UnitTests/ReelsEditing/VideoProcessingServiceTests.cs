namespace UnitTests.ReelsEditing
{
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

    /// <summary>
    /// Unit tests for the <see cref="VideoProcessingService"/> class.
    /// </summary>
    [TestFixture]
    public class VideoProcessingServiceTests
    {
        private Mock<IAudioLibraryRepository> mockAudioLibrary;
        private VideoProcessingService service;

        /// <summary>
        /// Sets up the test environment before each test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.mockAudioLibrary = new Mock<IAudioLibraryRepository>();
            this.service = new VideoProcessingService(this.mockAudioLibrary.Object);
        }

        /// <summary>
        /// Tests that applying a crop to a non-existent video file returns the original path.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task ApplyCropAsync_VideoFileDoesNotExist_ReturnsOriginalPath()
        {
            string fakePath = "C:\\does_not_exist_video.mp4";
            string cropJson = "{\"x\": 10, \"y\": 10, \"width\": 100, \"height\": 100}";

            string result = await this.service.ApplyCropAsync(fakePath, cropJson);

            Assert.That(result, Is.EqualTo(fakePath));
        }

        /// <summary>
        /// Tests that applying a crop with empty or null JSON returns the original path.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task ApplyCropAsync_EmptyOrNullJson_ReturnsOriginalPath()
        {
            string existingFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string? emptyJson = null;

            string result = await this.service.ApplyCropAsync(existingFilePath, emptyJson!);

            // If the JSON is empty, ReadCropData returns the BaseWidth/BaseHeight, 
            // which triggers the early exit returning the original path.
            Assert.That(result, Is.EqualTo(existingFilePath));
        }

        /// <summary>
        /// Tests that merging audio into a non-existent video file returns the original path.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task MergeAudioAsync_VideoFileDoesNotExist_ReturnsOriginalPath()
        {
            string fakePath = "C:\\does_not_exist_video.mp4";

            string result = await this.service.MergeAudioAsync(fakePath, 1, 0, 30, 100);

            Assert.That(result, Is.EqualTo(fakePath));
        }
    }
}