using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{

    public class DatabaseInitializer
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        private const string DatabaseName = "MeioAiDb";


        public DatabaseInitializer(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateTablesIfNotExistAsync()
        {

            await EnsureDatabaseExistsAsync();

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
                        Synopsis        NVARCHAR(MAX)   NULL,
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
                -- Seed Movies for Demo
-- Seed Movies for Demo
IF (SELECT COUNT(*) FROM Movie) = 0
BEGIN
    INSERT INTO Movie (Title, PosterUrl, PrimaryGenre, ReleaseYear)
    VALUES
    ('Inception', 'https://media.themoviedb.org/t/p/w600_and_h900_face/vr6ouTojPp0zlSpJvCbODPp19nd.jpg', 'Sci-Fi', 2010),
    ('The Dark Knight', 'https://media.themoviedb.org/t/p/w600_and_h900_face/a1UL3FTJDgQikYIebnMDhTPFVfm.jpg', 'Action', 2008),
    ('Interstellar', 'https://media.themoviedb.org/t/p/w600_and_h900_face/wbnrYkn59cdFuu0LNAZ2BWh2i37.jpg', 'Adventure', 2014),
    ('The Matrix1', 'https://media.themoviedb.org/t/p/w600_and_h900_face/p96dm7sCMn4VYAStA6siNz30G1r.jpg', 'Sci-Fi', 1999),
    ('Parasite', 'https://media.themoviedb.org/t/p/w600_and_h900_face/7IiTTgloJzvGI1TAYymCfbfl3vT.jpg', 'Thriller', 2019),
    ('La La Land', 'https://media.themoviedb.org/t/p/w600_and_h900_face/uDO8zWDhfWwoFdKS4fzkUJt0Rf0.jpg', 'Musical', 2016),
    ('Whiplash', 'https://media.themoviedb.org/t/p/w600_and_h900_face/7fn624j5lj3xTme2SgiLCeuedmO.jpg', 'Drama', 2014),
    ('The Grand Budapest Hotel', 'https://media.themoviedb.org/t/p/w600_and_h900_face/eWdyYQreja6JGCzqHWXpWHDrrPo.jpg', 'Comedy', 2014);
END

IF (SELECT COUNT(*) FROM Movie) < 38
BEGIN
    INSERT INTO Movie (Title, PosterUrl, PrimaryGenre, ReleaseYear)
    VALUES
    ('Avatar', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/gKY6q7SjCkAU6FqvqWybDYgUKIF.jpg', 'Sci-Fi', 2009),
    ('Titanic', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/9xjZS2rlVxm8SFx8kPC3aIGCOYQ.jpg', 'Romance', 1997),
    ('Gladiator', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/yafsp1whNDGqmn6vqHdgg0PbZA5.jpg', 'Action', 2000),
    ('The Shawshank Redemption', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/tsY4m4IH8MZly4kvxZszbommLKj.jpg', 'Drama', 1994),
    ('Forrest Gump', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/n7NEvy20kMLD0X6lrzoaSGXnr3I.jpg', 'Drama', 1994),
    ('The Godfather', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/z4lzwl3Gff5IOWKGiYY7gUFYXUb.jpg', 'Crime', 1972),
    ('Pulp Fiction', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/vQWk5YBFWF4bZaofAbv0tShwBvQ.jpg', 'Crime', 1994),
    ('Fight Club', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg', 'Drama', 1999),
    ('The Social Network', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/jD2LsdNu9zWwXjjlbxO0Iibpefz.jpg', 'Drama', 2010),
    ('Joker', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/6zIyfSojJxoCj6mL3TZPFZBByfP.jpg', 'Thriller', 2019),
    ('Avengers: Endgame', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/5hRf3rT7T5QNTmfbv00yXzpGvXw.jpg', 'Action', 2019),
    ('Iron Man', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/frW3QzyDondJdnd6ydzT8ekKHAw.jpg', 'Action', 2008),
    ('Toy Story', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/RyAWIbSmt4xC859hyQ43wUumM9.jpg', 'Animation', 1995),
    ('Finding Nemo', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/7YrKAe5GBg2f2WnwMrX9IdLFqCq.jpg', 'Animation', 2003),
    ('Up', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/f6ytrGR8IJ0qizc2gI0HSJN6OaU.jpg', 'Animation', 2009),
    ('The Lion King', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/Pa9euUkkwUqHJoMh6eIj2XVeV4.jpg', 'Animation', 1994),
    ('Frozen', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/itAKcobTYGpYT8Phwjd8c9hleTo.jpg', 'Animation', 2013),
    ('The Conjuring', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/A09nKeXAa0FlOr6Y6EVnvAINKQ2.jpg', 'Horror', 2013),
    ('Get Out', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/yY33WCqDYUHdT6LJWsJUekSM4E.jpg', 'Horror', 2017),
    ('A Quiet Place', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/yelqLnp4My4WWqD647wwwpw552P.jpg', 'Horror', 2018),
    ('Mad Max: Fury Road', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/89BIhHMvGAoxQkniNFB8ENrfzxk.jpg', 'Action', 2015),
    ('John Wick', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/rP7X52bxOEBc09h5qzHJnHXxE3C.jpg', 'Action', 2014),
    ('The Wolf of Wall Street', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/bm6TEyJMpvzTeBOP1V43UfRrrfg.jpg', 'Biography', 2013),
    ('Django Unchained', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/w1P4HHXJT6CRbcZ2x6Yq2sjWsdF.jpg', 'Western', 2012),
    ('The Revenant', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/6Z6bzPW2K6i5nD2NO8wRZRKQJ5y.jpg', 'Adventure', 2015),
    ('Blade Runner 2049', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/9pz5bPDufSrJd8yTNjp0apTAVf8.jpg', 'Sci-Fi', 2017),
    ('Her', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/kBK4UlVOIx6NZyD0QkHlFi9XnAw.jpg', 'Romance', 2013),
    ('The Prestige', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/cNpg2TjWtsut8QUBqezkbHXQFgb.jpg', 'Drama', 2006),
    ('Memento', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/fKTPH2WvH8nHTXeBYBVhawtRqtR.jpg', 'Thriller', 2000),
    ('Shutter Island', 'https://image.tmdb.org/t/p/w600_and_h900_bestv2/qfpFopx4AHd3oTOkj0VGG50AS39.jpg', 'Thriller', 2010);
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
                                        (1, 1, 'https://samplelib.com/lib/preview/mp4/sample-10s.mp4', 
                     'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg',
                     'Inception - Dream Within a Dream', 
                     'Mind-bending scene from Inception where reality bends',
                     45.5, 'youtube', DATEADD(day, -10, SYSUTCDATETIME())),
                    
                                        (1, 1, 'https://samplelib.com/lib/preview/mp4/sample-15s.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg',
                     'Inception - Rotating Hallway Fight',
                     'The iconic zero-gravity hallway fight sequence',
                     60.2, 'youtube', DATEADD(day, -9, SYSUTCDATETIME())),
                    
                                        (2, 1, 'https://samplelib.com/lib/preview/mp4/sample-20s.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg',
                     'The Dark Knight - Joker Interrogation',
                     'Heath Ledger''s legendary Joker interrogation scene',
                     55.0, 'youtube', DATEADD(day, -8, SYSUTCDATETIME())),
                    
                                        (2, 1, 'https://samplelib.com/lib/preview/mp4/sample-30s.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg',
                     'The Dark Knight - Batmobile Chase',
                     'Epic chase scene through Gotham streets',
                     70.8, 'youtube', DATEADD(day, -7, SYSUTCDATETIME())),
                    
                                        (3, 1, 'https://samplelib.com/lib/preview/mp4/sample-5s.mp4',
                     'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg',
                     'Interstellar - Docking Scene',
                     'The intense docking sequence with the spinning station',
                     90.3, 'youtube', DATEADD(day, -6, SYSUTCDATETIME())),
                    
                                        (3, 1, 'https://filesamples.com/samples/video/mp4/sample_640x360.mp4',
                     'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg',
                     'Interstellar - Tesseract Scene',
                     'Cooper in the 5th dimension tesseract',
                     80.0, 'youtube', DATEADD(day, -5, SYSUTCDATETIME())),
                    
                                        (4, 1, 'https://filesamples.com/samples/video/mp4/sample_960x400_ocean_with_audio.mp4',
                     'https://m.media-amazon.com/images/M/MV5BNzQzOTk3NTAtNDQ2Ny00Njc2LTk3M2QtN2FjYTJjNzQzYzQwXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg',
                     'The Matrix - Bullet Time',
                     'The revolutionary bullet time effect scene',
                     35.5, 'youtube', DATEADD(day, -4, SYSUTCDATETIME())),
                    
                                        (5, 1, 'https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/360/Big_Buck_Bunny_360_10s_1MB.mp4',
                     'https://m.media-amazon.com/images/M/MV5BYWZjMjk3ZTAtZGYzMC00ODQ0LWI2YTMtYjQ5NDU3N2NmZDIzXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg',
                     'Parasite - Basement Reveal',
                     'The shocking basement discovery scene',
                     50.0, 'youtube', DATEADD(day, -3, SYSUTCDATETIME())),
                    
                                        (6, 1, 'https://archive.org/download/ElephantsDream/ed_1024_512kb.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMjA2OTYxNTY2Nl5BMl5BanBnXkFtZTgwNzg4OTA5OTE@._V1_.jpg',
                     'La La Land - Highway Opening',
                     'The colorful highway opening dance number',
                     65.2, 'youtube', DATEADD(day, -2, SYSUTCDATETIME())),
                    
                                        (7, 1, 'https://media.w3.org/2010/05/sintel/trailer.mp4',
                     'https://m.media-amazon.com/images/M/MV5BMjE4NDYxNTAxNV5BMl5BanBnXkFtZTgwNzM0NDM1MjE@._V1_.jpg',
                     'Whiplash - Final Performance',
                     'The intense final drum performance',
                     75.0, 'youtube', DATEADD(day, -1, SYSUTCDATETIME()));
                END

                                -- Repair existing seeded reels that still point to inaccessible
                                -- commondatastorage sample URLs.
                                UPDATE Reel
                                SET VideoUrl = CASE Title
                                        WHEN 'Inception - Dream Within a Dream' THEN 'https://samplelib.com/lib/preview/mp4/sample-10s.mp4'
                                        WHEN 'Inception - Rotating Hallway Fight' THEN 'https://samplelib.com/lib/preview/mp4/sample-15s.mp4'
                                        WHEN 'The Dark Knight - Joker Interrogation' THEN 'https://samplelib.com/lib/preview/mp4/sample-20s.mp4'
                                        WHEN 'The Dark Knight - Batmobile Chase' THEN 'https://samplelib.com/lib/preview/mp4/sample-30s.mp4'
                                        WHEN 'Interstellar - Docking Scene' THEN 'https://samplelib.com/lib/preview/mp4/sample-5s.mp4'
                                        WHEN 'Interstellar - Tesseract Scene' THEN 'https://filesamples.com/samples/video/mp4/sample_640x360.mp4'
                                        WHEN 'The Matrix - Bullet Time' THEN 'https://filesamples.com/samples/video/mp4/sample_960x400_ocean_with_audio.mp4'
                                        WHEN 'Parasite - Basement Reveal' THEN 'https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/360/Big_Buck_Bunny_360_10s_1MB.mp4'
                                        WHEN 'La La Land - Highway Opening' THEN 'https://archive.org/download/ElephantsDream/ed_1024_512kb.mp4'
                                        WHEN 'Whiplash - Final Performance' THEN 'https://media.w3.org/2010/05/sintel/trailer.mp4'
                                        ELSE VideoUrl
                                END
                                WHERE CreatorUserId = 1
                                    AND VideoUrl LIKE 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/%';
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


            await using SqlConnection masterConnection = await _connectionFactory.CreateMasterConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, masterConnection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
