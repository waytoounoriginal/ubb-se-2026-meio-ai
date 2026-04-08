namespace UnitTests.ReelsEditing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Core.Models;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;

    /// <summary>
    /// Unit tests for the <see cref="ReelGalleryViewModel"/> class.
    /// </summary>
    [TestFixture]
    public class ReelGalleryViewModelTests
    {
        private Mock<IReelRepository> mockRepo;
        private ReelGalleryViewModel viewModel;

        /// <summary>
        /// Sets up the test environment before each test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.mockRepo = new Mock<IReelRepository>();
            this.viewModel = new ReelGalleryViewModel(this.mockRepo.Object);
        }

        /// <summary>
        /// Tests that loading reels populates the collection and sets the found message when reels exist.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task LoadReelsCommand_ReelsExist_PopulatesUserReelsAndSetsFoundMessage()
        {
            var expectedReels = new List<ReelModel>
            {
                new ReelModel { ReelId = 101, Title = "Cluj Coffee Vlog" },
                new ReelModel { ReelId = 102, Title = "Coding Session" },
            };

            this.mockRepo.Setup(r => r.GetUserReelsAsync(1)).ReturnsAsync(expectedReels);

            await this.viewModel.LoadReelsCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.UserReels, Has.Count.EqualTo(2));
            Assert.That(this.viewModel.UserReels[0].Title, Is.EqualTo("Cluj Coffee Vlog"));
            Assert.That(this.viewModel.IsLoaded, Is.True);
            Assert.That(this.viewModel.StatusMessage, Is.EqualTo("2 reel(s) found."));
        }

        /// <summary>
        /// Tests that loading reels sets the appropriate message when no reels are found.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task LoadReelsCommand_NoReelsFound_SetsNoReelsMessage()
        {
            this.mockRepo.Setup(r => r.GetUserReelsAsync(1)).ReturnsAsync(new List<ReelModel>());

            await this.viewModel.LoadReelsCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.UserReels, Is.Empty);
            Assert.That(this.viewModel.IsLoaded, Is.True);
            Assert.That(this.viewModel.StatusMessage, Is.EqualTo("No reels uploaded yet. Upload a reel first."));
        }

        /// <summary>
        /// Tests that an error message is set when the repository throws an exception.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task LoadReelsCommand_RepositoryThrowsException_SetsErrorMessage()
        {
            this.mockRepo.Setup(r => r.GetUserReelsAsync(It.IsAny<int>()))
                      .ThrowsAsync(new Exception("Database connection failed"));

            await this.viewModel.LoadReelsCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.UserReels, Is.Empty);
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Error loading reels: Database connection failed"));
            Assert.That(this.viewModel.IsLoaded, Is.False);
        }

        /// <summary>
        /// Tests that EnsureLoadedAsync calls the repository when not already loaded.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task EnsureLoadedAsync_NotLoaded_CallsRepository()
        {
            this.viewModel.IsLoaded = false;
            this.mockRepo.Setup(r => r.GetUserReelsAsync(It.IsAny<int>())).ReturnsAsync(new List<ReelModel>());

            await this.viewModel.EnsureLoadedAsync();

            this.mockRepo.Verify(r => r.GetUserReelsAsync(1), Times.Once);
        }

        /// <summary>
        /// Tests that EnsureLoadedAsync does not call the repository if already loaded.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task EnsureLoadedAsync_AlreadyLoaded_DoesNotCallRepository()
        {
            this.viewModel.IsLoaded = true;

            await this.viewModel.EnsureLoadedAsync();

            this.mockRepo.Verify(r => r.GetUserReelsAsync(It.IsAny<int>()), Times.Never);
        }
    }
}