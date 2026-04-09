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
        public async Task SearchTop10MoviesAsync_emptyString_returnsEmptyList()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(string.Empty);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_emptyString_doesNotCallRepo()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            await service.SearchTop10MoviesAsync(string.Empty);

            // Assert
            mockedRepository.Verify(mock => mock.SearchTop10MoviesAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_whitespaceString_returnsEmptyList()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync("   ");

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_whitespaceString_doesNotCallRepo()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            await service.SearchTop10MoviesAsync("   ");

            // Assert
            mockedRepository.Verify(mock => mock.SearchTop10MoviesAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_nullString_returnsEmptyList()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(null!);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_nullString_doesNotCallRepo()
        {
            // Arrange
            var mockedRepository = new Mock<IMovieRepository>();
            var service = new MovieService(mockedRepository.Object);

            // Act
            await service.SearchTop10MoviesAsync(null!);

            // Assert
            mockedRepository.Verify(mock => mock.SearchTop10MoviesAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_validString_returnsNonNullList()
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
                .Setup(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM))
                .ReturnsAsync(expectedResults);

            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(SEARCH_TERM);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SearchTop10MoviesAsync_validString_returnsExpectedCount()
        {
            // Arrange
            const string SEARCH_TERM = "Batman";
            var expectedResults = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Batman Begins" },
                new MovieCardModel { MovieId = 2, Title = "The Dark Knight" }
            };

            var mockedRepository = new Mock<IMovieRepository>();
            mockedRepository.Setup(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM)).ReturnsAsync(expectedResults);
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(SEARCH_TERM);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SearchTop10MoviesAsync_validString_returnsExpectedFirstTitle()
        {
            // Arrange
            const string SEARCH_TERM = "Batman";
            var expectedResults = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Batman Begins" },
                new MovieCardModel { MovieId = 2, Title = "The Dark Knight" }
            };

            var mockedRepository = new Mock<IMovieRepository>();
            mockedRepository.Setup(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM)).ReturnsAsync(expectedResults);
            var service = new MovieService(mockedRepository.Object);

            // Act
            var result = await service.SearchTop10MoviesAsync(SEARCH_TERM);

            // Assert
            Assert.That(result[0].Title, Is.EqualTo("Batman Begins"));
        }

        [Test]
        public async Task SearchTop10MoviesAsync_validString_callsRepoOnce()
        {
            // Arrange
            const string SEARCH_TERM = "Batman";
            var expectedResults = new List<MovieCardModel>
            {
                new MovieCardModel { MovieId = 1, Title = "Batman Begins" },
                new MovieCardModel { MovieId = 2, Title = "The Dark Knight" }
            };

            var mockedRepository = new Mock<IMovieRepository>();
            mockedRepository.Setup(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM)).ReturnsAsync(expectedResults);
            var service = new MovieService(mockedRepository.Object);

            // Act
            await service.SearchTop10MoviesAsync(SEARCH_TERM);

            // Assert
            mockedRepository.Verify(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM), Times.Once);
        }
    }
}
