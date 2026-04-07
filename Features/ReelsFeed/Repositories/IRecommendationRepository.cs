using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Repository for recommendation-related reel and preference data access.
    /// </summary>
    public interface IRecommendationRepository
    {
        /// <summary>
        /// Returns true if the user has at least one stored movie preference.
        /// </summary>
        Task<bool> UserHasPreferencesAsync(int userId);

        /// <summary>
        /// Retrieves all reels with associated movie metadata needed for recommendation ranking.
        /// </summary>
        Task<IList<ReelModel>> GetAllReelsAsync();

        /// <summary>
        /// Retrieves the user's preference score per movie.
        /// </summary>
        Task<Dictionary<int, double>> GetUserPreferenceScoresAsync(int userId);

        /// <summary>
        /// Retrieves all like counts grouped by reel (no date filtering).
        /// </summary>
        Task<Dictionary<int, int>> GetAllLikeCountsAsync();

        /// <summary>
        /// Retrieves all interactions where IsLiked = 1 and viewed within the last N days.
        /// </summary>
        /// <param name="days">Number of days to look back.</param>
        Task<List<UserReelInteractionModel>> GetLikesWithinDaysAsync(int days);
    }
}
