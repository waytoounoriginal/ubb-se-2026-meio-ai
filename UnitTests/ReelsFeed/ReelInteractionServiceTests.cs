using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace UnitTests.ReelsFeed
{
    [TestFixture]
    public class ReelInteractionServiceTests
    {
        [Test]
        public async Task ToggleLikeAsync_wasAlreadyLiked_doesNotBoostPreference()
        {
            var likedInteraction = new UserReelInteractionModel
            {
                IsLiked = true
            };

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(likedInteraction);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            await service.ToggleLikeAsync(0, 0);

            mockedInteractionRepository.Verify(x => x.GetReelMovieIdAsync(It.IsAny<int>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.PreferenceExistsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.InsertPreferenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.UpdatePreferenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
        }

        [Test]
        public async Task ToggleLikeAsync_wasNotLikedAndMovieNotAssociated_doesNotBoostPreference()
        {
            var unlikedInteraction = new UserReelInteractionModel
            {
                IsLiked = false
            };

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(unlikedInteraction);

            mockedInteractionRepository
                .Setup(x => x.GetReelMovieIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int?)null);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            await service.ToggleLikeAsync(0, 0);

            mockedInteractionRepository.Verify(x => x.GetReelMovieIdAsync(It.IsAny<int>()), Times.Once);
            mockedPreferenceRepository.Verify(x => x.PreferenceExistsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.InsertPreferenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.UpdatePreferenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
        }

        [Test]
        public async Task ToggleLikeAsync_interactionDoesNotExist_doesNotBoostPreference()
        {
            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedInteractionRepository
                .Setup(x => x.GetReelMovieIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int?)null);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            await service.ToggleLikeAsync(0, 0);

            mockedInteractionRepository.Verify(x => x.GetReelMovieIdAsync(It.IsAny<int>()), Times.Once);
            mockedPreferenceRepository.Verify(x => x.PreferenceExistsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.InsertPreferenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.UpdatePreferenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()), Times.Never);
        }

        [Test]
        public async Task ToggleLikeAsync_wasNotLikedAndMovieAssociated_bootsPreferenceUpdates()
        {
            const int USER_ID = 1;
            const int MOVIE_ID = 100;

            var unlikedInteraction = new UserReelInteractionModel
            {
                IsLiked = false
            };

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetInteractionAsync(USER_ID, It.IsAny<int>()))
                .ReturnsAsync(unlikedInteraction);

            mockedInteractionRepository
                .Setup(x => x.GetReelMovieIdAsync(It.IsAny<int>()))
                .ReturnsAsync(MOVIE_ID);

            mockedPreferenceRepository
                .Setup(x => x.PreferenceExistsAsync(USER_ID, MOVIE_ID))
                .ReturnsAsync(true);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            await service.ToggleLikeAsync(USER_ID, 0);

            mockedInteractionRepository.Verify(x => x.GetReelMovieIdAsync(It.IsAny<int>()), Times.Once);
            mockedPreferenceRepository.Verify(x => x.PreferenceExistsAsync(USER_ID, MOVIE_ID), Times.Once);
            mockedPreferenceRepository.Verify(x => x.UpdatePreferenceAsync(USER_ID, MOVIE_ID, It.IsAny<double>()), Times.Once);
            mockedPreferenceRepository.Verify(x => x.InsertPreferenceAsync(USER_ID, MOVIE_ID, It.IsAny<double>()), Times.Never);
        }

        [Test]
        public async Task ToggleLikeAsync_wasNotLikedAndMovieAssociated_bootsPreferenceInserts()
        {
            const int USER_ID = 1;
            const int MOVIE_ID = 100;

            var unlikedInteraction = new UserReelInteractionModel
            {
                IsLiked = false
            };

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetInteractionAsync(USER_ID, It.IsAny<int>()))
                .ReturnsAsync(unlikedInteraction);

            mockedInteractionRepository
                .Setup(x => x.GetReelMovieIdAsync(It.IsAny<int>()))
                .ReturnsAsync(MOVIE_ID);

            mockedPreferenceRepository
                .Setup(x => x.PreferenceExistsAsync(USER_ID, MOVIE_ID))
                .ReturnsAsync(false);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            await service.ToggleLikeAsync(USER_ID, 0);

            mockedInteractionRepository.Verify(x => x.GetReelMovieIdAsync(It.IsAny<int>()), Times.Once);
            mockedPreferenceRepository.Verify(x => x.PreferenceExistsAsync(USER_ID, MOVIE_ID), Times.Once);
            mockedPreferenceRepository.Verify(x => x.UpdatePreferenceAsync(USER_ID, MOVIE_ID, It.IsAny<double>()), Times.Never);
            mockedPreferenceRepository.Verify(x => x.InsertPreferenceAsync(USER_ID, MOVIE_ID, It.IsAny<double>()), Times.Once);
        }

        [Test]
        public async Task RecordViewAsync_callsUpdateViewDataWithSameValues()
        {
            const int USER_ID = 7;
            const int REEL_ID = 99;
            const double WATCH_DURATION = 12.5;
            const double WATCH_PERCENTAGE = 87.3;

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            await service.RecordViewAsync(USER_ID, REEL_ID, WATCH_DURATION, WATCH_PERCENTAGE);

            mockedInteractionRepository.Verify(
                x => x.UpdateViewDataAsync(USER_ID, REEL_ID, WATCH_DURATION, WATCH_PERCENTAGE),
                Times.Once);
        }

        [Test]
        public async Task GetInteractionAsync_returnsRepositoryResult()
        {
            const int USER_ID = 10;
            const int REEL_ID = 20;

            var expected = new UserReelInteractionModel
            {
                UserId = USER_ID,
                ReelId = REEL_ID,
                IsLiked = true,
                WatchDurationSec = 30,
                WatchPercentage = 75
            };

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetInteractionAsync(USER_ID, REEL_ID))
                .ReturnsAsync(expected);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            var result = await service.GetInteractionAsync(USER_ID, REEL_ID);

            Assert.That(result, Is.SameAs(expected));
            mockedInteractionRepository.Verify(x => x.GetInteractionAsync(USER_ID, REEL_ID), Times.Once);
        }

        [Test]
        public async Task GetLikeCountAsync_returnsRepositoryCount()
        {
            const int REEL_ID = 123;
            const int EXPECTED_LIKE_COUNT = 42;

            var mockedInteractionRepository = new Mock<IInteractionRepository>();
            var mockedPreferenceRepository = new Mock<IPreferenceRepository>();

            mockedInteractionRepository
                .Setup(x => x.GetLikeCountAsync(REEL_ID))
                .ReturnsAsync(EXPECTED_LIKE_COUNT);

            var service = new ReelInteractionService(
                mockedInteractionRepository.Object,
                mockedPreferenceRepository.Object
            );

            var result = await service.GetLikeCountAsync(REEL_ID);

            Assert.That(result, Is.EqualTo(EXPECTED_LIKE_COUNT));
            mockedInteractionRepository.Verify(x => x.GetLikeCountAsync(REEL_ID), Times.Once);
        }

    }
}
