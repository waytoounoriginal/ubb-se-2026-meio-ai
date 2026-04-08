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
    /// Concrete implementation of IVideoStorageRepository.
    /// Owner: Alex
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
            sqlCommand.Parameters.AddWithValue("@MovieId", reel.MovieId);
            sqlCommand.Parameters.AddWithValue("@CreatorUserId", reel.CreatorUserId);
            sqlCommand.Parameters.AddWithValue("@VideoUrl", reel.VideoUrl);
            sqlCommand.Parameters.AddWithValue("@ThumbnailUrl", reel.ThumbnailUrl);
            sqlCommand.Parameters.AddWithValue("@Title", reel.Title);
            sqlCommand.Parameters.AddWithValue("@Caption", reel.Caption);
            sqlCommand.Parameters.AddWithValue("@FeatureDurationSeconds", reel.FeatureDurationSeconds);
            sqlCommand.Parameters.AddWithValue("@CropDataJson", DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@BackgroundMusicId", DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@Source", reel.Source);

            await using var sqlCommandOutputReader = await sqlCommand.ExecuteReaderAsync();
            if (await sqlCommandOutputReader.ReadAsync())
            {
                // Update the model with the database-generated values
                reel.ReelId = sqlCommandOutputReader.GetInt32(0);
                reel.CreatedAt = sqlCommandOutputReader.GetDateTime(1);
            }

            return reel;
        }
    }
}
