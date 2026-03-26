USE [MeioAiDb];

-- Movie
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

-- ScrapeJob
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

-- ScrapeJobLog
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

-- MusicTrack
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

-- Reel
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

-- UserMoviePreference
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

-- UserProfile
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

    INSERT INTO UserProfile (UserId) VALUES (1);
END

-- UserReelInteraction
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

-- Seed Movies
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
    INSERT INTO MusicTrack (TrackName, AudioUrl, DurationSeconds)
    VALUES
    ('Epic Cinematic Theme', 'https://example.com/music/epic-theme.mp3', 180.5),
    ('Upbeat Pop Track', 'https://example.com/music/upbeat-pop.mp3', 150.0),
    ('Dramatic Orchestral', 'https://example.com/music/dramatic-orchestral.mp3', 200.3),
    ('Chill Lo-Fi Beats', 'https://example.com/music/lofi-beats.mp3', 120.0),
    ('Action Packed Rock', 'https://example.com/music/action-rock.mp3', 165.7);
END

-- Seed Reels (for Reel Editing feature - UserId = 1)
-- ThumbnailUrl uses the movie poster as the default first-frame thumbnail
IF (SELECT COUNT(*) FROM Reel WHERE CreatorUserId = 1) = 0
BEGIN
    INSERT INTO Reel (MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, FeatureDurationSeconds, Source, CreatedAt)
    VALUES
    (1, 1, 'https://example.com/videos/inception-dream-scene.mp4', 
     'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg',
     'Inception - Dream Within a Dream', 
     'Mind-bending scene from Inception where reality bends',
     45.5, 'youtube', DATEADD(day, -10, SYSUTCDATETIME())),
    
    (1, 1, 'https://example.com/videos/inception-hallway.mp4',
     'https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg',
     'Inception - Rotating Hallway Fight',
     'The iconic zero-gravity hallway fight sequence',
     60.2, 'youtube', DATEADD(day, -9, SYSUTCDATETIME())),
    
    (2, 1, 'https://example.com/videos/dark-knight-joker.mp4',
     'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg',
     'The Dark Knight - Joker Interrogation',
     'Heath Ledger''s legendary Joker interrogation scene',
     55.0, 'youtube', DATEADD(day, -8, SYSUTCDATETIME())),
    
    (2, 1, 'https://example.com/videos/dark-knight-chase.mp4',
     'https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg',
     'The Dark Knight - Batmobile Chase',
     'Epic chase scene through Gotham streets',
     70.8, 'youtube', DATEADD(day, -7, SYSUTCDATETIME())),
    
    (3, 1, 'https://example.com/videos/interstellar-docking.mp4',
     'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg',
     'Interstellar - Docking Scene',
     'The intense docking sequence with the spinning station',
     90.3, 'youtube', DATEADD(day, -6, SYSUTCDATETIME())),
    
    (3, 1, 'https://example.com/videos/interstellar-tesseract.mp4',
     'https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg',
     'Interstellar - Tesseract Scene',
     'Cooper in the 5th dimension tesseract',
     80.0, 'youtube', DATEADD(day, -5, SYSUTCDATETIME())),
    
    (4, 1, 'https://example.com/videos/matrix-bullet-time.mp4',
     'https://m.media-amazon.com/images/M/MV5BNzQzOTk3NTAtNDQ2Ny00Njc2LTk3M2QtN2FjYTJjNzQzYzQwXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg',
     'The Matrix - Bullet Time',
     'The revolutionary bullet time effect scene',
     35.5, 'youtube', DATEADD(day, -4, SYSUTCDATETIME())),
    
    (5, 1, 'https://example.com/videos/parasite-basement.mp4',
     'https://m.media-amazon.com/images/M/MV5BYWZjMjk3ZTAtZGYzMC00ODQ0LWI2YTMtYjQ5NDU3N2NmZDIzXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_.jpg',
     'Parasite - Basement Reveal',
     'The shocking basement discovery scene',
     50.0, 'youtube', DATEADD(day, -3, SYSUTCDATETIME())),
    
    (6, 1, 'https://example.com/videos/lalaland-opening.mp4',
     'https://m.media-amazon.com/images/M/MV5BMjA2OTYxNTY2Nl5BMl5BanBnXkFtZTgwNzg4OTA5OTE@._V1_.jpg',
     'La La Land - Highway Opening',
     'The colorful highway opening dance number',
     65.2, 'youtube', DATEADD(day, -2, SYSUTCDATETIME())),
    
    (7, 1, 'https://example.com/videos/whiplash-finale.mp4',
     'https://m.media-amazon.com/images/M/MV5BMjE4NDYxNTAxNV5BMl5BanBnXkFtZTgwNzM0NDM1MjE@._V1_.jpg',
     'Whiplash - Final Performance',
     'The intense final drum performance',
     75.0, 'youtube', DATEADD(day, -1, SYSUTCDATETIME()));
END
