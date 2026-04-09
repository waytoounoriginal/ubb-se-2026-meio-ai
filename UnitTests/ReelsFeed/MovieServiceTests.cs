using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Core.Repositories;
using ubb_se_2026_meio_ai.Core.Services;

namespace UnitTests.Core
{
    [TestFixture]
    public class MovieServiceTests
    {
        [Test]
        public async Task SearchTop10MoviesAsync_emptyString_returnsEmptyListAndDoesNotCallRepo()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(string.Empty);

            // Assert
            Assert.That(result, Is.Empty);
            mockedRepository.Verify(x => x.SearchTop10MoviesAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_whitespaceString_returnsEmptyListAndDoesNotCallRepo()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync("   ");

            // Assert
            Assert.That(result, Is.Empty);
            mockedRepository.Verify(x => x.SearchTop10MoviesAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_nullString_returnsEmptyListAndDoesNotCallRepo()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(null!);

            // Assert
            Assert.That(result, Is.Empty);
            mockedRepository.Verify(x => x.SearchTop10MoviesAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_validString_callsRepoAndReturnsResults()
        {
            // Arrange
            const string SEARCH_TERM = "Batman";
            var expectedResults = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Batman Begins" },
                new MovieCardModel { MovieId = 2, Title = "The Dark Knight" }
            };

            var mockedRepository = new Mock<IMovieRepository>();

            mockedRepository
                .Setup(x => x.SearchTop10MoviesAsync(SEARCH_TERM))
                .ReturnsAsync(expectedResults);

            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(SEARCH_TERM);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Title, Is.EqualTo("Batman Begins"));

            // Verify the repository was called exactly once with the correct search term
            mockedRepository.Verify(x => x.SearchTop10MoviesAsync(SEARCH_TERM), Times.Once);
        }
    }
}