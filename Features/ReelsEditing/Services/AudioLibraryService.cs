using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    public class AudioLibraryService : IAudioLibraryService
    {
        private readonly ISqlConnectionFactory _db;
        public AudioLibraryService(ISqlConnectionFactory db) => _db = db;

        public async Task<IList<MusicTrackModel>> GetAllTracksAsync()
        {
            const string sql = "SELECT MusicTrackId, TrackName, Author, AudioUrl, DurationSeconds FROM MusicTrack ORDER BY TrackName";
            var result = new List<MusicTrackModel>();
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MusicTrackModel
                {
                    MusicTrackId = reader.GetInt32(0),
                    TrackName = reader.GetString(1),
                    Author = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    AudioUrl = reader.GetString(3),
                    DurationSeconds = reader.GetDouble(4),
                });
            }
            return result;
        }

        public async Task<MusicTrackModel?> GetTrackByIdAsync(int musicTrackId)
        {
            const string sql = "SELECT MusicTrackId, TrackName, Author, AudioUrl, DurationSeconds FROM MusicTrack WHERE MusicTrackId = @Id";
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", musicTrackId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MusicTrackModel
                {
                    MusicTrackId = reader.GetInt32(0),
                    TrackName = reader.GetString(1),
                    Author = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    AudioUrl = reader.GetString(3),
                    DurationSeconds = reader.GetDouble(4),
                };
            }
            return null;
        }
    }
}
