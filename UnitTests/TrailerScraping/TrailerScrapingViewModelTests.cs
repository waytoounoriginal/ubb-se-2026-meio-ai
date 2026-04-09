namespace UnitTests.TrailerScraping
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Core.Models;
    using ubb_se_2026_meio_ai.Features.TrailerScraping.Services;
    using ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels;

    /// <summary>
    /// Unit tests for the <see cref="TrailerScrapingViewModel"/> class.
    /// </summary>
    [TestFixture]
    public class TrailerScrapingViewModelTests
    {
        private Mock<IScrapeJobRepository> mockRepo;
        private Mock<IVideoIngestionService> mockIngestionService;
        private TrailerScrapingViewModel viewModel;

        /// <summary>
        /// Sets up the test environment before each test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.mockRepo = new Mock<IScrapeJobRepository>();
            this.mockIngestionService = new Mock<IVideoIngestionService>();

            this.viewModel = new TrailerScrapingViewModel(this.mockIngestionService.Object, this.mockRepo.Object);
        }

        /// <summary>
        /// Tests that a search query shorter than the minimum length clears the suggestions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task SearchMoviesCommand_QueryTooShort_ClearsSuggestions()
        {
            this.viewModel.SuggestedMovies.Add(new MovieCardModel());

            await this.viewModel.SearchMoviesCommand.ExecuteAsync("A");

            Assert.That(this.viewModel.SuggestedMovies, Is.Empty);
        }

        [Test]
        public async Task SearchMoviesCommand_QueryTooShort_DoesNotCallRepository()
        {
            this.viewModel.SuggestedMovies.Add(new MovieCardModel());

            await this.viewModel.SearchMoviesCommand.ExecuteAsync("A");

            this.mockRepo.Verify(item => item.SearchMoviesByNameAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that a valid search query populates the suggested movies collection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task SearchMoviesCommand_ValidQuery_PopulatesSuggestions()
        {
            var mockResults = new List<MovieCardModel>
            {
                new MovieCardModel { Title = "Inception" },
                new MovieCardModel { Title = "Interstellar" },
            };

            this.mockRepo.Setup(item => item.SearchMoviesByNameAsync("Inc"))
                     .ReturnsAsync(mockResults);

            await this.viewModel.SearchMoviesCommand.ExecuteAsync("Inc");

            Assert.That(this.viewModel.SuggestedMovies.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SearchMoviesCommand_ValidQuery_SetsNoMovieFoundFalse()
        {
            var mockResults = new List<MovieCardModel>
            {
                new MovieCardModel { Title = "Inception" },
                new MovieCardModel { Title = "Interstellar" },
            };

            this.mockRepo.Setup(item => item.SearchMoviesByNameAsync("Inc")).ReturnsAsync(mockResults);
            await this.viewModel.SearchMoviesCommand.ExecuteAsync("Inc");

            Assert.That(this.viewModel.NoMovieFound, Is.False);
        }

        /// <summary>
        /// Tests that selecting a movie updates the view model properties and commands.
        /// </summary>
        [Test]
        public void SelectMovie_ValidMovie_SetsSelectedMovie()
        {
            var selectedMovie = new MovieCardModel { Title = "The Matrix" };

            this.viewModel.SelectMovie(selectedMovie);

            Assert.That(this.viewModel.SelectedMovie, Is.EqualTo(selectedMovie));
        }

        [Test]
        public void SelectMovie_ValidMovie_SetsSearchText()
        {
            var selectedMovie = new MovieCardModel { Title = "The Matrix" };

            this.viewModel.SelectMovie(selectedMovie);

            Assert.That(this.viewModel.SearchText, Is.EqualTo("The Matrix"));
        }

        [Test]
        public void SelectMovie_ValidMovie_EnablesStartScrapeCommand()
        {
            var selectedMovie = new MovieCardModel { Title = "The Matrix" };

            this.viewModel.SelectMovie(selectedMovie);

            Assert.That(this.viewModel.StartScrapeCommand.CanExecute(null), Is.True);
        }

        /// <summary>
        /// Tests that starting a scrape job with a valid movie calls the ingestion service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task StartScrapeCommand_ValidMovie_CallsIngestionService()
        {
            var selectedMovie = new MovieCardModel { Title = "Dune" };
            this.viewModel.SelectMovie(selectedMovie);
            this.viewModel.MaxResults = 10;

            this.mockIngestionService
                .Setup(item => item.RunScrapeJobAsync(selectedMovie, 10, It.IsAny<Func<ScrapeJobLogModel, Task>>()))
                .ReturnsAsync(new ScrapeJobModel());

            // Dummy stats to prevent null references during the finally block's RefreshAsync
            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(new DashboardStatsModel());

            await this.viewModel.StartScrapeCommand.ExecuteAsync(null);

            this.mockIngestionService.Verify(
                item => item.RunScrapeJobAsync(selectedMovie, 10, It.IsAny<Func<ScrapeJobLogModel, Task>>()),
                Times.Once);
        }

        [Test]
        public async Task StartScrapeCommand_ValidMovie_ResetsIsScraping()
        {
            var selectedMovie = new MovieCardModel { Title = "Dune" };
            this.viewModel.SelectMovie(selectedMovie);
            this.viewModel.MaxResults = 10;

            this.mockIngestionService
                .Setup(item => item.RunScrapeJobAsync(selectedMovie, 10, It.IsAny<Func<ScrapeJobLogModel, Task>>()))
                .ReturnsAsync(new ScrapeJobModel());

            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(new DashboardStatsModel());

            await this.viewModel.StartScrapeCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.IsScraping, Is.False);
        }

        [Test]
        public async Task StartScrapeCommand_ValidMovie_ResetsStatusTextToIdle()
        {
            var selectedMovie = new MovieCardModel { Title = "Dune" };
            this.viewModel.SelectMovie(selectedMovie);
            this.viewModel.MaxResults = 10;

            this.mockIngestionService
                .Setup(item => item.RunScrapeJobAsync(selectedMovie, 10, It.IsAny<Func<ScrapeJobLogModel, Task>>()))
                .ReturnsAsync(new ScrapeJobModel());

            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(new DashboardStatsModel());

            await this.viewModel.StartScrapeCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.StatusText, Is.EqualTo("Idle"));
        }

        /// <summary>
        /// Tests that the refresh command updates the dashboard statistics from the repository.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RefreshCommand_UpdatesTotalMovies()
        {
            var stats = new DashboardStatsModel
            {
                TotalMovies = 100,
                TotalReels = 50,
                TotalJobs = 10,
                RunningJobs = 2,
                CompletedJobs = 7,
                FailedJobs = 1,
            };

            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(stats);
            this.mockRepo.Setup(item => item.GetAllLogsAsync()).ReturnsAsync(new List<ScrapeJobLogModel>());
            this.mockRepo.Setup(item => item.GetAllMoviesAsync()).ReturnsAsync(new List<MovieCardModel>());
            this.mockRepo.Setup(item => item.GetAllReelsAsync()).ReturnsAsync(new List<ReelModel>());

            await this.viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.TotalMovies, Is.EqualTo(100));
        }

        [Test]
        public async Task RefreshCommand_UpdatesTotalReels()
        {
            var stats = new DashboardStatsModel
            {
                TotalMovies = 100,
                TotalReels = 50,
                TotalJobs = 10,
                RunningJobs = 2,
                CompletedJobs = 7,
                FailedJobs = 1,
            };

            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(stats);
            this.mockRepo.Setup(item => item.GetAllLogsAsync()).ReturnsAsync(new List<ScrapeJobLogModel>());
            this.mockRepo.Setup(item => item.GetAllMoviesAsync()).ReturnsAsync(new List<MovieCardModel>());
            this.mockRepo.Setup(item => item.GetAllReelsAsync()).ReturnsAsync(new List<ReelModel>());

            await this.viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.TotalReels, Is.EqualTo(50));
        }

        [Test]
        public async Task RefreshCommand_UpdatesRunningJobs()
        {
            var stats = new DashboardStatsModel
            {
                TotalMovies = 100,
                TotalReels = 50,
                TotalJobs = 10,
                RunningJobs = 2,
                CompletedJobs = 7,
                FailedJobs = 1,
            };

            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(stats);
            this.mockRepo.Setup(item => item.GetAllLogsAsync()).ReturnsAsync(new List<ScrapeJobLogModel>());
            this.mockRepo.Setup(item => item.GetAllMoviesAsync()).ReturnsAsync(new List<MovieCardModel>());
            this.mockRepo.Setup(item => item.GetAllReelsAsync()).ReturnsAsync(new List<ReelModel>());

            await this.viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.RunningJobs, Is.EqualTo(2));
        }

        [Test]
        public async Task RefreshCommand_UpdatesFailedJobs()
        {
            var stats = new DashboardStatsModel
            {
                TotalMovies = 100,
                TotalReels = 50,
                TotalJobs = 10,
                RunningJobs = 2,
                CompletedJobs = 7,
                FailedJobs = 1,
            };

            this.mockRepo.Setup(item => item.GetDashboardStatsAsync()).ReturnsAsync(stats);
            this.mockRepo.Setup(item => item.GetAllLogsAsync()).ReturnsAsync(new List<ScrapeJobLogModel>());
            this.mockRepo.Setup(item => item.GetAllMoviesAsync()).ReturnsAsync(new List<MovieCardModel>());
            this.mockRepo.Setup(item => item.GetAllReelsAsync()).ReturnsAsync(new List<ReelModel>());

            await this.viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.FailedJobs, Is.EqualTo(1));
        }
    }
}
