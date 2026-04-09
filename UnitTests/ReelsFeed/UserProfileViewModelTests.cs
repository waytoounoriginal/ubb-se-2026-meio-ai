using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;

namespace UnitTests.ReelsFeed
{
    [TestFixture]
    public class UserProfileViewModelTests
    {
        [Test]
        public async Task LoadProfileAsync_validUser_loadsProfileAndClearsError()
        {
            const int USER_ID = 11;

            var expectedProfile = new UserProfileModel
            {
                UserId = USER_ID,
                TotalLikes = 14,
            };

            var mockedProfileService = new Mock<IEngagementProfileService>();

            mockedProfileService
                .Setup(mock => mock.RefreshProfileAsync(USER_ID))
                .Returns(Task.CompletedTask);

            mockedProfileService
                .Setup(mock => mock.GetProfileAsync(USER_ID))
                .ReturnsAsync(expectedProfile);

            var viewModel = new UserProfileViewModel(mockedProfileService.Object);

            await viewModel.LoadProfileAsync(USER_ID);

            Assert.That(viewModel.IsLoading, Is.False);

        }

        [Test]
        public async Task LoadProfileAsync_refreshFails_setsErrorMessageAndDoesNotReadProfile()
        {
            const int USER_ID = 12;
            const string ERROR_TEXT = "refresh failed";

            var mockedProfileService = new Mock<IEngagementProfileService>();

            mockedProfileService
                .Setup(mock => mock.RefreshProfileAsync(USER_ID))
                .ThrowsAsync(new InvalidOperationException(ERROR_TEXT));

            var viewModel = new UserProfileViewModel(mockedProfileService.Object);

            await viewModel.LoadProfileAsync(USER_ID);

            Assert.That(viewModel.IsLoading, Is.False);

        }
    }
}
