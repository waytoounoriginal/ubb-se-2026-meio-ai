namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Repository for managing user movie preferences in the reels feed context.
    /// </summary>
    public interface IPreferenceRepository
    {
        /// <summary>
        /// Boosts a user's preference weight for a movie when they like a related reel.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="movieId">The ID of the movie to boost preference for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task BoostPreferenceOnLikeAsync(int userId, int movieId);
    }
}
