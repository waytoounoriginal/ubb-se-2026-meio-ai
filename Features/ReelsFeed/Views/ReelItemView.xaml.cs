using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using Windows.Media.Core;
using System;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    public sealed partial class ReelItemView : UserControl
    {
        public static readonly DependencyProperty ReelProperty =
            DependencyProperty.Register("Reel", typeof(ReelModel), typeof(ReelItemView), new PropertyMetadata(null, OnReelChanged));

        public ReelModel Reel
        {
            get => (ReelModel)GetValue(ReelProperty);
            set => SetValue(ReelProperty, value);
        }

        private readonly IClipPlaybackService _playbackService;

        public ReelItemView()
        {
            this.InitializeComponent();
            _playbackService = App.Services.GetRequiredService<IClipPlaybackService>();
            
            // Auto-scroll logic when a video hits the duration limit or finishes natively
            this.Loaded += (s, e) => {
                if (ReelPlayer.MediaPlayer != null)
                {
                    ReelPlayer.MediaPlayer.IsLoopingEnabled = false; // Prevent it from looping so the end fires
                    ReelPlayer.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                    ReelPlayer.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                }
            };

            // Unload the video and rigorously dispose the underlying COM objects when the control unloads to prevent Win32 exit crashes
            this.Unloaded += (s, e) => DisposeMediaPlayer();
        }

        private static void OnReelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReelItemView view && e.NewValue is ReelModel reel)
            {
                if (!string.IsNullOrEmpty(reel.VideoUrl))
                {
                    // Create the raw primitive media source
                    var source = MediaSource.CreateFromUri(new Uri(reel.VideoUrl));
                    
                    // Wrap in a MediaPlaybackItem to strictly limit memory/network buffering to the first 60 seconds.
                    // Since FlipView natively keeps the 1 previous and 1 next container alive in the background,
                    // just assigning this source triggers the OS to seamlessly pre-buffer the adjacent reels automatically!
                    var playbackItem = new Windows.Media.Playback.MediaPlaybackItem(source, TimeSpan.Zero, TimeSpan.FromSeconds(60));

                    view.ReelPlayer.Source = playbackItem;
                }
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

        /// <summary>
        /// Fully tears down the MediaPlayer COM object so it does not outlive the window handle.
        /// Must be called on the UI thread.
        /// </summary>
        public void DisposeMediaPlayer()
        {
            var player = ReelPlayer.MediaPlayer;
            if (player == null) return;

            player.MediaEnded -= MediaPlayer_MediaEnded;
            player.Pause();

            // Detach source and dispose it
            var oldSource = ReelPlayer.Source as IDisposable;
            ReelPlayer.Source = null;
            oldSource?.Dispose();

            // Detach the MediaPlayer from the element, then dispose the COM object
            ReelPlayer.SetMediaPlayer(null);
            player.Dispose();
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            // MediaEnded fires on a background thread, so we marshal back to the UI thread
            DispatcherQueue.TryEnqueue(() => {
                var flipView = GetParentFlipView(this);
                // If the video stops, simulate the user swiping one position down automatically.
                if (flipView != null && flipView.SelectedIndex < flipView.Items.Count - 1)
                {
                    flipView.SelectedIndex++;
                }
            });
        }
    }
}
