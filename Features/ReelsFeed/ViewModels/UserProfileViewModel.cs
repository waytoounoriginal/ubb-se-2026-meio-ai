using CommunityToolkit.Mvvm.ComponentModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels
{
    /// <summary>
    /// Exposes the user's engagement metrics from <see cref="IEngagementProfileService"/>.
    /// Read by Madi's PersonalityMatch feature for matched user details.
    /// Owner: Tudor
    /// </summary>
    public partial class UserProfileViewModel : ObservableObject
    {
        private readonly IEngagementProfileService _profileService;

        [ObservableProperty]
        private UserProfileModel? _profile;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public UserProfileViewModel(IEngagementProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Loads the engagement profile for the given user.
        /// </summary>
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public async Task LoadProfileAsync(int userId)
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                await _profileService.RefreshProfileAsync(userId);
                Profile = await _profileService.GetProfileAsync(userId);
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Failed to load profile: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
