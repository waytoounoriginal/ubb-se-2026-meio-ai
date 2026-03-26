using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Data access for user-movie preference records.
    /// Owner: Bogdan
    /// </summary>
    public interface IPreferenceRepository
    {
        Task<UserMoviePreferenceModel?> GetPreferenceAsync(int userId, int movieId);
        Task UpsertPreferenceAsync(UserMoviePreferenceModel preference);
    }
}
