using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Services;

namespace UnitTests.MovieSwipe
{
    [TestFixture]
    public class SwipeServiceTests
    {
        private Mock<IPreferenceRepository> mockedRepository = null!;
        private SwipeService service = null!;

        private const int UserId = 1;
        private const int MovieId = 99;

        [SetUp]
        public void SetUp()
        {
            this.mockedRepository = new Mock<IPreferenceRepository>();
            this.service = new SwipeService(this.mockedRepository.Object);
        }

        [Test]
        public async Task UpdatePreferenceScoreAsync_IsLikedTrue_UpsertsCorrectScore()
        {
            await this.service.UpdatePreferenceScoreAsync(UserId, MovieId, true);

            this.mockedRepository.Verify(repository => repository.UpsertPreferenceAsync(It.Is<UserMoviePreferenceModel>(preference =>
                preference.UserId == UserId && preference.Score == SwipeService.LikeDelta && preference.ChangeFromPreviousValue == 1)), Times.Once);
        }

        [Test]
        public async Task UpdatePreferenceScoreAsync_IsLikedFalse_UpsertsCorrectScore()
        {
            await this.service.UpdatePreferenceScoreAsync(UserId, MovieId, false);

            this.mockedRepository.Verify(repository => repository.UpsertPreferenceAsync(It.Is<UserMoviePreferenceModel>(preference =>
                preference.UserId == UserId && preference.Score == SwipeService.SkipDelta && preference.ChangeFromPreviousValue == -1)), Times.Once);
        }

        [Test]
        public async Task GetMovieFeedAsync_DelegatesToRepository()
        {
            var movieFeed = new List<MovieCardModel>();
            this.mockedRepository.Setup(repository => repository.GetMovieFeedAsync(UserId, 5)).ReturnsAsync(movieFeed);

            var result = await this.service.GetMovieFeedAsync(UserId, 5);

            Assert.That(result, Is.EqualTo(movieFeed));
        }
    }
}