namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Data access for the UserMoviePreference table (Tudor's portion).
    /// Shared table — also written by Bogdan (swipe) and Gabi (tournament).
    /// Owner: Tudor
    /// </summary>
    public interface IPreferenceRepository
    {
        /// <summary>
        /// Boosts the user's preference score for the movie linked to a liked reel.
        /// If no row exists, creates one at 0 then applies the boost (+1.5).
        /// </summary>
        Task BoostPreferenceOnLikeAsync(int userId, int movieId);
    }
}
