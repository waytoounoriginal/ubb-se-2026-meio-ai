using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Business logic for the movie swipe feature.
    /// Converts swipe actions into preference score deltas and delegates persistence.
    /// Owner: Bogdan
    /// </summary>
    public class SwipeService : ISwipeService
    {
        /// <summary>Score delta applied for a right-swipe (like).</summary>
        public const double LikeDelta = 1.0;

        /// <summary>Score delta applied for a left-swipe (skip).</summary>
        public const double SkipDelta = -0.5;

        private readonly IPreferenceRepository _preferenceRepository;

        public SwipeService(IPreferenceRepository preferenceRepository)
        {
            _preferenceRepository = preferenceRepository;
        }

        /// <inheritdoc />
        public async Task UpdatePreferenceScoreAsync(int userId, int movieId, bool isLiked)
        {
            double delta = isLiked ? LikeDelta : SkipDelta;

            var preference = new UserMoviePreferenceModel
            {
                UserId = userId,
                MovieId = movieId,
                Score = delta,
                LastModified = DateTime.UtcNow,
                ChangeFromPreviousValue = isLiked ? 1 : -1
            };

            await _preferenceRepository.UpsertPreferenceAsync(preference);
        }

        /// <inheritdoc />
        public async Task<List<MovieCardModel>> GetMovieFeedAsync(int userId, int count)
        {
            return await _preferenceRepository.GetMovieFeedAsync(userId, count);
        }
    }
}
