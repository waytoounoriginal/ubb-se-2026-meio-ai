using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Manages Tinder-like swiping logic for movies.
    /// Owner: Bogdan
    /// </summary>
    public interface ISwipeService
    {
        Task UpdatePreferenceScoreAsync(int userId, int movieId, double scoreDelta);
        Task<IList<MovieCardModel>> GetUnswipedMoviesAsync(int userId, int count);
    }
}
