using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels
{
    /// <summary>
    /// ViewModel for the main Reel Editor workspace.
    /// Owner: Beatrice
    /// </summary>
    public partial class ReelsEditingViewModel : ObservableObject
    {
        private readonly ReelRepository _reelRepository;
        private readonly IVideoProcessingService _videoProcessing;
        private readonly IAudioLibraryService _audioLibrary;

        [ObservableProperty]
        private ReelModel? _selectedReel;

        [ObservableProperty]
        private VideoEditMetadata _currentEdits = new();

        [ObservableProperty]
        private MusicTrackModel? _selectedMusicTrack;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusSuccess = true;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private bool _isEditing;

        // "" = no option selected, "Thumbnail", "Crop", "Music"
        [ObservableProperty]
        private string _selectedEditOption = string.Empty;

        // Music library
        [ObservableProperty]
        private ObservableCollection<MusicTrackModel> _musicTracks = new();

        [ObservableProperty]
        private bool _isMusicChosen;

        // Crop margins (percentage-based, 0-50)
        [ObservableProperty]
        private double _cropMarginLeft;

        [ObservableProperty]
        private double _cropMarginTop;

        [ObservableProperty]
        private double _cropMarginRight;

        [ObservableProperty]
        private double _cropMarginBottom;

        // Music parameters
        [ObservableProperty]
        private double _musicStartTime;

        [ObservableProperty]
        private double _musicDuration = 30.0;

        [ObservableProperty]
        private double _musicVolume = 80.0;

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        // Event that the view subscribes to for crop-mode video pause
        public event Action? CropModeEntered;
        public event Action? CropModeExited;

        public ReelsEditingViewModel(
            ReelRepository reelRepository,
            IVideoProcessingService videoProcessing,
            IAudioLibraryService audioLibrary)
        {
            _reelRepository = reelRepository;
            _videoProcessing = videoProcessing;
            _audioLibrary = audioLibrary;
        }

        [RelayCommand]
        private void SelectEditOption(string option)
        {
            // If same option clicked, toggle it off
            if (SelectedEditOption == option)
            {
                // Exiting crop mode
                if (SelectedEditOption == "Crop")
                    CropModeExited?.Invoke();

                SelectedEditOption = string.Empty;
                return;
            }

            // Exiting previous crop mode
            if (SelectedEditOption == "Crop")
                CropModeExited?.Invoke();

            SelectedEditOption = option;

            // Entering crop mode — pause video
            if (option == "Crop")
                CropModeEntered?.Invoke();

            // Entering music mode — load tracks
            if (option == "Music")
                _ = LoadMusicTracksAsync();
        }

        public async Task LoadReelAsync(ReelModel reel)
        {
            // Always fetch fresh data from the database so persisted edits
            // are never lost due to stale in-memory gallery models.
            var fresh = await _reelRepository.GetReelByIdAsync(reel.ReelId);
            if (fresh != null)
            {
                reel.CropDataJson = fresh.CropDataJson;
                reel.BackgroundMusicId = fresh.BackgroundMusicId;
                reel.LastEditedAt = fresh.LastEditedAt;
            }

            SelectedReel = reel;
            CurrentEdits = new VideoEditMetadata();
            SelectedMusicTrack = null;
            IsMusicChosen = false;
            IsEditing = true;
            SelectedEditOption = string.Empty;
            CropMarginLeft = 0;
            CropMarginTop = 0;
            CropMarginRight = 0;
            CropMarginBottom = 0;
            MusicStartTime = 0;
            MusicDuration = 30.0;
            MusicVolume = 80.0;
            StatusMessage = string.Empty;
            IsStatusSuccess = true;

            LoadPersistedEditData(reel.CropDataJson, reel.BackgroundMusicId);
        }

        [RelayCommand]
        private void GoBack()
        {
            if (SelectedEditOption == "Crop")
                CropModeExited?.Invoke();

            SelectedReel = null;
            IsEditing = false;
            SelectedEditOption = string.Empty;
            StatusMessage = string.Empty;
            IsStatusSuccess = true;
        }

        public void ApplyMusicSelection(MusicTrackModel track)
        {
            SelectedMusicTrack = track;
            CurrentEdits.SelectedMusicTrackId = track.MusicTrackId;
            IsMusicChosen = true;
            MusicDuration = Math.Min(30.0, track.DurationSeconds);
            IsStatusSuccess = true;
            StatusMessage = $"Music selected: {track.TrackName}";
        }

        private async Task LoadMusicTracksAsync()
        {
            try
            {
                var tracks = await _audioLibrary.GetAllTracksAsync();
                MusicTracks.Clear();
                foreach (var t in tracks)
                    MusicTracks.Add(t);
            }
            catch (Exception ex)
            {
                IsStatusSuccess = false;
                StatusMessage = $"Failed to load music: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveThumbnailAsync()
        {
            if (SelectedReel == null) return;

            IsSaving = true;
            StatusMessage = "Saving thumbnail...";
            IsStatusSuccess = true;
            try
            {
                var payload = CurrentEdits.ToCropDataJson();
                int rows = await _reelRepository.UpdateReelEditsAsync(
                    SelectedReel.ReelId,
                    payload,
                    CurrentEdits.SelectedMusicTrackId);

                if (rows == 0)
                    throw new InvalidOperationException($"No reel found with ReelId={SelectedReel.ReelId}.");

                var persisted = await _reelRepository.GetReelByIdAsync(SelectedReel.ReelId);
                if (persisted?.CropDataJson != payload)
                    throw new InvalidOperationException("Thumbnail edits were not persisted correctly.");

                SelectedReel.CropDataJson = persisted.CropDataJson;
                SelectedReel.LastEditedAt = persisted.LastEditedAt;
                StatusMessage = $"Thumbnail saved successfully at {TimeSpan.FromSeconds(CurrentEdits.ThumbnailFrameSeconds):mm\\:ss\\.f}.";
            }
            catch (Exception ex)
            {
                IsStatusSuccess = false;
                StatusMessage = $"Save failed: {ex.Message}";
            }
            finally { IsSaving = false; }
        }

        [RelayCommand]
        private async Task SaveCropAsync()
        {
            if (SelectedReel == null) return;

            IsSaving = true;
            StatusMessage = "Saving crop...";
            IsStatusSuccess = true;
            try
            {
                // Convert margin percentages to pixel crop data
                CurrentEdits.CropX = (int)(CropMarginLeft / 100.0 * 1920);
                CurrentEdits.CropY = (int)(CropMarginTop / 100.0 * 1080);
                CurrentEdits.CropWidth = (int)((1.0 - (CropMarginLeft + CropMarginRight) / 100.0) * 1920);
                CurrentEdits.CropHeight = (int)((1.0 - (CropMarginTop + CropMarginBottom) / 100.0) * 1080);

                string cropJson = CurrentEdits.ToCropDataJson();
                await _videoProcessing.ApplyCropAsync(SelectedReel.VideoUrl, cropJson);
                int rows = await _reelRepository.UpdateReelEditsAsync(SelectedReel.ReelId, cropJson, CurrentEdits.SelectedMusicTrackId);

                if (rows == 0)
                    throw new InvalidOperationException($"No reel found with ReelId={SelectedReel.ReelId}.");

                var persisted = await _reelRepository.GetReelByIdAsync(SelectedReel.ReelId);
                if (persisted?.CropDataJson != cropJson)
                    throw new InvalidOperationException("Crop edits were not persisted correctly.");

                SelectedReel.CropDataJson = persisted.CropDataJson;
                SelectedReel.LastEditedAt = persisted.LastEditedAt;
                StatusMessage = $"Crop dimensions updated successfully: X={CurrentEdits.CropX}, Y={CurrentEdits.CropY}, W={CurrentEdits.CropWidth}, H={CurrentEdits.CropHeight}.";
            }
            catch (Exception ex)
            {
                IsStatusSuccess = false;
                StatusMessage = $"Save failed: {ex.Message}";
            }
            finally { IsSaving = false; }
        }

        [RelayCommand]
        private async Task SaveMusicAsync()
        {
            if (SelectedReel == null || SelectedMusicTrack == null) return;

            IsSaving = true;
            StatusMessage = "Saving music...";
            IsStatusSuccess = true;
            try
            {
                CurrentEdits.MusicStartTime = MusicStartTime;
                CurrentEdits.MusicDuration = MusicDuration;
                CurrentEdits.MusicVolume = MusicVolume;

                await _videoProcessing.MergeAudioAsync(
                    SelectedReel.VideoUrl,
                    SelectedMusicTrack.MusicTrackId,
                    MusicStartTime);
                int rows = await _reelRepository.UpdateReelEditsAsync(
                    SelectedReel.ReelId,
                    CurrentEdits.ToCropDataJson(),
                    SelectedMusicTrack.MusicTrackId);

                if (rows == 0)
                    throw new InvalidOperationException($"No reel found with ReelId={SelectedReel.ReelId}.");

                var persisted = await _reelRepository.GetReelByIdAsync(SelectedReel.ReelId);
                SelectedReel.CropDataJson = persisted?.CropDataJson;
                SelectedReel.BackgroundMusicId = SelectedMusicTrack.MusicTrackId;
                SelectedReel.LastEditedAt = persisted?.LastEditedAt;
                StatusMessage = "Music saved!";
            }
            catch (Exception ex)
            {
                IsStatusSuccess = false;
                StatusMessage = $"Save failed: {ex.Message}";
            }
            finally { IsSaving = false; }
        }

        [RelayCommand]
        private async Task DeleteReelAsync()
        {
            if (SelectedReel == null) return;

            try
            {
                StatusMessage = "Deleting reel...";
                await _reelRepository.DeleteReelAsync(SelectedReel.ReelId);
                StatusMessage = "Reel deleted.";
                GoBack();
            }
            catch (Exception ex)
            {
                IsStatusSuccess = false;
                StatusMessage = $"Delete failed: {ex.Message}";
            }
        }

        partial void OnStatusMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasStatusMessage));
        }

        private void LoadPersistedEditData(string? cropDataJson, int? backgroundMusicId)
        {
            CurrentEdits.SelectedMusicTrackId = backgroundMusicId;
            if (backgroundMusicId.HasValue)
                IsMusicChosen = true;

            if (string.IsNullOrWhiteSpace(cropDataJson))
                return;

            try
            {
                using var doc = JsonDocument.Parse(cropDataJson);
                var root = doc.RootElement;

                CurrentEdits.CropX = ReadInt(root, "x", 0);
                CurrentEdits.CropY = ReadInt(root, "y", 0);
                CurrentEdits.CropWidth = ReadInt(root, "width", 1920);
                CurrentEdits.CropHeight = ReadInt(root, "height", 1080);
                CurrentEdits.ThumbnailFrameSeconds = ReadDouble(root, "thumbnailTimeSec", 0);
                CurrentEdits.MusicStartTime = ReadDouble(root, "musicStartTime", 0);
                CurrentEdits.MusicDuration = ReadDouble(root, "musicDuration", 30);
                CurrentEdits.MusicVolume = ReadDouble(root, "musicVolume", 80);

                CropMarginLeft = Math.Clamp(CurrentEdits.CropX / 1920.0 * 100.0, 0, 45);
                CropMarginTop = Math.Clamp(CurrentEdits.CropY / 1080.0 * 100.0, 0, 45);
                CropMarginRight = Math.Clamp((1920.0 - (CurrentEdits.CropX + CurrentEdits.CropWidth)) / 1920.0 * 100.0, 0, 45);
                CropMarginBottom = Math.Clamp((1080.0 - (CurrentEdits.CropY + CurrentEdits.CropHeight)) / 1080.0 * 100.0, 0, 45);

                MusicStartTime = CurrentEdits.MusicStartTime;
                MusicDuration = CurrentEdits.MusicDuration;
                MusicVolume = CurrentEdits.MusicVolume;
            }
            catch
            {
                // Keep defaults if previously stored JSON is malformed.
            }
        }

        private static int ReadInt(JsonElement root, string name, int fallback)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
                    return i;

                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return fallback;
        }

        private static double ReadDouble(JsonElement root, string name, double fallback)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d))
                    return d;

                if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return fallback;
        }
    }
}
