using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Feed page.
    /// Owner: Tudor
    /// </summary>
    public partial class ReelsFeedViewModel : ObservableObject
    {
        private const int MockUserId = 1;

        private readonly IRecommendationService _recommendationService;
        private readonly IClipPlaybackService _clipPlaybackService;
        private readonly IReelInteractionService _reelInteractionService;

        [ObservableProperty]
        private string _pageTitle = "Reels Feed";

        [ObservableProperty]
        private string _statusMessage = "Scroll to discover reels.";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private ReelModel? _currentReel;

        public System.Collections.ObjectModel.ObservableCollection<ReelModel> ReelQueue { get; } = new();

        public ReelsFeedViewModel(
            IRecommendationService recommendationService,
            IClipPlaybackService clipPlaybackService,
            IReelInteractionService reelInteractionService)
        {
            _recommendationService = recommendationService;
            _clipPlaybackService = clipPlaybackService;
            _reelInteractionService = reelInteractionService;
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public async Task LoadFeedAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            ReelQueue.Clear();

            try
            {
                var reels = await _recommendationService.GetRecommendedReelsAsync(MockUserId, 10);
                foreach (var r in reels)
                {
                    ReelQueue.Add(r);
                }

                // Load IsLiked and LikeCount onto each ReelModel so the UI can bind directly
                await LoadLikeDataAsync(reels);

                if (ReelQueue.Count > 0)
                {
                    CurrentReel = ReelQueue[0];
                    StatusMessage = string.Empty;
                    PrefetchUpcoming(0);
                }
                else
                {
                    StatusMessage = "No clips found. Feed empty.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading feed: {ex.Message}";
                StatusMessage = string.Empty;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrefetchUpcoming(int currentIndex)
        {
            for (int i = 1; i <= 3; i++)
            {
                if (currentIndex + i < ReelQueue.Count)
                {
                    var nextReel = ReelQueue[currentIndex + i];
                    if (!string.IsNullOrEmpty(nextReel.VideoUrl))
                    {
                        _ = _clipPlaybackService.PrefetchClipAsync(nextReel.VideoUrl);
                    }
                }
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public void ScrollNext(ReelModel newCurrent)
        {
            CurrentReel = newCurrent;
            var index = ReelQueue.IndexOf(newCurrent);
            if (index >= 0)
            {
                PrefetchUpcoming(index);
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public void ScrollPrevious(ReelModel newCurrent)
        {
            CurrentReel = newCurrent;
        }

        /// <summary>
        /// Populates IsLiked and LikeCount on each ReelModel in the batch.
        /// </summary>
        private async Task LoadLikeDataAsync(IList<ReelModel> reels)
        {
            foreach (var reel in reels)
            {
                try
                {
                    var interaction = await _reelInteractionService.GetInteractionAsync(MockUserId, reel.ReelId);
                    reel.IsLiked = interaction?.IsLiked ?? false;
                    reel.LikeCount = await _reelInteractionService.GetLikeCountAsync(reel.ReelId);
                }
                catch
                {
                    reel.IsLiked = false;
                    reel.LikeCount = 0;
                }
            }
        }
    }
}
