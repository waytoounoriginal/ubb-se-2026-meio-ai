using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Views
{
    public sealed partial class MatchedUserDetailPage : Page
    {
        public MatchedUserDetailViewModel ViewModel { get; }

        public MatchedUserDetailPage()
        {
            ViewModel = App.Services.GetRequiredService<MatchedUserDetailViewModel>();
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MatchResult match)
            {
                await ViewModel.LoadUserDetailAsync(
                    match.MatchedUserId,
                    match.MatchScore,
                    match.FacebookAccount,
                    match.MatchedUsername,
                    match.IsSelfView);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
