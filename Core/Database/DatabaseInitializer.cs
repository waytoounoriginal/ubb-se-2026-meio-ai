using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{
    /// <summary>
    /// Creates the shared database tables if they do not already exist.
    /// All SQL is raw — no ORM, no stored procedures.
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public DatabaseInitializer(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateTablesIfNotExistAsync()
        {
            // 1. Ensure the MeioAiDb database exists on the server
            await EnsureDatabaseExistsAsync();
            
            // 2. Create the tables in the MeioAiDb database
            const string sql = @"
                -- Movie (shared table — created here so JOINs work)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Movie')
                BEGIN
                    CREATE TABLE Movie
                    (
                        MovieId         INT             IDENTITY(1,1) PRIMARY KEY,
                        Title           NVARCHAR(256)   NOT NULL,
                        PosterUrl       NVARCHAR(1024)  NULL,
                        PrimaryGenre    NVARCHAR(128)   NULL,
                        Description     NVARCHAR(MAX)   NULL,
                        ReleaseYear     INT             NULL,
                        AverageRating   FLOAT           NULL
                    );
                END

                -- ScrapeJob (tracks each scraping job)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScrapeJob')
                BEGIN
                    CREATE TABLE ScrapeJob
                    (
                        ScrapeJobId     INT             IDENTITY(1,1) PRIMARY KEY,
                        SearchQuery     NVARCHAR(512)   NOT NULL,
                        MaxResults      INT             NOT NULL DEFAULT 5,
                        Status          NVARCHAR(64)    NOT NULL DEFAULT 'pending',
                        MoviesFound     INT             NOT NULL DEFAULT 0,
                        ReelsCreated    INT             NOT NULL DEFAULT 0,
                        StartedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
                        CompletedAt     DATETIME2       NULL,
                        ErrorMessage    NVARCHAR(MAX)   NULL
                    );
                END

                -- ScrapeJobLog (per-job log entries)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScrapeJobLog')
                BEGIN
                    CREATE TABLE ScrapeJobLog
                    (
                        LogId           BIGINT          IDENTITY(1,1) PRIMARY KEY,
                        ScrapeJobId     INT             NOT NULL,
                        Level           NVARCHAR(16)    NOT NULL DEFAULT 'Info',
                        Message         NVARCHAR(MAX)   NOT NULL,
                        Timestamp       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT FK_ScrapeJobLog_ScrapeJob
                            FOREIGN KEY (ScrapeJobId) REFERENCES ScrapeJob(ScrapeJobId)
                    );
                END

                -- MusicTrack (no FK dependencies)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MusicTrack')
                BEGIN
                    CREATE TABLE MusicTrack
                    (
                        MusicTrackId    INT             IDENTITY(1,1) PRIMARY KEY,
                        TrackName       NVARCHAR(256)   NOT NULL,
                        AudioUrl        NVARCHAR(1024)  NOT NULL,
                        DurationSeconds FLOAT           NOT NULL
                    );
                END

                -- Reel (references Movie, User, and MusicTrack — 
                --       Movie & User are assumed to exist externally)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reel')
                BEGIN
                    CREATE TABLE Reel
                    (
                        ReelId                  INT             IDENTITY(1,1) PRIMARY KEY,
                        MovieId                 INT             NOT NULL,
                        CreatorUserId           INT             NOT NULL,
                        VideoUrl                NVARCHAR(1024)  NOT NULL,
                        ThumbnailUrl            NVARCHAR(1024)  NOT NULL DEFAULT '',
                        Title                   NVARCHAR(256)   NOT NULL,
                        Caption                 NVARCHAR(2048)  NOT NULL DEFAULT '',
                        FeatureDurationSeconds  FLOAT           NOT NULL DEFAULT 0,
                        CropDataJson            NVARCHAR(MAX)   NULL,
                        BackgroundMusicId       INT             NULL,
                        Source                  NVARCHAR(128)   NOT NULL DEFAULT 'manual',
                        CreatedAt               DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
                        LastEditedAt            DATETIME2       NULL,
                        CONSTRAINT FK_Reel_MusicTrack 
                            FOREIGN KEY (BackgroundMusicId) REFERENCES MusicTrack(MusicTrackId)
                    );
                END

                -- UserMoviePreference (references User and Movie — external)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserMoviePreference')
                BEGIN
                    CREATE TABLE UserMoviePreference
                    (
                        UserMoviePreferenceId   INT         IDENTITY(1,1) PRIMARY KEY,
                        UserId                  INT         NOT NULL,
                        MovieId                 INT         NOT NULL,
                        Score                   FLOAT       NOT NULL DEFAULT 0,
                        LastModified            DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
                        ChangeFromPreviousValue INT         NULL,
                        CONSTRAINT UQ_UserMovie UNIQUE (UserId, MovieId)
                    );

                    -- Insert 8 mock preferences for the tournament (UserId = 1)
                    -- We check for UserId 1 to avoid duplicates if the table existed but was empty
                    IF NOT EXISTS (SELECT * FROM UserMoviePreference WHERE UserId = 1)
                    BEGIN
                        INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                        VALUES 
                        (1, 1, 8.5, 1),
                        (1, 2, 9.0, 1),
                        (1, 3, 7.5, 1),
                        (1, 4, 8.0, 1),
                        (1, 5, 9.5, 1),
                        (1, 6, 8.5, 1),
                        (1, 7, 7.0, 1),
                        (1, 8, 9.2, 1);
                    END
                END

                -- UserProfile (references User — external)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserProfile')
                BEGIN
                    CREATE TABLE UserProfile
                    (
                        UserProfileId       INT         IDENTITY(1,1) PRIMARY KEY,
                        UserId              INT         NOT NULL UNIQUE,
                        TotalLikes          INT         NOT NULL DEFAULT 0,
                        TotalWatchTimeSec   BIGINT      NOT NULL DEFAULT 0,
                        AvgWatchTimeSec     FLOAT       NOT NULL DEFAULT 0,
                        TotalClipsViewed    INT         NOT NULL DEFAULT 0,
                        LikeToViewRatio     FLOAT       NOT NULL DEFAULT 0,
                        LastUpdated         DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME()
                    );

                    -- Seed UserId = 1
                    INSERT INTO UserProfile (UserId) VALUES (1);
                END

                -- UserReelInteraction (references User and Reel)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserReelInteraction')
                BEGIN
                    CREATE TABLE UserReelInteraction
                    (
                        InteractionId       BIGINT      IDENTITY(1,1) PRIMARY KEY,
                        UserId              INT         NOT NULL,
                        ReelId              INT         NOT NULL,
                        IsLiked             BIT         NOT NULL DEFAULT 0,
                        WatchDurationSec    FLOAT       NOT NULL DEFAULT 0,
                        WatchPercentage     FLOAT       NOT NULL DEFAULT 0,
                        ViewedAt            DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT FK_Interaction_Reel 
                            FOREIGN KEY (ReelId) REFERENCES Reel(ReelId),
                        CONSTRAINT UQ_UserReel UNIQUE (UserId, ReelId)
                    );
                END

                -- Seed Movies for Demo
                IF (SELECT COUNT(*) FROM Movie) = 0
                BEGIN
                    INSERT INTO Movie (Title, PosterUrl, PrimaryGenre, ReleaseYear)
                    VALUES
                    ('Inception', 'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg', 'Sci-Fi', 2010),
                    ('The Dark Knight', 'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg', 'Action', 2008),
                    ('Interstellar', 'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg', 'Adventure', 2014),
                    ('The Matrix', 'https://m.media-amazon.com/images/M/MV5BNzQzOTk3NTAtNDQ2Ny00Njc2LTk3M2QtN2FjYTJjNzQzYzQwXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg', 'Sci-Fi', 1999),
                    ('Parasite', 'https://m.media-amazon.com/images/M/MV5BYWZjMjk3ZTAtZGYzMC00ODQ0LWI2YTMtYjQ5NDU3N2NmZDIzXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg', 'Thriller', 2019),
                    ('La La Land', 'https://m.media-amazon.com/images/M/MV5BMjA2OTYxNTY2Nl5BMl5BanBnXkFtZTgwNzg4OTA5OTE@._V1_.jpg', 'Musical', 2016),
                    ('Whiplash', 'https://m.media-amazon.com/images/M/MV5BMjE4NDYxNTAxNV5BMl5BanBnXkFtZTgwNzM0NDM1MjE@._V1_.jpg', 'Drama', 2014),
                    ('The Grand Budapest Hotel', 'https://m.media-amazon.com/images/M/MV5BMjM2NTQzMzc5OF5BMl5BanBnXkFtZTgwNzM2ODU3MDE@._V1_.jpg', 'Comedy', 2014);
                END
            ";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task EnsureDatabaseExistsAsync()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MeioAiDb')
                BEGIN
                    CREATE DATABASE [MeioAiDb];
                END
            ";

            // We must use the 'master' database connection to create a new database
            await using SqlConnection masterConnection = await _connectionFactory.CreateMasterConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, masterConnection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
