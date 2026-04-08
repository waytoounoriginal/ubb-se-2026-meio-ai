namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;
    using ubb_se_2026_meio_ai.Core.Database;
    using ubb_se_2026_meio_ai.Core.Models;

    /// <summary>
    /// Repository class for managing database operations related to Reels.
    /// </summary>
    public class ReelRepository : IReelRepository
    {
        private const string SqlSelectUserReels = @"
                SELECT ReelId, MovieId, CreatorUserId, VideoUrl, ThumbnailUrl,
                       Title, Caption, FeatureDurationSeconds, BackgroundMusicId,
                       CropDataJson, Source, CreatedAt, LastEditedAt
                FROM Reel
                WHERE CreatorUserId = @UserId
                ORDER BY CreatedAt DESC";

        private const string SqlUpdateReelEdits = @"
                UPDATE Reel
                SET CropDataJson = @Crop,
                    BackgroundMusicId = @MusicId,
                    VideoUrl = COALESCE(@VideoUrl, VideoUrl),
                    LastEditedAt = SYSUTCDATETIME()
                WHERE ReelId = @ReelId";

        private const string SqlSelectReelById = @"
                SELECT ReelId, MovieId, CreatorUserId, VideoUrl, ThumbnailUrl,
                       Title, Caption, FeatureDurationSeconds, BackgroundMusicId,
                       CropDataJson, Source, CreatedAt, LastEditedAt
                FROM Reel
                WHERE ReelId = @ReelId";

        private const string SqlDeleteReel = @"
                DELETE FROM UserReelInteraction WHERE ReelId = @ReelId;
                DELETE FROM Reel WHERE ReelId = @ReelId;";

        private const string ParameterUserId = "@UserId";
        private const string ParameterCropData = "@Crop";
        private const string ParameterMusicId = "@MusicId";
        private const string ParameterVideoUrl = "@VideoUrl";
        private const string ParameterReelId = "@ReelId";

        private const int ColumnIndexReelId = 0;
        private const int ColumnIndexMovieId = 1;
        private const int ColumnIndexCreatorUserId = 2;
        private const int ColumnIndexVideoUrl = 3;
        private const int ColumnIndexThumbnailUrl = 4;
        private const int ColumnIndexTitle = 5;
        private const int ColumnIndexCaption = 6;
        private const int ColumnIndexFeatureDurationSeconds = 7;
        private const int ColumnIndexBackgroundMusicId = 8;
        private const int ColumnIndexCropDataJson = 9;
        private const int ColumnIndexSource = 10;
        private const int ColumnIndexCreatedAt = 11;
        private const int ColumnIndexLastEditedAt = 12;

        private readonly ISqlConnectionFactory sqlConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReelRepository"/> class.
        /// </summary>
        /// <param name="sqlConnectionFactory">The SQL connection factory.</param>
        public ReelRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            this.sqlConnectionFactory = sqlConnectionFactory;
        }

        /// <summary>
        /// Returns all reels where CreatorUserId matches the provided user ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of user reels.</returns>
        public async Task<IList<ReelModel>> GetUserReelsAsync(int userId)
        {
            var resultList = new List<ReelModel>();

            await using var sqlConnection = await this.sqlConnectionFactory.CreateConnectionAsync();
            await using var sqlCommand = new SqlCommand(SqlSelectUserReels, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParameterUserId, userId);

            await using var dataReader = await sqlCommand.ExecuteReaderAsync();

            while (await dataReader.ReadAsync())
            {
                resultList.Add(new ReelModel
                {
                    ReelId = dataReader.GetInt32(ColumnIndexReelId),
                    MovieId = dataReader.GetInt32(ColumnIndexMovieId),
                    CreatorUserId = dataReader.GetInt32(ColumnIndexCreatorUserId),
                    VideoUrl = dataReader.GetString(ColumnIndexVideoUrl),
                    ThumbnailUrl = dataReader.IsDBNull(ColumnIndexThumbnailUrl) ? string.Empty : dataReader.GetString(ColumnIndexThumbnailUrl),
                    Title = dataReader.GetString(ColumnIndexTitle),
                    Caption = dataReader.IsDBNull(ColumnIndexCaption) ? string.Empty : dataReader.GetString(ColumnIndexCaption),
                    FeatureDurationSeconds = dataReader.IsDBNull(ColumnIndexFeatureDurationSeconds) ? 0 : dataReader.GetDouble(ColumnIndexFeatureDurationSeconds),
                    BackgroundMusicId = dataReader.IsDBNull(ColumnIndexBackgroundMusicId) ? null : dataReader.GetInt32(ColumnIndexBackgroundMusicId),
                    CropDataJson = dataReader.IsDBNull(ColumnIndexCropDataJson) ? null : dataReader.GetString(ColumnIndexCropDataJson),
                    Source = dataReader.IsDBNull(ColumnIndexSource) ? string.Empty : dataReader.GetString(ColumnIndexSource),
                    CreatedAt = dataReader.GetDateTime(ColumnIndexCreatedAt),
                    LastEditedAt = dataReader.IsDBNull(ColumnIndexLastEditedAt) ? null : dataReader.GetDateTime(ColumnIndexLastEditedAt),
                });
            }

            return resultList;
        }

        /// <summary>
        /// Updates CropDataJson, BackgroundMusicId, LastEditedAt, and optionally VideoUrl for a reel.
        /// </summary>
        /// <param name="reelId">The ID of the reel to update.</param>
        /// <param name="cropDataJson">The JSON containing crop metadata.</param>
        /// <param name="musicId">The ID of the background music track.</param>
        /// <param name="videoUrl">The optional updated video URL.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> UpdateReelEditsAsync(int reelId, string cropDataJson, int? musicId, string? videoUrl = null)
        {
            await using var sqlConnection = await this.sqlConnectionFactory.CreateConnectionAsync();
            await using var sqlCommand = new SqlCommand(SqlUpdateReelEdits, sqlConnection);

            sqlCommand.Parameters.AddWithValue(ParameterCropData, cropDataJson);
            sqlCommand.Parameters.AddWithValue(ParameterMusicId, (object?)musicId ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(ParameterVideoUrl, (object?)videoUrl ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(ParameterReelId, reelId);

            return await sqlCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves a specific reel by its ID.
        /// </summary>
        /// <param name="reelId">The ID of the reel.</param>
        /// <returns>The requested reel model or null if not found.</returns>
        public async Task<ReelModel?> GetReelByIdAsync(int reelId)
        {
            await using var sqlConnection = await this.sqlConnectionFactory.CreateConnectionAsync();
            await using var sqlCommand = new SqlCommand(SqlSelectReelById, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParameterReelId, reelId);

            await using var dataReader = await sqlCommand.ExecuteReaderAsync();

            if (!await dataReader.ReadAsync())
            {
                return null;
            }

            return new ReelModel
            {
                ReelId = dataReader.GetInt32(ColumnIndexReelId),
                MovieId = dataReader.GetInt32(ColumnIndexMovieId),
                CreatorUserId = dataReader.GetInt32(ColumnIndexCreatorUserId),
                VideoUrl = dataReader.GetString(ColumnIndexVideoUrl),
                ThumbnailUrl = dataReader.IsDBNull(ColumnIndexThumbnailUrl) ? string.Empty : dataReader.GetString(ColumnIndexThumbnailUrl),
                Title = dataReader.GetString(ColumnIndexTitle),
                Caption = dataReader.IsDBNull(ColumnIndexCaption) ? string.Empty : dataReader.GetString(ColumnIndexCaption),
                FeatureDurationSeconds = dataReader.IsDBNull(ColumnIndexFeatureDurationSeconds) ? 0 : dataReader.GetDouble(ColumnIndexFeatureDurationSeconds),
                BackgroundMusicId = dataReader.IsDBNull(ColumnIndexBackgroundMusicId) ? null : dataReader.GetInt32(ColumnIndexBackgroundMusicId),
                CropDataJson = dataReader.IsDBNull(ColumnIndexCropDataJson) ? null : dataReader.GetString(ColumnIndexCropDataJson),
                Source = dataReader.IsDBNull(ColumnIndexSource) ? string.Empty : dataReader.GetString(ColumnIndexSource),
                CreatedAt = dataReader.GetDateTime(ColumnIndexCreatedAt),
                LastEditedAt = dataReader.IsDBNull(ColumnIndexLastEditedAt) ? null : dataReader.GetDateTime(ColumnIndexLastEditedAt),
            };
        }

        /// <summary>
        /// Deletes a reel from the database.
        /// </summary>
        /// <param name="reelId">The ID of the reel to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteReelAsync(int reelId)
        {
            await using var sqlConnection = await this.sqlConnectionFactory.CreateConnectionAsync();
            await using var sqlCommand = new SqlCommand(SqlDeleteReel, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParameterReelId, reelId);
            await sqlCommand.ExecuteNonQueryAsync();
        }
    }
}