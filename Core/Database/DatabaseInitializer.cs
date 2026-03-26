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
            // 1. Ensure the databases exists on the server
            await EnsureDatabaseExistsAsync();

            // 2. Create the tables in the database
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
                        Author          NVARCHAR(256)   NOT NULL DEFAULT '',
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

                -- Seed Music Tracks (for Reel Editing feature)
                IF (SELECT COUNT(*) FROM MusicTrack) = 0
                BEGIN
                    INSERT INTO MusicTrack (TrackName, Author, AudioUrl, DurationSeconds)
                    VALUES
                    ('Epic Cinematic Theme', 'Hans Zimmer', 'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3', 180.5),
                    ('Upbeat Pop Track', 'Mark Ronson', 'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3', 150.0),
                    ('Dramatic Orchestral', 'John Williams', 'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3', 200.3),
                    ('Chill Lo-Fi Beats', 'Nujabes', 'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-4.mp3', 120.0),
                    ('Action Packed Rock', 'AC/DC', 'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-5.mp3', 165.7);
                END

                -- Seed Reels (for Reel Editing feature - UserId = 1)
                -- ThumbnailUrl uses the movie poster as the default first-frame thumbnail
                IF (SELECT COUNT(*) FROM Reel WHERE CreatorUserId = 1) = 0
                BEGIN
                    INSERT INTO Reel (MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, FeatureDurationSeconds, Source, CreatedAt)
                    VALUES
                    (1, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4', 
                     'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg',
                     'Inception - Dream Within a Dream', 
                     'Mind-bending scene from Inception where reality bends',
                     45.5, 'youtube', DATEADD(day, -10, SYSUTCDATETIME())),
                    
                    (1, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg',
                     'Inception - Rotating Hallway Fight',
                     'The iconic zero-gravity hallway fight sequence',
                     60.2, 'youtube', DATEADD(day, -9, SYSUTCDATETIME())),
                    
                    (2, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg',
                     'The Dark Knight - Joker Interrogation',
                     'Heath Ledger''s legendary Joker interrogation scene',
                     55.0, 'youtube', DATEADD(day, -8, SYSUTCDATETIME())),
                    
                    (2, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg',
                     'The Dark Knight - Batmobile Chase',
                     'Epic chase scene through Gotham streets',
                     70.8, 'youtube', DATEADD(day, -7, SYSUTCDATETIME())),
                    
                    (3, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerFun.mp4',
                     'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg',
                     'Interstellar - Docking Scene',
                     'The intense docking sequence with the spinning station',
                     90.3, 'youtube', DATEADD(day, -6, SYSUTCDATETIME())),
                    
                    (3, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyrides.mp4',
                     'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg',
                     'Interstellar - Tesseract Scene',
                     'Cooper in the 5th dimension tesseract',
                     80.0, 'youtube', DATEADD(day, -5, SYSUTCDATETIME())),
                    
                    (4, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerMeltdowns.mp4',
                     'https://m.media-amazon.com/images/M/MV5BNzQzOTk3NTAtNDQ2Ny00Njc2LTk3M2QtN2FjYTJjNzQzYzQwXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg',
                     'The Matrix - Bullet Time',
                     'The revolutionary bullet time effect scene',
                     35.5, 'youtube', DATEADD(day, -4, SYSUTCDATETIME())),
                    
                    (5, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4',
                     'https://m.media-amazon.com/images/M/MV5BYWZjMjk3ZTAtZGYzMC00ODQ0LWI2YTMtYjQ5NDU3N2NmZDIzXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg',
                     'Parasite - Basement Reveal',
                     'The shocking basement discovery scene',
                     50.0, 'youtube', DATEADD(day, -3, SYSUTCDATETIME())),
                    
                    (6, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/SubaruOutbackOnStreetAndDirt.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMjA2OTYxNTY2Nl5BMl5BanBnXkFtZTgwNzg4OTA5OTE@._V1_.jpg',
                     'La La Land - Highway Opening',
                     'The colorful highway opening dance number',
                     65.2, 'youtube', DATEADD(day, -2, SYSUTCDATETIME())),
                    
                    (7, 1, 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMjE4NDYxNTAxNV5BMl5BanBnXkFtZTgwNzM0NDM1MjE@._V1_.jpg',
                     'Whiplash - Final Performance',
                     'The intense final drum performance',
                     75.0, 'youtube', DATEADD(day, -1, SYSUTCDATETIME()));
                END
                -- ═══════════════════════════════════════════════════════════
                -- TODO (Madi — account switcher):
                -- When the team is aligned, create the User table and add these fields:
                --   UserId          INT PRIMARY KEY
                --   Username        NVARCHAR(100) NOT NULL
                --   FacebookAccount NVARCHAR(100) NULL
                --   IsLogged        BIT NOT NULL DEFAULT 0  (account added to the switcher)
                --   IsActive        BIT NOT NULL DEFAULT 0  (currently active session; only one row = 1)
                -- Seed User 1 with IsLogged=1, IsActive=1. Users 2–6 with IsLogged=0, IsActive=0.
                -- Then replace the hardcoded _demoAccounts, _activeUserId, and _loggedAccountIds
                -- in PersonalityMatchViewModel with DB calls to IPersonalityMatchRepository.
                -- ═══════════════════════════════════════════════════════════

                -- ═══════════════════════════════════════════════════════════
                -- Madi: Seed mock users 2–6 for personality matching demo
                -- Each user has different taste overlap with user 1
                -- ═══════════════════════════════════════════════════════════

                -- User 2 (Alice) — very similar to User 1 (high Sci-Fi/Action scores)
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 2)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (2, 1, 8.0, 1),   -- Inception          (User1: 8.5)
                    (2, 2, 9.2, 1),   -- The Dark Knight     (User1: 9.0)
                    (2, 3, 7.8, 1),   -- Interstellar        (User1: 7.5)
                    (2, 4, 8.5, 1),   -- The Matrix          (User1: 8.0)
                    (2, 5, 9.0, 1),   -- Parasite            (User1: 9.5)
                    (2, 6, 3.0, 1),   -- La La Land          (User1: 8.5)
                    (2, 7, 6.5, 1),   -- Whiplash            (User1: 7.0)
                    (2, 8, 9.0, 1);   -- Grand Budapest      (User1: 9.2)
                END

                -- User 3 (Bob) — moderate overlap, prefers dramas/musicals
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 3)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (3, 1, 5.0, 1),   -- Inception
                    (3, 2, 4.5, 1),   -- The Dark Knight
                    (3, 5, 8.5, 1),   -- Parasite
                    (3, 6, 9.5, 1),   -- La La Land
                    (3, 7, 9.0, 1),   -- Whiplash
                    (3, 8, 8.0, 1);   -- Grand Budapest
                END

                -- User 4 (Carol) — somewhat similar, likes Sci-Fi but differs on drama
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 4)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (4, 1, 9.0, 1),   -- Inception
                    (4, 3, 8.5, 1),   -- Interstellar
                    (4, 4, 9.0, 1),   -- The Matrix
                    (4, 6, 2.0, 1),   -- La La Land
                    (4, 7, 3.0, 1);   -- Whiplash
                END

                -- User 5 (Dave) — opposite taste, low scores on User 1's favourites
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 5)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (5, 1, 2.0, 1),   -- Inception
                    (5, 2, 3.0, 1),   -- The Dark Knight
                    (5, 3, 2.5, 1),   -- Interstellar
                    (5, 4, 1.5, 1),   -- The Matrix
                    (5, 5, 3.0, 1),   -- Parasite
                    (5, 6, 9.0, 1),   -- La La Land
                    (5, 7, 8.5, 1),   -- Whiplash
                    (5, 8, 2.0, 1);   -- Grand Budapest
                END

                -- User 6 (Eve) — partial overlap, only rated a few movies
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 6)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (6, 2, 8.8, 1),   -- The Dark Knight
                    (6, 5, 9.0, 1),   -- Parasite
                    (6, 8, 8.5, 1);   -- Grand Budapest
                END

                -- Seed UserProfile rows for users 2–6
                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 2)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (2, 42, 18000, 120.5, 150, 0.28);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 3)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (3, 15, 7200, 90.0, 80, 0.19);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 4)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (4, 68, 32000, 145.0, 220, 0.31);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 5)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (5, 8, 3600, 60.0, 60, 0.13);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 6)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (6, 25, 12000, 110.0, 109, 0.23);

               
                -- Madi: Extended demo users 7–13 for account-switcher testing
                --
                -- Users 7, 8, 10, 11, 12, 13 all share movies with Alice (User 2),
                -- giving her 11 total matches — enough to test the top-10 cap.
                --
                -- User 9 (Sam Taylor) has NO preferences intentionally — when logged
                -- in as Sam the app shows the No match screen and the Maybe you like fallback.
                

                -- User 7 (James Park) — sci-fi/action fan, overlaps with Alice on movies 1,2,4,5
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 7)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (7, 1, 8.2, 1),   -- Inception
                    (7, 2, 9.0, 1),   -- The Dark Knight
                    (7, 4, 8.8, 1),   -- The Matrix
                    (7, 5, 8.5, 1);   -- Parasite
                END

                -- User 8 (Luna Kim) — thriller/drama fan, overlaps with Alice on movies 2,3,8
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 8)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (8, 2, 9.5, 1),   -- The Dark Knight
                    (8, 3, 8.0, 1),   -- Interstellar
                    (8, 8, 9.2, 1);   -- The Grand Budapest Hotel
                END

                -- User 9 (Sam Taylor) — NO preferences intentionally.
                -- Triggers the No match screen and Maybe you like fallback. Do NOT add rows here.

                -- Add movies 9 and 10 for the Alice vs Alex match-count differentiation.
                -- Alex (User 1) only rates movies 1-8. Alice (User 2) will also rate 9 and 10.
                -- Users 10-13 are migrated to ONLY rate movies 9 and 10.
                -- Result: Alex gets ~7 matches, Alice gets 11 (tests the top-10 cap).
                IF NOT EXISTS (SELECT 1 FROM Movie WHERE Title = 'The Godfather')
                    INSERT INTO Movie (Title, PosterUrl, PrimaryGenre, ReleaseYear)
                    VALUES ('The Godfather', 'https://m.media-amazon.com/images/M/MV5BM2MyNjYxNmUtYTAwNi00MTYxLWJmNWYtYzZlODY3ZTk3OTFlXkEyXkFqcGdeQXVyNzkwMjQ5NzM@._V1_.jpg', 'Crime', 1972);

                IF NOT EXISTS (SELECT 1 FROM Movie WHERE Title = 'Forrest Gump')
                    INSERT INTO Movie (Title, PosterUrl, PrimaryGenre, ReleaseYear)
                    VALUES ('Forrest Gump', 'https://m.media-amazon.com/images/M/MV5BNWIwODRlZTUtY2U3ZS00Yzg1LWJhNzYtMmZiYmEyNmU1NjMzXkEyXkFqcGdeQXVyMTQxNzMzNDI@._V1_.jpg', 'Drama', 1994);

                -- Alice (User 2) also rates movies 9 and 10 so she overlaps with Users 10-13.
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 2 AND MovieId = (SELECT MovieId FROM Movie WHERE Title = 'The Godfather'))
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES (2, (SELECT MovieId FROM Movie WHERE Title = 'The Godfather'), 8.8, 1);

                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 2 AND MovieId = (SELECT MovieId FROM Movie WHERE Title = 'Forrest Gump'))
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES (2, (SELECT MovieId FROM Movie WHERE Title = 'Forrest Gump'), 9.1, 1);

                -- Migrate Users 10-13 to only rate movies 9-10.
                -- This DELETE is idempotent: if already migrated, nothing to remove.
                DELETE FROM UserMoviePreference WHERE UserId IN (10,11,12,13) AND MovieId BETWEEN 1 AND 8;

                -- User 10 (Nina Reeves) — The Godfather / Forrest Gump fan
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 10)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (10, (SELECT MovieId FROM Movie WHERE Title = 'The Godfather'), 8.5, 1),
                    (10, (SELECT MovieId FROM Movie WHERE Title = 'Forrest Gump'),  7.9, 1);
                END

                -- User 11 (Tom Walsh) — The Godfather / Forrest Gump fan
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 11)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (11, (SELECT MovieId FROM Movie WHERE Title = 'The Godfather'), 9.0, 1),
                    (11, (SELECT MovieId FROM Movie WHERE Title = 'Forrest Gump'),  8.3, 1);
                END

                -- User 12 (Zara Foster) — The Godfather / Forrest Gump fan
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 12)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (12, (SELECT MovieId FROM Movie WHERE Title = 'The Godfather'), 7.8, 1),
                    (12, (SELECT MovieId FROM Movie WHERE Title = 'Forrest Gump'),  9.2, 1);
                END

                -- User 13 (Kai Rivera) — The Godfather / Forrest Gump fan
                IF NOT EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = 13)
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, ChangeFromPreviousValue)
                    VALUES
                    (13, (SELECT MovieId FROM Movie WHERE Title = 'The Godfather'), 8.6, 1),
                    (13, (SELECT MovieId FROM Movie WHERE Title = 'Forrest Gump'),  8.0, 1);
                END

                -- UserProfile rows for users 7–13
                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 7)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (7, 35, 15000, 100.0, 130, 0.27);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 8)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (8, 55, 24000, 130.5, 180, 0.31);

                -- User 9 (Sam Taylor) — new account, no activity yet
                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 9)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (9, 0, 0, 0.0, 0, 0.0);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 10)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (10, 20, 9000, 80.0, 90, 0.22);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 11)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (11, 70, 35000, 160.0, 240, 0.29);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 12)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (12, 45, 20000, 115.0, 165, 0.27);

                IF NOT EXISTS (SELECT 1 FROM UserProfile WHERE UserId = 13)
                    INSERT INTO UserProfile (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio)
                    VALUES (13, 30, 13000, 95.0, 120, 0.25);
            ";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task EnsureDatabaseExistsAsync()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'andrei')
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
