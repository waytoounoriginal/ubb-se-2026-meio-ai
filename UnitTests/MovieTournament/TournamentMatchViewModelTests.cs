using System;
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
    public class TournamentMatchViewModelTests
    {
        private const int UserId = 1;

        private Mock<ITournamentLogicService> mockedTournamentLogicService = null!;
        private MatchPair defaultMatch = null!;
        private TournamentState defaultState = null!;
        private TournamentMatchViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            this.mockedTournamentLogicService = new Mock<ITournamentLogicService>();

            this.defaultMatch = new MatchPair(
                new MovieCardModel { MovieId = 1, Title = "Movie A" },
                new MovieCardModel { MovieId = 2, Title = "Movie B" });

            this.defaultState = new TournamentState();
            this.defaultState.PendingMatches.Add(this.defaultMatch);
            this.defaultState.CurrentRound = 1;

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(this.defaultMatch);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(this.defaultState);

            this.viewModel = new TournamentMatchViewModel(this.mockedTournamentLogicService.Object);
            this.viewModel.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            this.mockedTournamentLogicService = null!;
            this.defaultMatch = null!;
            this.defaultState = null!;
            this.viewModel = null!;
        }

        [Test]
        public void Initialize_validMatch_setsMovieOptionA()
        {
            Assert.That(this.viewModel.MovieOptionA, Is.SameAs(this.defaultMatch.FirstMovie));
        }

        [Test]
        public void Initialize_validMatch_setsMovieOptionB()
        {
            Assert.That(this.viewModel.MovieOptionB, Is.SameAs(this.defaultMatch.SecondMovie));
        }

        [Test]
        public void Initialize_validMatch_setsRoundDisplay()
        {
            Assert.That(this.viewModel.RoundDisplay, Is.EqualTo("Round 1"));
        }

        [Test]
        public void Initialize_nullCurrentMatch_doesNotThrowAndLeavesPropertiesDefault()
        {
            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns((MatchPair?)null);

            var viewModel = new TournamentMatchViewModel(this.mockedTournamentLogicService.Object);

            Assert.DoesNotThrow(() => viewModel.Initialize());

            Assert.That(viewModel.MovieOptionA, Is.Null);
            Assert.That(viewModel.MovieOptionB, Is.Null);
            Assert.That(viewModel.RoundDisplay, Is.EqualTo(string.Empty));
        }

        [Test]
        public void RefreshCurrentMatch_nullCurrentMatch_doesNotUpdateProperties()
        {
            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns((MatchPair?)null);

            this.viewModel.RefreshCurrentMatch();

            Assert.That(this.viewModel.MovieOptionA, Is.SameAs(this.defaultMatch.FirstMovie));
            Assert.That(this.viewModel.MovieOptionB, Is.SameAs(this.defaultMatch.SecondMovie));
        }

        [Test]
        public void RefreshCurrentMatch_newMatch_updatesMovieOptionA()
        {
            var newMatch = new MatchPair(
                new MovieCardModel { MovieId = 5, Title = "New A" },
                new MovieCardModel { MovieId = 6, Title = "New B" });

            var newState = new TournamentState();
            newState.PendingMatches.Add(newMatch);
            newState.CurrentRound = 3;

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(newMatch);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(newState);

            this.viewModel.RefreshCurrentMatch();

            Assert.That(this.viewModel.MovieOptionA, Is.SameAs(newMatch.FirstMovie));
        }

        [Test]
        public void RefreshCurrentMatch_newMatch_updatesMovieOptionB()
        {
            var newMatch = new MatchPair(
                new MovieCardModel { MovieId = 5, Title = "New A" },
                new MovieCardModel { MovieId = 6, Title = "New B" });

            var newState = new TournamentState();
            newState.PendingMatches.Add(newMatch);
            newState.CurrentRound = 3;

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(newMatch);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(newState);

            this.viewModel.RefreshCurrentMatch();

            Assert.That(this.viewModel.MovieOptionB, Is.SameAs(newMatch.SecondMovie));
        }

        [Test]
        public void RefreshCurrentMatch_newMatch_updatesRoundDisplay()
        {
            var newMatch = new MatchPair(
                new MovieCardModel { MovieId = 5, Title = "New A" },
                new MovieCardModel { MovieId = 6, Title = "New B" });

            var newState = new TournamentState();
            newState.PendingMatches.Add(newMatch);
            newState.CurrentRound = 3;

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(newMatch);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(newState);

            this.viewModel.RefreshCurrentMatch();

            Assert.That(this.viewModel.RoundDisplay, Is.EqualTo("Round 3"));
        }

        [Test]
        public async Task SelectMovieAsync_callsAdvanceWinnerWithCorrectArguments()
        {
            const int WinnerId = 1;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            await this.viewModel.SelectMovieAsync(WinnerId);

            this.mockedTournamentLogicService.Verify(
                x => x.AdvanceWinnerAsync(UserId, WinnerId),
                Times.Once);
        }

        [Test]
        public async Task SelectMovieAsync_tournamentNotComplete_refreshesCurrentMatch()
        {
            const int WinnerId = 1;

            var nextMatch = new MatchPair(
                new MovieCardModel { MovieId = 3, Title = "Next A" },
                new MovieCardModel { MovieId = 4, Title = "Next B" });

            var nextState = new TournamentState();
            nextState.PendingMatches.Add(nextMatch);
            nextState.CurrentRound = 2;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            this.mockedTournamentLogicService
                .Setup(x => x.GetCurrentMatch())
                .Returns(nextMatch);

            this.mockedTournamentLogicService
                .SetupGet(x => x.CurrentState)
                .Returns(nextState);

            await this.viewModel.SelectMovieAsync(WinnerId);

            Assert.That(this.viewModel.MovieOptionA, Is.SameAs(nextMatch.FirstMovie));
            Assert.That(this.viewModel.MovieOptionB, Is.SameAs(nextMatch.SecondMovie));
            Assert.That(this.viewModel.RoundDisplay, Is.EqualTo("Round 2"));
        }

        [Test]
        public async Task SelectMovieAsync_tournamentNotComplete_doesNotRaiseTournamentCompleteEvent()
        {
            const int WinnerId = 1;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            bool eventRaised = false;
            this.viewModel.TournamentComplete += (_, _) => eventRaised = true;

            await this.viewModel.SelectMovieAsync(WinnerId);

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public async Task SelectMovieAsync_tournamentComplete_raisesTournamentCompleteEvent()
        {
            const int WinnerId = 2;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            bool eventRaised = false;
            this.viewModel.TournamentComplete += (_, _) => eventRaised = true;

            await this.viewModel.SelectMovieAsync(WinnerId);

            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public async Task SelectMovieAsync_tournamentComplete_doesNotRefreshMatch()
        {
            const int WinnerId = 2;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            await this.viewModel.SelectMovieAsync(WinnerId);

            this.mockedTournamentLogicService.Verify(
                x => x.GetCurrentMatch(),
                Times.Once);
        }

        [Test]
        public async Task SelectMovieAsync_tournamentComplete_senderIsViewModel()
        {
            const int WinnerId = 1;

            this.mockedTournamentLogicService
                .Setup(x => x.AdvanceWinnerAsync(UserId, WinnerId))
                .Returns(Task.CompletedTask);

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            object? capturedSender = null;
            this.viewModel.TournamentComplete += (sender, _) => capturedSender = sender;

            await this.viewModel.SelectMovieAsync(WinnerId);

            Assert.That(capturedSender, Is.SameAs(this.viewModel));
        }

        [Test]
        public void GoBack_callsResetTournament()
        {
            this.viewModel.GoBack();

            this.mockedTournamentLogicService.Verify(x => x.ResetTournament(), Times.Once);
        }

        [Test]
        public void GoBack_raisesNavigateBackEvent()
        {
            bool eventRaised = false;
            this.viewModel.NavigateBack += (_, _) => eventRaised = true;

            this.viewModel.GoBack();

            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void GoBack_senderIsViewModel()
        {
            object? capturedSender = null;
            this.viewModel.NavigateBack += (sender, _) => capturedSender = sender;

            this.viewModel.GoBack();

            Assert.That(capturedSender, Is.SameAs(this.viewModel));
        }

        [Test]
        public void GoBack_resetsBeforeRaisingNavigateBackEvent()
        {
            bool resetCalledBeforeEvent = false;

            this.viewModel.NavigateBack += (_, _) =>
            {
                try
                {
                    this.mockedTournamentLogicService.Verify(x => x.ResetTournament(), Times.Once);
                    resetCalledBeforeEvent = true;
                }
                catch
                {
                    resetCalledBeforeEvent = false;
                }
            };

            this.viewModel.GoBack();

            Assert.That(resetCalledBeforeEvent, Is.True);
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
            const string InvalidUrl = "not a valid uri";

            var result = this.viewModel.GetImageSource(InvalidUrl);

            Assert.That(result, Is.Null);
        }
    }
}