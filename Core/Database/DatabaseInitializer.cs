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
            const string sql = @"
                -- Movie (external table — created here for standalone dev)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Movie')
                BEGIN
                    CREATE TABLE Movie
                    (
                        MovieId         INT             IDENTITY(1,1) PRIMARY KEY,
                        Title           NVARCHAR(256)   NOT NULL,
                        PosterUrl       NVARCHAR(1024)  NOT NULL DEFAULT '',
                        PrimaryGenre    NVARCHAR(128)   NOT NULL DEFAULT '',
                        Description     NVARCHAR(MAX)   NULL,
                        ReleaseYear     INT             NOT NULL DEFAULT 0,
                        AverageRating   FLOAT           NOT NULL DEFAULT 0
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

                -- ── Wipe and re-seed mock movies for testing ──
                DELETE FROM Movie WHERE MovieId NOT IN (SELECT DISTINCT MovieId FROM Reel);

                IF NOT EXISTS (SELECT 1 FROM Movie)
                BEGIN
                    INSERT INTO Movie (Title, PrimaryGenre, ReleaseYear, Description) VALUES
                        (N'The Batman',                         N'Action',    2022, N'When a sadistic serial killer begins murdering key political figures in Gotham, Batman is forced to investigate.'),
                        (N'Dune: Part Two',                     N'Sci-Fi',    2024, N'Paul Atreides unites with the Fremen to seek revenge against those who destroyed his family.'),
                        (N'Oppenheimer',                        N'Drama',     2023, N'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.'),
                        (N'Spider-Man: Across the Spider-Verse', N'Animation', 2023, N'Miles Morales catapults across the Multiverse, where he encounters a team of Spider-People.'),
                        (N'Interstellar',                       N'Sci-Fi',    2014, N'A team of explorers travel through a wormhole in space in an attempt to save the human race.'),
                        (N'The Dark Knight',                    N'Action',    2008, N'Batman raises the stakes in his war on crime with the help of Lt. Jim Gordon and Harvey Dent.'),
                        (N'Inception',                          N'Sci-Fi',    2010, N'A thief who steals corporate secrets through dream-sharing technology is given the task of planting an idea.'),
                        (N'Joker',                              N'Drama',     2019, N'A mentally troubled comedian embarks on a downward spiral that leads to the creation of an iconic villain.'),
                        (N'Avengers: Endgame',                  N'Action',    2019, N'After Thanos snaps away half of all life, the remaining Avengers must figure out a way to bring back their vanquished allies.'),
                        (N'Parasite',                           N'Thriller',  2019, N'Greed and class discrimination threaten the newly formed symbiotic relationship between the wealthy Park family and the destitute Kim clan.');
                END
            ";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
