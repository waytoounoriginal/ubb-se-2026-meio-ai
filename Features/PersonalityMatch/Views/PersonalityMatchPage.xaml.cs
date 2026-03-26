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

            ViewModel.NavigateToDetail += OnNavigateToDetail;
            ViewModel.NavigateToCurrentUserDetail += OnNavigateToCurrentUserDetail;
            this.Unloaded += (_, _) =>
            {
                ViewModel.NavigateToDetail -= OnNavigateToDetail;
                ViewModel.NavigateToCurrentUserDetail -= OnNavigateToCurrentUserDetail;
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.MatchResults.Count == 0 && !ViewModel.ShowNoMatch)
            {
                await ViewModel.LoadMatchesAsync();
            }
        }

        private void MatchListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MatchResult match)
                ViewModel.ViewUserDetailCommand.Execute(match);
        }

        private void FallbackListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MatchResult match)
                ViewModel.ViewUserDetailCommand.Execute(match);
        }

        private void OtherAccountsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is UserAccountModel account)
                ViewModel.SwitchAccountCommand.Execute(account);
        }

        private async void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            var available = ViewModel.GetAvailableAccountsToAdd();

            if (available.Count == 0)
            {
                var noMoreDialog = new ContentDialog
                {
                    Title = "Add account",
                    Content = "All available accounts have already been added.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot,
                };
                await noMoreDialog.ShowAsync();
                return;
            }

            var accountListView = new ListView
            {
                ItemsSource = available,
                SelectionMode = ListViewSelectionMode.Single,
                Height = 200,
                ItemTemplate = (DataTemplate)Resources["AccountPickerItemTemplate"],
            };

            var dialog = new ContentDialog
            {
                Title = "Add account",
                Content = accountListView,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && accountListView.SelectedItem is UserAccountModel selected)
            {
                ViewModel.AddAccount(selected);
            }
        }

        private void OnNavigateToDetail(MatchResult match)
        {
            Frame.Navigate(typeof(MatchedUserDetailPage), match);
        }

        private void OnNavigateToCurrentUserDetail(UserAccountModel account)
        {
            // Reuse MatchedUserDetailPage for the current user's own profile view.
            // IsSelfView=true hides the compatibility bar (meaningless for your own account).
            var selfMatch = new MatchResult
            {
                MatchedUserId = account.UserId,
                MatchedUsername = account.Username,
                MatchScore = 100,
                FacebookAccount = account.FacebookAccount,
                IsSelfView = true,
            };
            Frame.Navigate(typeof(MatchedUserDetailPage), selfMatch);
        }
    }
}
