using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using Windows.Media.Core;
using System;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    public sealed partial class ReelItemView : UserControl
    {
        private const int MockUserId = 1;

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

            this.Unloaded += (s, e) => DisposeMediaPlayer();
        }

        private static void OnReelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReelItemView view && e.NewValue is ReelModel reel)
            {
                // Set video source
                if (!string.IsNullOrEmpty(reel.VideoUrl))
                {
                    var source = MediaSource.CreateFromUri(new Uri(reel.VideoUrl));
                    var playbackItem = new Windows.Media.Playback.MediaPlaybackItem(source, TimeSpan.Zero, TimeSpan.FromSeconds(60));
                    view.ReelPlayer.Source = playbackItem;
                }

                // Sync like visuals from model state
                view.UpdateLikeVisuals(reel.IsLiked, reel.LikeCount);

                // Listen for model property changes (e.g. if liked state loaded async after binding)
                reel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName is nameof(ReelModel.IsLiked) or nameof(ReelModel.LikeCount))
                    {
                        view.DispatcherQueue?.TryEnqueue(() =>
                            view.UpdateLikeVisuals(reel.IsLiked, reel.LikeCount));
                    }
                };
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
            if (Reel == null) return;

            // Optimistic UI update
            bool wasLiked = Reel.IsLiked;
            Reel.IsLiked = !wasLiked;
            Reel.LikeCount += wasLiked ? -1 : 1;

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

        public void PlayVideo()
        {
            if (ReelPlayer.MediaPlayer != null)
            {
                ReelPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                ReelPlayer.MediaPlayer.Play();
            }
        }

        public void PauseVideo()
        {
            if (ReelPlayer.MediaPlayer != null)
            {
                ReelPlayer.MediaPlayer.Pause();
            }
        }

        /// <summary>
        /// Fully tears down the MediaPlayer COM object so it does not outlive the window handle.
        /// Guards on <c>ReelPlayer.MediaPlayer == null</c> so it is safe to call repeatedly
        /// and survives container recycling (VirtualizingStackPanel reuse).
        /// </summary>
        public void DisposeMediaPlayer()
        {
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
            // MediaPlayer is detached — this is a stale callback, ignore it
            if (ReelPlayer.MediaPlayer == null) return;

            DispatcherQueue?.TryEnqueue(() =>
            {
                if (ReelPlayer.MediaPlayer == null) return;
                var flipView = GetParentFlipView(this);
                if (flipView != null && flipView.SelectedIndex < flipView.Items.Count - 1)
                {
                    flipView.SelectedIndex++;
                }
            });
        }
    }
}
