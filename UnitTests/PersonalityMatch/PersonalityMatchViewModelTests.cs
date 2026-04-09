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
        public async Task LoadMatchesAsync_Success_UpdatesCollection()
        {
            // Arrange
            var results = new List<MatchResult>
            {
                new MatchResult { MatchedUserId = 2, MatchedUsername = "Alice" }
            };
            _mockService.Setup(s => s.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(results);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.Multiple(() =>
            {
                // Using MatchResults collection as defined in your file
                Assert.That(_viewModel.MatchResults.Count, Is.EqualTo(1));
                Assert.That(_viewModel.HasMatches, Is.True);
                Assert.That(_viewModel.StatusMessage, Is.EqualTo("Found 1 match!"));
            });
        }

        [Test]
        public async Task LoadMatchesAsync_NoMatches_PopulatesFallback()
        {
            // Arrange
            _mockService.Setup(s => s.GetTopMatchesAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(new List<MatchResult>());

            var fallback = new List<MatchResult> { new MatchResult { MatchedUserId = 99 } };
            _mockService.Setup(s => s.GetRandomUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(fallback);

            // Act
            await _viewModel.LoadMatchesAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.ShowNoMatch, Is.True);
                Assert.That(_viewModel.FallbackUsers.Count, Is.EqualTo(1));
                Assert.That(_viewModel.HasMatches, Is.False);
            });
        }

        [Test]
        public void BuildSelfViewMatchResult_SetsCorrectFlags()
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
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSelfView, Is.True);
                Assert.That(result.MatchScore, Is.EqualTo(100));
                Assert.That(result.MatchedUsername, Is.EqualTo("Alex"));
            });
        }
    }
}