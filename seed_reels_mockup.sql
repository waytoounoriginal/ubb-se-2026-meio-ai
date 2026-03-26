USE [MeioAiDb];
GO

-- Insert mockup music tracks if they don't exist
IF NOT EXISTS (SELECT * FROM MusicTrack WHERE TrackName = 'Epic Cinematic Theme')
BEGIN
    INSERT INTO MusicTrack (TrackName, AudioUrl, DurationSeconds)
    VALUES
    ('Epic Cinematic Theme', 'https://example.com/music/epic-theme.mp3', 180.5),
    ('Upbeat Pop Track', 'https://example.com/music/upbeat-pop.mp3', 150.0),
    ('Dramatic Orchestral', 'https://example.com/music/dramatic-orchestral.mp3', 200.3),
    ('Chill Lo-Fi Beats', 'https://example.com/music/lofi-beats.mp3', 120.0),
    ('Action Packed Rock', 'https://example.com/music/action-rock.mp3', 165.7);
    
    PRINT 'Inserted 5 music tracks';
END
ELSE
BEGIN
    PRINT 'Music tracks already exist';
END
GO

-- Insert mockup reels for UserId = 1
-- First, ensure we have movies (they should exist from init_db.sql)
IF EXISTS (SELECT * FROM Movie WHERE MovieId BETWEEN 1 AND 8)
BEGIN
    -- Delete existing reels for UserId = 1 to avoid duplicates
    DELETE FROM UserReelInteraction WHERE ReelId IN (SELECT ReelId FROM Reel WHERE CreatorUserId = 1);
    DELETE FROM Reel WHERE CreatorUserId = 1;
    
    INSERT INTO Reel (MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, FeatureDurationSeconds, Source, Genre, CreatedAt)
    VALUES
    -- Inception reels
    (1, 1, 'https://example.com/videos/inception-dream-scene.mp4', 
     'https://example.com/thumbs/inception-dream.jpg',
     'Inception - Dream Within a Dream', 
     'Mind-bending scene from Inception where reality bends',
     45.5, 'youtube', 'Sci-Fi', DATEADD(day, -10, SYSUTCDATETIME())),
    
    (1, 1, 'https://example.com/videos/inception-hallway.mp4',
     'https://example.com/thumbs/inception-hallway.jpg',
     'Inception - Rotating Hallway Fight',
     'The iconic zero-gravity hallway fight sequence',
     60.2, 'youtube', 'Sci-Fi', DATEADD(day, -9, SYSUTCDATETIME())),
    
    -- The Dark Knight reels
    (2, 1, 'https://example.com/videos/dark-knight-joker.mp4',
     'https://example.com/thumbs/dark-knight-joker.jpg',
     'The Dark Knight - Joker Interrogation',
     'Heath Ledger''s legendary Joker interrogation scene',
     55.0, 'youtube', 'Action', DATEADD(day, -8, SYSUTCDATETIME())),
    
    (2, 1, 'https://example.com/videos/dark-knight-chase.mp4',
     'https://example.com/thumbs/dark-knight-chase.jpg',
     'The Dark Knight - Batmobile Chase',
     'Epic chase scene through Gotham streets',
     70.8, 'youtube', 'Action', DATEADD(day, -7, SYSUTCDATETIME())),
    
    -- Interstellar reels
    (3, 1, 'https://example.com/videos/interstellar-docking.mp4',
     'https://example.com/thumbs/interstellar-docking.jpg',
     'Interstellar - Docking Scene',
     'The intense docking sequence with the spinning station',
     90.3, 'youtube', 'Adventure', DATEADD(day, -6, SYSUTCDATETIME())),
    
    (3, 1, 'https://example.com/videos/interstellar-tesseract.mp4',
     'https://example.com/thumbs/interstellar-tesseract.jpg',
     'Interstellar - Tesseract Scene',
     'Cooper in the 5th dimension tesseract',
     80.0, 'youtube', 'Adventure', DATEADD(day, -5, SYSUTCDATETIME())),
    
    -- The Matrix reels
    (4, 1, 'https://example.com/videos/matrix-bullet-time.mp4',
     'https://example.com/thumbs/matrix-bullet.jpg',
     'The Matrix - Bullet Time',
     'The revolutionary bullet time effect scene',
     35.5, 'youtube', 'Sci-Fi', DATEADD(day, -4, SYSUTCDATETIME())),
    
    -- Parasite reels
    (5, 1, 'https://example.com/videos/parasite-basement.mp4',
     'https://example.com/thumbs/parasite-basement.jpg',
     'Parasite - Basement Reveal',
     'The shocking basement discovery scene',
     50.0, 'youtube', 'Thriller', DATEADD(day, -3, SYSUTCDATETIME())),
    
    -- La La Land reels
    (6, 1, 'https://example.com/videos/lalaland-opening.mp4',
     'https://example.com/thumbs/lalaland-opening.jpg',
     'La La Land - Highway Opening',
     'The colorful highway opening dance number',
     65.2, 'youtube', 'Musical', DATEADD(day, -2, SYSUTCDATETIME())),
    
    -- Whiplash reels
    (7, 1, 'https://example.com/videos/whiplash-finale.mp4',
     'https://example.com/thumbs/whiplash-finale.jpg',
     'Whiplash - Final Performance',
     'The intense final drum performance',
     75.0, 'youtube', 'Drama', DATEADD(day, -1, SYSUTCDATETIME()));
    
    PRINT 'Inserted 10 mockup reels for UserId = 1';
END
ELSE
BEGIN
    PRINT 'ERROR: Movies not found. Please run init_db.sql first.';
END
GO

-- Verify the data
SELECT COUNT(*) AS TotalReels FROM Reel WHERE CreatorUserId = 1;
SELECT COUNT(*) AS TotalMusicTracks FROM MusicTrack;
GO
