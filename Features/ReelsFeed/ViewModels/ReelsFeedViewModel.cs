using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>Tracks wall-clock watch time for the currently visible reel.</summary>
        private readonly Stopwatch _watchStopwatch = new();
        private ReelModel? _previousReel;


        [ObservableProperty]
        private string _pageTitle = "Reels Feed";

        [ObservableProperty]
        private string _statusMessage = "Scroll to discover reels.";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        /// <summary>Visibility helper: true when ErrorMessage is non-null/non-empty.</summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>Visibility helper: true when feed loaded successfully but returned zero clips.</summary>
        [ObservableProperty]
        private bool _isEmpty;

        [ObservableProperty]
        private ReelModel? _currentReel;

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

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
            IsEmpty = false;
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
                    _previousReel = CurrentReel;
                    _watchStopwatch.Restart();
                    StatusMessage = string.Empty;
                    PrefetchNearby(0);
                }
                else
                {
                    IsEmpty = true;
                    StatusMessage = string.Empty;
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

        private void PrefetchNearby(int currentIndex)
        {
            // 2 forward, 2 backward (excluding current)
            for (int offset = -2; offset <= 2; offset++)
            {
                if (offset == 0) continue;
                int idx = currentIndex + offset;
                if (idx >= 0 && idx < ReelQueue.Count)
                {
                    var reel = ReelQueue[idx];
                    if (!string.IsNullOrEmpty(reel.VideoUrl))
                    {
                        _ = _clipPlaybackService.PrefetchClipAsync(reel.VideoUrl);
                    }
                }
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public void ScrollNext(ReelModel newCurrent)
        {
            FlushWatchData();
            CurrentReel = newCurrent;
            _previousReel = newCurrent;
            _watchStopwatch.Restart();

            var index = ReelQueue.IndexOf(newCurrent);
            if (index >= 0)
            {
                PrefetchNearby(index);
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public void ScrollPrevious(ReelModel newCurrent)
        {
            FlushWatchData();
            CurrentReel = newCurrent;
            _previousReel = newCurrent;
            _watchStopwatch.Restart();

            var index = ReelQueue.IndexOf(newCurrent);
            if (index >= 0)
            {
                PrefetchNearby(index);
            }
        }

        /// <summary>
        /// Persists watch-duration data for the reel the user just scrolled away from.
        /// Per Task 10: calls RecordViewAsync with elapsed seconds and watch percentage.
        /// </summary>
        private void FlushWatchData()
        {
            _watchStopwatch.Stop();
            var reel = _previousReel;
            if (reel == null) return;

            double watchedSec = _watchStopwatch.Elapsed.TotalSeconds;
            if (watchedSec < 0.5) return; // ignore trivial flicks

            double watchPercentage = reel.FeatureDurationSeconds > 0
                ? Math.Min(watchedSec / reel.FeatureDurationSeconds, 1.0) * 100.0
                : 0;

            // Fire-and-forget — feed stays navigable even if persistence fails (Task 10)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _reelInteractionService.RecordViewAsync(
                        MockUserId, reel.ReelId, watchedSec, watchPercentage);
                }
                catch { /* logged server-side; feed continues */ }
            });
        }

        /// <summary>
        /// Called when the page unloads (e.g. window closing) to flush the final reel's watch data.
        /// </summary>
        public void OnNavigatingAway()
        {
            FlushWatchData();
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
