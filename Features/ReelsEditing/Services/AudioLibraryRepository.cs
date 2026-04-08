namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;
    using ubb_se_2026_meio_ai.Core.Database;
    using ubb_se_2026_meio_ai.Core.Models;

    /// <summary>
    /// Service responsible for fetching audio track data from the database.
    /// </summary>
    public class AudioLibraryRepository : IAudioLibraryRepository
    {
        private const string SqlSelectAllTracks = "SELECT MusicTrackId, TrackName, Author, AudioUrl, DurationSeconds FROM MusicTrack ORDER BY TrackName";
        private const string SqlSelectTrackById = "SELECT MusicTrackId, TrackName, Author, AudioUrl, DurationSeconds FROM MusicTrack WHERE MusicTrackId = @Id";
        private const string ParameterTrackId = "@Id";

        private const int ColumnIndexMusicTrackId = 0;
        private const int ColumnIndexTrackName = 1;
        private const int ColumnIndexAuthor = 2;
        private const int ColumnIndexAudioUrl = 3;
        private const int ColumnIndexDurationSeconds = 4;

        private readonly ISqlConnectionFactory sqlConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioLibraryService"/> class.
        /// </summary>
        /// <param name="sqlConnectionFactory">The SQL connection factory used to access the database.</param>
        public AudioLibraryRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            this.sqlConnectionFactory = sqlConnectionFactory;
        }

        /// <summary>
        /// Retrieves all music tracks available in the library.
        /// </summary>
        /// <returns>A list of music tracks.</returns>
        public async Task<IList<MusicTrackModel>> GetAllTracksAsync()
        {
            var resultList = new List<MusicTrackModel>();

            await using var sqlConnection = await this.sqlConnectionFactory.CreateConnectionAsync();
            await using var sqlCommand = new SqlCommand(SqlSelectAllTracks, sqlConnection);
            await using var dataReader = await sqlCommand.ExecuteReaderAsync();

            while (await dataReader.ReadAsync())
            {
                resultList.Add(new MusicTrackModel
                {
                    MusicTrackId = dataReader.GetInt32(ColumnIndexMusicTrackId),
                    TrackName = dataReader.GetString(ColumnIndexTrackName),
                    Author = dataReader.IsDBNull(ColumnIndexAuthor) ? string.Empty : dataReader.GetString(ColumnIndexAuthor),
                    AudioUrl = dataReader.GetString(ColumnIndexAudioUrl),
                    DurationSeconds = dataReader.GetDouble(ColumnIndexDurationSeconds),
                });
            }

            return resultList;
        }

        /// <summary>
        /// Retrieves a specific music track by its unique identifier.
        /// </summary>
        /// <param name="musicTrackId">The unique identifier of the music track.</param>
        /// <returns>The music track if found; otherwise, null.</returns>
        public async Task<MusicTrackModel?> GetTrackByIdAsync(int musicTrackId)
        {
            await using var sqlConnection = await this.sqlConnectionFactory.CreateConnectionAsync();
            await using var sqlCommand = new SqlCommand(SqlSelectTrackById, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParameterTrackId, musicTrackId);

            await using var dataReader = await sqlCommand.ExecuteReaderAsync();

            if (await dataReader.ReadAsync())
            {
                return new MusicTrackModel
                {
                    MusicTrackId = dataReader.GetInt32(ColumnIndexMusicTrackId),
                    TrackName = dataReader.GetString(ColumnIndexTrackName),
                    Author = dataReader.IsDBNull(ColumnIndexAuthor) ? string.Empty : dataReader.GetString(ColumnIndexAuthor),
                    AudioUrl = dataReader.GetString(ColumnIndexAudioUrl),
                    DurationSeconds = dataReader.GetDouble(ColumnIndexDurationSeconds),
                };
            }

            return null;
        }
    }
}