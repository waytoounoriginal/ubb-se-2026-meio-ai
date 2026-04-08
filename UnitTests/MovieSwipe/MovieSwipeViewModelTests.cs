using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Services;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests.MovieSwipe
{
    [TestFixture]
    public class MovieSwipeViewModelTests
    {
        private Mock<ISwipeService> mockedSwipeService = null!;
        private Mock<IMovieCardFeedService> mockedFeedService = null!;
        private MovieSwipeViewModel viewModel = null!;

        private const int DefaultUserId = 1;
        private const int BufferSize = 5;

        [SetUp]
        public void SetUp()
        {
            this.mockedSwipeService = new Mock<ISwipeService>();
            this.mockedFeedService = new Mock<IMovieCardFeedService>();
            this.viewModel = new MovieSwipeViewModel(
                this.mockedSwipeService.Object,
                this.mockedFeedService.Object);
        }

        [Test]
        public async Task InitializeAsync_Success_PopulatesProperties()
        {
            // Arrange
            var movies = new List<MovieCardModel> { new MovieCardModel { MovieId = 1, Title = "Test" } };
            this.mockedFeedService
                .Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize))
                .ReturnsAsync(movies);

            // Act
            await this.viewModel.InitializeAsync();

            // Assert
            Assert.That(this.viewModel.CurrentCard, Is.Not.Null);
            Assert.That(this.viewModel.IsLoading, Is.False);
            Assert.That(this.viewModel.IsAllCaughtUp, Is.False);
        }

        [Test]
        public async Task InitializeAsync_NoMovies_SetsCaughtUpState()
        {
            // Arrange
            this.mockedFeedService
                .Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize))
                .ReturnsAsync(new List<MovieCardModel>());

            // Act
            await this.viewModel.InitializeAsync();

            // Assert
            Assert.That(this.viewModel.IsAllCaughtUp, Is.True);
            Assert.That(this.viewModel.CurrentCard, Is.Null);
        }

        [Test]
        public async Task InitializeAsync_Exception_SetsStatusMessage()
        {
            // Arrange
            this.mockedFeedService
                .Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize))
                .ThrowsAsync(new System.Exception("Critical Failure"));

            // Act
            await this.viewModel.InitializeAsync();

            // Assert
            Assert.That(this.viewModel.StatusMessage, Contains.Substring("Critical Failure"));
            Assert.That(this.viewModel.IsLoading, Is.False);
        }

        [Test]
        public async Task SwipeRightAsync_UpdatesServiceAndAdvances()
        {
            // Arrange
            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 10 },
                new MovieCardModel { MovieId = 20 }
            };
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(movies);
            await this.viewModel.InitializeAsync();

            // Act
            await this.viewModel.SwipeRightCommand.ExecuteAsync(null);

            // Assert
            this.mockedSwipeService.Verify(x => x.UpdatePreferenceScoreAsync(DefaultUserId, 10, true), Times.Once);
            Assert.That(this.viewModel.CurrentCard!.MovieId, Is.EqualTo(20));
        }

        [Test]
        public async Task SwipeLeftAsync_NoCurrentCard_DoesNothing()
        {
            // Arrange
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(new List<MovieCardModel>());
            await this.viewModel.InitializeAsync();

            // Act
            await this.viewModel.SwipeLeftCommand.ExecuteAsync(null);

            // Assert
            this.mockedSwipeService.Verify(x => x.UpdatePreferenceScoreAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task TryRefillQueue_ConcurrencyLock_PreventsMultipleCalls()
        {
            // Arrange
            var movies = new List<MovieCardModel> { new MovieCardModel { MovieId = 1 } };
            var tcs = new TaskCompletionSource<List<MovieCardModel>>();

            // Setup feed to hang so we can test the lock
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).Returns(tcs.Task);

            // Trigger initialization (which calls refill once)
            var initTask = this.viewModel.InitializeAsync();

            // Act: Attempt to swipe (which triggers a second refill while first is pending)
            await this.viewModel.SwipeRightCommand.ExecuteAsync(null);

            // Assert: Feed service should only be called once because the lock (_isRefilling) is held
            this.mockedFeedService.Verify(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize), Times.Once);
        }

        [Test]
        public async Task TryRefillQueue_FiltersDuplicatesAndRecentlySwiped()
        {
            // Arrange: Setup 2 cards in queue (Threshold is 2, so this triggers refill)
            var initialMovies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1 },
                new MovieCardModel { MovieId = 2 },
                new MovieCardModel { MovieId = 3 }
            };
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(initialMovies);
            await this.viewModel.InitializeAsync(); // Current=1, Queue=[2, 3]

            // Setup refill response: ID 1 (swiped), ID 3 (already in queue), ID 4 (new)
            var refillMovies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1 },
                new MovieCardModel { MovieId = 3 },
                new MovieCardModel { MovieId = 4 }
            };
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(refillMovies);

            // Act: Swipe ID 1
            await this.viewModel.SwipeRightCommand.ExecuteAsync(null);

            // Assert: Queue should contain ID 3 (original) and ID 4 (new). ID 1 filtered as recent, ID 3 filtered as duplicate.
            Assert.That(this.viewModel.CardQueue.Any(m => m.MovieId == 4), Is.True);
            Assert.That(this.viewModel.CardQueue.Count(m => m.MovieId == 3), Is.EqualTo(1));
            Assert.That(this.viewModel.CardQueue.Any(m => m.MovieId == 1), Is.False);
        }

        [Test]
        public async Task TryRefillQueue_RecoversFromEmptyState()
        {
            // Arrange: Start with nothing
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(new List<MovieCardModel>());
            await this.viewModel.InitializeAsync();
            Assert.That(this.viewModel.CurrentCard, Is.Null);

            // Act: Refill finds new movies
            var newMovies = new List<MovieCardModel> { new MovieCardModel { MovieId = 100 } };
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(newMovies);

            // Execute refill manually via a swipe (even though current is null, the logic branch for refill is still reachable)
            // Or trigger a scenario where the queue is refilled when current was null
            await this.viewModel.InitializeAsync();

            // Assert
            Assert.That(this.viewModel.CurrentCard, Is.Not.Null);
            Assert.That(this.viewModel.IsAllCaughtUp, Is.False);
        }

        [Test]
        public async Task TryRefillQueue_HandlesExceptionGracefully()
        {
            // Arrange
            var initial = new List<MovieCardModel> { new MovieCardModel { MovieId = 1 }, new MovieCardModel { MovieId = 2 } };
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ReturnsAsync(initial);
            await this.viewModel.InitializeAsync();

            // Refill throws
            this.mockedFeedService.Setup(x => x.FetchMovieFeedAsync(DefaultUserId, BufferSize)).ThrowsAsync(new System.Exception("Refill failed"));

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await this.viewModel.SwipeRightCommand.ExecuteAsync(null));
        }
    }
}