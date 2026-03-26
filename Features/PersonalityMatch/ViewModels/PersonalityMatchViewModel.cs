using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels
{
    /// <summary>
    /// ViewModel for the Personality Match list page.
    /// Handles loading matches, fallback to random users, user selection,
    /// and the account switcher panel.
    /// Owner: Madi
    /// </summary>
    public partial class PersonalityMatchViewModel : ObservableObject
    {
        private readonly IPersonalityMatchingService _matchingService;

        private const int MaxMatches = 10;

        // TODO: Replace with DB query — SELECT UserId, Username, FacebookAccount FROM [User]
        // When the team adds IsLogged/IsActive fields to the User table, remove this dictionary
        // and load from the DB instead. See DatabaseInitializer.cs for the schema TODO.
        // Only accounts the user can switch between are listed here (not every DB user).
        private static readonly Dictionary<int, (string Username, string FacebookAccount)> _demoAccounts = new()
        {
            [1] = ("Alex Carter",  "fb_alex_carter"),
            [2] = ("Alice Rivers", "fb_alice_rivers"),
            [3] = ("Bob Chen",     "fb_bob_chen"),
            [4] = ("Carol Hayes",  "fb_carol_hayes"),
            [9] = ("Sam Taylor",   "fb_sam_taylor"),
        };

        // TODO: Replace with DB query — SELECT UserId FROM [User] WHERE IsActive = 1
        // Tracks the currently active user. Resets to User 1 on app restart until DB is wired.
        private int _activeUserId = 1;

        // TODO: Replace with DB query — SELECT UserId FROM [User] WHERE IsLogged = 1
        // Pre-logged: Alex (1), Alice (2), Sam (9). Bob (3) and Carol (4) are available to add.
        // Resets on app restart until DB is wired.
        private readonly List<int> _loggedAccountIds = new() { 1, 2, 9 };

        [ObservableProperty]
        private string _pageTitle = "Personality Match";

        [ObservableProperty]
        private string _statusMessage = "Discover users with similar taste.";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _showNoMatch;

        [ObservableProperty]
        private bool _hasMatches;

        [ObservableProperty]
        private bool _isAccountPanelOpen;

        [ObservableProperty]
        private string _currentUsername = "Alex Carter";

        [ObservableProperty]
        private string _currentFacebookAccount = "fb_alex_carter";

        /// <summary>Top personality matches (up to 10).</summary>
        public ObservableCollection<MatchResult> MatchResults { get; } = new();

        /// <summary>Random fallback users shown when no matches found.</summary>
        public ObservableCollection<MatchResult> FallbackUsers { get; } = new();

        /// <summary>Other logged-in accounts available for switching.</summary>
        public ObservableCollection<UserAccountModel> OtherAccounts { get; } = new();

        /// <summary>
        /// Fired when the user wants to view a matched user's details.
        /// The subscriber (code-behind) handles Frame navigation.
        /// </summary>
        public event Action<MatchResult>? NavigateToDetail;

        /// <summary>
        /// Fired when the user clicks their own account to view their details.
        /// The subscriber (code-behind) handles Frame navigation.
        /// </summary>
        public event Action<UserAccountModel>? NavigateToCurrentUserDetail;

        public PersonalityMatchViewModel(IPersonalityMatchingService matchingService)
        {
            _matchingService = matchingService;
            RefreshAccountCollections();
        }

        [RelayCommand]
        public async Task LoadMatchesAsync()
        {
            IsLoading = true;
            ShowNoMatch = false;
            HasMatches = false;
            MatchResults.Clear();
            FallbackUsers.Clear();
            StatusMessage = "Computing matches…";

            try
            {
                var matches = await _matchingService.GetTopMatchesAsync(_activeUserId, MaxMatches);

                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                        MatchResults.Add(match);

                    HasMatches = true;
                    StatusMessage = $"Found {matches.Count} match{(matches.Count == 1 ? "" : "es")}!";
                }
                else
                {
                    ShowNoMatch = true;
                    StatusMessage = string.Empty;

                    var randomUsers = await _matchingService.GetRandomUsersAsync(_activeUserId, MaxMatches);
                    foreach (var user in randomUsers)
                        FallbackUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ViewUserDetail(MatchResult? match)
        {
            if (match != null)
                NavigateToDetail?.Invoke(match);
        }

        [RelayCommand]
        private void ToggleAccountPanel()
        {
            IsAccountPanelOpen = !IsAccountPanelOpen;
        }

        [RelayCommand]
        private async Task SwitchAccount(UserAccountModel? account)
        {
            if (account == null || account.UserId == _activeUserId)
                return;

            _activeUserId = account.UserId;
            CurrentUsername = account.Username;
            CurrentFacebookAccount = account.FacebookAccount;
            IsAccountPanelOpen = false;

            RefreshAccountCollections();
            await LoadMatchesAsync();
        }

        [RelayCommand]
        private void ViewCurrentAccountDetail()
        {
            IsAccountPanelOpen = false;
            NavigateToCurrentUserDetail?.Invoke(new UserAccountModel
            {
                UserId = _activeUserId,
                Username = CurrentUsername,
                FacebookAccount = CurrentFacebookAccount,
            });
        }

        /// <summary>
        /// Adds an account to the logged-in list and refreshes the switcher.
        /// TODO: Replace with DB call — UPDATE [User] SET IsLogged = 1 WHERE UserId = @userId
        /// </summary>
        public void AddAccount(UserAccountModel account)
        {
            if (!_loggedAccountIds.Contains(account.UserId))
            {
                _loggedAccountIds.Add(account.UserId);
                RefreshAccountCollections();
            }
        }

        /// <summary>
        /// Returns accounts not yet added to the switcher (IsLogged = 0 equivalent).
        /// TODO: Replace with DB query — SELECT * FROM [User] WHERE IsLogged = 0
        /// </summary>
        public IReadOnlyList<UserAccountModel> GetAvailableAccountsToAdd()
        {
            return _demoAccounts
                .Where(kvp => !_loggedAccountIds.Contains(kvp.Key))
                .Select(kvp => new UserAccountModel
                {
                    UserId = kvp.Key,
                    Username = kvp.Value.Username,
                    FacebookAccount = kvp.Value.FacebookAccount,
                })
                .ToList();
        }

        private void RefreshAccountCollections()
        {
            OtherAccounts.Clear();
            foreach (int userId in _loggedAccountIds)
            {
                if (userId == _activeUserId) continue;
                if (!_demoAccounts.TryGetValue(userId, out var info)) continue;
                OtherAccounts.Add(new UserAccountModel
                {
                    UserId = userId,
                    Username = info.Username,
                    FacebookAccount = info.FacebookAccount,
                });
            }
        }
    }
}
