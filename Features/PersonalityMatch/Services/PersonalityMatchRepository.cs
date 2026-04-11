using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Services
{
    /// <summary>
    /// Implements <see cref="IPersonalityMatchRepository"/> using raw SQL queries
    /// against the application database, providing data access for user movie preferences,
    /// user profiles, and related personality match operations.
    /// </summary>
    public class PersonalityMatchRepository : IPersonalityMatchRepository
    {
        private readonly ISqlConnectionFactory connectionFactory;

        private const string FallbackUsernamePrefix = "User";

        private const int ColumnIndexUserMoviePreferenceId = 0;
        private const int ColumnIndexUserId = 1;
        private const int ColumnIndexMovieId = 2;
        private const int ColumnIndexScore = 3;
        private const int ColumnIndexLastModified = 4;

        private const int ColumnIndexUserProfileId = 0;
        private const int ColumnIndexUserProfileUserId = 1;
        private const int ColumnIndexTotalLikes = 2;
        private const int ColumnIndexTotalWatchTimeSec = 3;
        private const int ColumnIndexAvgWatchTimeSec = 4;

        private const int ColumnIndexTotalClipsViewed = 5;
        private const int ColumnIndexLikeToViewRatio = 6;
        private const int ColumnIndexLastUpdated = 7;

        private const int ColumnIndexMovieTitle = 1;
        private const int ColumnIndexMovieScore = 2;

        /// <summary>
        /// Initializes a new instance of <see cref="PersonalityMatchRepository"/> with the specified connection factory.
        /// </summary>
        /// <param name="connectionFactory">The factory used to create and open SQL connections.</param>
        public PersonalityMatchRepository(ISqlConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Executes a SELECT against the <c>UserMoviePreference</c> table, excluding rows
        /// belonging to the specified user, and groups the results by user identifier.
        /// </remarks>
        public async Task<Dictionary<int, List<UserMoviePreferenceModel>>> GetAllPreferencesExceptUserAsync(int excludedUserId)
        {
            const string getAllPreferencesExceptUserSql = @"
                SELECT UserMoviePreferenceId, UserId, MovieId, Score, LastModified
                FROM   UserMoviePreference
                WHERE  UserId <> @ExcludeUserId;";

            Dictionary<int, List<UserMoviePreferenceModel>> preferencesByUserId = new Dictionary<int, List<UserMoviePreferenceModel>>();

            await using SqlConnection connection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand getAllPreferencesExceptUserCommand = new SqlCommand(getAllPreferencesExceptUserSql, connection);
            getAllPreferencesExceptUserCommand.Parameters.AddWithValue("@ExcludeUserId", excludedUserId);

            await using SqlDataReader dataReader = await getAllPreferencesExceptUserCommand.ExecuteReaderAsync();
            while (await dataReader.ReadAsync())
            {
                UserMoviePreferenceModel moviePreference = ReadUserMoviePreference(dataReader);

                if (!preferencesByUserId.ContainsKey(moviePreference.UserId))
                {
                    preferencesByUserId[moviePreference.UserId] = new List<UserMoviePreferenceModel>();
                }

                preferencesByUserId[moviePreference.UserId].Add(moviePreference);
            }

            return preferencesByUserId;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Executes a SELECT against the <c>UserMoviePreference</c> table filtered by the specified user identifier.
        /// </remarks>
        public async Task<List<UserMoviePreferenceModel>> GetCurrentUserPreferencesAsync(int userId)
        {
            const string getCurrentUserPreferencesSql = @"
                SELECT UserMoviePreferenceId, UserId, MovieId, Score, LastModified
                FROM   UserMoviePreference
                WHERE  UserId = @UserId;";

            List<UserMoviePreferenceModel> userPreferences = new List<UserMoviePreferenceModel>();

            await using SqlConnection connection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand getCurrentUserPreferencesCommand = new SqlCommand(getCurrentUserPreferencesSql, connection);
            getCurrentUserPreferencesCommand.Parameters.AddWithValue("@UserId", userId);

            await using SqlDataReader dataReader = await getCurrentUserPreferencesCommand.ExecuteReaderAsync();
            while (await dataReader.ReadAsync())
            {
                userPreferences.Add(ReadUserMoviePreference(dataReader));
            }

            return userPreferences;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Executes a SELECT against the <c>UserProfile</c> table for the specified user identifier.
        /// Returns <see langword="null"/> if no profile record exists for the user.
        /// </remarks>
        public async Task<UserProfileModel?> GetUserProfileAsync(int userId)
        {
            const string getUserProfileSql = @"
                SELECT UserProfileId, UserId, TotalLikes, TotalWatchTimeSec,
                       AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio, LastUpdated
                FROM   UserProfile
                WHERE  UserId = @UserId;";

            await using SqlConnection connection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand getUserProfileCommand = new SqlCommand(getUserProfileSql, connection);
            getUserProfileCommand.Parameters.AddWithValue("@UserId", userId);

            await using SqlDataReader dataReader = await getUserProfileCommand.ExecuteReaderAsync();
            if (await dataReader.ReadAsync())
            {
                return ReadUserProfile(dataReader);
            }

            return null;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Selects a random sample of distinct user identifiers from the <c>UserMoviePreference</c> table,
        /// excluding the specified user, using <c>ORDER BY NEWID()</c> for randomisation.
        /// </remarks>
        public async Task<List<int>> GetRandomUserIdsAsync(int excludedUserId, int userIdsCount)
        {
            const string getRandomUserIdsSql = @"
                SELECT TOP (@Count) UserId
                FROM (
                    SELECT DISTINCT UserId
                    FROM   UserMoviePreference
                    WHERE  UserId <> @ExcludeUserId
                ) AS UniqueUsers
                ORDER BY NEWID();";

            List<int> randomUserIds = new List<int>();

            await using SqlConnection connection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand getRandomUserIdsCommand = new SqlCommand(getRandomUserIdsSql, connection);
            getRandomUserIdsCommand.Parameters.AddWithValue("@ExcludeUserId", excludedUserId);
            getRandomUserIdsCommand.Parameters.AddWithValue("@Count", userIdsCount);

            await using SqlDataReader dataReader = await getRandomUserIdsCommand.ExecuteReaderAsync();
            while (await dataReader.ReadAsync())
            {
                randomUserIds.Add(dataReader.GetInt32(ColumnIndexUserMoviePreferenceId));
            }

            return randomUserIds;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Joins the <c>UserMoviePreference</c> and <c>Movie</c> tables to retrieve the top-scored movies
        /// for the specified user, ordered by score descending. The first result in the returned list
        /// is flagged as the best movie via <see cref="MoviePreferenceDisplayModel.IsBestMovie"/>.
        /// </remarks>
        public async Task<List<MoviePreferenceDisplayModel>> GetTopPreferencesWithTitlesAsync(int userId, int topMoviePreferencesCount)
        {
            const string getTopMoviePreferencesWithTitleSql = @"
                SELECT TOP (@Count) ump.MovieId, m.Title, ump.Score
                FROM   UserMoviePreference ump
                INNER JOIN Movie m ON ump.MovieId = m.MovieId
                WHERE  ump.UserId = @UserId
                ORDER BY ump.Score DESC;";

            List<MoviePreferenceDisplayModel> topMoviePreferences = new List<MoviePreferenceDisplayModel>();

            await using SqlConnection connection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand getTopMoviePreferencesWithTitleCommand = new SqlCommand(getTopMoviePreferencesWithTitleSql, connection);
            getTopMoviePreferencesWithTitleCommand.Parameters.AddWithValue("@UserId", userId);
            getTopMoviePreferencesWithTitleCommand.Parameters.AddWithValue("@Count", topMoviePreferencesCount);

            await using SqlDataReader dataReader = await getTopMoviePreferencesWithTitleCommand.ExecuteReaderAsync();
            bool isFirstRow = true;
            while (await dataReader.ReadAsync())
            {
                topMoviePreferences.Add(ReadMoviePreferenceDisplayModel(dataReader, isBestMovie: isFirstRow));
                isFirstRow = false;
            }

            return topMoviePreferences;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Queries the <c>[User]</c> table only if it exists in the database schema,
        /// guarding against environments where the table has not yet been created.
        /// Falls back to a generated placeholder of the form <c>User {userId}</c>
        /// if the table is absent, the user is not found, or any exception occurs.
        /// </remarks>
        public async Task<string> GetUsernameAsync(int userId)
        {
            const string getUsernameSql = @"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'User')
                BEGIN
                    SELECT Username FROM [User] WHERE UserId = @UserId;
                END";

            try
            {
                await using SqlConnection connection = await this.connectionFactory.CreateConnectionAsync();
                await using SqlCommand getUsernameCommand = new SqlCommand(getUsernameSql, connection);
                getUsernameCommand.Parameters.AddWithValue("@UserId", userId);

                object? usernameQueryResult = await getUsernameCommand.ExecuteScalarAsync();
                bool usernameWasFound = usernameQueryResult != null && usernameQueryResult != DBNull.Value;
                if (usernameWasFound)
                {
                    return (string)usernameQueryResult!;
                }
            }
            catch
            {
            }

            return $"{FallbackUsernamePrefix} {userId}";
        }

        /// <summary>
        /// Reads a single row from the provided <see cref="SqlDataReader"/> and maps it to a <see cref="UserMoviePreferenceModel"/>.
        /// </summary>
        /// <param name="dataReader">The data reader positioned at the row to read.</param>
        /// <returns>
        /// A <see cref="UserMoviePreferenceModel"/> populated with the values from the current row.
        /// </returns>
        private static UserMoviePreferenceModel ReadUserMoviePreference(SqlDataReader dataReader)
        {
            return new UserMoviePreferenceModel
            {
                UserMoviePreferenceId = dataReader.GetInt32(ColumnIndexUserMoviePreferenceId),
                UserId = dataReader.GetInt32(ColumnIndexUserId),
                MovieId = dataReader.GetInt32(ColumnIndexMovieId),
                Score = dataReader.GetDouble(ColumnIndexScore),
                LastModified = dataReader.GetDateTime(ColumnIndexLastModified),
            };
        }

        /// <summary>
        /// Reads a single row from the provided <see cref="SqlDataReader"/> and maps it to a <see cref="UserProfileModel"/>.
        /// </summary>
        /// <param name="dataReader">The data reader positioned at the row to read.</param>
        /// <returns>
        /// A <see cref="UserProfileModel"/> populated with the values from the current row.
        /// </returns>
        private static UserProfileModel ReadUserProfile(SqlDataReader dataReader)
        {
            return new UserProfileModel
            {
                UserProfileId = dataReader.GetInt32(ColumnIndexUserProfileId),
                UserId = dataReader.GetInt32(ColumnIndexUserProfileUserId),
                TotalLikes = dataReader.GetInt32(ColumnIndexTotalLikes),
                TotalWatchTimeSec = dataReader.GetInt64(ColumnIndexTotalWatchTimeSec),
                AvgWatchTimeSec = dataReader.GetDouble(ColumnIndexAvgWatchTimeSec),
                TotalClipsViewed = dataReader.GetInt32(ColumnIndexTotalClipsViewed),
                LikeToViewRatio = dataReader.GetDouble(ColumnIndexLikeToViewRatio),
                LastUpdated = dataReader.GetDateTime(ColumnIndexLastUpdated),
            };
        }

        /// <summary>
        /// Reads a single row from the provided <see cref="SqlDataReader"/> and maps it to a <see cref="MoviePreferenceDisplayModel"/>, flagging it as the best movie if specified.
        /// </summary>
        /// <param name="dataReader">The data reader positioned at the row to read.</param>
        /// <param name="isBestMovie">
        /// Indicates whether this movie should be flagged as the user's highest-scored movie.
        /// Pass <see langword="true"/> only for the first row in the result set.
        /// </param>
        /// <returns>
        /// A <see cref="MoviePreferenceDisplayModel"/> populated with the values from the current row.
        /// </returns>
        private static MoviePreferenceDisplayModel ReadMoviePreferenceDisplayModel(SqlDataReader dataReader, bool isBestMovie)
        {
            return new MoviePreferenceDisplayModel
            {
                MovieId = dataReader.GetInt32(ColumnIndexUserMoviePreferenceId),
                Title = dataReader.GetString(ColumnIndexMovieTitle),
                Score = dataReader.GetDouble(ColumnIndexMovieScore),
                IsBestMovie = isBestMovie,
            };
        }
    }
}