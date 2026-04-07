using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace UnitTests.ReelsFeed
{
    [TestFixture]
    public class EngagementProfileServiceTests
    {
        [Test]
        public async Task GetProfileAsync_userIdExists_returnsProfile()
        {
            const int USER_ID = 1;
            UserProfileModel? EXPECTED_MODEL = new UserProfileModel
            {
                UserId = USER_ID,
            };

            var mockedProfileRepository = new Mock<IProfileRepository>();

            mockedProfileRepository
                .Setup(x => x.GetProfileAsync(It.IsAny<int>()))
                .ReturnsAsync(EXPECTED_MODEL);

            var result = await new EngagementProfileService(mockedProfileRepository.Object).GetProfileAsync(USER_ID);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UserId, Is.EqualTo(EXPECTED_MODEL.UserId));
        }

        [Test]
        public async Task GetProfileAsync_userIdDoesNotExist_returnsNull()
        {
            const int USER_ID = 1;

            var mockedProfileRepository = new Mock<IProfileRepository>();

            mockedProfileRepository
                .Setup(x => x.GetProfileAsync(It.IsAny<int>()))
                .ReturnsAsync((UserProfileModel?)null);

            var result = await new EngagementProfileService(mockedProfileRepository.Object).GetProfileAsync(USER_ID);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetProfileAsync_userIdProvided_callsRepositoryOnce()
        {
            const int USER_ID = 42;

            var mockedProfileRepository = new Mock<IProfileRepository>();

            mockedProfileRepository
                .Setup(x => x.GetProfileAsync(It.IsAny<int>()))
                .ReturnsAsync((UserProfileModel?)null);

            await new EngagementProfileService(mockedProfileRepository.Object).GetProfileAsync(USER_ID);

            mockedProfileRepository.Verify(x => x.GetProfileAsync(USER_ID), Times.Once);
        }

        [Test]
        public async Task RefreshProfileAsync_validUserId_upsertsAggregatedProfile()
        {
            const int USER_ID = 5;
            var aggregatedProfile = new UserProfileModel { UserId = USER_ID };

            var mockedProfileRepository = new Mock<IProfileRepository>();

            mockedProfileRepository
                .Setup(x => x.BuildProfileFromInteractionsAsync(USER_ID))
                .ReturnsAsync(aggregatedProfile);

            await new EngagementProfileService(mockedProfileRepository.Object).RefreshProfileAsync(USER_ID);

            mockedProfileRepository.Verify(x => x.BuildProfileFromInteractionsAsync(USER_ID), Times.Once);
            mockedProfileRepository.Verify(x => x.UpsertProfileAsync(aggregatedProfile), Times.Once);
        }

    }
}
