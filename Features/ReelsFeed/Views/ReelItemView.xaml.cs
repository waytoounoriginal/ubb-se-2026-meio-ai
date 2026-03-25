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
        /// Stored reference so we can unhook PropertyChanged when the Reel changes
        /// or the control is recycled by the VirtualizingStackPanel.
        /// </summary>
        private ReelModel? _subscribedReel;

        public ReelItemView()
        {
            this.InitializeComponent();
            _playbackService = App.Services.GetRequiredService<IClipPlaybackService>();
            _interactionService = App.Services.GetRequiredService<IReelInteractionService>();

            this.Loaded += (s, e) =>
            {
                if (ReelPlayer.MediaPlayer != null)
                {
                    ReelPlayer.MediaPlayer.IsLoopingEnabled = false;
                    ReelPlayer.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                    ReelPlayer.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                }
            };

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

                // Use prefetched MediaSource if available, otherwise create fresh
                if (!string.IsNullOrEmpty(reel.VideoUrl))
                {
                    var playbackService = view._playbackService as ClipPlaybackService;
                    var source = playbackService?.GetMediaSource(reel.VideoUrl)
                        ?? MediaSource.CreateFromUri(new Uri(reel.VideoUrl));
                    view.ReelPlayer.Source = new Windows.Media.Playback.MediaPlaybackItem(source);
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
            if (IsAppClosing) return;
            if (args.PropertyName is nameof(ReelModel.IsLiked) or nameof(ReelModel.LikeCount))
            {
                if (sender is ReelModel reel)
                {
                    DispatcherQueue?.TryEnqueue(() =>
                    {
                        if (IsAppClosing) return;
                        try { UpdateLikeVisuals(reel.IsLiked, reel.LikeCount); }
                        catch { /* view may be torn down */ }
                    });
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
            if (ReelPlayer.MediaPlayer != null)
            {
                ReelPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                ReelPlayer.MediaPlayer.Play();
                StartProgressTimer();
            }
        }

        public void PauseVideo()
        {
            if (ReelPlayer.MediaPlayer != null)
            {
                ReelPlayer.MediaPlayer.Pause();
                StopProgressTimer();
            }
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
            _progressTimer?.Stop();
        }

        private void ProgressTimer_Tick(object? sender, object e)
        {
            if (IsAppClosing) { StopProgressTimer(); return; }
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
        /// Fully tears down the MediaPlayer COM object so it does not outlive the window handle.
        /// Guards on <c>ReelPlayer.MediaPlayer == null</c> so it is safe to call repeatedly
        /// and survives container recycling (VirtualizingStackPanel reuse).
        /// </summary>
        public void DisposeMediaPlayer()
        {
            StopProgressTimer();

            try
            {
                var player = ReelPlayer.MediaPlayer;
                if (player == null) return;

                player.MediaEnded -= MediaPlayer_MediaEnded;

                // 1. Stop the Media Foundation pipeline inside the player itself
                player.Pause();
                player.Source = null;

                // 2. Detach the playback source from the XAML element
                var oldElementSource = ReelPlayer.Source as IDisposable;
                ReelPlayer.Source = null;
                oldElementSource?.Dispose();

                // 3. Sever the link between the element and the COM player,
                //    then release the COM object.
                ReelPlayer.SetMediaPlayer(null);
                player.Dispose();
            }
            catch
            {
                // Swallow disposal errors — we are tearing down
            }
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
            // Do NOT access ReelPlayer.MediaPlayer here — it's a DependencyProperty
            // and touching it off the UI thread throws a COMException.
            if (IsAppClosing) return;
            DispatcherQueue?.TryEnqueue(() =>
            {
                if (IsAppClosing) return;
                try
                {
                    if (ReelPlayer.MediaPlayer == null) return;
                    var flipView = GetParentFlipView(this);
                    if (flipView != null && flipView.SelectedIndex < flipView.Items.Count - 1)
                    {
                        flipView.SelectedIndex++;
                    }
                }
                catch { /* view may be torn down */ }
            });
        }
    }
}
