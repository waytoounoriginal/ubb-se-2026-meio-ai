using CommunityToolkit.Mvvm.ComponentModel;
using ubb_se_2026_meio_ai;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels
{
    /// <summary>
    /// Exposes the user's engagement metrics from <see cref="IEngagementProfileService"/>.
    /// Read by Madi's PersonalityMatch feature for matched user details.
    /// Owner: Tudor.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfileViewModel"/> class.
        /// </summary>
        /// <param name="profileService">Service used to load and refresh user engagement profiles.</param>
        public UserProfileViewModel(IEngagementProfileService profileService)
        {
            this._profileService = profileService;
        }

        /// <summary>
        /// Loads the engagement profile for the given user.
        /// </summary>
        /// <param name="userId">The ID of the user whose profile should be loaded.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        public async Task LoadProfileAsync(int userId)
        {
            this.IsLoading = true;
            this.ErrorMessage = null;

            try
            {
                await this._profileService.RefreshProfileAsync(userId);
                this.Profile = await this._profileService.GetProfileAsync(userId);
            }
            catch (System.Exception ex)
            {
                this.ErrorMessage = string.Format(AppMessages.UserProfileLoadErrorFormat, ex.Message);
            }
            finally
            {
                this.IsLoading = false;
            }
        }
    }
}
