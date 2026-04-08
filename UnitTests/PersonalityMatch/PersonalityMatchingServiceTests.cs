using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;

namespace UnitTests.PersonalityMatch
{
    [TestFixture]
    public class PersonalityMatchingServiceTests
    {
        private Mock<IPersonalityMatchRepository> mockedRepository = null!;
        private PersonalityMatchingService service = null!;

        private const int CurrentUserId = 1;
        private const int OtherUserId = 2;

        [SetUp]
        public void SetUp()
        {
            this.mockedRepository = new Mock<IPersonalityMatchRepository>();
            this.service = new PersonalityMatchingService(this.mockedRepository.Object);
        }

        [Test]
        public async Task GetTopMatchesAsync_PerfectMatch_Returns100Percent()
        {
            // Logic: Vector A [1.0], Vector B [1.0]. Dot product = 1, Magnitudes = 1. Similarity = 1.
            var currentPrefs = new List<UserMoviePreferenceModel>
            {
                new UserMoviePreferenceModel { MovieId = 10, Score = 1.0 }
            };
            var othersPrefs = new Dictionary<int, List<UserMoviePreferenceModel>>
            {
                [OtherUserId] = new List<UserMoviePreferenceModel>
                {
                    new UserMoviePreferenceModel { MovieId = 10, Score = 1.0 }
                }
            };

            this.mockedRepository.Setup(x => x.GetCurrentUserPreferencesAsync(CurrentUserId)).ReturnsAsync(currentPrefs);
            this.mockedRepository.Setup(x => x.GetAllPreferencesExceptUserAsync(CurrentUserId)).ReturnsAsync(othersPrefs);

            var results = await this.service.GetTopMatchesAsync(CurrentUserId, 1);

            Assert.That(results[0].MatchScore, Is.EqualTo(100.0));
            Assert.That(results[0].MatchedUserId, Is.EqualTo(OtherUserId));
        }

        [Test]
        public async Task GetTopMatchesAsync_NoOverlap_ReturnsZero()
        {
            // Logic: User A rated Movie 1, User B rated Movie 2. No common dimensions.
            var currentPrefs = new List<UserMoviePreferenceModel> { new UserMoviePreferenceModel { MovieId = 1, Score = 1.0 } };
            var othersPrefs = new Dictionary<int, List<UserMoviePreferenceModel>>
            {
                [OtherUserId] = new List<UserMoviePreferenceModel> { new UserMoviePreferenceModel { MovieId = 2, Score = 1.0 } }
            };

            this.mockedRepository.Setup(x => x.GetCurrentUserPreferencesAsync(CurrentUserId)).ReturnsAsync(currentPrefs);
            this.mockedRepository.Setup(x => x.GetAllPreferencesExceptUserAsync(CurrentUserId)).ReturnsAsync(othersPrefs);

            var results = await this.service.GetTopMatchesAsync(CurrentUserId, 1);

            Assert.That(results[0].MatchScore, Is.EqualTo(0.0));
        }

        [Test]
        public async Task GetTopMatchesAsync_EmptyPrefs_ReturnsEmptyList()
        {
            // Branch coverage: if (currentUserPreferences.Count == 0)
            this.mockedRepository.Setup(x => x.GetCurrentUserPreferencesAsync(CurrentUserId))
                .ReturnsAsync(new List<UserMoviePreferenceModel>());

            var results = await this.service.GetTopMatchesAsync(CurrentUserId, 5);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task GetRandomMatchesAsync_MapsCorrectScoreAndIds()
        {
            // Logic: Random matches should have a hardcoded score of 0
            var randomIds = new List<int> { 10, 20 };
            this.mockedRepository.Setup(x => x.GetRandomUserIdsAsync(CurrentUserId, 2)).ReturnsAsync(randomIds);

            var results = await this.service.GetRandomMatchesAsync(CurrentUserId, 2);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(r => r.MatchScore == 0), Is.True);
            Assert.That(results[0].MatchedUserId, Is.EqualTo(10));
        }

        [Test]
        public async Task GetUserProfileAsync_WithValidId_ReturnsHardcodedData()
        {
            // Covers the private GetHardcodedUsername and GetHardcodedFacebookAccount methods
            var profile = new UserProfileModel { UserId = 1 };
            this.mockedRepository.Setup(x => x.GetUserProfileAsync(1)).ReturnsAsync(profile);

            var result = await this.service.GetUserProfileAsync(1);

            Assert.That(result, Is.Not.Null);
            // "Alex Carter" is hardcoded for ID 1 in PersonalityMatchingService.cs
            this.mockedRepository.Verify(x => x.GetUsernameAsync(1), Times.Never);
        }

        [Test]
        public async Task GetTopMoviePreferencesAsync_FlagsFirstMovieAsBest()
        {
            // Branch coverage: isBestMovie = (i == 0)
            var prefs = new List<MoviePreferenceDisplayModel>
            {
                new MoviePreferenceDisplayModel { MovieId = 1, Score = 10 },
                new MoviePreferenceDisplayModel { MovieId = 2, Score = 5 }
            };
            this.mockedRepository.Setup(x => x.GetTopPreferencesWithTitlesAsync(OtherUserId, 2)).ReturnsAsync(prefs);

            var results = await this.service.GetTopMoviePreferencesAsync(OtherUserId, 2);

            Assert.That(results[0].IsBestMovie, Is.True);
            Assert.That(results[1].IsBestMovie, Is.False);
        }
    }
}