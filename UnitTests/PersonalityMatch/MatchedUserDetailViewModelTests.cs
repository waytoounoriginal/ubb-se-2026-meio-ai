using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.PersonalityMatch
{
    [TestFixture]
    public class MatchedUserDetailViewModelTests
    {
        private Mock<IPersonalityMatchingService> _mockService;
        private MatchedUserDetailViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _mockService = new Mock<IPersonalityMatchingService>();
            _viewModel = new MatchedUserDetailViewModel(_mockService.Object);
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

            _mockService.Setup(s => s.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(s => s.GetTopMoviePreferencesAsync(userId, It.IsAny<int>()))
                        .ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, score, fb, username);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.UserProfile, Is.Not.Null);
                Assert.That(_viewModel.HasProfile, Is.True);
                Assert.That(_viewModel.MatchedUsername, Is.EqualTo(username));
                Assert.That(_viewModel.MatchScore, Is.EqualTo(score));
                Assert.That(_viewModel.FacebookAccount, Is.EqualTo(fb));
                Assert.That(_viewModel.IsLoading, Is.False);
            });
        }

        [Test]
        public async Task LoadUserDetailAsync_ServiceError_SetsErrorMessage()
        {
            // Arrange
            _mockService.Setup(s => s.GetUserProfileAsync(It.IsAny<int>()))
                        .ThrowsAsync(new Exception("Connection Failed"));

            // Act
            await _viewModel.LoadUserDetailAsync(1, 0, "fb");

            // Assert
            // Using ErrorMessage as defined in your MatchedUserDetailViewModel.cs
            Assert.That(_viewModel.ErrorMessage, Does.Contain("Failed to load details: Connection Failed"));
            Assert.That(_viewModel.IsLoading, Is.False);
        }
    }
}