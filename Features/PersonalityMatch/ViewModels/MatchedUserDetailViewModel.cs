using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels
{
    /// <summary>
    /// ViewModel for the Matched User Detail page.
    /// Displays the selected user's engagement stats, top preferences,
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

        /// <summary>The selected user's top movie preferences.</summary>
        public ObservableCollection<UserMoviePreferenceModel> TopPreferences { get; } = new();

        public MatchedUserDetailViewModel(IPersonalityMatchRepository repository)
        {
            _repository = repository;
        }

        public async Task LoadUserDetailAsync(int userId, double matchScore, string facebookAccount)
        {
            IsLoading = true;
            ErrorMessage = null;
            HasProfile = false;
            TopPreferences.Clear();

            MatchedUsername = await _repository.GetUsernameAsync(userId);
            MatchScore = matchScore;
            FacebookAccount = facebookAccount;

            try
            {
                // Load engagement profile
                UserProfileModel? profile = await _repository.GetUserProfileAsync(userId);
                if (profile != null)
                {
                    UserProfile = profile;
                    HasProfile = true;
                }

                // Load top movie preferences (sorted by score descending, top 5)
                var prefs = await _repository.GetCurrentUserPreferencesAsync(userId);
                var topPrefs = prefs.OrderByDescending(p => p.Score).Take(5);
                foreach (var pref in topPrefs)
                {
                    TopPreferences.Add(pref);
                }
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
