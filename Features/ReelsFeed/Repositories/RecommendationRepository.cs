using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for recommendation inputs.
    /// </summary>
    public class RecommendationRepository : IRecommendationRepository
    {
        private const int ReelModel_ReelId_Index = 0;
        private const int ReelModel_MovieId_Index = 1;
        private const int ReelModel_CreatorUserId_Index = 2;
        private const int ReelModel_VideoUrl_Index = 3;
        private const int ReelModel_ThumbnailUrl_Index = 4;
        private const int ReelModel_Title_Index = 5;
        private const int ReelModel_Caption_Index = 6;
        private const int ReelModel_FeatureDurationSeconds_Index = 7;
        private const int ReelModel_CropDataJson_Index = 8;
        private const int ReelModel_BackgroundMusicId_Index = 9;
        private const int ReelModel_Source_Index = 10;
        private const int ReelModel_CreatedAt_Index = 11;
        private const int ReelModel_LastEditedAt_Index = 12;
        private const int ReelModel_PrimaryGenre_Index = 13;

        private const int UserMoviePreference_MovieId_Index = 0;
        private const int UserMoviePreference_Score_Index = 1;

        private const int UserReelInteraction_ReelId_Index = 0;
        private const int UserReelInteraction_LikeCount_Index = 1;

        private const int UserReelInteraction_UserId_Index = 0;
        private const int UserReelInteraction_DetailReelId_Index = 1;
        private const int UserReelInteraction_IsLiked_Index = 2;
        private const int UserReelInteraction_ViewedAt_Index = 3;

        private readonly ISqlConnectionFactory _connectionFactory;

        public RecommendationRepository(ISqlConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<bool> UserHasPreferencesAsync(int userId)
        {
            const string checkUserHasPreferencesSql = @"
                SELECT TOP 1 1
                FROM UserMoviePreference
                WHERE UserId = @UserId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var checkUserHasPreferencesCommand = new SqlCommand(checkUserHasPreferencesSql, connection);
            checkUserHasPreferencesCommand.Parameters.AddWithValue("@UserId", userId);
            var hasPreferencesResult = await checkUserHasPreferencesCommand.ExecuteScalarAsync();
            return hasPreferencesResult != null;
        }

        /// <inheritdoc />
        public async Task<IList<ReelModel>> GetAllReelsAsync()
        {
            const string getAllReelsSql = @"
                SELECT
                    r.ReelId, r.MovieId, r.CreatorUserId, r.VideoUrl, r.ThumbnailUrl,
                    r.Title, r.Caption, r.FeatureDurationSeconds, r.CropDataJson,
                    r.BackgroundMusicId, r.Source, r.CreatedAt, r.LastEditedAt,
                    m.PrimaryGenre
                FROM Reel r
                LEFT JOIN Movie m ON m.MovieId = r.MovieId
            ";

            var allReels = new List<ReelModel>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getAllReelsCommand = new SqlCommand(getAllReelsSql, connection);

            await using var reelReader = await getAllReelsCommand.ExecuteReaderAsync();
            while (await reelReader.ReadAsync())
            {
                allReels.Add(this.MapReel(reelReader));
            }

            return allReels;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, double>> GetUserPreferenceScoresAsync(int userId)
        {
            const string getUserPreferenceScoresSql = @"
                SELECT MovieId, Score
                FROM UserMoviePreference
                WHERE UserId = @UserId
            ";

            var preferenceScoresByMovieId = new Dictionary<int, double>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getUserPreferenceScoresCommand = new SqlCommand(getUserPreferenceScoresSql, connection);
            getUserPreferenceScoresCommand.Parameters.AddWithValue("@UserId", userId);

            await using var preferenceScoreReader = await getUserPreferenceScoresCommand.ExecuteReaderAsync();
            while (await preferenceScoreReader.ReadAsync())
            {
                preferenceScoresByMovieId[preferenceScoreReader.GetInt32(UserMoviePreference_MovieId_Index)] =
                    preferenceScoreReader.GetDouble(UserMoviePreference_Score_Index);
            }

            return preferenceScoresByMovieId;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetAllLikeCountsAsync()
        {
            const string getAllLikeCountsSql = @"
                SELECT ReelId, COUNT(*) AS LikeCount
                FROM UserReelInteraction
                WHERE IsLiked = 1
                GROUP BY ReelId
            ";

            var likeCountsByReelId = new Dictionary<int, int>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getAllLikeCountsCommand = new SqlCommand(getAllLikeCountsSql, connection);

            await using var likeCountReader = await getAllLikeCountsCommand.ExecuteReaderAsync();
            while (await likeCountReader.ReadAsync())
            {
                likeCountsByReelId[likeCountReader.GetInt32(UserReelInteraction_ReelId_Index)] =
                    likeCountReader.GetInt32(UserReelInteraction_LikeCount_Index);
            }

            return likeCountsByReelId;
        }

        /// <inheritdoc />
        public async Task<List<UserReelInteractionModel>> GetLikesWithinDaysAsync(int days)
        {
            const string sql = @"
                SELECT UserId, ReelId, IsLiked, ViewedAt
                FROM UserReelInteraction
                WHERE IsLiked = 1 AND ViewedAt >= DATEADD(DAY, @Days, SYSUTCDATETIME())
            ";

            var interactions = new List<UserReelInteractionModel>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Days", -days);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                interactions.Add(new UserReelInteractionModel
                {
                    UserId = reader.GetInt32(UserReelInteraction_UserId_Index),
                    ReelId = reader.GetInt32(UserReelInteraction_DetailReelId_Index),
                    IsLiked = reader.GetBoolean(UserReelInteraction_IsLiked_Index),
                    ViewedAt = reader.GetDateTime(UserReelInteraction_ViewedAt_Index),
                });
            }

            return interactions;
        }

        /// <summary>
        /// Maps a reel query result row to a <see cref="ReelModel"/>.
        /// </summary>
        private ReelModel MapReel(SqlDataReader reelReader)
        {
            return new ReelModel
            {
                ReelId = reelReader.GetInt32(ReelModel_ReelId_Index),
                MovieId = reelReader.GetInt32(ReelModel_MovieId_Index),
                CreatorUserId = reelReader.GetInt32(ReelModel_CreatorUserId_Index),
                VideoUrl = reelReader.GetString(ReelModel_VideoUrl_Index),
                ThumbnailUrl = reelReader.GetString(ReelModel_ThumbnailUrl_Index),
                Title = reelReader.GetString(ReelModel_Title_Index),
                Caption = reelReader.GetString(ReelModel_Caption_Index),
                FeatureDurationSeconds = reelReader.GetDouble(ReelModel_FeatureDurationSeconds_Index),
                CropDataJson = reelReader.IsDBNull(ReelModel_CropDataJson_Index) ? null : reelReader.GetString(ReelModel_CropDataJson_Index),
                BackgroundMusicId = reelReader.IsDBNull(ReelModel_BackgroundMusicId_Index) ? null : reelReader.GetInt32(ReelModel_BackgroundMusicId_Index),
                Source = reelReader.GetString(ReelModel_Source_Index),
                CreatedAt = reelReader.GetDateTime(ReelModel_CreatedAt_Index),
                LastEditedAt = reelReader.IsDBNull(ReelModel_LastEditedAt_Index) ? null : reelReader.GetDateTime(ReelModel_LastEditedAt_Index),
                Genre = reelReader.IsDBNull(ReelModel_PrimaryGenre_Index) ? null : reelReader.GetString(ReelModel_PrimaryGenre_Index),
            };
        }
    }
}
