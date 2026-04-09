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
        public async Task LoadUserDetailAsync_ValidUser_PopulatesUserProfile()
        {
            // Arrange
            int userId = 5;
            double score = 95.5;
            string fb = "fb_test_user";
            string username = "TestUser";
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };

            _mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>()))
                        .ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, score, fb, username);

            // Assert
            Assert.That(_viewModel.UserProfile, Is.Not.Null);
        }

        [Test]
        public async Task LoadUserDetailAsync_ValidUser_SetsHasProfileTrue()
        {
            // Arrange
            int userId = 5;
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };
            _mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>())).ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, 95.5, "fb_test_user", "TestUser");

            // Assert
            Assert.That(_viewModel.HasProfile, Is.True);
        }

        [Test]
        public async Task LoadUserDetailAsync_ValidUser_SetsMatchedUsername()
        {
            // Arrange
            int userId = 5;
            string username = "TestUser";
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };
            _mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>())).ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, 95.5, "fb_test_user", username);

            // Assert
            Assert.That(_viewModel.MatchedUsername, Is.EqualTo(username));
        }

        [Test]
        public async Task LoadUserDetailAsync_ValidUser_SetsMatchScore()
        {
            // Arrange
            int userId = 5;
            double score = 95.5;
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };
            _mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>())).ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, score, "fb_test_user", "TestUser");

            // Assert
            Assert.That(_viewModel.MatchScore, Is.EqualTo(score));
        }

        [Test]
        public async Task LoadUserDetailAsync_ValidUser_SetsFacebookAccount()
        {
            // Arrange
            int userId = 5;
            string fb = "fb_test_user";
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };
            _mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>())).ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, 95.5, fb, "TestUser");

            // Assert
            Assert.That(_viewModel.FacebookAccount, Is.EqualTo(fb));
        }

        [Test]
        public async Task LoadUserDetailAsync_ValidUser_SetsIsLoadingFalse()
        {
            // Arrange
            int userId = 5;
            var profile = new UserProfileModel { UserId = userId, TotalLikes = 10 };
            _mockService.Setup(item => item.GetUserProfileAsync(userId)).ReturnsAsync(profile);
            _mockService.Setup(item => item.GetTopMoviePreferencesAsync(userId, It.IsAny<int>())).ReturnsAsync(new List<MoviePreferenceDisplayModel>());

            // Act
            await _viewModel.LoadUserDetailAsync(userId, 95.5, "fb_test_user", "TestUser");

            // Assert
            Assert.That(_viewModel.IsLoading, Is.False);
        }

        [Test]
        public async Task LoadUserDetailAsync_ServiceError_SetsErrorMessage()
        {
            // Arrange
            _mockService.Setup(item => item.GetUserProfileAsync(It.IsAny<int>()))
                        .ThrowsAsync(new Exception("Connection Failed"));

            // Act
            await _viewModel.LoadUserDetailAsync(1, 0, "fb");

            // Assert
            Assert.That(_viewModel.ErrorMessage, Does.Contain("Failed to load details: Connection Failed"));
        }

        [Test]
        public async Task LoadUserDetailAsync_ServiceError_SetsIsLoadingFalse()
        {
            // Arrange
            _mockService.Setup(item => item.GetUserProfileAsync(It.IsAny<int>())).ThrowsAsync(new Exception("Connection Failed"));

            // Act
            await _viewModel.LoadUserDetailAsync(1, 0, "fb");

            // Assert
            Assert.That(_viewModel.IsLoading, Is.False);
        }
    }
}
