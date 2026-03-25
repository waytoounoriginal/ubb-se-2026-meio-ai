using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using Windows.Media.Core;
using System;
using System.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    /// <summary>
    /// Single reel card with video player, like button, genre badge, progress bar,
    /// double-tap gesture, and heart animations.
    /// Owner: Tudor
    /// </summary>
    public sealed partial class ReelItemView : UserControl
    {
        private const int MockUserId = 1;

        /// <summary>
        /// Static flag set by MainWindow.Closed before disposal begins.
        /// Prevents any queued DispatcherQueue callbacks from touching XAML elements
        /// after the window starts tearing down.
        /// </summary>
        internal static bool IsAppClosing { get; set; }

        // Heart glyphs in Segoe MDL2 Assets
        private const string HeartOutline = "\uEB51";
        private const string HeartFilled = "\uEB52";

        private static readonly SolidColorBrush WhiteBrush = new(Colors.White);
        private static readonly SolidColorBrush RedBrush = new(Colors.Red);

        public static readonly DependencyProperty ReelProperty =
            DependencyProperty.Register("Reel", typeof(ReelModel), typeof(ReelItemView), new PropertyMetadata(null, OnReelChanged));

        public ReelModel Reel
        {
            get => (ReelModel)GetValue(ReelProperty);
            set => SetValue(ReelProperty, value);
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

        public ReelItemView()
        {
            this.InitializeComponent();
            _playbackService = App.Services.GetRequiredService<IClipPlaybackService>();
            _interactionService = App.Services.GetRequiredService<IReelInteractionService>();

            // Do NOT hook MediaEnded here — it's done in OnReelChanged after setting Source,
            // so we always hook the correct auto-created MediaPlayer instance.

            this.Unloaded += (s, e) =>
            {
                StopProgressTimer();
                UnsubscribeFromReel();
                DisposeMediaPlayer();
            };
        }

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
                    var source = playbackService?.GetMediaSource(reel.VideoUrl)
                        ?? MediaSource.CreateFromUri(new Uri(reel.VideoUrl));
                    view.ReelPlayer.Source = new Windows.Media.Playback.MediaPlaybackItem(source);
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

        private void OnReelPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (IsAppClosing || _disposed) return;
            if (args.PropertyName is nameof(ReelModel.IsLiked) or nameof(ReelModel.LikeCount))
            {
                if (sender is ReelModel reel)
                {
                    try
                    {
                        var dq = DispatcherQueue;
                        if (dq == null) return;
                        dq.TryEnqueue(() =>
                        {
                            if (IsAppClosing || _disposed) return;
                            try { UpdateLikeVisuals(reel.IsLiked, reel.LikeCount); }
                            catch { /* view may be torn down */ }
                        });
                    }
                    catch { /* DispatcherQueue torn down during close */ }
                }
            }
        }

        private void UnsubscribeFromReel()
        {
            if (_subscribedReel != null)
            {
                _subscribedReel.PropertyChanged -= OnReelPropertyChanged;
                _subscribedReel = null;
            }
        }

        private void UpdateGenreBadge(string? genre)
        {
            if (!string.IsNullOrEmpty(genre))
            {
                GenreText.Text = genre;
                GenreBadge.Visibility = Visibility.Visible;
            }
            else
            {
                GenreBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateLikeVisuals(bool isLiked, int likeCount)
        {
            HeartIcon.Glyph = isLiked ? HeartFilled : HeartOutline;
            HeartIcon.Foreground = isLiked ? RedBrush : WhiteBrush;
            LikeCountText.Text = likeCount.ToString();
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            await ToggleLikeWithAnimationAsync();
        }

        /// <summary>
        /// Double-tap gesture toggles like and plays heart burst animation at center.
        /// Per Task 12: double-tap gesture recognizer triggers ToggleLike + heart burst.
        /// </summary>
        private async void ReelItemView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (Reel == null) return;

            // Only like on double-tap (never unlike)
            if (!Reel.IsLiked)
            {
                await ToggleLikeWithAnimationAsync();
            }

            PlayHeartBurstAnimation();
        }

        private async Task ToggleLikeWithAnimationAsync()
        {
            if (Reel == null) return;

            // Optimistic UI update
            bool wasLiked = Reel.IsLiked;
            Reel.IsLiked = !wasLiked;
            Reel.LikeCount += wasLiked ? -1 : 1;

            // Scale-bounce animation on heart icon (Task 12)
            PlayHeartBounceAnimation();

            try
            {
                await _interactionService.ToggleLikeAsync(MockUserId, Reel.ReelId);
            }
            catch
            {
                // Revert on failure
                Reel.IsLiked = wasLiked;
                Reel.LikeCount += wasLiked ? 1 : -1;
            }
        }

        /// <summary>
        /// Scale-bounce animation: heart scales up to 1.4x then back to 1x.
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
                Storyboard.SetTarget(scaleX, HeartScale);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");

                var scaleY = new DoubleAnimationUsingKeyFrames();
                scaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1.0 });
                scaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(150), Value = 1.4 });
                scaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(300), Value = 1.0 });
                Storyboard.SetTarget(scaleY, HeartScale);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");

                storyboard.Children.Add(scaleX);
                storyboard.Children.Add(scaleY);
                storyboard.Begin();
            }
            catch { /* animation on torn-down element */ }
        }

        /// <summary>
        /// Heart burst animation at center: large heart fades in, scales up, then fades out.
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
                Storyboard.SetTarget(opacity, HeartBurst);
                Storyboard.SetTargetProperty(opacity, "Opacity");

                var burstScaleX = new DoubleAnimationUsingKeyFrames();
                burstScaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0.5 });
                burstScaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(200), Value = 1.3 });
                burstScaleX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(600), Value = 1.5 });
                Storyboard.SetTarget(burstScaleX, BurstScale);
                Storyboard.SetTargetProperty(burstScaleX, "ScaleX");

                var burstScaleY = new DoubleAnimationUsingKeyFrames();
                burstScaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0.5 });
                burstScaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(200), Value = 1.3 });
                burstScaleY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(600), Value = 1.5 });
                Storyboard.SetTarget(burstScaleY, BurstScale);
                Storyboard.SetTargetProperty(burstScaleY, "ScaleY");

                storyboard.Children.Add(opacity);
                storyboard.Children.Add(burstScaleX);
                storyboard.Children.Add(burstScaleY);
                storyboard.Begin();
            }
            catch { /* animation on torn-down element */ }
        }

        // ── Playback ──────────────────────────────────────────────────────

        public void PlayVideo()
        {
            if (_disposed || IsAppClosing) return;
            try
            {
                if (ReelPlayer.MediaPlayer != null)
                {
                    ReelPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                    ReelPlayer.MediaPlayer.Play();
                    StartProgressTimer();
                }
            }
            catch { }
        }

        public void PauseVideo()
        {
            if (_disposed) return;
            try
            {
                if (ReelPlayer.MediaPlayer != null)
                {
                    ReelPlayer.MediaPlayer.Pause();
                    StopProgressTimer();
                }
            }
            catch { }
        }

        // ── Progress bar timer ────────────────────────────────────────────

        private void StartProgressTimer()
        {
            if (_progressTimer == null)
            {
                _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                _progressTimer.Tick += ProgressTimer_Tick;
            }
            _progressTimer.Start();
        }

        private void StopProgressTimer()
        {
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _progressTimer.Tick -= ProgressTimer_Tick;
                _progressTimer = null;
            }
        }

        private void ProgressTimer_Tick(object? sender, object e)
        {
            if (IsAppClosing || _disposed) { StopProgressTimer(); return; }
            try
            {
                var player = ReelPlayer.MediaPlayer;
                if (player == null) { StopProgressTimer(); return; }

                var session = player.PlaybackSession;
                if (session.NaturalDuration.TotalSeconds > 0)
                {
                    double percent = (session.Position.TotalSeconds / session.NaturalDuration.TotalSeconds) * 100.0;
                    PlaybackProgress.Value = Math.Min(percent, 100);
                }
            }
            catch
            {
                // COM object may be revoked during teardown — stop polling
                StopProgressTimer();
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
                var player = ReelPlayer.MediaPlayer;
                if (player == null) return;

                // 1. Unhook events first — prevents callbacks during teardown
                player.MediaEnded -= MediaPlayer_MediaEnded;

                // 2. Sever XAML element connection (stops internal MF callbacks)
                var oldSource = ReelPlayer.Source as IDisposable;
                ReelPlayer.Source = null;
                ReelPlayer.SetMediaPlayer(null);

                // 3. Now safely tear down the COM player
                player.Pause();
                player.Source = null;
                oldSource?.Dispose();
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
            if (_disposed) return;
            _disposed = true;

            StopProgressTimer();
            DisposeCurrentPlayer();
        }

        private FlipView? GetParentFlipView(DependencyObject element)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is FlipView flipView)
                    return flipView;
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            // This callback fires on a Media Foundation background thread.
            // Do NOT access any DependencyProperty here.
            if (IsAppClosing || _disposed) return;

            // Capture DispatcherQueue reference while we know it's valid.
            // TryEnqueue returns false if the queue is shut down — no exception.
            try
            {
                var dq = DispatcherQueue;
                if (dq == null) return;

                bool enqueued = dq.TryEnqueue(() =>
                {
                    if (IsAppClosing || _disposed) return;
                    try
                    {
                        var player = ReelPlayer?.MediaPlayer;
                        if (player == null) return;
                        var flipView = GetParentFlipView(this);
                        if (flipView != null && flipView.SelectedIndex < flipView.Items.Count - 1)
                        {
                            flipView.SelectedIndex++;
                        }
                    }
                    catch { /* view may be torn down */ }
                });
            }
            catch
            {
                // DispatcherQueue may already be torn down — this is expected during close
            }
        }
    }
}
