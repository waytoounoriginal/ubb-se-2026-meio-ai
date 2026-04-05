using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Services
{
    /// <summary>
    /// Concrete implementation of IVideoStorageService.
    /// Owner: Alex
    /// </summary>
    public class VideoStorageService : IVideoStorageService
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        
        // Simulating a blob storage directory inside the AppData folder for local development
        private readonly string _blobStorageDirectory;

        public VideoStorageService(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
            
            _blobStorageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "MeioAI", 
                "Videos");
                
            if (!Directory.Exists(_blobStorageDirectory))
            {
                Directory.CreateDirectory(_blobStorageDirectory);
            }
        }

        public async Task<bool> ValidateVideoAsync(string localFilePath)
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
                return false;

            string videoFileExtension = ".mp4";
            var fileExtension = Path.GetExtension(localFilePath).ToLowerInvariant();
            if (fileExtension != videoFileExtension)
            {
                return false;
            }

            // Real video parsing using Native Windows 10/11 APIs!
            try
            {
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(localFilePath);
                var videoProperties = await storageFile.Properties.GetVideoPropertiesAsync();
                double maximumReelDurationSeconds = 60.0;

                if (videoProperties.Duration.TotalSeconds > maximumReelDurationSeconds)
                {
                    return false; // Video is too long
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async Task<ReelModel> UploadVideoAsync(ReelUploadRequest request)
        {
            if (!File.Exists(request.LocalFilePath))
                throw new FileNotFoundException("The selected video file could not be found.", request.LocalFilePath);

            // 1. "Upload" to Blob Storage (Simulated by copying file locally to AppData)
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.LocalFilePath);
            string destinationBlobPath = Path.Combine(_blobStorageDirectory, fileName);
            
            // We'll store the physical path as the 'VideoUrl' for local viewing
            await Task.Run(() => File.Copy(request.LocalFilePath, destinationBlobPath, overwrite: true));

            // 2. Compute TRUE duration natively using Windows Storage Properties
            double computedDurationSeconds = 0;
            try
            {
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(request.LocalFilePath);
                var videoProps = await storageFile.Properties.GetVideoPropertiesAsync();
                computedDurationSeconds = videoProps.Duration.TotalSeconds;
            }
            catch
            {
                computedDurationSeconds = 15.0; // Fallback just in case
            }

            // 3. Insert metadata into the database
            await using var databaseConnection = await _sqlConnectionFactory.CreateConnectionAsync();
            
            string sqlInsertInstruction = @"
                INSERT INTO Reel (MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, FeatureDurationSeconds, CropDataJson, BackgroundMusicId, Source, CreatedAt)
                OUTPUT INSERTED.ReelId, INSERTED.CreatedAt
                VALUES (@MovieId, @CreatorUserId, @VideoUrl, @ThumbnailUrl, @Title, @Caption, @FeatureDurationSeconds, @CropDataJson, @BackgroundMusicId, @Source, SYSUTCDATETIME());
            ";

            await using var sqlCommand = new SqlCommand(sqlInsertInstruction, databaseConnection);

            // Map nullable MovieId to 0 to satisfy your NOT NULL db constraint (since we can't use DBNull anymore)
            int nullId = 0;

            sqlCommand.Parameters.AddWithValue("@MovieId", request.MovieId ?? nullId);
            sqlCommand.Parameters.AddWithValue("@CreatorUserId", request.UploaderUserId);
            sqlCommand.Parameters.AddWithValue("@VideoUrl", destinationBlobPath);
            sqlCommand.Parameters.AddWithValue("@ThumbnailUrl", String.Empty); // Optional for now
            sqlCommand.Parameters.AddWithValue("@Title", request.Title ?? String.Empty);
            sqlCommand.Parameters.AddWithValue("@Caption", request.Caption ?? String.Empty);
            sqlCommand.Parameters.AddWithValue("@FeatureDurationSeconds", computedDurationSeconds);
            sqlCommand.Parameters.AddWithValue("@CropDataJson", DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@BackgroundMusicId", DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@Source", "upload");

            int generatedReelId = 0;
            DateTime generatedCreatedAt = DateTime.UtcNow;

            await using var sqlCommandOutputReader = await sqlCommand.ExecuteReaderAsync();
            if (await sqlCommandOutputReader.ReadAsync())
            {
                generatedReelId = sqlCommandOutputReader.GetInt32(0);
                generatedCreatedAt = sqlCommandOutputReader.GetDateTime(1);
            }

            // 4. Return the constructed ReelModel
            string emptyURL = "", uploadSource = "upload";

            return new ReelModel
            {
                ReelId = generatedReelId,
                MovieId = request.MovieId ?? nullId,
                CreatorUserId = request.UploaderUserId,
                VideoUrl = destinationBlobPath,
                ThumbnailUrl = emptyURL,
                Title = request.Title ?? String.Empty,
                Caption = request.Caption ?? String.Empty,
                FeatureDurationSeconds = computedDurationSeconds,
                Source = uploadSource,
                CreatedAt = generatedCreatedAt
            };
        }
    }
}
