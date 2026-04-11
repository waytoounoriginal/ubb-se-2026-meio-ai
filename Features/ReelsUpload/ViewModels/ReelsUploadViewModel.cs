using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Core.Platform;
using ubb_se_2026_meio_ai.Core.Services;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Upload page.
    /// Owner: Alex
    /// </summary>
    public partial class ReelsUploadViewModel : ObservableObject
    {
        private readonly IAppWindowContext appWindowContext;
        private readonly IVideoStorageService videoStorageService;
        private readonly IMovieService movieService;

        private const string UntitledName = "Untitled Reel";

        public ReelsUploadViewModel(
            IAppWindowContext appWindowContext,
            IVideoStorageService videoStorageService,
            IMovieService movieService)
        {
            this.appWindowContext = appWindowContext;
            this.videoStorageService = videoStorageService;
            this.movieService = movieService;
            SuggestedMovies = new ObservableCollection<MovieCardModel>();
        }

        public ObservableCollection<MovieCardModel> SuggestedMovies { get; }

        [ObservableProperty]
        private string pageTitle = "Reels Upload";

        [ObservableProperty]
        private string statusMessage = "Ready to upload.";

        // TODO: Replace with actual authenticated user ID later
        private const int CurrentUserID = 1;

        [ObservableProperty]
        private string reelTitle = string.Empty;

        [ObservableProperty]
        private string reelCaption = string.Empty;

        [ObservableProperty]
        private MovieCardModel? linkedMovie;

        [ObservableProperty]
        private string localVideoFilePath = string.Empty;

        private const string VideoFileExtension = ".mp4";

        [RelayCommand]
        private async Task SelectVideoFileAsync()
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(VideoFileExtension);

            var windowHandle = appWindowContext.GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, windowHandle);

            var selectedMovieFile = await filePicker.PickSingleFileAsync();
            if (selectedMovieFile != null)
            {
                LocalVideoFilePath = selectedMovieFile.Path;
            }
        }

        [RelayCommand]
        private async Task UploadReelAsync()
        {
            if (string.IsNullOrWhiteSpace(LocalVideoFilePath))
            {
                StatusMessage = "Please select a video first!";
                return;
            }

            if (string.IsNullOrWhiteSpace(ReelTitle))
            {
                StatusMessage = "Please enter a title for the reel!";
                return;
            }

            if (LinkedMovie == null)
            {
                StatusMessage = "Please link a movie to the reel!";
                return;
            }

            appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Validating video format...");

            try
            {
                bool isValid = await videoStorageService.ValidateVideoAsync(LocalVideoFilePath);
                if (!isValid)
                {
                    appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Invalid file! Must be a non-empty MP4 file\nno longer than 60 seconds.");
                    return;
                }

                appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Uploading to Blob Storage & saving metadata...");

                var request = new Models.ReelUploadRequest
                {
                    LocalFilePath = LocalVideoFilePath,
                    Title = ReelTitle,
                    Caption = ReelCaption ?? string.Empty,
                    UploaderUserId = CurrentUserID,
                    MovieId = LinkedMovie.MovieId
                };

                var savedReel = await videoStorageService.UploadVideoAsync(request);

                appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    StatusMessage = $"Success! Reel uploaded with ID {savedReel.ReelId}.";
                    LocalVideoFilePath = string.Empty;
                    ReelTitle = string.Empty;
                    ReelCaption = string.Empty;
                    LinkedMovie = null;
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Upload Failed: {ex.Message}";
                appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = errorMessage);
            }
        }

        [RelayCommand]
        private void SelectMovie(MovieCardModel movieToSelect)
        {
            LinkedMovie = movieToSelect;
        }

        [RelayCommand]
        private async Task SearchMovieAsync(string partialMovieName)
        {
            if (string.IsNullOrWhiteSpace(partialMovieName))
            {
                appWindowContext.TryEnqueueOnUiThread(() => SuggestedMovies.Clear());
                return;
            }

            try
            {
                // Ask the service for the data
                var searchResults = await movieService.SearchTop10MoviesAsync(partialMovieName);

                // Update UI
                appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    SuggestedMovies.Clear();
                    foreach (var movie in searchResults)
                    {
                        SuggestedMovies.Add(movie);
                    }
                });
            }
            catch (Exception ex)
            {
                appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = $"Search Error: {ex.Message}");
            }
        }
    }
}