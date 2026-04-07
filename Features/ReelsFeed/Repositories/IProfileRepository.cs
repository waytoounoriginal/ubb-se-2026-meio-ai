using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Repository for managing user engagement profiles in the reels feed context.
    /// </summary>
    public interface IProfileRepository
    {
        /// <summary>
        /// Retrieves the cached engagement profile for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The user's engagement profile, or null if no profile exists.</returns>
        Task<UserProfileModel?> GetProfileAsync(int userId);

        /// <summary>Builds an engagement profile by aggregating user interaction data.</summary>
        Task<UserProfileModel> BuildProfileFromInteractionsAsync(int userId);

        /// <summary>
        /// Inserts or updates the engagement profile for a user.
        /// </summary>
        /// <param name="profile">The profile model to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertProfileAsync(UserProfileModel profile);
    }
}
