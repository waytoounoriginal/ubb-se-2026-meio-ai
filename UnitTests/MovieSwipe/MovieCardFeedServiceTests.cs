using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Services;

namespace UnitTests.MovieSwipe
{
    [TestFixture]
    public class MovieCardFeedServiceTests
    {
        private Mock<IPreferenceRepository> mockedRepository = null!;
        private MovieCardFeedService service = null!;

        private const int UserId = 1;
        private const int Count = 10;

        [SetUp]
        public void SetUp()
        {
            this.mockedRepository = new Mock<IPreferenceRepository>();
            this.service = new MovieCardFeedService(this.mockedRepository.Object);
        }

        [Test]
        public async Task FetchMovieFeedAsync_CallsRepository_ReturnsData()
        {
            // Arrange
            var expected = new List<MovieCardModel> { new MovieCardModel { MovieId = 1 } };
            this.mockedRepository
                .Setup(x => x.GetMovieFeedAsync(UserId, Count))
                .ReturnsAsync(expected);

            // Act
            var result = await this.service.FetchMovieFeedAsync(UserId, Count);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
            this.mockedRepository.Verify(x => x.GetMovieFeedAsync(UserId, Count), Times.Once);
        }
    }
}