using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    /// <summary>
    /// Hosts the reels feed page and coordinates page-level lifecycle and playback handoff.
    /// </summary>
    public sealed partial class ReelsFeedPage : Page
    {
        /// <summary>
        /// Gets the view model bound to this page.
        /// </summary>
        public ReelsFeedViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReelsFeedPage"/> class.
        /// </summary>
        public ReelsFeedPage()
        {
            this.ViewModel = App.Services.GetRequiredService<ReelsFeedViewModel>();
            this.InitializeComponent();

            this.Loaded += this.ReelsFeedPage_Loaded;
            this.Unloaded += this.ReelsFeedPage_Unloaded;
        }

        /// <summary>
        /// When the page is removed from the visual tree (e.g. window closing or navigating away),
        /// iterate every realized FlipView container and dispose its MediaPlayer.
        /// This catches containers the MainWindow visual-tree walk might miss.
        /// </summary>
        private void ReelsFeedPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Flush the final reel's watch-duration data before tearing down
            this.ViewModel.OnNavigatingAway();

            for (int queueIndex = 0; queueIndex < this.ViewModel.ReelQueue.Count; queueIndex++)
            {
                var reelContainer = this.FeedFlipView.ContainerFromIndex(queueIndex) as DependencyObject;
                if (reelContainer != null)
                {
                    var reelView = this.FindVisualChild<ReelItemView>(reelContainer);
                    reelView?.DisposeMediaPlayer();
                }
            }
        }

        /// <summary>
        /// Loads feed data when the page is displayed for the first time.
        /// </summary>
        private async void ReelsFeedPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel.ReelQueue.Count == 0)
            {
                await this.ViewModel.LoadFeedAsync();
            }
        }

        /// <summary>
        /// Retries feed loading after an error or empty-state refresh action.
        /// </summary>
        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            await this.ViewModel.LoadFeedAsync();
        }

        /// <summary>
        /// Handles FlipView selection changes and schedules playback updates.
        /// </summary>
        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems.First() is ReelModel selectedReel)
            {
                this.ViewModel.ScrollNext(selectedReel);
            }

            // Queue the playback orchestration so it executes AFTER the FlipView finishes virtualizing and generating the new UI container.
            this.DispatcherQueue.TryEnqueue(() => this.TriggerPlaybackForCurrent());
        }

        /// <summary>
        /// Plays the selected reel and pauses all other realized reel views.
        /// </summary>
        private void TriggerPlaybackForCurrent()
        {
            if (this.FeedFlipView == null || this.ViewModel.ReelQueue.Count == 0)
            {
                return;
            }

            int selectedIndex = this.FeedFlipView.SelectedIndex;
            for (int queueIndex = 0; queueIndex < this.ViewModel.ReelQueue.Count; queueIndex++)
            {
                var reelContainer = this.FeedFlipView.ContainerFromIndex(queueIndex) as DependencyObject;
                if (reelContainer != null)
                {
                    var reelView = this.FindVisualChild<ReelItemView>(reelContainer);
                    if (reelView != null)
                    {
                        if (queueIndex == selectedIndex)
                        {
                            var selectedReel = this.ViewModel.ReelQueue[queueIndex];
                            reelView.SetPlaybackItem(
                                selectedReel.VideoUrl,
                                this.ViewModel.BuildPlaybackItem(selectedReel.VideoUrl));
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

        /// <summary>
        /// Recursively searches for the first visual child of the specified type.
        /// </summary>
        /// <typeparam name="T">The visual child type to locate.</typeparam>
        /// <param name="parent">The parent element to search beneath.</param>
        /// <returns>The first matching child, or null when none is found.</returns>
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(parent); childIndex++)
            {
                var child = VisualTreeHelper.GetChild(parent, childIndex);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var nestedMatch = this.FindVisualChild<T>(child);
                if (nestedMatch != null)
                {
                    return nestedMatch;
                }
            }

            return null;
        }
    }
}
