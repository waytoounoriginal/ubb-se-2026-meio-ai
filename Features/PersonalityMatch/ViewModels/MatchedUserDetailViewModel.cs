using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels
{
    /// <summary>
    /// ViewModel for the Matched User Detail page.
    /// Displays the selected user's engagement stats, top preferences (with movie titles),
    /// compatibility score, and Facebook nickname.
    /// Owner: Madi
    /// </summary>
    public partial class MatchedUserDetailViewModel : ObservableObject
    {
        private readonly IPersonalityMatchRepository _repository;

        [ObservableProperty]
        private string _matchedUsername = string.Empty;

        [ObservableProperty]
        private double _matchScore;

        [ObservableProperty]
        private string _facebookAccount = string.Empty;

        [ObservableProperty]
        private UserProfileModel? _userProfile;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _hasProfile;

        /// <summary>
        /// False when viewing your own account (self-view).
        /// Hides the compatibility bar since 100% match with yourself is obvious.
        /// </summary>
        [ObservableProperty]
        private bool _showCompatibility = true;

        /// <summary>The selected user's top 5 movie preferences with resolved titles.</summary>
        public ObservableCollection<MoviePreferenceDisplayModel> TopPreferences { get; } = new();

        public MatchedUserDetailViewModel(IPersonalityMatchRepository repository)
        {
            _repository = repository;
        }

        public async Task LoadUserDetailAsync(int userId, double matchScore, string facebookAccount, string username = "", bool isSelfView = false)
        {
            IsLoading = true;
            ErrorMessage = null;
            HasProfile = false;
            ShowCompatibility = !isSelfView;
            TopPreferences.Clear();

            MatchedUsername = string.IsNullOrEmpty(username)
                ? await _repository.GetUsernameAsync(userId)
                : username;
            MatchScore = matchScore;
            FacebookAccount = facebookAccount;

            try
            {
                UserProfileModel? profile = await _repository.GetUserProfileAsync(userId);
                if (profile != null)
                {
                    UserProfile = profile;
                    HasProfile = true;
                }

                // Load top 5 preferences with movie titles, best movie flagged first
                var topPrefs = await _repository.GetTopPreferencesWithTitlesAsync(userId, 5);
                foreach (var pref in topPrefs)
                    TopPreferences.Add(pref);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load details: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
