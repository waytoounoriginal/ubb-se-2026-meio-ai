using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Repository
{
    /// <summary>
    /// Concreate implementation of IVideoStorageRepository
    /// </summary>
    public class VideoStorageRepository : IVideoStorageRepository
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public VideoStorageRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            this._sqlConnectionFactory = sqlConnectionFactory;
        }

        public async Task<ReelModel> InsertReelAsync(ReelModel reel)
        {
            await using var databaseConnection = await _sqlConnectionFactory.CreateConnectionAsync();

            string sqlInsertInstruction = @"
                INSERT INTO Reel (MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, FeatureDurationSeconds, CropDataJson, BackgroundMusicId, Source, CreatedAt)
                OUTPUT INSERTED.ReelId, INSERTED.CreatedAt
                VALUES (@MovieId, @CreatorUserId, @VideoUrl, @ThumbnailUrl, @Title, @Caption, @FeatureDurationSeconds, @CropDataJson, @BackgroundMusicId, @Source, SYSUTCDATETIME());
            ";

            await using var sqlCommand = new SqlCommand(sqlInsertInstruction, databaseConnection);

            // Use the data from the passed-in ReelModel
            string movieIdParameter = "@MovieId", creatorIdParameter = "@CreatorUserId", videoUrlParameter = "@VideoUrl", 
                thumbnailUrpParamater = "@ThumbnailUrl", titleParameter = "@Title", captionParamater = "@Caption", 
                featureDurationSecondsParamater = "@FeatureDurationSeconds", cropDataParamater = "@CropDataJson", 
                backgroundMusicParameter = "@BackgroundMusicId", sourceParameter = "@Source";

            sqlCommand.Parameters.AddWithValue(movieIdParameter, reel.MovieId);
            sqlCommand.Parameters.AddWithValue(creatorIdParameter, reel.CreatorUserId);
            sqlCommand.Parameters.AddWithValue(videoUrlParameter, reel.VideoUrl);
            sqlCommand.Parameters.AddWithValue(thumbnailUrpParamater, reel.ThumbnailUrl);
            sqlCommand.Parameters.AddWithValue(titleParameter, reel.Title);
            sqlCommand.Parameters.AddWithValue(captionParamater, reel.Caption);
            sqlCommand.Parameters.AddWithValue(featureDurationSecondsParamater, reel.FeatureDurationSeconds);
            sqlCommand.Parameters.AddWithValue(cropDataParamater, DBNull.Value);
            sqlCommand.Parameters.AddWithValue(backgroundMusicParameter, DBNull.Value);
            sqlCommand.Parameters.AddWithValue(sourceParameter, reel.Source);

            await using var sqlCommandOutputReader = await sqlCommand.ExecuteReaderAsync();
            if (await sqlCommandOutputReader.ReadAsync())
            {
                // Update the model with the database-generated values
                const int firstColumnPosition = 0, secondColumnPosition = 1;

                reel.ReelId = sqlCommandOutputReader.GetInt32(firstColumnPosition);
                reel.CreatedAt = sqlCommandOutputReader.GetDateTime(secondColumnPosition);
            }

            return reel;
        }
    }
}
