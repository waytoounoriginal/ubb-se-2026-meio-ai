namespace UnitTests.TrailerScraping
{
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Features.TrailerScraping.Services;

    /// <summary>
    /// Unit tests for the <see cref="YouTubeScraperService"/> and related classes.
    /// </summary>
    [TestFixture]
    public class YouTubeScraperServiceTests
    {
        /// <summary>
        /// Tests that the VideoUrl property of <see cref="ScrapedVideoResult"/> is formatted correctly.
        /// </summary>
        [Test]
        public void ScrapedVideoResult_VideoUrl_IsFormattedCorrectly()
        {
            var result = new ScrapedVideoResult
            {
                VideoId = "dQw4w9WgXcQ",
            };

            string actualUrl = result.VideoUrl;

            Assert.That(actualUrl, Is.EqualTo("https://www.youtube.com/watch?v=dQw4w9WgXcQ"));
        }

        /// <summary>
        /// Tests that the constructor accepts an API key without throwing an exception.
        /// </summary>
        [Test]
        public void Constructor_AcceptsApiKey()
        {
            string dummyKey = "AIzaSyDummyKeyThatDoesntMatterForThisTest";

            // If the constructor fails or throws, this test will fail.
            var service = new YouTubeScraperService(dummyKey);

            Assert.That(service, Is.Not.Null);
        }
    }
}