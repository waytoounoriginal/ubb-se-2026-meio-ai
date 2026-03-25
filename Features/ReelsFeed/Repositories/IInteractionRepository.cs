using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Data access for the UserReelInteraction table.
    /// Owner: Tudor
    /// </summary>
    public interface IInteractionRepository
    {
        /// <summary>Inserts a brand-new interaction row.</summary>
        Task InsertInteractionAsync(UserReelInteractionModel interaction);

        /// <summary>
        /// Ensures a row exists for (userId, reelId).
        /// If the row already exists the call is a no-op; otherwise a default row is inserted.
        /// </summary>
        Task UpsertInteractionAsync(int userId, int reelId);

        /// <summary>
        /// Flips the IsLiked flag for a (userId, reelId) pair.
        /// If no row exists yet, inserts one with IsLiked = true.
        /// </summary>
        Task ToggleLikeAsync(int userId, int reelId);

        /// <summary>Upserts watch-duration data for a (userId, reelId) pair.</summary>
        Task UpdateViewDataAsync(int userId, int reelId, double watchDurationSec, double watchPercentage);

        /// <summary>Returns the interaction for a user–reel pair, or null.</summary>
        Task<UserReelInteractionModel?> GetInteractionAsync(int userId, int reelId);

        /// <summary>Returns the total number of likes a reel has received across all users.</summary>
        Task<int> GetLikeCountAsync(int reelId);

        /// <summary>Returns the MovieId for a given reel, or null if the reel does not exist.</summary>
        Task<int?> GetReelMovieIdAsync(int reelId);
    }
}
