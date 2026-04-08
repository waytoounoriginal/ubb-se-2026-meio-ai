using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;

namespace UnitTests.MovieTournament
{
    [TestFixture]
    public class TournamentLogicServiceTests
    {
        [Test]
        public void CurrentState_noActiveTournament_throwsInvalidOperationException()
        {
            var mockedRepository = new Mock<IMovieTournamentRepository>();
            var service = new TournamentLogicService(mockedRepository.Object);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = service.CurrentState;
            });
        }

        [Test]
        public void IsTournamentActive_noActiveTournament_returnsFalse()
        {
            var mockedRepository = new Mock<IMovieTournamentRepository>();
            var service = new TournamentLogicService(mockedRepository.Object);

            var result = service.IsTournamentActive;

            Assert.That(result, Is.False);
        }

        [Test]
        public void StartTournamentAsync_poolSizeLessThanMinimum_throwsArgumentException()
        {
            const int USER_ID = 1;
            const int INVALID_POOL_SIZE = 3;

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            var service = new TournamentLogicService(mockedRepository.Object);

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.StartTournamentAsync(USER_ID, INVALID_POOL_SIZE));
        }

        [Test]
        public void StartTournamentAsync_notEnoughMovies_throwsInvalidOperationException()
        {
            const int USER_ID = 1;
            const int POOL_SIZE = 4;

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(new List<MovieCardModel>
                {
                    new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                    new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                });

            var service = new TournamentLogicService(mockedRepository.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.StartTournamentAsync(USER_ID, POOL_SIZE));
        }

        [Test]
        public async Task StartTournamentAsync_validPool_startsTournamentAndCreatesMatches()
        {
            const int USER_ID = 1;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);

            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            Assert.That(service.IsTournamentActive, Is.True);
            Assert.That(service.CurrentState, Is.Not.Null);
            Assert.That(service.CurrentState.PendingMatches.Count, Is.EqualTo(2));
            Assert.That(service.CurrentState.CompletedMatches.Count, Is.EqualTo(0));
            Assert.That(service.CurrentState.CurrentRoundWinners.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task StartTournamentAsync_validPool_callsRepositoryOnce()
        {
            const int USER_ID = 7;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            await new TournamentLogicService(mockedRepository.Object)
                .StartTournamentAsync(USER_ID, POOL_SIZE);

            mockedRepository.Verify(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE), Times.Once);
        }

        [Test]
        public void AdvanceWinnerAsync_noActiveTournament_throwsInvalidOperationException()
        {
            const int USER_ID = 1;
            const int WINNER_ID = 1;

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            var service = new TournamentLogicService(mockedRepository.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.AdvanceWinnerAsync(USER_ID, WINNER_ID));
        }

        [Test]
        public async Task AdvanceWinnerAsync_winnerNotInCurrentMatch_throwsArgumentException()
        {
            const int USER_ID = 1;
            const int POOL_SIZE = 4;
            const int INVALID_WINNER_ID = 999;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);
            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.AdvanceWinnerAsync(USER_ID, INVALID_WINNER_ID));
        }

        [Test]
        public async Task GetCurrentMatch_tournamentStarted_returnsFirstPendingMatch()
        {
            const int USER_ID = 1;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);
            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            var result = service.GetCurrentMatch();

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.FirstMovie, Is.Not.Null);
        }

        [Test]
        public async Task IsTournamentComplete_tournamentJustStarted_returnsFalse()
        {
            const int USER_ID = 1;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);
            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            var result = service.IsTournamentComplete();

            Assert.That(result, Is.False);
        }

        [Test]
        public void GetFinalWinner_tournamentNotComplete_throwsInvalidOperationException()
        {
            var mockedRepository = new Mock<IMovieTournamentRepository>();
            var service = new TournamentLogicService(mockedRepository.Object);

            Assert.Throws<InvalidOperationException>(() => service.GetFinalWinner());
        }

        [Test]
        public async Task AdvanceWinnerAsync_finalMatchCompleted_boostsWinnerScoreOnce()
        {
            const int USER_ID = 10;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);
            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            while (!service.IsTournamentComplete())
            {
                var currentMatch = service.GetCurrentMatch();
                Assert.That(currentMatch, Is.Not.Null);

                await service.AdvanceWinnerAsync(USER_ID, currentMatch!.FirstMovie.MovieId);
            }

            var finalWinner = service.GetFinalWinner();

            mockedRepository.Verify(
                x => x.BoostMovieScoreAsync(USER_ID, finalWinner.MovieId, 2.0),
                Times.Once);
        }

        [Test]
        public async Task GetFinalWinner_tournamentCompleted_returnsWinner()
        {
            const int USER_ID = 10;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);
            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            while (!service.IsTournamentComplete())
            {
                var currentMatch = service.GetCurrentMatch();
                await service.AdvanceWinnerAsync(USER_ID, currentMatch!.FirstMovie.MovieId);
            }

            var result = service.GetFinalWinner();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MovieId, Is.GreaterThan(0));
        }

        [Test]
        public async Task StartTournamentAsync_oddPoolSize_createsByeMatchAndAdvancesOneMovie()
        {
            const int USER_ID = 3;
            const int POOL_SIZE = 5;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
                new MovieCardModel { MovieId = 5, Title = "Movie 5" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);

            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            Assert.That(service.CurrentState.PendingMatches.Count, Is.EqualTo(2));
            Assert.That(service.CurrentState.CompletedMatches.Count, Is.EqualTo(1));
            Assert.That(service.CurrentState.CurrentRoundWinners.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task ResetTournament_afterStart_clearsTournamentState()
        {
            const int USER_ID = 1;
            const int POOL_SIZE = 4;

            var movies = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Movie 1" },
                new MovieCardModel { MovieId = 2, Title = "Movie 2" },
                new MovieCardModel { MovieId = 3, Title = "Movie 3" },
                new MovieCardModel { MovieId = 4, Title = "Movie 4" },
            };

            var mockedRepository = new Mock<IMovieTournamentRepository>();
            mockedRepository
                .Setup(x => x.GetTournamentPoolAsync(USER_ID, POOL_SIZE))
                .ReturnsAsync(movies);

            var service = new TournamentLogicService(mockedRepository.Object);
            await service.StartTournamentAsync(USER_ID, POOL_SIZE);

            service.ResetTournament();

            Assert.That(service.IsTournamentActive, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = service.CurrentState;
            });
        }
    }
}