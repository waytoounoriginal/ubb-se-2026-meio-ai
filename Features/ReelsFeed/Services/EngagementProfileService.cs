using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Orchestrates engagement profile operations and delegates all data access
    /// concerns to <see cref="IProfileRepository"/>.
    /// Owner: Tudor
    /// </summary>
    public class EngagementProfileService : IEngagementProfileService
    {
        private readonly IProfileRepository _profileRepository;

        public EngagementProfileService(IProfileRepository profileRepository)
        {
            _profileRepository = profileRepository;
        }

        public async Task<UserProfileModel?> GetProfileAsync(int userId)
        {
            return await _profileRepository.GetProfileAsync(userId);
        }

        /// <summary>
        /// Rebuilds and persists the engagement profile for a user.
        /// </summary>
        public async Task RefreshProfileAsync(int userId)
        {
            // Step 1: aggregate raw interaction data in repository
            var profile = await _profileRepository.BuildProfileFromInteractionsAsync(userId);

            // Step 2: persist via repository
            await _profileRepository.UpsertProfileAsync(profile);
        }
    }
}
