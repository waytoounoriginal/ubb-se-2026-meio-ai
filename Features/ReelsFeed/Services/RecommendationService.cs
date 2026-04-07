using System.Collections.Generic;
using System.Linq;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Provides personalized reel recommendations by scoring unwatched reels
    /// against the user's movie preferences, with a cold-start fallback for new users.
    /// Owner: Tudor.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private const int RecentlyLikedDaysWindow = 7;
        private readonly IRecommendationRepository _recommendationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommendationService"/> class.
        /// </summary>
        /// <param name="recommendationRepository">Repository used to load recommendation inputs.</param>
        public RecommendationService(IRecommendationRepository recommendationRepository)
        {
            this._recommendationRepository = recommendationRepository;
        }

        /// <inheritdoc />
        public async Task<IList<ReelModel>> GetRecommendedReelsAsync(int userId, int count)
        {
            // Choose recommendation strategy based on whether user preference history exists.
            bool userHasPreferences = await this._recommendationRepository.UserHasPreferencesAsync(userId);

            return userHasPreferences
                ? await this.GetPersonalizedReelsAsync(userId, count)
                : await this.GetColdStartReelsAsync(userId, count);
        }

        /// <summary>
        /// Warm-user path: ranks reels by user movie preference score in C#,
        /// using recency as a tiebreaker.
        /// </summary>
        private async Task<IList<ReelModel>> GetPersonalizedReelsAsync(int userId, int count)
        {
            var allReels = await this._recommendationRepository.GetAllReelsAsync();
            var userPreferenceScores = await this._recommendationRepository.GetUserPreferenceScoresAsync(userId);

            return allReels
                .OrderByDescending(reel =>
                    userPreferenceScores.TryGetValue(reel.MovieId, out var preferenceScore) ? preferenceScore : 0)
                .ThenByDescending(reel => reel.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Cold-start path: ranks reels by recent like count in C#,
        /// filtering interactions within the recent window and aggregating by reel,
        /// using recency as a tiebreaker.
        /// </summary>
        private async Task<IList<ReelModel>> GetColdStartReelsAsync(int userId, int count)
        {
            var allReels = await this._recommendationRepository.GetAllReelsAsync();
            var recentInteractions = await this._recommendationRepository.GetLikesWithinDaysAsync(RecentlyLikedDaysWindow);
            
            // Aggregate likes by reel within the recent window
            var recentLikeCountsByReelId = recentInteractions
                .GroupBy(interaction => interaction.ReelId)
                .ToDictionary(group => group.Key, group => group.Count());

            return allReels
                .OrderByDescending(reel =>
                    recentLikeCountsByReelId.TryGetValue(reel.ReelId, out var recentLikeCount) ? recentLikeCount : 0)
                .ThenByDescending(reel => reel.CreatedAt)
                .Take(count)
                .ToList();
        }

    }
}
