using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;
using Windows.Media.Core;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Views
{
    public sealed partial class ReelsEditingPage : Page
    {
        public ReelsEditingViewModel ViewModel { get; }
        public ReelGalleryViewModel GalleryViewModel { get; }
        public MusicSelectionDialogViewModel MusicDialogViewModel { get; }
        private double _selectedThumbnailFrameSeconds;

        public ReelsEditingPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsEditingViewModel>();
            GalleryViewModel = App.Services.GetRequiredService<ReelGalleryViewModel>();
            MusicDialogViewModel = App.Services.GetRequiredService<MusicSelectionDialogViewModel>();
            this.InitializeComponent();

            ViewModel.CropModeEntered += OnCropModeEntered;
            ViewModel.CropModeExited += OnCropModeExited;

            // Listen for IsEditing changes to toggle panels
            ViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsEditing))
                {
                    UpdatePanelVisibility();
                    // Reload gallery when returning from editor (e.g., after deletion)
                    if (!ViewModel.IsEditing)
                    {
                        await GalleryViewModel.LoadReelsCommand.ExecuteAsync(null);
                    }
                }
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await GalleryViewModel.EnsureLoadedAsync();
            UpdatePanelVisibility();

            if (ThumbnailPreviewPlayer != null)
                ThumbnailPreviewPlayer.Visibility = Visibility.Collapsed;
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
                SyncSavedThumbnailSelection();
            }
        }

        private void LoadVideo(string videoUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(videoUrl) && Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
                {
                    ReelPlayer.Source = MediaSource.CreateFromUri(uri);
                    ThumbnailPreviewPlayer.Source = MediaSource.CreateFromUri(uri);
                }
            }
            catch
            {
                // Video URL may be invalid; player will show empty
            }
        }

        private void StopVideo()
        {
            try
            {
                ReelPlayer.Source = null;
                ThumbnailPreviewPlayer.Source = null;
            }
            catch { }
        }

        private void SelectThumbnailFrameButton_Click(object sender, RoutedEventArgs e)
        {
            var currentPosition = ReelPlayer.MediaPlayer?.PlaybackSession?.Position ?? TimeSpan.Zero;
            _selectedThumbnailFrameSeconds = currentPosition.TotalSeconds;
            ViewModel.CurrentEdits.ThumbnailFrameSeconds = _selectedThumbnailFrameSeconds;
            SelectedThumbnailTimestampText.Text = $"Selected frame: {TimeSpan.FromSeconds(_selectedThumbnailFrameSeconds):mm\\:ss\\.f}";
            ViewModel.StatusMessage = "Frame selected. Click Save Edits to persist thumbnail selection.";
            ViewModel.IsStatusSuccess = true;

            try
            {
                var target = TimeSpan.FromSeconds(_selectedThumbnailFrameSeconds);
                if (ThumbnailPreviewPlayer.MediaPlayer?.PlaybackSession != null)
                {
                    ThumbnailPreviewPlayer.MediaPlayer.PlaybackSession.Position = target;
                    ThumbnailPreviewPlayer.MediaPlayer.Pause();
                    ThumbnailPreviewPlayer.Visibility = Visibility.Visible;
                }

                if (ReelPlayer.MediaPlayer?.PlaybackSession != null)
                {
                    ReelPlayer.MediaPlayer.PlaybackSession.Position = target;
                    ReelPlayer.MediaPlayer.Pause();
                }
            }
            catch
            {
                // Ignore if media is not seekable.
            }
        }

        private void SyncSavedThumbnailSelection()
        {
            var savedSeconds = ViewModel.CurrentEdits.ThumbnailFrameSeconds;
            if (savedSeconds <= 0)
            {
                SelectedThumbnailTimestampText.Text = "No frame selected";
                ThumbnailPreviewPlayer.Visibility = Visibility.Collapsed;
                return;
            }

            _selectedThumbnailFrameSeconds = savedSeconds;
            SelectedThumbnailTimestampText.Text = $"Selected frame: {TimeSpan.FromSeconds(savedSeconds):mm\\:ss\\.f}";

            try
            {
                if (ThumbnailPreviewPlayer.MediaPlayer?.PlaybackSession != null)
                {
                    ThumbnailPreviewPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(savedSeconds);
                    ThumbnailPreviewPlayer.MediaPlayer.Pause();
                    ThumbnailPreviewPlayer.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // Ignore seek failures for remote/invalid media.
            }
        }

        public Visibility GetStatusVisibility(bool hasStatus)
        {
            return hasStatus ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnCropModeEntered()
        {
            try
            {
                ReelPlayer.MediaPlayer?.Pause();
            }
            catch
            {
                // Ignore pause failures.
            }

            CropOverlayRoot.Visibility = Visibility.Visible;
            UpdateCropOverlay();
        }

        private void OnCropModeExited()
        {
            CropOverlayRoot.Visibility = Visibility.Collapsed;
        }

        private void CropResumePreview_Click(object sender, RoutedEventArgs e)
        {
            ReelPlayer.MediaPlayer?.Play();
        }

        private void CropPausePreview_Click(object sender, RoutedEventArgs e)
        {
            ReelPlayer.MediaPlayer?.Pause();
        }

        private void CropMarginSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateCropOverlay();
        }

        private void CropOverlayRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCropOverlay();
        }

        private void UpdateCropOverlay()
        {
            if (CropOverlayRoot.Visibility != Visibility.Visible)
                return;

            var width = CropOverlayRoot.ActualWidth;
            var height = CropOverlayRoot.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            var left = width * (ViewModel.CropMarginLeft / 100.0);
            var top = height * (ViewModel.CropMarginTop / 100.0);
            var right = width * (ViewModel.CropMarginRight / 100.0);
            var bottom = height * (ViewModel.CropMarginBottom / 100.0);

            if (left + right > width - 20)
            {
                right = Math.Max(0, width - left - 20);
            }
            if (top + bottom > height - 20)
            {
                bottom = Math.Max(0, height - top - 20);
            }

            CropRectangleBorder.Margin = new Thickness(left, top, right, bottom);
        }

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
