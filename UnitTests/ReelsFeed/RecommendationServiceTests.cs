using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace UnitTests.ReelsFeed
{
    [TestFixture]
    public class RecommendationServiceTests
    {
        [Test]
        public async Task GetRecommendedReelsAsync_userHasPreferences_returnsPreferenceRankedReels()
        {
            const int USER_ID = 7;
            const int COUNT = 3;
            const int FIRST_REEL_ID = 11;
            const int FIRST_MOVIE_ID = 100;
            const int SECOND_REEL_ID = 12;
            const int SECOND_MOVIE_ID = 101;
            const int THIRD_REEL_ID = 13;
            const int THIRD_MOVIE_ID = 102;

            var firstReelByRecencyForSameScore = new ReelModel { ReelId = FIRST_REEL_ID, MovieId = FIRST_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 6) };
            var secondReelByRecencyForSameScore = new ReelModel { ReelId = SECOND_REEL_ID, MovieId = SECOND_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 5) };
            var lowerPreferenceReel = new ReelModel { ReelId = THIRD_REEL_ID, MovieId = THIRD_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 7) };

            var allReels = new List<ReelModel>
            {
                lowerPreferenceReel,
                secondReelByRecencyForSameScore,
                firstReelByRecencyForSameScore,
            };

            var preferenceScoresByMovieId = new Dictionary<int, double>
            {
                [FIRST_MOVIE_ID] = 0.9,
                [SECOND_MOVIE_ID] = 0.9,
                [THIRD_MOVIE_ID] = 0.3,
            };

            var mockedRecommendationRepository = new Mock<IRecommendationRepository>();

            mockedRecommendationRepository
                .Setup(x => x.UserHasPreferencesAsync(USER_ID))
                .ReturnsAsync(true);

            mockedRecommendationRepository
                .Setup(x => x.GetAllReelsAsync())
                .ReturnsAsync(allReels);

            mockedRecommendationRepository
                .Setup(x => x.GetUserPreferenceScoresAsync(USER_ID))
                .ReturnsAsync(preferenceScoresByMovieId);

            var service = new RecommendationService(mockedRecommendationRepository.Object);

            var recommendedReels = await service.GetRecommendedReelsAsync(USER_ID, COUNT);

            Assert.That(recommendedReels.Select(x => x.ReelId), Is.EqualTo(new[] { FIRST_REEL_ID, SECOND_REEL_ID, THIRD_REEL_ID }));
        }

        [Test]
        public async Task GetRecommendedReelsAsync_userHasNoPreferences_returnsLikeRankedReels()
        {
            const int USER_ID = 9;
            const int COUNT = 3;
            const int FIRST_REEL_ID = 21;
            const int FIRST_MOVIE_ID = 201;
            const int SECOND_REEL_ID = 22;
            const int SECOND_MOVIE_ID = 202;
            const int THIRD_REEL_ID = 23;
            const int THIRD_MOVIE_ID = 203;

            var firstReelByRecencyForSameLikeCount = new ReelModel { ReelId = FIRST_REEL_ID, MovieId = FIRST_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 6) };
            var secondReelByRecencyForSameLikeCount = new ReelModel { ReelId = SECOND_REEL_ID, MovieId = SECOND_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 5) };
            var lowerLikeCountReel = new ReelModel { ReelId = THIRD_REEL_ID, MovieId = THIRD_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 7) };

            var allReels = new List<ReelModel>
            {
                lowerLikeCountReel,
                secondReelByRecencyForSameLikeCount,
                firstReelByRecencyForSameLikeCount,
            };

            var recentInteractions = new List<UserReelInteractionModel>
            {
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = FIRST_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = SECOND_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
                new () { ReelId = THIRD_REEL_ID, IsLiked = true, ViewedAt = DateTime.UtcNow },
            };

            var mockedRecommendationRepository = new Mock<IRecommendationRepository>();

            mockedRecommendationRepository
                .Setup(x => x.UserHasPreferencesAsync(USER_ID))
                .ReturnsAsync(false);

            mockedRecommendationRepository
                .Setup(x => x.GetAllReelsAsync())
                .ReturnsAsync(allReels);

            mockedRecommendationRepository
                .Setup(x => x.GetLikesWithinDaysAsync(It.IsAny<int>()))
                .ReturnsAsync(recentInteractions);

            var service = new RecommendationService(mockedRecommendationRepository.Object);

            var recommendedReels = await service.GetRecommendedReelsAsync(USER_ID, COUNT);

            Assert.That(recommendedReels.Select(x => x.ReelId), Is.EqualTo(new[] { FIRST_REEL_ID, SECOND_REEL_ID, THIRD_REEL_ID }));
        }

        [Test]
        public async Task GetRecommendedReelsAsync_countLowerThanAvailable_returnsRequestedCount()
        {
            const int USER_ID = 10;
            const int REQUESTED_COUNT = 2;
            const int FIRST_REEL_ID = 31;
            const int FIRST_MOVIE_ID = 301;
            const int SECOND_REEL_ID = 32;
            const int SECOND_MOVIE_ID = 302;
            const int THIRD_REEL_ID = 33;
            const int THIRD_MOVIE_ID = 303;

            var allReels = new List<ReelModel>
            {
                new ReelModel { ReelId = FIRST_REEL_ID, MovieId = FIRST_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 7) },
                new ReelModel { ReelId = SECOND_REEL_ID, MovieId = SECOND_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 6) },
                new ReelModel { ReelId = THIRD_REEL_ID, MovieId = THIRD_MOVIE_ID, CreatedAt = new DateTime(2026, 4, 5) },
            };

            var preferenceScoresByMovieId = new Dictionary<int, double>
            {
                [FIRST_MOVIE_ID] = 0.9,
                [SECOND_MOVIE_ID] = 0.8,
                [THIRD_MOVIE_ID] = 0.7,
            };

            var mockedRecommendationRepository = new Mock<IRecommendationRepository>();

            mockedRecommendationRepository
                .Setup(x => x.UserHasPreferencesAsync(USER_ID))
                .ReturnsAsync(true);

            mockedRecommendationRepository
                .Setup(x => x.GetAllReelsAsync())
                .ReturnsAsync(allReels);

            mockedRecommendationRepository
                .Setup(x => x.GetUserPreferenceScoresAsync(USER_ID))
                .ReturnsAsync(preferenceScoresByMovieId);

            var service = new RecommendationService(mockedRecommendationRepository.Object);

            var recommendedReels = await service.GetRecommendedReelsAsync(USER_ID, REQUESTED_COUNT);

            Assert.That(recommendedReels.Count, Is.EqualTo(REQUESTED_COUNT));
            Assert.That(recommendedReels.Select(x => x.ReelId), Is.EqualTo(new[] { FIRST_REEL_ID, SECOND_REEL_ID }));
        }

        [Test]
        public async Task GetRecommendedReelsAsync_userHasPreferences_callsPersonalizationRepositoriesOnly()
        {
            const int USER_ID = 12;

            var mockedRecommendationRepository = new Mock<IRecommendationRepository>();

            mockedRecommendationRepository
                .Setup(x => x.UserHasPreferencesAsync(USER_ID))
                .ReturnsAsync(true);

            mockedRecommendationRepository
                .Setup(x => x.GetAllReelsAsync())
                .ReturnsAsync(new List<ReelModel>()
                {
                    new () { MovieId = 0 },
                    new () { MovieId = 1 },
                }
                );

            mockedRecommendationRepository
                .Setup(x => x.GetUserPreferenceScoresAsync(USER_ID))
                .ReturnsAsync(new Dictionary<int, double>());

            var service = new RecommendationService(mockedRecommendationRepository.Object);

            await service.GetRecommendedReelsAsync(USER_ID, 5);

            mockedRecommendationRepository.Verify(x => x.UserHasPreferencesAsync(USER_ID), Times.Once);
            mockedRecommendationRepository.Verify(x => x.GetAllReelsAsync(), Times.Once);
            mockedRecommendationRepository.Verify(x => x.GetUserPreferenceScoresAsync(USER_ID), Times.Once);
            mockedRecommendationRepository.Verify(x => x.GetLikesWithinDaysAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task GetRecommendedReelsAsync_userHasNoPreferences_callsColdStartRepositoriesOnly()
        {
            const int USER_ID = 13;

            var mockedRecommendationRepository = new Mock<IRecommendationRepository>();

            mockedRecommendationRepository
                .Setup(x => x.UserHasPreferencesAsync(USER_ID))
                .ReturnsAsync(false);

            mockedRecommendationRepository
                .Setup(x => x.GetAllReelsAsync())
                .ReturnsAsync(new List<ReelModel>() 
                {
                    new () { MovieId = 0 },
                    new () { MovieId = 1 },
                }
                );

            mockedRecommendationRepository
                .Setup(x => x.GetLikesWithinDaysAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<UserReelInteractionModel>());

            var service = new RecommendationService(mockedRecommendationRepository.Object);

            await service.GetRecommendedReelsAsync(USER_ID, 5);

            mockedRecommendationRepository.Verify(x => x.UserHasPreferencesAsync(USER_ID), Times.Once);
            mockedRecommendationRepository.Verify(x => x.GetAllReelsAsync(), Times.Once);
            mockedRecommendationRepository.Verify(x => x.GetLikesWithinDaysAsync(It.IsAny<int>()), Times.Once);
            mockedRecommendationRepository.Verify(x => x.GetUserPreferenceScoresAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
