using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels
{
    /// <summary>
    /// ViewModel for the Personality Match list page.
    /// Handles loading matches, fallback to random users, and user selection.
    /// Owner: Madi
    /// </summary>
    public partial class PersonalityMatchViewModel : ObservableObject
    {
        private readonly IPersonalityMatchingService _matchingService;

        // Hardcoded current user ID — in production this comes from auth
        private const int CurrentUserId = 1;
        private const int MaxMatches = 10;

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

        /// <summary>Top personality matches (up to 10).</summary>
        public ObservableCollection<MatchResult> MatchResults { get; } = new();

        /// <summary>Random fallback users shown when no matches found.</summary>
        public ObservableCollection<MatchResult> FallbackUsers { get; } = new();

        /// <summary>
        /// Fired when the user wants to view a matched user's details.
        /// The subscriber (code-behind) handles Frame navigation.
        /// </summary>
        public event Action<MatchResult>? NavigateToDetail;

        public PersonalityMatchViewModel(IPersonalityMatchingService matchingService)
        {
            _matchingService = matchingService;
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
                var matches = await _matchingService.GetTopMatchesAsync(CurrentUserId, MaxMatches);

                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        MatchResults.Add(match);
                    }

                    HasMatches = true;
                    StatusMessage = $"Found {matches.Count} match{(matches.Count == 1 ? "" : "es")}!";
                }
                else
                {
                    // No matches — show fallback
                    ShowNoMatch = true;
                    StatusMessage = "No match";

                    var randomUsers = await _matchingService.GetRandomUsersAsync(CurrentUserId, MaxMatches);
                    foreach (var user in randomUsers)
                    {
                        FallbackUsers.Add(user);
                    }
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
            {
                NavigateToDetail?.Invoke(match);
            }
        }
    }
}
