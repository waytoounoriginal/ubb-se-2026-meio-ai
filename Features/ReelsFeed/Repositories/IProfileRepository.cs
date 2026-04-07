using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{

    public interface IProfileRepository
    {
        /// <summary>Returns the cached engagement profile, or null if none exists.</summary>
        Task<UserProfileModel?> GetProfileAsync(int userId);

        /// <summary>Builds an engagement profile by aggregating user interaction data.</summary>
        Task<UserProfileModel> BuildProfileFromInteractionsAsync(int userId);

        /// <summary>Inserts or updates the engagement profile for a user.</summary>
        Task UpsertProfileAsync(UserProfileModel profile);
    }
}
