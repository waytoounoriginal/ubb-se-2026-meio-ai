namespace UnitTests.TrailerScraping
{
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Core.Models;
    using ubb_se_2026_meio_ai.Features.TrailerScraping.Services;

    /// <summary>
    /// Unit tests for the <see cref="VideoIngestionService"/> class.
    /// </summary>
    [TestFixture]
    public class VideoIngestionServiceTests
    {
        private Mock<IYouTubeScraperService> mockScraper;
        private Mock<IScrapeJobRepository> mockRepo;
        private Mock<IVideoDownloadService> mockDownloader;
        private VideoIngestionService service;

        /// <summary>
        /// Sets up the test environment before each test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.mockScraper = new Mock<IYouTubeScraperService>();
            this.mockRepo = new Mock<IScrapeJobRepository>();
            this.mockDownloader = new Mock<IVideoDownloadService>();

            this.service = new VideoIngestionService(
                this.mockScraper.Object,
                this.mockRepo.Object,
                this.mockDownloader.Object);
        }

        /// <summary>
        /// Tests that ingesting a video from a URL returns an empty string if the reel already exists.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task IngestVideoFromUrlAsync_ReelAlreadyExists_ReturnsEmptyString()
        {
            this.mockRepo.Setup(r => r.ReelExistsByVideoUrlAsync("duplicate_url")).ReturnsAsync(true);

            string result = await this.service.IngestVideoFromUrlAsync("duplicate_url", 1);

            Assert.That(result, Is.Empty);
            this.mockDownloader.Verify(d => d.DownloadVideoAsMp4Async(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that ingesting a video from a URL returns an empty string if the download fails.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task IngestVideoFromUrlAsync_DownloadFails_ReturnsEmptyString()
        {
            this.mockRepo.Setup(r => r.ReelExistsByVideoUrlAsync("new_url")).ReturnsAsync(false);
            this.mockDownloader.Setup(d => d.DownloadVideoAsMp4Async("new_url", 60)).ReturnsAsync((string?)null);

            string result = await this.service.IngestVideoFromUrlAsync("new_url", 1);

            Assert.That(result, Is.Empty);
            this.mockRepo.Verify(r => r.InsertScrapedReelAsync(It.IsAny<ReelModel>()), Times.Never);
        }

        /// <summary>
        /// Tests that ingesting a video from a URL returns the new reel ID on success.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task IngestVideoFromUrlAsync_Success_ReturnsReelId()
        {
            this.mockRepo.Setup(r => r.ReelExistsByVideoUrlAsync("new_url")).ReturnsAsync(false);
            this.mockDownloader.Setup(d => d.DownloadVideoAsMp4Async("new_url", 60)).ReturnsAsync("C:\\temp\\video.mp4");
            this.mockRepo.Setup(r => r.InsertScrapedReelAsync(It.IsAny<ReelModel>())).ReturnsAsync(99);

            string result = await this.service.IngestVideoFromUrlAsync("new_url", 1);

            Assert.That(result, Is.EqualTo("99"));
            this.mockRepo.Verify(r => r.InsertScrapedReelAsync(It.Is<ReelModel>(m => m.VideoUrl == "C:\\temp\\video.mp4")), Times.Once);
        }

        /// <summary>
        /// Tests that a full scrape job runs successfully, processes a video, and updates the job status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RunScrapeJobAsync_CompletesSuccessfully_UpdatesJobStatus()
        {
            var movie = new MovieCardModel { MovieId = 1, Title = "Dune" };
            var scrapedVideos = new List<ScrapedVideoResult>
            {
                // We only need to set the VideoId; the VideoUrl is computed automatically!
                new ScrapedVideoResult { VideoId = "123", Title = "Trailer" },
            };

            this.mockRepo.Setup(repository => repository.CreateJobAsync(It.IsAny<ScrapeJobModel>())).ReturnsAsync(10);
            this.mockScraper.Setup(scraper => scraper.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(scrapedVideos);
            this.mockRepo.Setup(repository => repository.ReelExistsByVideoUrlAsync(It.IsAny<string>())).ReturnsAsync(false);
            this.mockDownloader.Setup(downloader => downloader.DownloadVideoAsMp4Async(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync("C:\\temp\\dune.mp4");
            this.mockRepo.Setup(repository => repository.InsertScrapedReelAsync(It.IsAny<ReelModel>())).ReturnsAsync(5);

            var jobResult = await this.service.RunScrapeJobAsync(movie, 1);

            Assert.That(jobResult, Is.Not.Null);
            Assert.That(jobResult.Status, Is.EqualTo("completed"));
            Assert.That(jobResult.ReelsCreated, Is.EqualTo(1));
            this.mockRepo.Verify(repository => repository.UpdateJobAsync(It.Is<ScrapeJobModel>(job => job.Status == "completed")), Times.Once);
            this.mockRepo.Verify(repository => repository.AddLogEntryAsync(It.IsAny<ScrapeJobLogModel>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that RunScrapeJobAsync skips processing a video if the reel already exists.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RunScrapeJobAsync_VideoAlreadyExists_SkipsAndDoesNotDownload()
        {
            var movie = new MovieCardModel { MovieId = 1, Title = "Dune" };
            var scrapedVideos = new System.Collections.Generic.List<ScrapedVideoResult>
            {
                new ScrapedVideoResult { VideoId = "123", Title = "Trailer" },
            };

            this.mockScraper.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(scrapedVideos);

            // Simulate the reel already existing in the database
            this.mockRepo.Setup(r => r.ReelExistsByVideoUrlAsync(It.IsAny<string>())).ReturnsAsync(true);

            var jobResult = await this.service.RunScrapeJobAsync(movie, 1);

            Assert.That(jobResult.ReelsCreated, Is.EqualTo(0));
            this.mockDownloader.Verify(d => d.DownloadVideoAsMp4Async(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that RunScrapeJobAsync catches exceptions thrown by the YouTube scraper and marks the job as failed.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RunScrapeJobAsync_ScraperThrowsException_MarksJobAsFailed()
        {
            var movie = new MovieCardModel { MovieId = 1, Title = "Dune" };

            // Simulate the API crashing
            this.mockScraper.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new System.Exception("YouTube API quota exceeded"));

            var jobResult = await this.service.RunScrapeJobAsync(movie, 1);

            Assert.That(jobResult.Status, Is.EqualTo("failed"));
            Assert.That(jobResult.ErrorMessage, Is.EqualTo("YouTube API quota exceeded"));
            this.mockRepo.Verify(r => r.UpdateJobAsync(It.Is<ScrapeJobModel>(job => job.Status == "failed")), Times.Once);
        }

        /// <summary>
        /// Tests that RunScrapeJobAsync continues processing other videos if one video download fails.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RunScrapeJobAsync_OneDownloadFails_ContinuesProcessingRemainingVideos()
        {
            var movie = new MovieCardModel { MovieId = 1, Title = "Dune" };
            var scrapedVideos = new System.Collections.Generic.List<ScrapedVideoResult>
            {
                new ScrapedVideoResult { VideoId = "fail123", Title = "Bad Trailer" },
                new ScrapedVideoResult { VideoId = "good123", Title = "Good Trailer" }
            };

            this.mockScraper.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(scrapedVideos);
            this.mockRepo.Setup(r => r.ReelExistsByVideoUrlAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Simulate the first download returning null (failing) and the second succeeding
            this.mockDownloader.Setup(d => d.DownloadVideoAsMp4Async(It.Is<string>(url => url.Contains("fail123")), It.IsAny<int>())).ReturnsAsync((string?)null);
            this.mockDownloader.Setup(d => d.DownloadVideoAsMp4Async(It.Is<string>(url => url.Contains("good123")), It.IsAny<int>())).ReturnsAsync("C:\\temp\\good.mp4");

            this.mockRepo.Setup(r => r.InsertScrapedReelAsync(It.IsAny<ReelModel>())).ReturnsAsync(99);

            var jobResult = await this.service.RunScrapeJobAsync(movie, 2);

            // It should skip the first one, but successfully create 1 reel for the second one!
            Assert.That(jobResult.ReelsCreated, Is.EqualTo(1));
            this.mockRepo.Verify(r => r.InsertScrapedReelAsync(It.IsAny<ReelModel>()), Times.Once);
        }
    }
}