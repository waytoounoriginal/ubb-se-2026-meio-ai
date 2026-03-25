using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    public sealed partial class ReelsFeedPage : Page
    {
        public ReelsFeedViewModel ViewModel { get; }

        public ReelsFeedPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsFeedViewModel>();
            this.InitializeComponent();

            this.Loaded += ReelsFeedPage_Loaded;
            this.Unloaded += ReelsFeedPage_Unloaded;
        }

        /// <summary>
        /// When the page is removed from the visual tree (e.g. window closing or navigating away),
        /// iterate every realized FlipView container and dispose its MediaPlayer.
        /// This catches containers the MainWindow visual-tree walk might miss.
        /// </summary>
        private void ReelsFeedPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Flush the final reel's watch-duration data before tearing down
            ViewModel.OnNavigatingAway();

            for (int i = 0; i < ViewModel.ReelQueue.Count; i++)
            {
                var container = FeedFlipView.ContainerFromIndex(i) as DependencyObject;
                if (container != null)
                {
                    var reelView = FindVisualChild<ReelItemView>(container);
                    reelView?.DisposeMediaPlayer();
                }
            }
        }

        private async void ReelsFeedPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ReelQueue.Count == 0)
            {
                await ViewModel.LoadFeedAsync();
            }
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadFeedAsync();
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ubb_se_2026_meio_ai.Core.Models.ReelModel newReel)
            {
                ViewModel.ScrollNext(newReel);
            }
            
            // Queue the playback orchestration so it executes AFTER the FlipView finishes virtualizing and generating the new UI container.
            DispatcherQueue.TryEnqueue(() => TriggerPlaybackForCurrent());
        }

        private void TriggerPlaybackForCurrent()
        {
            if (FeedFlipView == null || ViewModel.ReelQueue.Count == 0) return;

            int currentIndex = FeedFlipView.SelectedIndex;
            for (int i = 0; i < ViewModel.ReelQueue.Count; i++)
            {
                var container = FeedFlipView.ContainerFromIndex(i) as DependencyObject;
                if (container != null)
                {
                    var reelView = FindVisualChild<ReelItemView>(container);
                    if (reelView != null)
                    {
                        if (i == currentIndex)
                        {
                            reelView.PlayVideo();
                        }
                        else
                        {
                            reelView.PauseVideo();
                        }
                    }
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
