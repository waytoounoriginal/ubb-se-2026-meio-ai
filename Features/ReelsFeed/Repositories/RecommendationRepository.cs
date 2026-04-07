using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for recommendation inputs.
    /// </summary>
    public class RecommendationRepository : IRecommendationRepository
    {

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
                preferenceScoresByMovieId[preferenceScoreReader.GetInt32(DataReaderColumnIndexes.UserMoviePreference.MovieId)] =
                    preferenceScoreReader.GetDouble(DataReaderColumnIndexes.UserMoviePreference.Score);
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
                likeCountsByReelId[likeCountReader.GetInt32(DataReaderColumnIndexes.UserReelInteractionLike.ReelId)] =
                    likeCountReader.GetInt32(DataReaderColumnIndexes.UserReelInteractionLike.LikeCount);
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
                    UserId = reader.GetInt32(DataReaderColumnIndexes.UserReelInteractionDetail.UserId),
                    ReelId = reader.GetInt32(DataReaderColumnIndexes.UserReelInteractionDetail.ReelId),
                    IsLiked = reader.GetBoolean(DataReaderColumnIndexes.UserReelInteractionDetail.IsLiked),
                    ViewedAt = reader.GetDateTime(DataReaderColumnIndexes.UserReelInteractionDetail.ViewedAt),
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
                ReelId = reelReader.GetInt32(DataReaderColumnIndexes.ReelModel.ReelId),
                MovieId = reelReader.GetInt32(DataReaderColumnIndexes.ReelModel.MovieId),
                CreatorUserId = reelReader.GetInt32(DataReaderColumnIndexes.ReelModel.CreatorUserId),
                VideoUrl = reelReader.GetString(DataReaderColumnIndexes.ReelModel.VideoUrl),
                ThumbnailUrl = reelReader.GetString(DataReaderColumnIndexes.ReelModel.ThumbnailUrl),
                Title = reelReader.GetString(DataReaderColumnIndexes.ReelModel.Title),
                Caption = reelReader.GetString(DataReaderColumnIndexes.ReelModel.Caption),
                FeatureDurationSeconds = reelReader.GetDouble(DataReaderColumnIndexes.ReelModel.FeatureDurationSeconds),
                CropDataJson = reelReader.IsDBNull(DataReaderColumnIndexes.ReelModel.CropDataJson) ? null : reelReader.GetString(DataReaderColumnIndexes.ReelModel.CropDataJson),
                BackgroundMusicId = reelReader.IsDBNull(DataReaderColumnIndexes.ReelModel.BackgroundMusicId) ? null : reelReader.GetInt32(DataReaderColumnIndexes.ReelModel.BackgroundMusicId),
                Source = reelReader.GetString(DataReaderColumnIndexes.ReelModel.Source),
                CreatedAt = reelReader.GetDateTime(DataReaderColumnIndexes.ReelModel.CreatedAt),
                LastEditedAt = reelReader.IsDBNull(DataReaderColumnIndexes.ReelModel.LastEditedAt) ? null : reelReader.GetDateTime(DataReaderColumnIndexes.ReelModel.LastEditedAt),
                Genre = reelReader.IsDBNull(DataReaderColumnIndexes.ReelModel.PrimaryGenre) ? null : reelReader.GetString(DataReaderColumnIndexes.ReelModel.PrimaryGenre),
            };
        }
    }
}
