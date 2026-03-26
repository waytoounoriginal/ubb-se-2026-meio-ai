using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Views
{
    public sealed partial class PersonalityMatchPage : Page
    {
        public PersonalityMatchViewModel ViewModel { get; }

        public PersonalityMatchPage()
        {
            ViewModel = App.Services.GetRequiredService<PersonalityMatchViewModel>();
            this.InitializeComponent();

            // Subscribe to navigation event
            ViewModel.NavigateToDetail += OnNavigateToDetail;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Load matches when the page is first shown
            if (ViewModel.MatchResults.Count == 0 && !ViewModel.ShowNoMatch)
            {
                await ViewModel.LoadMatchesAsync();
            }
        }

        private void MatchListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MatchResult match)
            {
                ViewModel.ViewUserDetailCommand.Execute(match);
            }
        }

        private void FallbackListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MatchResult match)
            {
                ViewModel.ViewUserDetailCommand.Execute(match);
            }
        }

        private void OnNavigateToDetail(MatchResult match)
        {
            // Navigate to the detail page, passing the match result
            Frame.Navigate(typeof(MatchedUserDetailPage), match);
        }
    }
}
