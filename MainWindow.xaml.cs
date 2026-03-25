using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Views;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Views;
using ubb_se_2026_meio_ai.Features.TrailerScraping.Views;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Views;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Views;
using ubb_se_2026_meio_ai.Features.MovieTournament.Views;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Views;

namespace ubb_se_2026_meio_ai
{
    /// <summary>
    /// Shell window with a left NavigationView routing to each feature page.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static readonly Dictionary<string, Type> PageMap = new()
        {
            ["ReelsUpload"]      = typeof(ReelsUploadPage),
            ["TrailerScraping"]  = typeof(TrailerScrapingPage),
            ["ReelsEditing"]     = typeof(ReelsEditingPage),
            ["MovieSwipe"]       = typeof(MovieSwipeView),
            ["MovieTournament"]  = typeof(MovieTournamentPage),
            ["PersonalityMatch"] = typeof(PersonalityMatchPage),
            ["ReelsFeed"]        = typeof(ReelsFeedPage),
        };

        public MainWindow()
        {
            this.InitializeComponent();

            // Navigate to an empty page on startup so reels are opt-in
            ContentFrame.Navigate(typeof(Page));

            // BUG FIX: WinUI 3 Media Foundation COM access violation on closing.
            // Walk the visual tree and dispose every MediaPlayer synchronously while
            // the Dispatcher and HWND are still alive, then detach the tree.
            // Do NOT call Navigate() here — it is internally async and races with cleanup.
            this.Closed += (sender, args) =>
            {
                // Signal all ReelItemViews to stop processing callbacks immediately
                ReelItemView.IsAppClosing = true;

                // Dispose all prefetched MediaSource COM objects
                try { (App.Services.GetService<IClipPlaybackService>() as IDisposable)?.Dispose(); } catch { }

                // Walk the visual tree and dispose every MediaPlayer synchronously
                try { DisposeAllMediaPlayers(ContentFrame); } catch { }

                // Do NOT set ContentFrame.Content = null — it triggers Unloaded events
                // that race with the disposal we just did. The window is closing anyway.
            };
        }

        /// <summary>
        /// Recursively walks the visual tree to find and dispose all ReelItemView media players.
        /// </summary>
        private static void DisposeAllMediaPlayers(DependencyObject root)
        {
            if (root == null) return;

            if (root is ReelItemView reelItem)
            {
                reelItem.PauseVideo();
                reelItem.DisposeMediaPlayer();
                return;
            }

            int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                DisposeAllMediaPlayers(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i));
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item &&
                item.Tag is string tag &&
                PageMap.TryGetValue(tag, out Type? pageType))
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
