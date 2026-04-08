using Moq;
using NUnit.Framework;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;

namespace UnitTests.PersonalityMatch
{
    [TestFixture]
    public class PersonalityMatchRepositoryTests
    {
        private Mock<ISqlConnectionFactory> mockedFactory = null!;
        private PersonalityMatchRepository repository = null!;

        [SetUp]
        public void SetUp()
        {
            this.mockedFactory = new Mock<ISqlConnectionFactory>();
            this.repository = new PersonalityMatchRepository(this.mockedFactory.Object);
        }

        [Test]
        public async Task GetUsernameAsync_HandlesEmptyResults()
        {
            // This verifies the fallback logic in the repository when no DB records are found.
            // Even if we cannot easily mock the SqlDataReader in a unit test, 
            // the logic for generating "User {id}" is a critical branch.

            var username = await this.repository.GetUsernameAsync(999);
            Assert.That(username, Is.EqualTo("User 999"));
        }
    }
}