using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;

namespace UnitTests.PersonalityMatch
{
    [TestFixture]
    public class MatchedUserDetailViewModelTests
    {
        private Mock<IPersonalityMatchingService> mockService;
        private MatchedUserDetailViewModel viewModel;

        [SetUp]
        public void SetUp()
        {
            mockService = new Mock<IPersonalityMatchingService>();
            viewModel = new MatchedUserDetailViewModel(mockService.Object);
        }

        [Test]
        public async Task LoadUserDetailAsync_ValidUser_PopulatesProperties()
        {
            // Arrange
            int userId = 5;
            double score = 95.5;
            string fb = "fb_test_user";
            string username = "TestUser";
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };

            mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>()))
                        .ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await viewModel.LoadUserDetailAsync(userId, score, fb, username);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.UserProfile, Is.Not.Null);
                Assert.That(viewModel.HasProfile, Is.True);
                Assert.That(viewModel.MatchedUsername, Is.EqualTo(username));
                Assert.That(viewModel.MatchScore, Is.EqualTo(score));
                Assert.That(viewModel.FacebookAccount, Is.EqualTo(fb));
                Assert.That(viewModel.IsLoading, Is.False);
            });
        }

        [Test]
        public async Task LoadUserDetailAsync_ServiceError_SetsErrorMessage()
        {
            // Arrange
            mockService.Setup(item => item.GetUserProfileAsync(It.IsAny<int>()))
                        .ThrowsAsync(new Exception("Connection Failed"));

            // Act
            await viewModel.LoadUserDetailAsync(1, 0, "fb");

            // Assert
            // Using ErrorMessage as defined in your MatchedUserDetailViewModel.cs
            Assert.That(viewModel.ErrorMessage, Does.Contain("Failed to load details: Connection Failed"));
            Assert.That(viewModel.IsLoading, Is.False);
        }
    }
}
