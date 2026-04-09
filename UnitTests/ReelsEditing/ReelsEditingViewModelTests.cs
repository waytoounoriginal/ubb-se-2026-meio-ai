namespace UnitTests.ReelsEditing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Core.Models;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.Models;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;

    /// <summary>
    /// Unit tests for the <see cref="ReelsEditingViewModel"/> class.
    /// </summary>
    [TestFixture]
    public class ReelsEditingViewModelTests
    {
        private Mock<IReelRepository> mockRepo;
        private Mock<IVideoProcessingService> mockVideoService;
        private Mock<IAudioLibraryRepository> mockAudioService;
        private ReelsEditingViewModel viewModel;

        /// <summary>
        /// Sets up the test environment before each test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.mockRepo = new Mock<IReelRepository>();
            this.mockVideoService = new Mock<IVideoProcessingService>();
            this.mockAudioService = new Mock<IAudioLibraryRepository>();

            this.viewModel = new ReelsEditingViewModel(
                this.mockRepo.Object,
                this.mockVideoService.Object,
                this.mockAudioService.Object);
        }

        #region Initialization & Property Tests

        /// <summary>
        /// Tests that setting the status message updates the HasStatusMessage property correctly.
        /// </summary>
        [Test]
        public void HasStatusMessage_UpdatesWhenStatusMessageChanges()
        {
            Assert.That(this.viewModel.HasStatusMessage, Is.False);

            this.viewModel.StatusMessage = "Processing...";
            Assert.That(this.viewModel.HasStatusMessage, Is.True);

            this.viewModel.StatusMessage = string.Empty;
            Assert.That(this.viewModel.HasStatusMessage, Is.False);
        }

        #endregion

        #region LoadReelAsync Tests

        /// <summary>
        /// Tests that loading a reel populates the correct state and fetches the fresh reel data.
        /// </summary>
        [Test]
        public async Task LoadReelAsync_ValidReel_PopulatesStateAndLoadsCropData()
        {
            var reelToLoad = new ReelModel { ReelId = 5, VideoUrl = "old.mp4" };
            var freshReel = new ReelModel { ReelId = 5, VideoUrl = "fresh.mp4", CropDataJson = "{\"x\":0}", BackgroundMusicId = 2 };
            var musicTrack = new MusicTrackModel { MusicTrackId = 2, TrackName = "Background Beat" };

            this.mockRepo.Setup(repository => repository.GetReelByIdAsync(5)).ReturnsAsync(freshReel);
            this.mockAudioService.Setup(audio => audio.GetTrackByIdAsync(2)).ReturnsAsync(musicTrack);

            await this.viewModel.LoadReelAsync(reelToLoad);

            Assert.That(this.viewModel.SelectedReel, Is.Not.Null);
            Assert.That(this.viewModel.SelectedReel!.VideoUrl, Is.EqualTo("fresh.mp4"));
            Assert.That(this.viewModel.IsEditing, Is.True);
            Assert.That(this.viewModel.SelectedMusicTrack, Is.Not.Null);
            Assert.That(this.viewModel.SelectedMusicTrack!.TrackName, Is.EqualTo("Background Beat"));
            Assert.That(this.viewModel.IsMusicChosen, Is.True);
        }

        /// <summary>
        /// Tests that if fresh reel data is not found in the DB, it safely uses the provided reel.
        /// </summary>
        [Test]
        public async Task LoadReelAsync_FreshReelDataNull_KeepsProvidedReelData()
        {
            var reelToLoad = new ReelModel { ReelId = 99, VideoUrl = "provided.mp4" };
            this.mockRepo.Setup(repository => repository.GetReelByIdAsync(99)).ReturnsAsync((ReelModel?)null);

            await this.viewModel.LoadReelAsync(reelToLoad);

            Assert.That(this.viewModel.SelectedReel!.VideoUrl, Is.EqualTo("provided.mp4"));
        }

        /// <summary>
        /// Tests that an exception while fetching the background music track is caught and ignored gracefully.
        /// </summary>
        [Test]
        public async Task LoadReelAsync_AudioLibraryThrows_IgnoresExceptionAndContinues()
        {
            var reelToLoad = new ReelModel { ReelId = 1, BackgroundMusicId = 2 };
            this.mockRepo.Setup(repository => repository.GetReelByIdAsync(1)).ReturnsAsync(reelToLoad);
            this.mockAudioService.Setup(audio => audio.GetTrackByIdAsync(2)).ThrowsAsync(new Exception("DB Timeout"));

            await this.viewModel.LoadReelAsync(reelToLoad);

            // Should not crash, but track won't be loaded
            Assert.That(this.viewModel.SelectedMusicTrack, Is.Null);
            Assert.That(this.viewModel.IsMusicChosen, Is.True); // Still true because ID exists
        }

        /// <summary>
        /// Tests that loading with an empty crop JSON simply skips parsing without throwing.
        /// </summary>
        [Test]
        public async Task LoadReelAsync_EmptyCropJson_DoesNotParse()
        {
            var reelToLoad = new ReelModel { ReelId = 1, CropDataJson = string.Empty };
            this.mockRepo.Setup(repository => repository.GetReelByIdAsync(1)).ReturnsAsync(reelToLoad);

            await this.viewModel.LoadReelAsync(reelToLoad);

            Assert.That(this.viewModel.CropMarginLeft, Is.EqualTo(0));
        }

        /// <summary>
        /// Tests that loading malformed JSON safely falls back to default margin values.
        /// </summary>
        [Test]
        public async Task LoadReelAsync_MalformedCropJson_KeepsDefaultMargins()
        {
            var reelToLoad = new ReelModel { ReelId = 1, CropDataJson = "{ invalid_json: 1 " };
            this.mockRepo.Setup(r => r.GetReelByIdAsync(1)).ReturnsAsync(reelToLoad);

            await this.viewModel.LoadReelAsync(reelToLoad);

            Assert.That(this.viewModel.CropMarginLeft, Is.EqualTo(0));
            Assert.That(this.viewModel.CropMarginTop, Is.EqualTo(0));
        }

        /// <summary>
        /// Tests the private ReadInt and ReadDouble methods by passing string values and invalid strings in JSON.
        /// </summary>
        [Test]
        public async Task LoadReelAsync_JsonWithStringValuesAndInvalidStrings_ParsesOrFallsBackCorrectly()
        {
            // Provides strings that can be parsed ("10", "45") and strings that cannot ("bad_int", "bad_double")
            // Note: We use "45" instead of "45.5" to avoid cross-culture decimal separator issues.
            string complexJson = "{\"x\": \"10\", \"y\": \"bad_int\", \"musicDuration\": \"45\", \"musicVolume\": \"bad_double\"}";
            var reelToLoad = new ReelModel { ReelId = 1, CropDataJson = complexJson };
            this.mockRepo.Setup(r => r.GetReelByIdAsync(1)).ReturnsAsync(reelToLoad);

            await this.viewModel.LoadReelAsync(reelToLoad);

            // X was parsed successfully from string "10"
            Assert.That(this.viewModel.CurrentEdits.CropXCoordinate, Is.EqualTo(10));

            // Y failed to parse "bad_int", so it falls back to 0
            Assert.That(this.viewModel.CurrentEdits.CropYCoordinate, Is.EqualTo(0));

            // Duration parsed successfully from string "45"
            Assert.That(this.viewModel.CurrentEdits.MusicDuration, Is.EqualTo(45.0));

            // Volume failed to parse "bad_double", falls back to default 80
            Assert.That(this.viewModel.CurrentEdits.MusicVolume, Is.EqualTo(80.0));
        }

        #endregion

        #region SelectEditOption & GoBack Tests

        /// <summary>
        /// Tests that selecting the crop option triggers the crop mode entered event.
        /// </summary>
        [Test]
        public void SelectEditOptionCommand_SelectsCrop_TriggersCropModeEnteredEvent()
        {
            bool enteredTriggered = false;
            this.viewModel.CropModeEntered += () => enteredTriggered = true;

            this.viewModel.SelectEditOptionCommand.Execute("Crop");

            Assert.That(this.viewModel.SelectedEditOption, Is.EqualTo("Crop"));
            Assert.That(enteredTriggered, Is.True);
        }

        /// <summary>
        /// Tests that selecting Crop again when it is already selected toggles it off.
        /// </summary>
        [Test]
        public void SelectEditOptionCommand_SameOptionCrop_ClearsSelectionAndTriggersExit()
        {
            bool exitedTriggered = false;
            this.viewModel.CropModeExited += () => exitedTriggered = true;

            this.viewModel.SelectedEditOption = "Crop"; // Already selected
            this.viewModel.SelectEditOptionCommand.Execute("Crop");

            Assert.That(this.viewModel.SelectedEditOption, Is.EqualTo(string.Empty));
            Assert.That(exitedTriggered, Is.True);
        }

        /// <summary>
        /// Tests that switching from Crop to Music triggers Crop exited and loads music tracks.
        /// </summary>
        [Test]
        public void SelectEditOptionCommand_DifferentOption_SwitchesAndLoadsMusic()
        {
            bool exitedTriggered = false;
            this.viewModel.CropModeExited += () => exitedTriggered = true;
            this.viewModel.SelectedEditOption = "Crop";

            var tracks = new List<MusicTrackModel> { new MusicTrackModel() };
            this.mockAudioService.Setup(a => a.GetAllTracksAsync()).ReturnsAsync(tracks);

            this.viewModel.SelectEditOptionCommand.Execute("Music");

            Assert.That(this.viewModel.SelectedEditOption, Is.EqualTo("Music"));
            Assert.That(exitedTriggered, Is.True);
        }

        /// <summary>
        /// Tests that if loading music tracks fails, the status is updated with an error.
        /// </summary>
        [Test]
        public void SelectEditOptionCommand_LoadMusicThrows_SetsErrorStatus()
        {
            this.mockAudioService.Setup(a => a.GetAllTracksAsync()).ThrowsAsync(new Exception("Network Down"));

            this.viewModel.SelectEditOptionCommand.Execute("Music");

            Assert.That(this.viewModel.IsStatusSuccess, Is.False);
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Failed to load music"));
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Network Down"));
        }

        /// <summary>
        /// Tests that executing GoBack while in crop mode triggers the exit event and clears state.
        /// </summary>
        [Test]
        public void GoBackCommand_InCropMode_TriggersCropModeExitedEventAndClearsState()
        {
            bool exitedEventTriggered = false;
            this.viewModel.CropModeExited += () => exitedEventTriggered = true;
            this.viewModel.SelectedEditOption = "Crop";
            this.viewModel.SelectedReel = new ReelModel();

            this.viewModel.GoBackCommand.Execute(null);

            Assert.That(exitedEventTriggered, Is.True);
            Assert.That(this.viewModel.SelectedReel, Is.Null);
            Assert.That(this.viewModel.IsEditing, Is.False);
        }

        /// <summary>
        /// Tests that GoBack does not trigger the exit event if not in Crop mode.
        /// </summary>
        [Test]
        public void GoBackCommand_NotInCropMode_DoesNotTriggerExitEvent()
        {
            bool exitedEventTriggered = false;
            this.viewModel.CropModeExited += () => exitedEventTriggered = true;
            this.viewModel.SelectedEditOption = "Music";

            this.viewModel.GoBackCommand.Execute(null);

            Assert.That(exitedEventTriggered, Is.False);
        }

        #endregion

        #region ApplyMusicSelection Tests

        /// <summary>
        /// Tests that applying a valid music selection updates the view model state and status.
        /// </summary>
        [Test]
        public void ApplyMusicSelection_ValidTrack_UpdatesStateAndStatus()
        {
            var track = new MusicTrackModel { MusicTrackId = 1, TrackName = "LoFi Chill" };
            this.viewModel.SelectedReel = new ReelModel { FeatureDurationSeconds = 15.0 };

            this.viewModel.ApplyMusicSelection(track);

            Assert.That(this.viewModel.SelectedMusicTrack, Is.Not.Null);
            Assert.That(this.viewModel.SelectedMusicTrack!.TrackName, Is.EqualTo("LoFi Chill"));
            Assert.That(this.viewModel.IsMusicChosen, Is.True);
            Assert.That(this.viewModel.MusicDuration, Is.EqualTo(15.0)); // Should clamp to reel duration
            Assert.That(this.viewModel.StatusMessage, Is.EqualTo("Music selected: LoFi Chill"));
        }

        /// <summary>
        /// Tests that if the selected reel is null, ApplyMusicSelection uses the default duration.
        /// </summary>
        [Test]
        public void ApplyMusicSelection_SelectedReelIsNull_UsesDefaultDuration()
        {
            var track = new MusicTrackModel { MusicTrackId = 1 };
            this.viewModel.SelectedReel = null;

            this.viewModel.ApplyMusicSelection(track);

            // Default music duration is 30.0 seconds
            Assert.That(this.viewModel.MusicDuration, Is.EqualTo(30.0));
        }

        #endregion

        #region SaveCropAsync Tests

        /// <summary>
        /// Tests that SaveCrop returns early if no reel is selected.
        /// </summary>
        [Test]
        public async Task SaveCropCommand_SelectedReelIsNull_ReturnsEarly()
        {
            this.viewModel.SelectedReel = null;
            await this.viewModel.SaveCropCommand.ExecuteAsync(null);

            // If it returns early, IsSaving will never be set to true and then false
            Assert.That(this.viewModel.StatusMessage, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// Tests that saving a crop with set margins calculates the correct coordinates and saves successfully.
        /// </summary>
        [Test]
        public async Task SaveCropCommand_MarginsSet_CalculatesCorrectCoordinatesAndSaves()
        {
            this.viewModel.SelectedReel = new ReelModel { ReelId = 1, VideoUrl = "original.mp4" };
            this.viewModel.CurrentEdits = new VideoEditMetadata();

            // Set 10% margins on all sides
            this.viewModel.CropMarginLeft = 10;
            this.viewModel.CropMarginTop = 10;
            this.viewModel.CropMarginRight = 10;
            this.viewModel.CropMarginBottom = 10;

            this.mockVideoService
                .Setup(v => v.ApplyCropAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("cropped.mp4");

            this.mockRepo
                .Setup(r => r.UpdateReelEditsAsync(1, It.IsAny<string>(), It.IsAny<int?>(), "cropped.mp4"))
                .ReturnsAsync(1); // Simulate 1 row affected

            this.mockRepo.Setup(r => r.GetReelByIdAsync(1))
                        .ReturnsAsync(() => new ReelModel { ReelId = 1, CropDataJson = this.viewModel.CurrentEdits.ToCropDataJson() });

            await this.viewModel.SaveCropCommand.ExecuteAsync(null);

            // 10% of BaseVideoWidth (1920) = 192, 10% of BaseVideoHeight (1080) = 108
            Assert.That(this.viewModel.CurrentEdits.CropXCoordinate, Is.EqualTo(192));
            Assert.That(this.viewModel.CurrentEdits.CropYCoordinate, Is.EqualTo(108));
            Assert.That(this.viewModel.CurrentEdits.CropWidth, Is.EqualTo(1536)); // 80%

            Assert.That(this.viewModel.IsStatusSuccess, Is.True);
        }

        /// <summary>
        /// Tests that a database failure during SaveCrop updates the status message and fires the update event.
        /// </summary>
        [Test]
        public async Task SaveCropCommand_RepositoryReturnsZero_SetsErrorStatus()
        {
            this.viewModel.SelectedReel = new ReelModel { ReelId = 1, VideoUrl = "original.mp4" };
            this.mockVideoService
                .Setup(v => v.ApplyCropAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("cropped.mp4");

            // Simulating a scenario where the database fails to find the reel to update (returns 0 rows affected)
            this.mockRepo
                .Setup(r => r.UpdateReelEditsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                .ReturnsAsync(0);

            bool eventFired = false;
            this.viewModel.CropVideoUpdated += (url) => eventFired = true;

            await this.viewModel.SaveCropCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.IsStatusSuccess, Is.False);
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Save failed"));
            Assert.That(eventFired, Is.True); // Ensure catch block fires event
        }

        #endregion

        #region SaveMusicAsync Tests

        /// <summary>
        /// Tests that SaveMusic returns early if Reel or Track is null.
        /// </summary>
        [Test]
        public async Task SaveMusicCommand_SelectedReelOrTrackNull_ReturnsEarly()
        {
            this.viewModel.SelectedReel = new ReelModel();
            this.viewModel.SelectedMusicTrack = null;

            await this.viewModel.SaveMusicCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.StatusMessage, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// Tests that saving music calls the video processing service and updates the database.
        /// </summary>
        [Test]
        public async Task SaveMusicAsync_ValidTrack_UpdatesReelAndPersists()
        {
            this.viewModel.SelectedReel = new ReelModel { ReelId = 1, VideoUrl = "cropped.mp4" };
            this.viewModel.SelectedMusicTrack = new MusicTrackModel { MusicTrackId = 3, TrackName = "Jazz" };
            this.viewModel.CurrentEdits = new VideoEditMetadata();

            this.mockVideoService
                .Setup(video => video.MergeAudioAsync("cropped.mp4", 3, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync("final.mp4");

            this.mockRepo
                .Setup(repository => repository.UpdateReelEditsAsync(1, It.IsAny<string>(), 3, "final.mp4"))
                .ReturnsAsync(1);

            this.mockRepo.Setup(repository => repository.GetReelByIdAsync(1))
                        .ReturnsAsync(new ReelModel { ReelId = 1, BackgroundMusicId = 3 });

            await this.viewModel.SaveMusicCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.IsStatusSuccess, Is.True);
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Music selected: Jazz"));
        }

        /// <summary>
        /// Tests that a persistence mismatch during SaveMusic throws exception caught in catch block.
        /// </summary>
        [Test]
        public async Task SaveMusicCommand_GetReelReturnsMismatchedData_SetsErrorStatus()
        {
            this.viewModel.SelectedReel = new ReelModel { ReelId = 1, VideoUrl = "original.mp4" };
            this.viewModel.SelectedMusicTrack = new MusicTrackModel { MusicTrackId = 5 };

            this.mockVideoService.Setup(v => v.MergeAudioAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync("merged.mp4");

            this.mockRepo.Setup(r => r.UpdateReelEditsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                .ReturnsAsync(1);

            // Return a reel with a DIFFERENT music ID to trigger the mismatch exception
            this.mockRepo.Setup(r => r.GetReelByIdAsync(1))
                .ReturnsAsync(new ReelModel { BackgroundMusicId = 99 });

            await this.viewModel.SaveMusicCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.IsStatusSuccess, Is.False);
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Music edits were not persisted"));
        }

        #endregion

        #region DeleteReelAsync Tests

        /// <summary>
        /// Tests that DeleteReel returns early if no reel is selected.
        /// </summary>
        [Test]
        public async Task DeleteReelCommand_SelectedReelIsNull_ReturnsEarly()
        {
            this.viewModel.SelectedReel = null;
            await this.viewModel.DeleteReelCommand.ExecuteAsync(null);

            this.mockRepo.Verify(r => r.DeleteReelAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that deleting a valid reel calls the repository and goes back to the gallery.
        /// </summary>
        [Test]
        public async Task DeleteReelCommand_ValidReel_CallsRepositoryAndGoesBack()
        {
            this.viewModel.SelectedReel = new ReelModel { ReelId = 99 };
            this.viewModel.IsEditing = true;

            await this.viewModel.DeleteReelCommand.ExecuteAsync(null);

            this.mockRepo.Verify(r => r.DeleteReelAsync(99), Times.Once);
            Assert.That(this.viewModel.SelectedReel, Is.Null);
            Assert.That(this.viewModel.IsEditing, Is.False);
        }

        /// <summary>
        /// Tests that a database exception during deletion updates the status message.
        /// </summary>
        [Test]
        public async Task DeleteReelCommand_RepositoryThrowsException_SetsErrorStatus()
        {
            this.viewModel.SelectedReel = new ReelModel { ReelId = 1 };
            this.mockRepo.Setup(r => r.DeleteReelAsync(1)).ThrowsAsync(new InvalidOperationException("DB Lock"));

            await this.viewModel.DeleteReelCommand.ExecuteAsync(null);

            Assert.That(this.viewModel.IsStatusSuccess, Is.False);
            Assert.That(this.viewModel.StatusMessage, Does.Contain("Delete failed"));
            Assert.That(this.viewModel.StatusMessage, Does.Contain("DB Lock"));
        }

        #endregion
    }
}