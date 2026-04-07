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
                .Setup(x => x.RefreshProfileAsync(USER_ID))
                .Returns(Task.CompletedTask);

            mockedProfileService
                .Setup(x => x.GetProfileAsync(USER_ID))
                .ReturnsAsync(expectedProfile);

            var viewModel = new UserProfileViewModel(mockedProfileService.Object);

            await viewModel.LoadProfileAsync(USER_ID);

            Assert.That(viewModel.IsLoading, Is.False);
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.Profile, Is.Not.Null);
            Assert.That(viewModel.Profile!.UserId, Is.EqualTo(USER_ID));

            mockedProfileService.Verify(x => x.RefreshProfileAsync(USER_ID), Times.Once);
            mockedProfileService.Verify(x => x.GetProfileAsync(USER_ID), Times.Once);
        }

        [Test]
        public async Task LoadProfileAsync_refreshFails_setsErrorMessageAndDoesNotReadProfile()
        {
            const int USER_ID = 12;
            const string ERROR_TEXT = "refresh failed";

            var mockedProfileService = new Mock<IEngagementProfileService>();

            mockedProfileService
                .Setup(x => x.RefreshProfileAsync(USER_ID))
                .ThrowsAsync(new InvalidOperationException(ERROR_TEXT));

            var viewModel = new UserProfileViewModel(mockedProfileService.Object);

            await viewModel.LoadProfileAsync(USER_ID);

            Assert.That(viewModel.IsLoading, Is.False);
            Assert.That(viewModel.Profile, Is.Null);
            Assert.That(viewModel.ErrorMessage, Is.Not.Null);
            Assert.That(viewModel.ErrorMessage, Does.Contain(ERROR_TEXT));

            mockedProfileService.Verify(x => x.RefreshProfileAsync(USER_ID), Times.Once);
            mockedProfileService.Verify(x => x.GetProfileAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
