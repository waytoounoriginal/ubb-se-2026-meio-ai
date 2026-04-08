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

            this.mockedRepository.Verify(x => x.UpsertPreferenceAsync(It.Is<UserMoviePreferenceModel>(p =>
                p.UserId == UserId && p.Score == SwipeService.LikeDelta && p.ChangeFromPreviousValue == 1)), Times.Once);
        }

        [Test]
        public async Task UpdatePreferenceScoreAsync_IsLikedFalse_UpsertsCorrectScore()
        {
            await this.service.UpdatePreferenceScoreAsync(UserId, MovieId, false);

            this.mockedRepository.Verify(x => x.UpsertPreferenceAsync(It.Is<UserMoviePreferenceModel>(p =>
                p.UserId == UserId && p.Score == SwipeService.SkipDelta && p.ChangeFromPreviousValue == -1)), Times.Once);
        }

        [Test]
        public async Task GetMovieFeedAsync_DelegatesToRepository()
        {
            var list = new List<MovieCardModel>();
            this.mockedRepository.Setup(x => x.GetMovieFeedAsync(UserId, 5)).ReturnsAsync(list);

            var result = await this.service.GetMovieFeedAsync(UserId, 5);

            Assert.That(result, Is.EqualTo(list));
        }
    }
}