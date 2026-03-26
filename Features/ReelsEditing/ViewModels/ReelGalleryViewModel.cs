using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels
{
    public partial class ReelGalleryViewModel : ObservableObject
    {
        private readonly ReelRepository _reelRepository;

        // Hard-coded to UserId=1 (same as the rest of the app)
        private const int CurrentUserId = 1;

        [ObservableProperty]
        private ObservableCollection<ReelModel> _userReels = new();

        [ObservableProperty]
        private ReelModel? _selectedReel;

        [ObservableProperty]
        private string _statusMessage = "Select a reel to edit.";

        [ObservableProperty]
        private bool _isLoaded;

        public ReelGalleryViewModel(ReelRepository reelRepository)
        {
            _reelRepository = reelRepository;
        }

        public async Task EnsureLoadedAsync()
        {
            if (!IsLoaded)
                await LoadReelsAsync();
        }

        [RelayCommand]
        private async Task LoadReelsAsync()
        {
            StatusMessage = "Loading reels...";
            try
            {
                var reels = await _reelRepository.GetUserReelsAsync(CurrentUserId);
                UserReels.Clear();
                foreach (var reel in reels)
                    UserReels.Add(reel);
                IsLoaded = true;
                StatusMessage = UserReels.Count > 0
                    ? $"{UserReels.Count} reel(s) found."
                    : "No reels uploaded yet. Upload a reel first.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading reels: {ex.Message}";
            }
        }
    }
}
