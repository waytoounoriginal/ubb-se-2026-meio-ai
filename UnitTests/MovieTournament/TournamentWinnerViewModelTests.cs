using System;
using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace UnitTests.MovieTournament
{
    [TestFixture]
    public class TournamentWinnerViewModelTests
    {
        private Mock<ITournamentLogicService> mockedTournamentLogicService = null!;

        [SetUp]
        public void SetUp()
        {
            this.mockedTournamentLogicService = new Mock<ITournamentLogicService>();

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockedTournamentLogicService = null!;
        }

        private TournamentWinnerViewModel CreateViewModel()
        {
            return new TournamentWinnerViewModel(this.mockedTournamentLogicService.Object);
        }

        [Test]
        public void Constructor_tournamentNotComplete_winnerMovieIsNull()
        {
            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.WinnerMovie, Is.Null);
        }

        [Test]
        public void Constructor_tournamentNotComplete_doesNotCallGetFinalWinner()
        {
            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(false);

            this.CreateViewModel();

            this.mockedTournamentLogicService.Verify(
                x => x.GetFinalWinner(),
                Times.Never);
        }

        [Test]
        public void Constructor_tournamentComplete_setsWinnerMovie()
        {
            var winner = new MovieCardModel { MovieId = 1, Title = "The Winner" };

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            this.mockedTournamentLogicService
                .Setup(x => x.GetFinalWinner())
                .Returns(winner);

            var viewModel = this.CreateViewModel();

            Assert.That(viewModel.WinnerMovie, Is.SameAs(winner));
        }

        [Test]
        public void Constructor_tournamentComplete_callsGetFinalWinnerOnce()
        {
            var winner = new MovieCardModel { MovieId = 1, Title = "The Winner" };

            this.mockedTournamentLogicService
                .Setup(x => x.IsTournamentComplete())
                .Returns(true);

            this.mockedTournamentLogicService
                .Setup(x => x.GetFinalWinner())
                .Returns(winner);

            this.CreateViewModel();

            this.mockedTournamentLogicService.Verify(
                x => x.GetFinalWinner(),
                Times.Once);
        }

        [Test]
        public void StartAnotherTournament_callsResetTournament()
        {
            var viewModel = this.CreateViewModel();

            viewModel.StartAnotherTournament();

            this.mockedTournamentLogicService.Verify(x => x.ResetTournament(), Times.Once);
        }

        [Test]
        public void StartAnotherTournament_raisesNavigateToSetupEvent()
        {
            var viewModel = this.CreateViewModel();

            bool eventRaised = false;
            viewModel.NavigateToSetup += (_, _) => eventRaised = true;

            viewModel.StartAnotherTournament();

            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void StartAnotherTournament_senderIsViewModel()
        {
            var viewModel = this.CreateViewModel();

            object? capturedSender = null;
            viewModel.NavigateToSetup += (sender, _) => capturedSender = sender;

            viewModel.StartAnotherTournament();

            Assert.That(capturedSender, Is.SameAs(viewModel));
        }

        [Test]
        public void StartAnotherTournament_resetsBeforeRaisingEvent()
        {
            var viewModel = this.CreateViewModel();

            bool resetCalledBeforeEvent = false;
            viewModel.NavigateToSetup += (_, _) =>
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

            viewModel.StartAnotherTournament();

            Assert.That(resetCalledBeforeEvent, Is.True);
        }

        [Test]
        public void StartAnotherTournament_noSubscribers_doesNotThrow()
        {
            var viewModel = this.CreateViewModel();

            Assert.DoesNotThrow(() => viewModel.StartAnotherTournament());
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