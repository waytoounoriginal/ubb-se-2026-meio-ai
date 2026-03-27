using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Views
{
    public sealed partial class ReelsEditingPage : Page
    {
        public ReelsEditingViewModel ViewModel { get; }
        public ReelGalleryViewModel GalleryViewModel { get; }
        public MusicSelectionDialogViewModel MusicDialogViewModel { get; }

        // Audio-only player for music preview (no MediaPlayerElement needed)
        private readonly MediaPlayer _musicPreviewPlayer = new();
        private readonly DispatcherTimer _videoProgressTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        private MediaPlayer? _subscribedVideoPlayer;
        private bool _isSyncingVideoControls;

        public ReelsEditingPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsEditingViewModel>();
            GalleryViewModel = App.Services.GetRequiredService<ReelGalleryViewModel>();
            MusicDialogViewModel = App.Services.GetRequiredService<MusicSelectionDialogViewModel>();
            this.InitializeComponent();
            _videoProgressTimer.Tick += VideoProgressTimer_Tick;
            this.Unloaded += Page_Unloaded;
            ResetVideoTransportUi();

            ViewModel.CropModeEntered += OnCropModeEntered;
            ViewModel.CropModeExited += OnCropModeExited;
            ViewModel.CropSaveStarted += OnCropSaveStarted;
            ViewModel.CropVideoUpdated += OnCropVideoUpdated;

            // Listen for IsEditing changes to toggle panels
            ViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsEditing))
                {
                    UpdatePanelVisibility();
                    // Reload gallery when returning from editor (e.g., after deletion)
                    if (!ViewModel.IsEditing)
                    {
                        StopMusicPreview();
                        await GalleryViewModel.LoadReelsCommand.ExecuteAsync(null);
                    }
                }
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await GalleryViewModel.EnsureLoadedAsync();
            if (!_videoProgressTimer.IsEnabled)
                _videoProgressTimer.Start();
            AttachVideoPlayerEvents();
            UpdateVideoTransportUi();
            UpdatePanelVisibility();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _videoProgressTimer.Stop();
            DetachVideoPlayerEvents();
            StopMusicPreview();
        }

        private void UpdatePanelVisibility()
        {
            if (ViewModel.IsEditing)
            {
                GalleryPanel.Visibility = Visibility.Collapsed;
                EditorPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StopVideo();
                GalleryPanel.Visibility = Visibility.Visible;
                EditorPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ReelGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection tracked by TwoWay binding
        }

        private async void ReelGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ubb_se_2026_meio_ai.Core.Models.ReelModel reel)
            {
                GalleryViewModel.SelectedReel = reel;
                await ViewModel.LoadReelAsync(reel);
                LoadVideo(reel.VideoUrl);
            }
        }

        private void LoadVideo(string videoUrl)
        {
            try
            {
                DetachVideoPlayerEvents();
                if (!string.IsNullOrEmpty(videoUrl) && Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
                {
                    ReelPlayer.Source = MediaSource.CreateFromUri(uri);
                    AttachVideoPlayerEvents();
                }
            }
            catch
            {
                // Video URL may be invalid; player will show empty
            }

            UpdateVideoTransportUi();
        }

        private void StopVideo()
        {
            try
            {
                DetachVideoPlayerEvents();
                ReelPlayer.Source = null;
            }
            catch { }

            ResetVideoTransportUi();
        }

        public Visibility GetStatusVisibility(bool hasStatus)
            => hasStatus ? Visibility.Visible : Visibility.Collapsed;

        private void AttachVideoPlayerEvents()
        {
            var mediaPlayer = ReelPlayer.MediaPlayer;
            if (mediaPlayer == null || ReferenceEquals(mediaPlayer, _subscribedVideoPlayer))
                return;

            DetachVideoPlayerEvents();
            _subscribedVideoPlayer = mediaPlayer;
            _subscribedVideoPlayer.MediaOpened += ReelPlayer_MediaOpened;
            _subscribedVideoPlayer.MediaEnded += ReelPlayer_MediaEnded;
        }

        private void DetachVideoPlayerEvents()
        {
            if (_subscribedVideoPlayer == null)
                return;

            _subscribedVideoPlayer.MediaOpened -= ReelPlayer_MediaOpened;
            _subscribedVideoPlayer.MediaEnded -= ReelPlayer_MediaEnded;
            _subscribedVideoPlayer = null;
        }

        private void ReelPlayer_MediaOpened(MediaPlayer sender, object args)
            => DispatcherQueue.TryEnqueue(UpdateVideoTransportUi);

        private void ReelPlayer_MediaEnded(MediaPlayer sender, object args)
            => DispatcherQueue.TryEnqueue(UpdateVideoTransportUi);

        private void VideoProgressTimer_Tick(object? sender, object e)
            => UpdateVideoTransportUi();

        private void VideoPlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = ReelPlayer.MediaPlayer;
            if (mediaPlayer == null)
                return;

            if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                mediaPlayer.Pause();
            else
                mediaPlayer.Play();

            UpdateVideoTransportUi();
        }

        private void VideoSeekSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isSyncingVideoControls || Math.Abs(e.NewValue - e.OldValue) < 0.001)
                return;

            var mediaPlayer = ReelPlayer.MediaPlayer;
            var session = mediaPlayer?.PlaybackSession;
            if (session == null)
                return;

            var durationSeconds = session.NaturalDuration.TotalSeconds;
            if (durationSeconds <= 0)
                return;

            var clampedSeconds = Math.Clamp(e.NewValue, 0, durationSeconds);
            session.Position = TimeSpan.FromSeconds(clampedSeconds);
            UpdateVideoTransportUi();
        }

        private void VideoMuteButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingVideoControls)
                return;

            if (ReelPlayer.MediaPlayer != null)
                ReelPlayer.MediaPlayer.IsMuted = true;

            UpdateVideoTransportUi();
        }

        private void VideoMuteButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingVideoControls)
                return;

            if (ReelPlayer.MediaPlayer != null)
                ReelPlayer.MediaPlayer.IsMuted = false;

            UpdateVideoTransportUi();
        }

        private void VideoFullscreenButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingVideoControls)
                return;

            ReelPlayer.IsFullWindow = true;
            UpdateVideoTransportUi();
        }

        private void VideoFullscreenButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingVideoControls)
                return;

            ReelPlayer.IsFullWindow = false;
            UpdateVideoTransportUi();
        }

        private void UpdateVideoTransportUi()
        {
            var mediaPlayer = ReelPlayer.MediaPlayer;
            var session = mediaPlayer?.PlaybackSession;

            if (mediaPlayer == null || session == null || ReelPlayer.Source == null)
            {
                ResetVideoTransportUi();
                return;
            }

            var duration = session.NaturalDuration;
            var durationSeconds = Math.Max(duration.TotalSeconds, 0);
            var currentSeconds = Math.Max(session.Position.TotalSeconds, 0);
            var clampedCurrent = durationSeconds > 0
                ? Math.Min(currentSeconds, durationSeconds)
                : 0;

            _isSyncingVideoControls = true;
            try
            {
                VideoSeekSlider.Maximum = durationSeconds;
                VideoSeekSlider.Value = clampedCurrent;
                VideoCurrentTimeText.Text = FormatPlaybackTime(TimeSpan.FromSeconds(clampedCurrent));
                VideoDurationText.Text = FormatPlaybackTime(duration);
                VideoPlayPauseIcon.Symbol = session.PlaybackState == MediaPlaybackState.Playing
                    ? Symbol.Pause
                    : Symbol.Play;
                VideoMuteButton.IsChecked = mediaPlayer.IsMuted;
                VideoMuteIcon.Symbol = mediaPlayer.IsMuted ? Symbol.Mute : Symbol.Volume;
                VideoFullscreenButton.IsChecked = ReelPlayer.IsFullWindow;
            }
            finally
            {
                _isSyncingVideoControls = false;
            }
        }

        private void ResetVideoTransportUi()
        {
            _isSyncingVideoControls = true;
            try
            {
                VideoSeekSlider.Maximum = 0;
                VideoSeekSlider.Value = 0;
                VideoCurrentTimeText.Text = "0:00";
                VideoDurationText.Text = "0:00";
                VideoPlayPauseIcon.Symbol = Symbol.Play;
                VideoMuteButton.IsChecked = false;
                VideoMuteIcon.Symbol = Symbol.Volume;
                VideoFullscreenButton.IsChecked = false;
            }
            finally
            {
                _isSyncingVideoControls = false;
            }
        }

        private static string FormatPlaybackTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return time.ToString(@"h\:mm\:ss");

            return time.ToString(@"m\:ss");
        }

        // ── Crop mode ────────────────────────────────────────────────────────

        private void OnCropModeEntered()
        {
            try { ReelPlayer.MediaPlayer?.Pause(); }
            catch { }

            CropOverlayRoot.Visibility = Visibility.Visible;
            UpdateCropOverlay();
        }

        private void OnCropModeExited()
        {
            CropOverlayRoot.Visibility = Visibility.Collapsed;
        }

        private void OnCropSaveStarted()
        {
            try
            {
                ReelPlayer.MediaPlayer?.Pause();
            }
            catch { }

            StopVideo();
        }

        private void OnCropVideoUpdated(string videoUrl)
            => LoadVideo(videoUrl);

        private void CropResumePreview_Click(object sender, RoutedEventArgs e)
        {
            ReelPlayer.MediaPlayer?.Play();
            UpdateVideoTransportUi();
        }

        private void CropPausePreview_Click(object sender, RoutedEventArgs e)
        {
            ReelPlayer.MediaPlayer?.Pause();
            UpdateVideoTransportUi();
        }

        private void CropMarginSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
            => UpdateCropOverlay();

        private void CropOverlayRoot_SizeChanged(object sender, SizeChangedEventArgs e)
            => UpdateCropOverlay();

        private void UpdateCropOverlay()
        {
            if (CropOverlayRoot.Visibility != Visibility.Visible)
                return;

            var width = CropOverlayRoot.ActualWidth;
            var height = CropOverlayRoot.ActualHeight;
            if (width <= 0 || height <= 0) return;

            var left = width * (ViewModel.CropMarginLeft / 100.0);
            var top = height * (ViewModel.CropMarginTop / 100.0);
            var right = width * (ViewModel.CropMarginRight / 100.0);
            var bottom = height * (ViewModel.CropMarginBottom / 100.0);

            if (left + right > width - 20)
                right = Math.Max(0, width - left - 20);
            if (top + bottom > height - 20)
                bottom = Math.Max(0, height - top - 20);

            CropRectangleBorder.Margin = new Thickness(left, top, right, bottom);
        }

        // ── Music preview ────────────────────────────────────────────────────

        /// <summary>
        /// Plays the selected music track starting from the chosen MusicStartTime.
        /// </summary>
        private void PlayMusicPreview_Click(object sender, RoutedEventArgs e)
        {
            var track = ViewModel.SelectedMusicTrack;
            if (track == null || string.IsNullOrWhiteSpace(track.AudioUrl))
            {
                ViewModel.StatusMessage = "No music track selected. Choose a track first.";
                ViewModel.IsStatusSuccess = false;
                return;
            }

            try
            {
                if (!Uri.TryCreate(track.AudioUrl, UriKind.Absolute, out var uri))
                {
                    ViewModel.StatusMessage = "Music URL is not valid.";
                    ViewModel.IsStatusSuccess = false;
                    return;
                }

                _musicPreviewPlayer.Source = MediaSource.CreateFromUri(uri);
                _musicPreviewPlayer.Volume = ViewModel.MusicVolume / 100.0;

                // Seek to the saved start time once the media is opened
                var startOffset = TimeSpan.FromSeconds(ViewModel.MusicStartTime);
                _musicPreviewPlayer.MediaOpened += OnMusicMediaOpened;
                void OnMusicMediaOpened(MediaPlayer mp, object? _)
                {
                    mp.MediaOpened -= OnMusicMediaOpened;
                    mp.PlaybackSession.Position = startOffset;
                    mp.Play();
                }

                ViewModel.StatusMessage = $"Playing preview from {startOffset:mm\\:ss}…";
                ViewModel.IsStatusSuccess = true;
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Preview failed: {ex.Message}";
                ViewModel.IsStatusSuccess = false;
            }
        }

        private void StopMusicPreview_Click(object sender, RoutedEventArgs e)
            => StopMusicPreview();

        private void StopMusicPreview()
        {
            try
            {
                _musicPreviewPlayer.Pause();
                _musicPreviewPlayer.Source = null;
            }
            catch { }
        }

        // ── Music selection dialog ────────────────────────────────────────────

        private async void ChooseMusicButton_Click(object sender, RoutedEventArgs e)
        {
            await MusicDialogViewModel.LoadTracksCommand.ExecuteAsync(null);

            var dialog = new ContentDialog
            {
                Title = "Choose Background Music",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Confirm",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            var listView = new ListView
            {
                ItemsSource = MusicDialogViewModel.AvailableTracks,
                Height = 300,
            };
            listView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                "<TextBlock Text=\"{Binding TrackName}\" Padding=\"8,4\"/>" +
                "</DataTemplate>");

            listView.SelectionChanged += (s, args) =>
            {
                if (listView.SelectedItem is ubb_se_2026_meio_ai.Core.Models.MusicTrackModel track)
                    MusicDialogViewModel.SelectedTrack = track;
            };

            dialog.Content = listView;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && MusicDialogViewModel.SelectedTrack != null)
                ViewModel.ApplyMusicSelection(MusicDialogViewModel.SelectedTrack);
        }
    }
}
