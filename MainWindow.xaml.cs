using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Views;
using ubb_se_2026_meio_ai.Features.TrailerScraping.Views;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Views;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Views;
using ubb_se_2026_meio_ai.Features.MovieTournament.Views;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Views;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Views;

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
            ["MovieSwipe"]       = typeof(MovieSwipePage),
            ["MovieTournament"]  = typeof(MovieTournamentPage),
            ["PersonalityMatch"] = typeof(PersonalityMatchPage),
            ["ReelsFeed"]        = typeof(ReelsFeedPage),
        };

        public MainWindow()
        {
            this.InitializeComponent();

            // Navigate to the first page on startup
            ContentFrame.Navigate(typeof(ReelsFeedPage));
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
