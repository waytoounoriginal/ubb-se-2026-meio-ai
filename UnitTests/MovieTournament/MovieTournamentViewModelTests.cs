using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace UnitTests.MovieTournament
{
    [TestFixture]
    public class MovieTournamentViewModelTests
    {
        private const int UserId = 1;
        private const int MinPoolSize = 4;
        private const int BackgroundCount = 4;

        private Mock<ITournamentLogicService> mockedTournamentLogicService = null!;
        private Mock<IMovieTournamentRepository> mockedTournamentRepository = null!;
        private MovieTournamentViewModel viewModel = null!;

        [SetUp]
        public async Task SetUp()
        {
            this.mockedTournamentLogicService = new Mock<ITournamentLogicService>();
            this.mockedTournamentRepository = new Mock<IMovieTournamentRepository>();

            this.mockedTournamentLogicService
                .SetupGet(x => x.IsTournamentActive)
                .Returns(false);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(0);

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>());

            this.viewModel = new MovieTournamentViewModel(
                this.mockedTournamentLogicService.Object,
                this.mockedTournamentRepository.Object);

            await this.viewModel.InitializeAsync();
        }

        [TearDown]
        public void TearDown()
        {
            this.mockedTournamentLogicService = null!;
            this.mockedTournamentRepository = null!;
            this.viewModel = null!;
        }

        [Test]
        public async Task InitializeAsync_noActiveTournament_setsViewStateToSetup()
        {
            var viewModel = new MovieTournamentViewModel(
                this.mockedTournamentLogicService.Object,
                this.mockedTournamentRepository.Object);

            await viewModel.InitializeAsync();

            Assert.That(viewModel.CurrentViewState, Is.EqualTo(0));
        }

        [Test]
        public async Task InitializeAsync_tournamentIsActive_setsViewStateToMatchAndUpdatesDisplay()
        {
            var match = new MatchPair(
                new MovieCardModel { MovieId = 1, Title = "Match 1" },
                new MovieCardModel { MovieId = 2, Title = "Match 2" });

            var state = new TournamentState();
            state.PendingMatches.Add(match);
            state.CurrentRound = 2;

            this.mockedTournamentLogicService
                .SetupGet(x => x.IsTournamentActive)
                .Returns(true);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(state);

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(match);

            var viewModel = new MovieTournamentViewModel(
                this.mockedTournamentLogicService.Object,
                this.mockedTournamentRepository.Object);

            await viewModel.InitializeAsync();

            Assert.That(viewModel.CurrentViewState, Is.EqualTo(1));
            Assert.That(viewModel.MovieOptionA, Is.SameAs(match.FirstMovie));
            Assert.That(viewModel.MovieOptionB, Is.SameAs(match.SecondMovie));
            Assert.That(viewModel.RoundDisplay, Is.EqualTo("Round 2"));
        }

        [Test]
        public async Task InitializeAsync_tournamentIsComplete_setsViewStateToWinnerAndSetsWinnerMovie()
        {
            var winner = new MovieCardModel { MovieId = 1, Title = "Final Winner" };

            this.mockedTournamentLogicService
                .SetupGet(x => x.IsTournamentActive)
                .Returns(false);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            this.mockedTournamentLogicService
                .Setup(x => x.GetFinalWinner())
                .Returns(winner);

            var viewModel = new MovieTournamentViewModel(
                this.mockedTournamentLogicService.Object,
                this.mockedTournamentRepository.Object);

            await viewModel.InitializeAsync();

            Assert.That(viewModel.CurrentViewState, Is.EqualTo(2));
            Assert.That(viewModel.WinnerMovie, Is.SameAs(winner));
        }

        [Test]
        public async Task LoadSetupDataAsync_loadsMaxPoolSizeAndBackgroundPosters()
        {
            const int MaxPool = 16;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, Title = "BG 1", PosterUrl = "http://1.jpg" },
                    new MovieCardModel { MovieId = 2, Title = "BG 2", PosterUrl = "http://2.jpg" },
                    new MovieCardModel { MovieId = 3, Title = "BG 3", PosterUrl = "http://3.jpg" },
                    new MovieCardModel { MovieId = 4, Title = "BG 4", PosterUrl = "http://4.jpg" },
                });

            await this.viewModel.LoadSetupDataAsync();

            Assert.That(this.viewModel.MaxPoolSize, Is.EqualTo(MaxPool));
            Assert.That(this.viewModel.BackgroundPoster1, Is.EqualTo("http://1.jpg"));
            Assert.That(this.viewModel.BackgroundPoster2, Is.EqualTo("http://2.jpg"));
            Assert.That(this.viewModel.BackgroundPoster3, Is.EqualTo("http://3.jpg"));
            Assert.That(this.viewModel.BackgroundPoster4, Is.EqualTo("http://4.jpg"));
        }

        [Test]
        public async Task LoadSetupDataAsync_notEnoughBackgroundMovies_usesFallbacksForMissingSlots()
        {
            const int MaxPool = 8;

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(MaxPool);

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, Title = "BG 1", PosterUrl = "http://1.jpg" },
                    new MovieCardModel { MovieId = 2, Title = "BG 2", PosterUrl = "http://2.jpg" },
                });

            await this.viewModel.LoadSetupDataAsync();

            Assert.That(this.viewModel.MaxPoolSize, Is.EqualTo(MaxPool));
            Assert.That(this.viewModel.BackgroundPoster1, Is.EqualTo("http://1.jpg"));
            Assert.That(this.viewModel.BackgroundPoster2, Is.EqualTo("http://2.jpg"));
            Assert.That(this.viewModel.BackgroundPoster3, Is.Not.Null.And.Contains("themoviedb.org"));
            Assert.That(this.viewModel.BackgroundPoster4, Is.Not.Null.And.Contains("themoviedb.org"));
        }

        [Test]
        public async Task LoadSetupDataAsync_repositoryThrows_setsSetupErrorMessage()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ThrowsAsync(new InvalidOperationException("Loading failed"));

            await this.viewModel.LoadSetupDataAsync();

            Assert.That(this.viewModel.SetupErrorMessage, Is.Not.Null.And.Contains("Loading failed"));
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeTooSmall_setsErrorMessageAndDoesNotStartTournament()
        {
            const int SmallSize = 3;

            this.viewModel.PoolSize = SmallSize;

            await this.viewModel.StartTournamentAsync();

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(0));
            Assert.That(this.viewModel.SetupErrorMessage, Is.Not.Empty);
            Assert.That(this.viewModel.SetupErrorMessage, Does.Contain(MinPoolSize.ToString()));

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(UserId, SmallSize),
                Times.Never);
        }

        [Test]
        public async Task StartTournamentAsync_poolSizeTooLarge_setsErrorMessageAndDoesNotStartTournament()
        {
            const int LargeSize = 20;

            this.viewModel.MaxPoolSize = 10;
            this.viewModel.PoolSize = LargeSize;

            await this.viewModel.StartTournamentAsync();

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(0));
            Assert.That(this.viewModel.SetupErrorMessage, Is.Not.Empty);
            Assert.That(this.viewModel.SetupErrorMessage, Does.Contain("10"));

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(UserId, LargeSize),
                Times.Never);
        }

        [Test]
        public async Task StartTournamentAsync_validPoolSize_callsServiceAndTransitionsToMatchView()
        {
            const int GoodSize = 8;

            this.viewModel.PoolSize = GoodSize;
            this.viewModel.MaxPoolSize = 16;

            var match = new MatchPair(
                new MovieCardModel { MovieId = 1, Title = "Movie A" },
                new MovieCardModel { MovieId = 2, Title = "Movie B" });

            var state = new TournamentState();
            state.PendingMatches.Add(match);
            state.CurrentRound = 1;

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, GoodSize))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .SetupGet(x => x.IsTournamentActive)
                .Returns(true);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(state);

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(match);

            await this.viewModel.StartTournamentAsync();

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(1));
            Assert.That(this.viewModel.SetupErrorMessage, Is.Empty);
            Assert.That(this.viewModel.MovieOptionA, Is.SameAs(match.FirstMovie));
            Assert.That(this.viewModel.MovieOptionB, Is.SameAs(match.SecondMovie));
            Assert.That(this.viewModel.RoundDisplay, Is.EqualTo("Round 1"));

            this.mockedTournamentLogicService.Verify(
                x => x.StartTournamentAsync(UserId, GoodSize),
                Times.Once);
        }

        [Test]
        public async Task StartTournamentAsync_serviceThrows_setsErrorMessageAndStaysInSetupView()
        {
            const int BadSize = 8;

            this.viewModel.PoolSize = BadSize;
            this.viewModel.MaxPoolSize = 16;

            this.mockedTournamentLogicService
                .Setup(x => x.StartTournamentAsync(UserId, BadSize))
                .ThrowsAsync(new InvalidOperationException("Service boom"));

            await this.viewModel.StartTournamentAsync();

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(0));
            Assert.That(this.viewModel.SetupErrorMessage, Is.Not.Empty);
            Assert.That(this.viewModel.SetupErrorMessage, Does.Contain("Service boom"));
        }

        [Test]
        public async Task SelectMovieAsync_tournamentComplete_setsWinnerAndTransitionsToWinnerView()
        {
            const int WinnerId = 1;

            var winner = new MovieCardModel { MovieId = WinnerId, Title = "Winner" };

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            this.mockedTournamentLogicService
                .Setup(x => x.GetFinalWinner())
                .Returns(winner);

            this.viewModel.CurrentViewState = 1;

            await this.viewModel.SelectMovieAsync(WinnerId);

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(2));
            Assert.That(this.viewModel.WinnerMovie, Is.SameAs(winner));
        }

        [Test]
        public async Task SelectMovieAsync_tournamentNotComplete_updatesCurrentMatchDisplay()
        {
            const int WinnerId = 1;

            var nextMatch = new MatchPair(
                new MovieCardModel { MovieId = 3, Title = "Next A" },
                new MovieCardModel { MovieId = 4, Title = "Next B" });

            var state = new TournamentState();
            state.PendingMatches.Add(nextMatch);
            state.CurrentRound = 2;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(state);

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(nextMatch);

            this.viewModel.CurrentViewState = 1;

            await this.viewModel.SelectMovieAsync(WinnerId);

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(1));
            Assert.That(this.viewModel.MovieOptionA, Is.SameAs(nextMatch.FirstMovie));
            Assert.That(this.viewModel.MovieOptionB, Is.SameAs(nextMatch.SecondMovie));
            Assert.That(this.viewModel.RoundDisplay, Is.EqualTo("Round 2"));
        }

        [Test]
        public async Task SelectMovieAsync_getCurrentMatchReturnsNull_doesNotThrowAndKeepsMatchViewState()
        {
            const int WinnerId = 1;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns((MatchPair?)null);

            this.viewModel.CurrentViewState = 1;

            Assert.DoesNotThrowAsync(async () => await this.viewModel.SelectMovieAsync(WinnerId));
            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(1));
        }

        [Test]
        public void ResetTournament_callsServiceAndReturnsToSetupView()
        {
            this.viewModel.CurrentViewState = 1;

            this.viewModel.ResetTournament();

            Assert.That(this.viewModel.CurrentViewState, Is.EqualTo(0));
            this.mockedTournamentLogicService.Verify(x => x.ResetTournament(), Times.Once);
        }

        [Test]
        public async Task ResetTournament_triggersLoadSetupDataAsync()
        {
            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolSizeAsync(UserId))
                .ReturnsAsync(9);

            this.mockedTournamentRepository
                .Setup(x => x.GetTournamentPoolAsync(UserId, BackgroundCount))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, PosterUrl = "http://1.jpg" },
                    new MovieCardModel { MovieId = 2, PosterUrl = "http://2.jpg" },
                    new MovieCardModel { MovieId = 3, PosterUrl = "http://3.jpg" },
                    new MovieCardModel { MovieId = 4, PosterUrl = "http://4.jpg" },
                });

            this.viewModel.ResetTournament();

            await Task.Delay(50);

            Assert.That(this.viewModel.MaxPoolSize, Is.EqualTo(9));
            Assert.That(this.viewModel.BackgroundPoster1, Is.EqualTo("http://1.jpg"));
        }

        [Test]
        public void GetImageSource_nullString_returnsNull()
        {
            var result = this.viewModel.GetImageSource(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetImageSource_emptyString_returnsNull()
        {
            var result = this.viewModel.GetImageSource(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetImageSource_whitespaceString_returnsNull()
        {
            var result = this.viewModel.GetImageSource("   ");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetImageSource_invalidUri_returnsNull()
        {
            const string InvalidUrl = "not a uri";

            var result = this.viewModel.GetImageSource(InvalidUrl);

            Assert.That(result, Is.Null);
        }
    }
}