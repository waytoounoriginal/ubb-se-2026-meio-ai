using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace UnitTests.MovieTournament
{
    [TestFixture]
    public class TournamentSetupViewModelTests
    {
        private const int UserId = 1;
        private const int MinPoolSize = 4;
        private const int BackgroundCount = 4;

        private Mock<ITournamentLogicService> mockedTournamentLogicService = null!;
        private Mock<IMovieTournamentRepository> mockedTournamentRepository = null!;

        [SetUp]
        public void SetUp()
        {
            this.mockedTournamentLogicService = new Mock<ITournamentLogicService>();
            this.mockedTournamentRepository = new Mock<IMovieTournamentRepository>();

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MinPoolSize);

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>());
        }

        [TearDown]
        public void TearDown()
        {
            this.mockedTournamentLogicService = null!;
            this.mockedTournamentRepository = null!;
        }

        private TournamentSetupViewModel CreateViewModel()
        {
            return new TournamentSetupViewModel(
                this.mockedTournamentLogicService.Object,
                this.mockedTournamentRepository.Object);
        }

        [Test]
        public void Constructor_defaultPoolSize_isSetToMinimum()
        {
            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.PoolSize, Is.EqualTo(MinPoolSize));
        }

        [Test]
        public void Constructor_defaultSetupErrorMessage_isEmpty()
        {
            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.SetupErrorMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task LoadSetupDataAsync_setsMaxPoolSize()
        {
            const int MaxPool = 16;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.MaxPoolSize, Is.EqualTo(MaxPool));
        }

        [Test]
        public async Task LoadSetupDataAsync_fourBackgroundMovies_setsAllFourPosters()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, PosterUrl = "http://1.jpg" },
                    new MovieCardModel { MovieId = 2, PosterUrl = "http://2.jpg" },
                    new MovieCardModel { MovieId = 3, PosterUrl = "http://3.jpg" },
                    new MovieCardModel { MovieId = 4, PosterUrl = "http://4.jpg" },
                });

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.BackgroundPoster1, Is.EqualTo("http://1.jpg"));
            Assert.That(viewModel.BackgroundPoster2, Is.EqualTo("http://2.jpg"));
            Assert.That(viewModel.BackgroundPoster3, Is.EqualTo("http://3.jpg"));
            Assert.That(viewModel.BackgroundPoster4, Is.EqualTo("http://4.jpg"));
        }

        [Test]
        public async Task LoadSetupDataAsync_moreThanFourMovies_usesFirstFour()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, PosterUrl = "http://1.jpg" },
                    new MovieCardModel { MovieId = 2, PosterUrl = "http://2.jpg" },
                    new MovieCardModel { MovieId = 3, PosterUrl = "http://3.jpg" },
                    new MovieCardModel { MovieId = 4, PosterUrl = "http://4.jpg" },
                    new MovieCardModel { MovieId = 5, PosterUrl = "http://5.jpg" },
                });

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.BackgroundPoster1, Is.EqualTo("http://1.jpg"));
            Assert.That(viewModel.BackgroundPoster4, Is.EqualTo("http://4.jpg"));
        }

        [Test]
        public async Task LoadSetupDataAsync_fewerThanFourMovies_usesFallbacks()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, PosterUrl = "http://1.jpg" },
                });

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.BackgroundPoster1, Is.EqualTo("http://1.jpg"));
            Assert.That(viewModel.BackgroundPoster2, Is.EqualTo("https://media.themoviedb.org/t/p/w600_and_h900_face/qJ2tW6WMUDux911r6m7haRef0WH.jpg"));
            Assert.That(viewModel.BackgroundPoster3, Is.EqualTo("https://media.themoviedb.org/t/p/w600_and_h900_face/q2qXg4OmJgm0qGaBYLdXzP8nHPy.jpg"));
            Assert.That(viewModel.BackgroundPoster4, Is.EqualTo("https://media.themoviedb.org/t/p/w600_and_h900_face/nrmXQ0zcZUL8jFLrakWc90IR8z9.jpg"));
        }

        [Test]
        public async Task LoadSetupDataAsync_emptyMovieList_usesAllFallbacks()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>());

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.BackgroundPoster1, Is.EqualTo("https://image.tmdb.org/t/p/w500/3bhkrj58Vtu7enYsRolD1fZdja1.jpg"));
            Assert.That(viewModel.BackgroundPoster2, Is.EqualTo("https://media.themoviedb.org/t/p/w600_and_h900_face/qJ2tW6WMUDux911r6m7haRef0WH.jpg"));
            Assert.That(viewModel.BackgroundPoster3, Is.EqualTo("https://media.themoviedb.org/t/p/w600_and_h900_face/q2qXg4OmJgm0qGaBYLdXzP8nHPy.jpg"));
            Assert.That(viewModel.BackgroundPoster4, Is.EqualTo("https://media.themoviedb.org/t/p/w600_and_h900_face/nrmXQ0zcZUL8jFLrakWc90IR8z9.jpg"));
        }

        [Test]
        public async Task LoadSetupDataAsync_repositoryThrowsOnPoolSize_setsSetupErrorMessage()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.SetupErrorMessage, Is.Not.Empty.And.Contains("DB is down"));
        }

        [Test]
        public async Task LoadSetupDataAsync_repositoryThrowsOnPool_setsSetupErrorMessage()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ThrowsAsync(new InvalidOperationException("Pool fetch failed"));

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();

            Assert.That(viewModel.SetupErrorMessage, Is.Not.Empty.And.Contains("Pool fetch failed"));
        }

        [Test]
        public void LoadSetupDataAsync_repositoryThrows_doesNotThrowToCallerSurface()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ThrowsAsync(new Exception("Boom"));

            var viewModel = this.CreateViewModel();

            Assert.DoesNotThrowAsync(async () => await viewModel.LoadSetupDataAsync());
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeBelowMinimum_setsErrorMessageContainingMinimum()
        {
            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MinPoolSize - 1;

            await viewModel.StartTournamentAsync();

            Assert.That(viewModel.SetupErrorMessage, Is.Not.Empty.And.Contains(MinPoolSize.ToString()));
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeBelowMinimum_doesNotCallService()
        {
            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MinPoolSize - 1;

            await viewModel.StartTournamentAsync();

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeBelowMinimum_doesNotRaiseTournamentStarted()
        {
            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MinPoolSize - 1;

            bool eventRaised = false;
            viewModel.TournamentStarted += (_, _) => eventRaised = true;

            await viewModel.StartTournamentAsync();

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeAboveMaximum_setsErrorMessageContainingMaximum()
        {
            const int MaxPool = 10;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MaxPool + 1;

            await viewModel.StartTournamentAsync();

            Assert.That(viewModel.SetupErrorMessage, Is.Not.Empty.And.Contains(MaxPool.ToString()));
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeAboveMaximum_doesNotCallService()
        {
            const int MaxPool = 10;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MaxPool + 1;

            await viewModel.StartTournamentAsync();

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeAboveMaximum_doesNotRaiseTournamentStarted()
        {
            const int MaxPool = 10;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MaxPool + 1;

            bool eventRaised = false;
            viewModel.TournamentStarted += (_, _) => eventRaised = true;

            await viewModel.StartTournamentAsync();

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public async Task StartTournamentAsync_validPoolSize_callsServiceWithCorrectArguments()
        {
            const int MaxPool = 16;
            const int GoodSize = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .Returns(Task.CompletedTask);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = GoodSize;

            await viewModel.StartTournamentAsync();

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(UserId, GoodSize),
                Times.Once);
        }

        [Test]
        public async Task StartTournamentAsync_validPoolSize_clearsPreviousErrorMessage()
        {
            const int MaxPool = 16;
            const int GoodSize = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .Returns(Task.CompletedTask);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = GoodSize;
            viewModel.SetupErrorMessage = "some previous error";

            await viewModel.StartTournamentAsync();

            Assert.That(viewModel.SetupErrorMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task StartTournamentAsync_validPoolSize_raisesTournamentStartedEvent()
        {
            const int MaxPool = 16;
            const int GoodSize = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .Returns(Task.CompletedTask);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = GoodSize;

            bool eventRaised = false;
            viewModel.TournamentStarted += (_, _) => eventRaised = true;

            await viewModel.StartTournamentAsync();

            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public async Task StartTournamentAsync_validPoolSize_raisesEvent_senderIsViewModel()
        {
            const int MaxPool = 16;
            const int GoodSize = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .Returns(Task.CompletedTask);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = GoodSize;

            object? capturedSender = null;
            viewModel.TournamentStarted += (sender, _) => capturedSender = sender;

            await viewModel.StartTournamentAsync();

            Assert.That(capturedSender, Is.SameAs(viewModel));
        }

        [Test]
        public async Task StartTournamentAsync_exactlyMinPoolSize_isAccepted()
        {
            const int MaxPool = 16;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, MinPoolSize))
                .Returns(Task.CompletedTask);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MinPoolSize;

            await viewModel.StartTournamentAsync();

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(UserId, MinPoolSize),
                Times.Once);
        }

        [Test]
        public async Task StartTournamentAsync_exactlyMaxPoolSize_isAccepted()
        {
            const int MaxPool = 16;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, MaxPool))
                .Returns(Task.CompletedTask);

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = MaxPool;

            await viewModel.StartTournamentAsync();

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(UserId, MaxPool),
                Times.Once);
        }

        [Test]
        public async Task StartTournamentAsync_serviceThrows_setsErrorMessageContainingExceptionMessage()
        {
            const int MaxPool = 16;
            const int GoodSize = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .ThrowsAsync(new InvalidOperationException("Tournament exploded"));

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = GoodSize;

            await viewModel.StartTournamentAsync();

            Assert.That(viewModel.SetupErrorMessage, Is.Not.Empty.And.Contains("Tournament exploded"));
        }

        [Test]
        public async Task StartTournamentAsync_serviceThrows_doesNotRaiseTournamentStarted()
        {
            const int MaxPool = 16;
            const int GoodSize = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .ThrowsAsync(new InvalidOperationException("Tournament exploded"));

            var viewModel = this.CreateViewModel();
            await viewModel.LoadSetupDataAsync();
            viewModel.PoolSize = GoodSize;

            bool eventRaised = false;
            viewModel.TournamentStarted += (_, _) => eventRaised = true;

            await viewModel.StartTournamentAsync();

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public void GetImageSource_nullString_returnsNull()
        {
            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.GetImageSource(null), Is.Null);
        }

        [Test]
        public void GetImageSource_emptyString_returnsNull()
        {
            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.GetImageSource(string.Empty), Is.Null);
        }

        [Test]
        public void GetImageSource_whitespaceString_returnsNull()
        {
            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.GetImageSource("   "), Is.Null);
        }

        [Test]
        public void GetImageSource_invalidUri_returnsNull()
        {
            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.GetImageSource("not a valid uri"), Is.Null);
        }
    }
}