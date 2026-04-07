using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Core.Platform;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Upload page.
    /// Owner: Alex
    /// </summary>
    public partial class ReelsUploadViewModel : ObservableObject
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IAppWindowContext _appWindowContext;
        private readonly ubb_se_2026_meio_ai.Features.ReelsUpload.Services.IVideoStorageService _videoStorageService;

        public ReelsUploadViewModel(
            ISqlConnectionFactory connectionFactory,
            IAppWindowContext appWindowContext,
            ubb_se_2026_meio_ai.Features.ReelsUpload.Services.IVideoStorageService videoStorageService)
        {
            _connectionFactory = connectionFactory;
            _appWindowContext = appWindowContext;
            _videoStorageService = videoStorageService;
            SuggestedMovies = new ObservableCollection<MovieCardModel>();
        }

        public ObservableCollection<MovieCardModel> SuggestedMovies { get; }

        [ObservableProperty]
        private string _pageTitle = "Reels Upload";

        [ObservableProperty]
        private string _statusMessage = "Ready to upload.";

        // Reel attributes
        int currentUserID = 1;

        [ObservableProperty]
        private string _reelTitle = string.Empty;

        [ObservableProperty]
        private string _reelCaption = string.Empty;

        [ObservableProperty]
        private Core.Models.MovieCardModel? _linkedMovie;

        [ObservableProperty]
        private string _localVideoFilePath = string.Empty;

        // Button 1 click: let the user browse their PC for a video
        [RelayCommand]
        private async Task SelectVideoFileAsync()
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".mp4");

            // In WinUI 3 Desktop apps, the file picker needs to know WHICH window it belongs to!
            var hwnd = _appWindowContext.GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                LocalVideoFilePath = file.Path;
            }
        }

        // Button 3 click: upload everything to the database and blob storage
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

            _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Validating video format...");

            try
            {
                bool isValid = await _videoStorageService.ValidateVideoAsync(LocalVideoFilePath);
                if (!isValid)
                {
                    _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Invalid file! Must be a non-empty MP4 file\nno longer than 60 seconds.");
                    return;
                }

                _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Uploading to Blob Storage & saving metadata...");

                var request = new ubb_se_2026_meio_ai.Features.ReelsUpload.Models.ReelUploadRequest
                {
                    LocalFilePath = LocalVideoFilePath,
                    Title = string.IsNullOrWhiteSpace(ReelTitle) ? "Untitled Reel" : ReelTitle,
                    Caption = ReelCaption ?? string.Empty,
                    UploaderUserId = currentUserID,
                    MovieId = LinkedMovie?.MovieId
                };

                var savedReel = await _videoStorageService.UploadVideoAsync(request);

                _appWindowContext.TryEnqueueOnUiThread(() =>
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
                _appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    StatusMessage = errorMessage;
                });
            }
        }

        // AutoSuggestBox click: select the movie
        [RelayCommand]
        private void SelectMovie(MovieCardModel movie)
        {
            LinkedMovie = movie;
        }

        // AutoSuggestBox 1 type: search for a movie to link to the reel
        [RelayCommand]
        private async Task SearchMovieAsync(string partialMovieName)
        {
            if (string.IsNullOrWhiteSpace(partialMovieName))
            {
                // Safely clear UI on the main thread
                _appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    SuggestedMovies.Clear();
                });
                return;
            }

            try
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                
                string sql = "SELECT TOP 10 MovieId, Title, PosterUrl, PrimaryGenre, ReleaseYear, Description FROM Movie WHERE Title LIKE @SearchTerm";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@SearchTerm", $"%{partialMovieName}%");

                await using var reader = await command.ExecuteReaderAsync();
                
                var newResults = new System.Collections.Generic.List<MovieCardModel>();
                while (await reader.ReadAsync())
                {
                    newResults.Add(new MovieCardModel
                    {
                        MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        PosterUrl = reader.IsDBNull(reader.GetOrdinal("PosterUrl")) ? "" : reader.GetString(reader.GetOrdinal("PosterUrl")),
                        PrimaryGenre = reader.IsDBNull(reader.GetOrdinal("PrimaryGenre")) ? "" : reader.GetString(reader.GetOrdinal("PrimaryGenre")),
                        ReleaseYear = reader.IsDBNull(reader.GetOrdinal("ReleaseYear")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReleaseYear")),
                        Synopsis = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description"))
                    });
                }

                // 2. Only modify the UI collection once we have all the data safely!
                _appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    SuggestedMovies.Clear();
                    foreach (var movie in newResults)
                    {
                        SuggestedMovies.Add(movie);
                    }
                });
            }
            catch (Exception ex)
            {
                // If a background database exception occurs (like Table Doesn't Exist), 
                // setting an [ObservableProperty] from the wrong thread causes an instant FailFast crash (0xffffffff).
                // We must marshal the error message update to the UI Thread!
                string errorMessage = $"DB Note: {ex.Message}";
                _appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    StatusMessage = errorMessage;
                });
            }
        }

        

    }
}
