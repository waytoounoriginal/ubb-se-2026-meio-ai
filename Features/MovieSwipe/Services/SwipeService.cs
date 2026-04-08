using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Implements swipe processing using defined score constants.
    /// </summary>
    public class SwipeService : ISwipeService
    {
        /// <summary> The score increase for a positive swipe. </summary>
        public const double LikeDelta = 1.0;

        /// <summary> The score decrease for a negative swipe. </summary>
        public const double SkipDelta = -0.5;

        /// <summary> Indicator for a positive change value. </summary>
        private const int LikedIndicator = 1;

        /// <summary> Indicator for a negative change value. </summary>
        private const int SkippedIndicator = -1;

        /// <summary> The preference repository for data persistence. </summary>
        private readonly IPreferenceRepository _preferenceRepository;

        /// <summary> Initializes a new instance of the <see cref="SwipeService"/> class. </summary>
        /// <param name="preferenceRepository">The preference repository.</param>
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
                ChangeFromPreviousValue = isLiked ? LikedIndicator : SkippedIndicator
            };

            await _preferenceRepository.UpsertPreferenceAsync(preference);
        }

        public async Task<List<MovieCardModel>> GetMovieFeedAsync(int userId, int count)
        {
            // This satisfies your "DelegatesToRepository" test
            return await _preferenceRepository.GetMovieFeedAsync(userId, count);
        }
    }
}