using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.ComponentModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using Windows.Media.Core;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    /// <summary>
    /// Single reel card with video player, like button, genre badge, progress bar,
    /// double-tap gesture, and heart animations.
    /// Owner: Tudor.
    /// </summary>
    public sealed partial class ReelItemView : UserControl
    {
        private const int MockUserId = 1;
        private const int NoItemsCount = 0;
        private const int LastItemOffset = 1;
        private const int ProgressTimerIntervalMs = 250;
        private const double PercentageMultiplier = 100.0;
        private const double MaxProgressPercentage = 100.0;

        /// <summary>
        /// Gets or sets a value indicating whether the app is closing.
        /// Prevents any queued DispatcherQueue callbacks from touching XAML elements
        /// after the window starts tearing down.
        /// </summary>
        internal static bool IsAppClosing { get; set; }

        // Heart glyphs in Segoe MDL2 Assets
        private const string HeartOutline = "\uEB51";
        private const string HeartFilled = "\uEB52";

        private static readonly SolidColorBrush WhiteBrush = new (Colors.White);
        private static readonly SolidColorBrush RedBrush = new (Colors.Red);

        /// <summary>
        /// Identifies the <see cref="Reel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ReelProperty =
            DependencyProperty.Register("Reel", typeof(ReelModel), typeof(ReelItemView), new PropertyMetadata(null, OnReelChanged));

        /// <summary>
        /// Gets or sets the reel displayed by this view.
        /// </summary>
        public ReelModel Reel
        {
            get
            {
                return (ReelModel)this.GetValue(ReelProperty);
            }

            set
            {
                this.SetValue(ReelProperty, value);
            }
        }

        private readonly IClipPlaybackService _playbackService;
        private readonly IReelInteractionService _interactionService;
        private DispatcherTimer? _progressTimer;

        /// <summary>
        /// Per-instance flag to prevent double-disposal and post-dispose access.
        /// Reset when the container is recycled via OnReelChanged.
        /// </summary>
        private volatile bool _disposed;

        /// <summary>
        /// Stored reference so we can unhook PropertyChanged when the Reel changes
        /// or the control is recycled by the VirtualizingStackPanel.
        /// </summary>
        private ReelModel? _subscribedReel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReelItemView"/> class.
        /// </summary>
        public ReelItemView()
        {
            this.InitializeComponent();
            this._playbackService = App.Services.GetRequiredService<IClipPlaybackService>();
            this._interactionService = App.Services.GetRequiredService<IReelInteractionService>();

            // Do NOT hook MediaEnded here — it's done in OnReelChanged after setting Source,
            // so we always hook the correct auto-created MediaPlayer instance.
            this.Unloaded += (s, e) =>
            {
                this.StopProgressTimer();
                this.UnsubscribeFromReel();
                this.DisposeMediaPlayer();
            };
        }

        /// <summary>
        /// Updates the view when the bound reel changes.
        /// </summary>
        /// <param name="d">The dependency object that owns the property.</param>
        /// <param name="e">The property change data.</param>
        private static void OnReelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReelItemView view && e.NewValue is ReelModel reel)
            {
                // Unhook from the previous model before subscribing to the new one
                view.UnsubscribeFromReel();

                // Reset progress bar immediately so stale position doesn't linger
                view.PlaybackProgress.Value = 0;
                view.StopProgressTimer();

                // Dispose the PREVIOUS MediaPlayer before setting a new source.
                // This prevents orphaned COM objects from container recycling.
                view.DisposeCurrentPlayer();

                // Container is being reused — clear the disposed flag
                view._disposed = false;

                // Set the new source — MediaPlayerElement will auto-create a new MediaPlayer
                if (!string.IsNullOrEmpty(reel.VideoUrl))
                {
                    var playbackService = view._playbackService as ClipPlaybackService;
                    var mediaSource = playbackService?.GetMediaSource(reel.VideoUrl)
                        ?? MediaSource.CreateFromUri(new Uri(reel.VideoUrl));
                    view.ReelPlayer.Source = new Windows.Media.Playback.MediaPlaybackItem(mediaSource);
                }

                // Hook MediaEnded on the newly created MediaPlayer
                if (view.ReelPlayer.MediaPlayer != null)
                {
                    view.ReelPlayer.MediaPlayer.IsLoopingEnabled = false;
                    view.ReelPlayer.MediaPlayer.MediaEnded += view.MediaPlayer_MediaEnded;
                }

                // Sync like visuals from model state
                view.UpdateLikeVisuals(reel.IsLiked, reel.LikeCount);

                // Update genre badge
                view.UpdateGenreBadge(reel.Genre);

                // Listen for model property changes (e.g. if liked state loaded async after binding)
                view._subscribedReel = reel;
                reel.PropertyChanged += view.OnReelPropertyChanged;
            }
        }

        /// <summary>
        /// Reacts to reel model changes that affect the like visuals.
        /// </summary>
        /// <param name="sender">The reel model that raised the event.</param>
        /// <param name="args">The property change arguments.</param>
        private void OnReelPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (ReelItemView.IsAppClosing || this._disposed)
            {
                return;
            }

            if (args.PropertyName is nameof(ReelModel.IsLiked) or nameof(ReelModel.LikeCount))
            {
                if (sender is ReelModel reel)
                {
                    try
                    {
                        var dispatcherQueue = this.DispatcherQueue;
                        if (dispatcherQueue == null)
                        {
                            return;
                        }

                        dispatcherQueue.TryEnqueue(() =>
                        {
                            if (ReelItemView.IsAppClosing || this._disposed)
                            {
                                return;
                            }

                            try
                            {
                                this.UpdateLikeVisuals(reel.IsLiked, reel.LikeCount);
                            }
                            catch
                            {
                                // view may be torn down
                            }
                        });
                    }
                    catch
                    {
                        // DispatcherQueue torn down during close
                    }
                }
            }
        }

        /// <summary>
        /// Detaches the property-changed handler from the current reel model.
        /// </summary>
        private void UnsubscribeFromReel()
        {
            if (this._subscribedReel != null)
            {
                this._subscribedReel.PropertyChanged -= this.OnReelPropertyChanged;
                this._subscribedReel = null;
            }
        }

        /// <summary>
        /// Updates the genre badge text and visibility.
        /// </summary>
        /// <param name="genre">The genre text to show, or <c>null</c> to hide the badge.</param>
        private void UpdateGenreBadge(string? genre)
        {
            if (!string.IsNullOrEmpty(genre))
            {
                this.GenreText.Text = genre;
                this.GenreBadge.Visibility = Visibility.Visible;
            }
            else
            {
                this.GenreBadge.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Synchronizes the heart icon and like count with the reel state.
        /// </summary>
        /// <param name="isLiked">A value indicating whether the reel is liked.</param>
        /// <param name="likeCount">The current like count.</param>
        private void UpdateLikeVisuals(bool isLiked, int likeCount)
        {
            this.HeartIcon.Glyph = isLiked ? HeartFilled : HeartOutline;
            this.HeartIcon.Foreground = isLiked ? RedBrush : WhiteBrush;
            this.LikeCountText.Text = likeCount.ToString();
        }

        /// <summary>
        /// Handles the like button click by toggling the current reel state.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The routed event data.</param>
        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            await this.ToggleLikeWithAnimationAsync();
        }

        /// <summary>
        /// Double-tap gesture toggles like and plays heart burst animation at center.
        /// Per Task 12: double-tap gesture recognizer triggers ToggleLike + heart burst.
        /// </summary>
        private async void ReelItemView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (this.Reel == null)
            {
                return;
            }

            // Only like on double-tap (never unlike)
            if (!this.Reel.IsLiked)
            {
                await this.ToggleLikeWithAnimationAsync();
            }

            this.PlayHeartBurstAnimation();
        }

        /// <summary>
        /// Toggles the reel like state and plays the associated animation.
        /// </summary>
        /// <returns>A task that completes when the persistence call finishes.</returns>
        private async Task ToggleLikeWithAnimationAsync()
        {
            if (this.Reel == null)
            {
                return;
            }

            // Optimistic UI update
            bool wasLiked = this.Reel.IsLiked;
            this.Reel.IsLiked = !wasLiked;
            this.Reel.LikeCount += wasLiked ? -1 : 1;

            // Scale-bounce animation on heart icon (Task 12)
            this.PlayHeartBounceAnimation();

            try
            {
                await this._interactionService.ToggleLikeAsync(MockUserId, this.Reel.ReelId);
            }
            catch
            {
                // Revert on failure
                this.Reel.IsLiked = wasLiked;
                this.Reel.LikeCount += wasLiked ? 1 : -1;
            }
        }

        /// <summary>
        /// Plays the compact heart bounce animation on the like icon.
        /// </summary>
        private void PlayHeartBounceAnimation()
        {
            try
            {
                var storyboard = new Storyboard();

                var scaleX = new DoubleAnimationUsingKeyFrames();
                scaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1.0 });
                scaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(150), Value = 1.4 });
                scaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(300), Value = 1.0 });
                Storyboard.SetTarget(scaleX, this.HeartScale);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");

                var scaleY = new DoubleAnimationUsingKeyFrames();
                scaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1.0 });
                scaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(150), Value = 1.4 });
                scaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(300), Value = 1.0 });
                Storyboard.SetTarget(scaleY, this.HeartScale);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");

                storyboard.Children.Add(scaleX);
                storyboard.Children.Add(scaleY);
                storyboard.Begin();
            }
            catch
            {
                // animation on torn-down element
            }
        }

        /// <summary>
        /// Plays the large heart burst animation used for double-tap likes.
        /// </summary>
        private void PlayHeartBurstAnimation()
        {
            try
            {
                var storyboard = new Storyboard();

                var opacity = new DoubleAnimationUsingKeyFrames();
                opacity.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0 });
                opacity.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(100), Value = 1.0 });
                opacity.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(600), Value = 0 });
                Storyboard.SetTarget(opacity, this.HeartBurst);
                Storyboard.SetTargetProperty(opacity, "Opacity");

                var burstScaleX = new DoubleAnimationUsingKeyFrames();
                burstScaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0.5 });
                burstScaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(200), Value = 1.3 });
                burstScaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(600), Value = 1.5 });
                Storyboard.SetTarget(burstScaleX, this.BurstScale);
                Storyboard.SetTargetProperty(burstScaleX, "ScaleX");

                var burstScaleY = new DoubleAnimationUsingKeyFrames();
                burstScaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0.5 });
                burstScaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(200), Value = 1.3 });
                burstScaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(600), Value = 1.5 });
                Storyboard.SetTarget(burstScaleY, this.BurstScale);
                Storyboard.SetTargetProperty(burstScaleY, "ScaleY");

                storyboard.Children.Add(opacity);
                storyboard.Children.Add(burstScaleX);
                storyboard.Children.Add(burstScaleY);
                storyboard.Begin();
            }
            catch
            {
                // animation on torn-down element
            }
        }

        // ── Playback ──────────────────────────────────────────────────────

        /// <summary>
        /// Starts video playback for the current reel and begins progress updates.
        /// </summary>
        public void PlayVideo()
        {
            if (this._disposed || ReelItemView.IsAppClosing)
            {
                return;
            }

            try
            {
                if (this.ReelPlayer.MediaPlayer != null)
                {
                    this.ReelPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                    this.ReelPlayer.MediaPlayer.Play();
                    this.StartProgressTimer();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Pauses video playback for the current reel and stops progress updates.
        /// </summary>
        public void PauseVideo()
        {
            if (this._disposed)
            {
                return;
            }

            try
            {
                if (this.ReelPlayer.MediaPlayer != null)
                {
                    this.ReelPlayer.MediaPlayer.Pause();
                    this.StopProgressTimer();
                }
            }
            catch
            {
            }
        }

        // ── Progress bar timer ────────────────────────────────────────────

        /// <summary>
        /// Starts the timer that keeps the playback progress bar in sync.
        /// </summary>
        private void StartProgressTimer()
        {
            if (this._progressTimer == null)
            {
                this._progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ProgressTimerIntervalMs) };
                this._progressTimer.Tick += this.ProgressTimer_Tick;
            }

            this._progressTimer.Start();
        }

        /// <summary>
        /// Stops the playback progress timer and releases its tick handler.
        /// </summary>
        private void StopProgressTimer()
        {
            if (this._progressTimer != null)
            {
                this._progressTimer.Stop();
                this._progressTimer.Tick -= this.ProgressTimer_Tick;
                this._progressTimer = null;
            }
        }

        /// <summary>
        /// Updates the playback progress bar from the current media session.
        /// </summary>
        /// <param name="sender">The timer that raised the tick.</param>
        /// <param name="e">The event data.</param>
        private void ProgressTimer_Tick(object? sender, object e)
        {
            if (ReelItemView.IsAppClosing || this._disposed)
            {
                this.StopProgressTimer();
                return;
            }

            try
            {
                var player = this.ReelPlayer.MediaPlayer;
                if (player == null)
                {
                    this.StopProgressTimer();
                    return;
                }

                var session = player.PlaybackSession;
                if (session.NaturalDuration.TotalSeconds > 0)
                {
                    double progressPercentage = (session.Position.TotalSeconds / session.NaturalDuration.TotalSeconds) * PercentageMultiplier;
                    this.PlaybackProgress.Value = Math.Min(progressPercentage, MaxProgressPercentage);
                }
            }
            catch
            {
                // COM object may be revoked during teardown — stop polling
                this.StopProgressTimer();
            }
        }

        // ── Disposal ──────────────────────────────────────────────────────

        /// <summary>
        /// Tears down the current MediaPlayer without setting <c>_disposed</c>,
        /// so the container can be recycled with a new Source in OnReelChanged.
        /// </summary>
        private void DisposeCurrentPlayer()
        {
            try
            {
                var player = this.ReelPlayer.MediaPlayer;
                if (player == null)
                {
                    return;
                }

                // 1. Unhook events first — prevents callbacks during teardown
                player.MediaEnded -= this.MediaPlayer_MediaEnded;

                // 2. Pause and detach sources safely while COM player is still active
                player.Pause();
                var previousSource = this.ReelPlayer.Source as IDisposable;

                // 3. Sever XAML element connection (stops internal MF callbacks)
                this.ReelPlayer.Source = null;
                this.ReelPlayer.SetMediaPlayer(null);

                // 4. Dispose safely
                player.Source = null;
                previousSource?.Dispose();
                player.Dispose();
            }
            catch
            {
                // Swallow — we are tearing down
            }
        }

        /// <summary>
        /// Final disposal — sets <c>_disposed</c> to block all future access.
        /// Called by Unloaded and by MainWindow.Closed tree walk.
        /// </summary>
        public void DisposeMediaPlayer()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;

            this.StopProgressTimer();
            this.DisposeCurrentPlayer();
        }

        /// <summary>
        /// Walks up the visual tree to find the containing <see cref="FlipView"/>.
        /// </summary>
        /// <param name="element">The starting element.</param>
        /// <returns>The nearest parent <see cref="FlipView"/>, or <c>null</c> if none exists.</returns>
        private FlipView? GetParentFlipView(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is FlipView flipView)
                {
                    return flipView;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            // This callback fires on a Media Foundation background thread.
            // Do NOT access any DependencyProperty here.
            if (ReelItemView.IsAppClosing || this._disposed)
            {
                return;
            }

            // Capture DispatcherQueue reference while we know it's valid.
            // TryEnqueue returns false if the queue is shut down — no exception.
            try
            {
                var dispatcherQueue = this.DispatcherQueue;
                if (dispatcherQueue == null)
                {
                    return;
                }

                dispatcherQueue.TryEnqueue(() =>
                {
                    if (ReelItemView.IsAppClosing || this._disposed)
                    {
                        return;
                    }

                    try
                    {
                        var mediaPlayer = this.ReelPlayer?.MediaPlayer;
                        if (mediaPlayer == null)
                        {
                            return;
                        }

                        var parentFlipView = this.GetParentFlipView(this);
                        if (parentFlipView != null && parentFlipView.SelectedIndex < parentFlipView.Items.Count - LastItemOffset)
                        {
                            parentFlipView.SelectedIndex++;
                        }
                    }
                    catch
                    {
                        // view may be torn down
                    }
                });
            }
            catch
            {
                // DispatcherQueue may already be torn down — this is expected during close
            }
        }
    }
}
