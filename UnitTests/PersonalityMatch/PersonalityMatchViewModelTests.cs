using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.PersonalityMatch
{
    [TestFixture]
    public class PersonalityMatchViewModelTests
    {
        private Mock<IPersonalityMatchingService> _mockService;
        private PersonalityMatchViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _mockService = new Mock<IPersonalityMatchingService>();
            _viewModel = new PersonalityMatchViewModel(_mockService.Object);
        }

        [Test]
        public async Task LoadMatchesAsync_Success_UpdatesCollectionCount()
        {
            // Arrange
            var results = new List<MatchResult>
            {
                new MatchResult { MatchedUserId = 2, MatchedUsername = "Alice" }
            };
            _mockService.Setup(item => item.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(results);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.That(_viewModel.MatchResults.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task LoadMatchesAsync_Success_SetsHasMatchesTrue()
        {
            // Arrange
            var results = new List<MatchResult>
            {
                new MatchResult { MatchedUserId = 2, MatchedUsername = "Alice" }
            };
            _mockService.Setup(item => item.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(results);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.That(_viewModel.HasMatches, Is.True);
        }

        [Test]
        public async Task LoadMatchesAsync_Success_SetsStatusMessage()
        {
            // Arrange
            var results = new List<MatchResult>
            {
                new MatchResult { MatchedUserId = 2, MatchedUsername = "Alice" }
            };
            _mockService.Setup(item => item.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(results);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.That(_viewModel.StatusMessage, Is.EqualTo("Found 1 match!"));
        }

        [Test]
        public async Task LoadMatchesAsync_NoMatches_SetsShowNoMatchTrue()
        {
            // Arrange
            _mockService.Setup(item => item.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(new List<MatchResult>());

            var fallback = new List<MatchResult> { new MatchResult { MatchedUserId = 99 } };
            _mockService.Setup(item => item.GetRandomUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(fallback);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.That(_viewModel.ShowNoMatch, Is.True);
        }

        [Test]
        public async Task LoadMatchesAsync_NoMatches_PopulatesFallbackUsers()
        {
            // Arrange
            _mockService.Setup(item => item.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<MatchResult>());
            var fallback = new List<MatchResult> { new MatchResult { MatchedUserId = 99 } };
            _mockService.Setup(item => item.GetRandomUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(fallback);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.That(_viewModel.FallbackUsers.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task LoadMatchesAsync_NoMatches_LeavesHasMatchesFalse()
        {
            // Arrange
            _mockService.Setup(item => item.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<MatchResult>());
            var fallback = new List<MatchResult> { new MatchResult { MatchedUserId = 99 } };
            _mockService.Setup(item => item.GetRandomUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(fallback);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.That(_viewModel.HasMatches, Is.False);
        }

        [Test]
        public void BuildSelfViewMatchResult_SetsSelfViewFlag()
        {
            // Arrange
            var account = new UserAccountModel
            {
                UserId = 1,
                Username = "Alex",
                FacebookAccount = "fb_alex"
            };

            // Act
            var result = _viewModel.BuildSelfViewMatchResult(account);

            // Assert
            Assert.That(result.IsSelfView, Is.True);
        }

        [Test]
        public void BuildSelfViewMatchResult_SetsMatchScoreTo100()
        {
            // Arrange
            var account = new UserAccountModel
            {
                UserId = 1,
                Username = "Alex",
                FacebookAccount = "fb_alex"
            };

            // Act
            var result = _viewModel.BuildSelfViewMatchResult(account);

            // Assert
            Assert.That(result.MatchScore, Is.EqualTo(100));
        }

        [Test]
        public void BuildSelfViewMatchResult_SetsMatchedUsername()
        {
            // Arrange
            var account = new UserAccountModel
            {
                UserId = 1,
                Username = "Alex",
                FacebookAccount = "fb_alex"
            };

            // Act
            var result = _viewModel.BuildSelfViewMatchResult(account);

            // Assert
            Assert.That(result.MatchedUsername, Is.EqualTo("Alex"));
        }
    }
}
