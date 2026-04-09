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
		public async Task FetchMovieFeedAsync_ReturnsData()
		{
			var expected = new List<MovieCardModel> { new MovieCardModel { MovieId = 1 } };
			this.mockedRepository
				.Setup(mock => mock.GetMovieFeedAsync(UserId, Count))
				.ReturnsAsync(expected);

			var result = await this.service.FetchMovieFeedAsync(UserId, Count);

			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public async Task FetchMovieFeedAsync_CallsRepository()
		{
			var expected = new List<MovieCardModel> { new MovieCardModel { MovieId = 1 } };
			this.mockedRepository
				.Setup(mock => mock.GetMovieFeedAsync(UserId, Count))
				.ReturnsAsync(expected);

			await this.service.FetchMovieFeedAsync(UserId, Count);

			this.mockedRepository.Verify(mock => mock.GetMovieFeedAsync(UserId, Count), Times.Once);
		}
	}
}
