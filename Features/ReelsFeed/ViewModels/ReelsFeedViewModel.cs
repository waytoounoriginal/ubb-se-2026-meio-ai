using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ubb_se_2026_meio_ai;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Feed page.
    /// Owner: Tudor.
    /// </summary>
    public partial class ReelsFeedViewModel : ObservableObject
    {
        private const int MockUserId = 1;
        private const int RecommendedReelCount = 10;
        private const int PrefetchRange = 2;
        private const int InitialQueueIndex = 0;
        private const double MinTrackedWatchSeconds = 0.5;
        private const double MaxWatchRatio = 1.0;
        private const double PercentageMultiplier = 100.0;

        private readonly IRecommendationService _recommendationService;
        private readonly IClipPlaybackService _clipPlaybackService;
        private readonly IReelInteractionService _reelInteractionService;

        /// <summary>Tracks wall-clock watch time for the currently visible reel.</summary>
        private readonly Stopwatch _watchStopwatch = new ();
        private ReelModel? _previousReel;

        [ObservableProperty]
        private string _pageTitle = AppMessages.ReelsFeedPageTitle;

        [ObservableProperty]
        private string _statusMessage = AppMessages.ReelsFeedInitialStatus;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        /// <summary>Gets a value indicating whether <see cref="ErrorMessage"/> has a value.</summary>
        public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);

        /// <summary>Visibility helper: true when feed loaded successfully but returned zero clips.</summary>
        [ObservableProperty]
        private bool _isEmpty;

        [ObservableProperty]
        private ReelModel? _currentReel;

        partial void OnErrorMessageChanged(string? value) => this.OnPropertyChanged(nameof(HasError));

        /// <summary>
        /// Gets the queue of reels currently loaded for the feed.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<ReelModel> ReelQueue { get; } = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReelsFeedViewModel"/> class.
        /// </summary>
        /// <param name="recommendationService">Service used to retrieve recommended reels.</param>
        /// <param name="clipPlaybackService">Service used to prefetch and control clip playback state.</param>
        /// <param name="reelInteractionService">Service used to record reel views and likes.</param>
        public ReelsFeedViewModel(
            IRecommendationService recommendationService,
            IClipPlaybackService clipPlaybackService,
            IReelInteractionService reelInteractionService)
        {
            this._recommendationService = recommendationService;
            this._clipPlaybackService = clipPlaybackService;
            this._reelInteractionService = reelInteractionService;
        }

        /// <summary>
        /// Loads the reels feed, initializes current reel state, and updates UI status flags.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public async Task LoadFeedAsync()
        {
            this.IsLoading = true;
            this.ErrorMessage = null;
            this.IsEmpty = false;
            this.ReelQueue.Clear();

            try
            {
                var recommendedReels = await this._recommendationService.GetRecommendedReelsAsync(MockUserId, RecommendedReelCount);
                foreach (var recommendedReel in recommendedReels)
                {
                    this.ReelQueue.Add(recommendedReel);
                }

                // Load IsLiked and LikeCount onto each ReelModel so the UI can bind directly
                await this.LoadLikeDataAsync(recommendedReels);

                if (this.ReelQueue.Count > 0)
                {
                    this.CurrentReel = this.ReelQueue.First();
                    this._previousReel = this.CurrentReel;
                    this._watchStopwatch.Restart();
                    this.StatusMessage = string.Empty;
                    this.PrefetchNearby(InitialQueueIndex);
                }
                else
                {
                    this.IsEmpty = true;
                    this.StatusMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessage = string.Format(AppMessages.ReelsFeedLoadErrorFormat, ex.Message);
                this.StatusMessage = string.Empty;
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        private void PrefetchNearby(int currentIndex)
        {
            // 2 forward, 2 backward (excluding current)
            for (int offset = -PrefetchRange; offset <= PrefetchRange; offset++)
            {
                if (offset == InitialQueueIndex)
                {
                    continue;
                }

                int queueIndex = currentIndex + offset;
                if (queueIndex >= 0 && queueIndex < this.ReelQueue.Count)
                {
                    var nearbyReel = this.ReelQueue[queueIndex];
                    if (!string.IsNullOrEmpty(nearbyReel.VideoUrl))
                    {
                        _ = this._clipPlaybackService.PrefetchClipAsync(nearbyReel.VideoUrl);
                    }
                }
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        /// <summary>
        /// Handles forward scroll selection changes and prefetches nearby reels.
        /// </summary>
        /// <param name="newCurrent">The newly selected reel.</param>
        public void ScrollNext(ReelModel newCurrent)
        {
            this.FlushWatchData();
            this.CurrentReel = newCurrent;
            this._previousReel = newCurrent;
            this._watchStopwatch.Restart();

            var queueIndex = this.ReelQueue.IndexOf(newCurrent);
            if (queueIndex >= 0)
            {
                this.PrefetchNearby(queueIndex);
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        /// <summary>
        /// Handles backward scroll selection changes and prefetches nearby reels.
        /// </summary>
        /// <param name="newCurrent">The newly selected reel.</param>
        public void ScrollPrevious(ReelModel newCurrent)
        {
            this.FlushWatchData();
            this.CurrentReel = newCurrent;
            this._previousReel = newCurrent;
            this._watchStopwatch.Restart();

            var queueIndex = this.ReelQueue.IndexOf(newCurrent);
            if (queueIndex >= 0)
            {
                this.PrefetchNearby(queueIndex);
            }
        }

        /// <summary>
        /// Persists watch-duration data for the reel the user just scrolled away from.
        /// Per Task 10: calls RecordViewAsync with elapsed seconds and watch percentage.
        /// </summary>
        private void FlushWatchData()
        {
            this._watchStopwatch.Stop();
            var previouslyVisibleReel = this._previousReel;
            if (previouslyVisibleReel == null)
            {
                return;
            }

            double watchedSeconds = this._watchStopwatch.Elapsed.TotalSeconds;
            if (watchedSeconds < MinTrackedWatchSeconds)
            {
                return; // ignore trivial flicks
            }

            double watchPercentage = this.CalculateWatchPercentage(watchedSeconds, previouslyVisibleReel.FeatureDurationSeconds);

            // Fire-and-forget — feed stays navigable even if persistence fails (Task 10)
            _ = Task.Run(async () =>
            {
                try
                {
                    await this._reelInteractionService.RecordViewAsync(
                        MockUserId, previouslyVisibleReel.ReelId, watchedSeconds, watchPercentage);
                }
                catch
                {
                    // Logged server-side; feed continues.
                }
            });
        }

        /// <summary>
        /// Called when the page unloads (e.g. window closing) to flush the final reel's watch data.
        /// </summary>
        public void OnNavigatingAway()
        {
            this.FlushWatchData();
        }

        /// <summary>
        /// Calculates watched percentage from elapsed seconds and reel duration.
        /// </summary>
        /// <param name="watchedSeconds">Observed watch duration in seconds.</param>
        /// <param name="featureDurationSeconds">Total reel duration in seconds.</param>
        /// <returns>Watch percentage in the range 0-100.</returns>
        private double CalculateWatchPercentage(double watchedSeconds, double featureDurationSeconds)
        {
            if (featureDurationSeconds <= 0)
            {
                return 0;
            }

            return Math.Min(watchedSeconds / featureDurationSeconds, MaxWatchRatio) * PercentageMultiplier;
        }

        /// <summary>
        /// Populates IsLiked and LikeCount on each ReelModel in the batch.
        /// </summary>
        private async Task LoadLikeDataAsync(IList<ReelModel> reelBatch)
        {
            foreach (var reelItem in reelBatch)
            {
                try
                {
                    var userInteraction = await this._reelInteractionService.GetInteractionAsync(MockUserId, reelItem.ReelId);
                    reelItem.IsLiked = userInteraction?.IsLiked ?? false;
                    reelItem.LikeCount = await this._reelInteractionService.GetLikeCountAsync(reelItem.ReelId);
                }
                catch
                {
                    reelItem.IsLiked = false;
                    reelItem.LikeCount = 0;
                }
            }
        }
    }
}
