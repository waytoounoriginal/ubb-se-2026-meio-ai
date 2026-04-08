namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;
    using ubb_se_2026_meio_ai.Core.Database;
    using ubb_se_2026_meio_ai.Core.Models;

    /// <summary>
    /// Raw SQL implementation of <see cref="IScrapeJobRepository"/>.
    /// Owner: Andrei.
    /// </summary>
    public class ScrapeJobRepository : IScrapeJobRepository
    {
        private const int MaxLogsToRetrieve = 200;
        private const int MaxMoviesToSearch = 20;
        private const int EmptyCount = 0;

        private const string SqlInsertJob = @"
                INSERT INTO ScrapeJob (SearchQuery, MaxResults, Status, MoviesFound, ReelsCreated, StartedAt)
                VALUES (@SearchQuery, @MaxResults, @Status, @MoviesFound, @ReelsCreated, @StartedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

        private const string SqlUpdateJob = @"
                UPDATE ScrapeJob
                SET Status          = @Status,
                    MoviesFound     = @MoviesFound,
                    ReelsCreated    = @ReelsCreated,
                    CompletedAt     = @CompletedAt,
                    ErrorMessage    = @ErrorMessage
                WHERE ScrapeJobId   = @ScrapeJobId;";

        private const string SqlInsertLog = @"
                INSERT INTO ScrapeJobLog (ScrapeJobId, Level, Message, Timestamp)
                VALUES (@ScrapeJobId, @Level, @Message, @Timestamp);";

        private const string SqlSelectAllJobs = "SELECT * FROM ScrapeJob ORDER BY StartedAt DESC;";
        private const string SqlSelectLogsForJob = "SELECT * FROM ScrapeJobLog WHERE ScrapeJobId = @ScrapeJobId ORDER BY Timestamp;";
        private const string SqlSelectAllLogsFormat = "SELECT TOP {0} * FROM ScrapeJobLog ORDER BY Timestamp DESC;";

        private const string SqlSelectDashboardStats = @"
                SELECT
                    (SELECT COUNT(*) FROM Movie)                                                    AS TotalMovies,
                    (SELECT COUNT(*) FROM Reel)                                                     AS TotalReels,
                    (SELECT COUNT(*) FROM ScrapeJob)                                                AS TotalJobs,
                    (SELECT COUNT(*) FROM ScrapeJob WHERE Status = 'running')                       AS RunningJobs,
                    (SELECT COUNT(*) FROM ScrapeJob WHERE Status = 'completed')                     AS CompletedJobs,
                    (SELECT COUNT(*) FROM ScrapeJob WHERE Status = 'failed')                        AS FailedJobs;";

        private const string SqlSearchMoviesFormat = @"
                SELECT TOP {0} MovieId, Title, PosterUrl, PrimaryGenre, ReleaseYear, Description
                FROM Movie
                WHERE Title LIKE '%' + @Name + '%' COLLATE SQL_Latin1_General_CP1_CI_AS
                ORDER BY Title;";

        private const string SqlFindMovieByTitle = "SELECT TOP 1 MovieId FROM Movie WHERE Title = @Title;";
        private const string SqlCountReelByUrl = "SELECT COUNT(*) FROM Reel WHERE VideoUrl = @VideoUrl;";

        private const string SqlInsertReel = @"
                INSERT INTO Reel (MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, Source, CreatedAt)
                VALUES (@MovieId, @CreatorUserId, @VideoUrl, @ThumbnailUrl, @Title, @Caption, @Source, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

        private const string SqlSelectAllMovies = "SELECT MovieId, Title, PosterUrl, PrimaryGenre, ReleaseYear, Description FROM Movie ORDER BY Title;";
        private const string SqlSelectAllReels = "SELECT ReelId, MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, Source, CreatedAt FROM Reel ORDER BY CreatedAt DESC;";

        private const string ParamSearchQuery = "@SearchQuery";
        private const string ParamMaxResults = "@MaxResults";
        private const string ParamStatus = "@Status";
        private const string ParamMoviesFound = "@MoviesFound";
        private const string ParamReelsCreated = "@ReelsCreated";
        private const string ParamStartedAt = "@StartedAt";
        private const string ParamCompletedAt = "@CompletedAt";
        private const string ParamErrorMessage = "@ErrorMessage";
        private const string ParamScrapeJobId = "@ScrapeJobId";
        private const string ParamLevel = "@Level";
        private const string ParamMessage = "@Message";
        private const string ParamTimestamp = "@Timestamp";
        private const string ParamName = "@Name";
        private const string ParamTitle = "@Title";
        private const string ParamVideoUrl = "@VideoUrl";
        private const string ParamMovieId = "@MovieId";
        private const string ParamCreatorUserId = "@CreatorUserId";
        private const string ParamThumbnailUrl = "@ThumbnailUrl";
        private const string ParamCaption = "@Caption";
        private const string ParamSource = "@Source";
        private const string ParamCreatedAt = "@CreatedAt";

        private const string ColTotalMovies = "TotalMovies";
        private const string ColTotalReels = "TotalReels";
        private const string ColTotalJobs = "TotalJobs";
        private const string ColRunningJobs = "RunningJobs";
        private const string ColCompletedJobs = "CompletedJobs";
        private const string ColFailedJobs = "FailedJobs";
        private const string ColMovieId = "MovieId";
        private const string ColTitle = "Title";
        private const string ColPosterUrl = "PosterUrl";
        private const string ColPrimaryGenre = "PrimaryGenre";
        private const string ColReleaseYear = "ReleaseYear";
        private const string ColDescription = "Description";
        private const string ColReelId = "ReelId";
        private const string ColCreatorUserId = "CreatorUserId";
        private const string ColVideoUrl = "VideoUrl";
        private const string ColThumbnailUrl = "ThumbnailUrl";
        private const string ColCaption = "Caption";
        private const string ColSource = "Source";
        private const string ColCreatedAt = "CreatedAt";
        private const string ColScrapeJobId = "ScrapeJobId";
        private const string ColSearchQuery = "SearchQuery";
        private const string ColMaxResults = "MaxResults";
        private const string ColStatus = "Status";
        private const string ColMoviesFound = "MoviesFound";
        private const string ColReelsCreated = "ReelsCreated";
        private const string ColStartedAt = "StartedAt";
        private const string ColCompletedAt = "CompletedAt";
        private const string ColErrorMessage = "ErrorMessage";
        private const string ColLogId = "LogId";
        private const string ColLevel = "Level";
        private const string ColMessage = "Message";
        private const string ColTimestamp = "Timestamp";

        private readonly ISqlConnectionFactory connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapeJobRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The SQL connection factory used for database operations.</param>
        public ScrapeJobRepository(ISqlConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Creates a new scrape job in the database.
        /// </summary>
        /// <param name="job">The job model to insert.</param>
        /// <returns>A task containing the newly generated job ID.</returns>
        public async Task<int> CreateJobAsync(ScrapeJobModel job)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlInsertJob, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamSearchQuery, job.SearchQuery);
            sqlCommand.Parameters.AddWithValue(ParamMaxResults, job.MaxResults);
            sqlCommand.Parameters.AddWithValue(ParamStatus, job.Status);
            sqlCommand.Parameters.AddWithValue(ParamMoviesFound, job.MoviesFound);
            sqlCommand.Parameters.AddWithValue(ParamReelsCreated, job.ReelsCreated);
            sqlCommand.Parameters.AddWithValue(ParamStartedAt, job.StartedAt);

            object? result = await sqlCommand.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates an existing scrape job in the database.
        /// </summary>
        /// <param name="job">The job model to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateJobAsync(ScrapeJobModel job)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlUpdateJob, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamStatus, job.Status);
            sqlCommand.Parameters.AddWithValue(ParamMoviesFound, job.MoviesFound);
            sqlCommand.Parameters.AddWithValue(ParamReelsCreated, job.ReelsCreated);
            sqlCommand.Parameters.AddWithValue(ParamCompletedAt, (object?)job.CompletedAt ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(ParamErrorMessage, (object?)job.ErrorMessage ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(ParamScrapeJobId, job.ScrapeJobId);

            await sqlCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Adds a log entry to the database for a specific scrape job.
        /// </summary>
        /// <param name="log">The log entry to insert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddLogEntryAsync(ScrapeJobLogModel log)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlInsertLog, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamScrapeJobId, log.ScrapeJobId);
            sqlCommand.Parameters.AddWithValue(ParamLevel, log.Level);
            sqlCommand.Parameters.AddWithValue(ParamMessage, log.Message);
            sqlCommand.Parameters.AddWithValue(ParamTimestamp, log.Timestamp);

            await sqlCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves all scrape jobs from the database.
        /// </summary>
        /// <returns>A task containing the list of scrape jobs.</returns>
        public async Task<IList<ScrapeJobModel>> GetAllJobsAsync()
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlSelectAllJobs, sqlConnection);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var jobs = new List<ScrapeJobModel>();
            while (await sqlDataReader.ReadAsync())
            {
                jobs.Add(MapJob(sqlDataReader));
            }

            return jobs;
        }

        /// <summary>
        /// Retrieves all log entries associated with a specific scrape job.
        /// </summary>
        /// <param name="jobId">The unique identifier of the scrape job.</param>
        /// <returns>A task containing the list of log entries.</returns>
        public async Task<IList<ScrapeJobLogModel>> GetLogsForJobAsync(int jobId)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlSelectLogsForJob, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamScrapeJobId, jobId);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var logs = new List<ScrapeJobLogModel>();
            while (await sqlDataReader.ReadAsync())
            {
                logs.Add(MapLog(sqlDataReader));
            }

            return logs;
        }

        /// <summary>
        /// Retrieves the most recent log entries across all scrape jobs.
        /// </summary>
        /// <returns>A task containing the list of recent log entries.</returns>
        public async Task<IList<ScrapeJobLogModel>> GetAllLogsAsync()
        {
            string sqlQuery = string.Format(SqlSelectAllLogsFormat, MaxLogsToRetrieve);
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(sqlQuery, sqlConnection);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var logs = new List<ScrapeJobLogModel>();
            while (await sqlDataReader.ReadAsync())
            {
                logs.Add(MapLog(sqlDataReader));
            }

            return logs;
        }

        /// <summary>
        /// Retrieves dashboard statistics summarizing database records.
        /// </summary>
        /// <returns>A task containing the dashboard statistics model.</returns>
        public async Task<DashboardStatsModel> GetDashboardStatsAsync()
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlSelectDashboardStats, sqlConnection);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var stats = new DashboardStatsModel();
            if (await sqlDataReader.ReadAsync())
            {
                stats.TotalMovies = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColTotalMovies));
                stats.TotalReels = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColTotalReels));
                stats.TotalJobs = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColTotalJobs));
                stats.RunningJobs = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColRunningJobs));
                stats.CompletedJobs = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColCompletedJobs));
                stats.FailedJobs = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColFailedJobs));
            }

            return stats;
        }

        /// <summary>
        /// Searches for movies matching a partial name.
        /// </summary>
        /// <param name="partialName">The partial movie title to search for.</param>
        /// <returns>A task containing a list of matching movies.</returns>
        public async Task<IList<MovieCardModel>> SearchMoviesByNameAsync(string partialName)
        {
            string sqlQuery = string.Format(SqlSearchMoviesFormat, MaxMoviesToSearch);
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(sqlQuery, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamName, partialName);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var movies = new List<MovieCardModel>();
            while (await sqlDataReader.ReadAsync())
            {
                movies.Add(new MovieCardModel
                {
                    MovieId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColMovieId)),
                    Title = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColTitle)),
                    PosterUrl = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColPosterUrl)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColPosterUrl)),
                    Genre = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColPrimaryGenre)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColPrimaryGenre)),
                    ReleaseYear = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColReleaseYear)),
                    Synopsis = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColDescription)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColDescription)),
                });
            }

            return movies;
        }

        /// <summary>
        /// Finds a movie ID by its exact title.
        /// </summary>
        /// <param name="title">The exact movie title.</param>
        /// <returns>A task containing the movie ID if found; otherwise, null.</returns>
        public async Task<int?> FindMovieByTitleAsync(string title)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlFindMovieByTitle, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamTitle, title);

            object? result = await sqlCommand.ExecuteScalarAsync();
            return result is null or DBNull ? null : Convert.ToInt32(result);
        }

        /// <summary>
        /// Checks if a reel exists for the specified video URL.
        /// </summary>
        /// <param name="videoUrl">The video URL to check.</param>
        /// <returns>A task returning true if the reel exists; otherwise, false.</returns>
        public async Task<bool> ReelExistsByVideoUrlAsync(string videoUrl)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlCountReelByUrl, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamVideoUrl, videoUrl);

            object? result = await sqlCommand.ExecuteScalarAsync();
            return Convert.ToInt32(result) > EmptyCount;
        }

        /// <summary>
        /// Inserts a new scraped reel into the database.
        /// </summary>
        /// <param name="reel">The reel model to insert.</param>
        /// <returns>A task containing the new reel ID.</returns>
        public async Task<int> InsertScrapedReelAsync(ReelModel reel)
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlInsertReel, sqlConnection);
            sqlCommand.Parameters.AddWithValue(ParamMovieId, reel.MovieId);
            sqlCommand.Parameters.AddWithValue(ParamCreatorUserId, reel.CreatorUserId);
            sqlCommand.Parameters.AddWithValue(ParamVideoUrl, reel.VideoUrl);
            sqlCommand.Parameters.AddWithValue(ParamThumbnailUrl, reel.ThumbnailUrl);
            sqlCommand.Parameters.AddWithValue(ParamTitle, reel.Title);
            sqlCommand.Parameters.AddWithValue(ParamCaption, reel.Caption);
            sqlCommand.Parameters.AddWithValue(ParamSource, reel.Source);
            sqlCommand.Parameters.AddWithValue(ParamCreatedAt, reel.CreatedAt);

            object? result = await sqlCommand.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Retrieves all movies from the database.
        /// </summary>
        /// <returns>A task containing the list of all movies.</returns>
        public async Task<IList<MovieCardModel>> GetAllMoviesAsync()
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlSelectAllMovies, sqlConnection);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var movies = new List<MovieCardModel>();
            while (await sqlDataReader.ReadAsync())
            {
                movies.Add(new MovieCardModel
                {
                    MovieId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColMovieId)),
                    Title = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColTitle)),
                    PosterUrl = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColPosterUrl)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColPosterUrl)),
                    Genre = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColPrimaryGenre)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColPrimaryGenre)),
                    ReleaseYear = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColReleaseYear)),
                    Synopsis = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColDescription)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColDescription)),
                });
            }

            return movies;
        }

        /// <summary>
        /// Retrieves all reels from the database.
        /// </summary>
        /// <returns>A task containing the list of all reels.</returns>
        public async Task<IList<ReelModel>> GetAllReelsAsync()
        {
            await using SqlConnection sqlConnection = await this.connectionFactory.CreateConnectionAsync();
            await using SqlCommand sqlCommand = new SqlCommand(SqlSelectAllReels, sqlConnection);
            await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

            var reels = new List<ReelModel>();
            while (await sqlDataReader.ReadAsync())
            {
                reels.Add(new ReelModel
                {
                    ReelId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColReelId)),
                    MovieId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColMovieId)),
                    CreatorUserId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColCreatorUserId)),
                    VideoUrl = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColVideoUrl)),
                    ThumbnailUrl = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColThumbnailUrl)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColThumbnailUrl)),
                    Title = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColTitle)),
                    Caption = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColCaption)) ? string.Empty : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColCaption)),
                    Source = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColSource)),
                    CreatedAt = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal(ColCreatedAt)),
                });
            }

            return reels;
        }

        /// <summary>
        /// Maps a <see cref="SqlDataReader"/> row to a <see cref="ScrapeJobModel"/>.
        /// </summary>
        /// <param name="sqlDataReader">The SQL data reader.</param>
        /// <returns>A mapped job model.</returns>
        private static ScrapeJobModel MapJob(SqlDataReader sqlDataReader)
        {
            return new ScrapeJobModel
            {
                ScrapeJobId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColScrapeJobId)),
                SearchQuery = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColSearchQuery)),
                MaxResults = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColMaxResults)),
                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColStatus)),
                MoviesFound = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColMoviesFound)),
                ReelsCreated = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColReelsCreated)),
                StartedAt = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal(ColStartedAt)),
                CompletedAt = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColCompletedAt))
                                    ? null : sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal(ColCompletedAt)),
                ErrorMessage = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal(ColErrorMessage))
                                    ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColErrorMessage)),
            };
        }

        /// <summary>
        /// Maps a <see cref="SqlDataReader"/> row to a <see cref="ScrapeJobLogModel"/>.
        /// </summary>
        /// <param name="sqlDataReader">The SQL data reader.</param>
        /// <returns>A mapped log model.</returns>
        private static ScrapeJobLogModel MapLog(SqlDataReader sqlDataReader)
        {
            return new ScrapeJobLogModel
            {
                LogId = sqlDataReader.GetInt64(sqlDataReader.GetOrdinal(ColLogId)),
                ScrapeJobId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal(ColScrapeJobId)),
                Level = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColLevel)),
                Message = sqlDataReader.GetString(sqlDataReader.GetOrdinal(ColMessage)),
                Timestamp = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal(ColTimestamp)),
            };
        }
    }
}