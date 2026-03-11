# Class Diagram — Andrei (Trailer Scraping)

```mermaid
classDiagram
    direction TB

    %% ── Models ──
    class ReelModel {
        +int ReelId
        +int? MovieId
        +int? CreatorUserId
        +string VideoUrl
        +string ThumbnailUrl
        +string Title
        +string Caption
        +int DurationSeconds
        +string Source
        +DateTime CreatedAt
    }

    %% ── Services ──
    class IWebScraperService {
        <<interface>>
        +ScrapeVideosForMovieAsync(string movieTitle) List~RawVideo~
    }

    class WebScraperBackgroundService {
        +ExecuteAsync() void
    }

    class VideoIngestionService {
        +IngestTrailersAsync() void
    }

    class ReelRepository {
        +BulkInsertScrapedReelsAsync(List~ReelModel~) void
        +GetReelsByMovieAndSourceAsync(int movieId, string source) List~ReelModel~
    }

    %% ── ViewModel ──
    class MovieTrailerPlayerViewModel {
        +ObservableCollection~ReelModel~ Trailers
        +string SelectedVideoUrl
        +ICommand LoadTrailersCommand
    }

    %% ── View ──
    class MovieTrailerPlayerView {
        <<View>>
    }

    %% ── Relationships ──
    MovieTrailerPlayerView --> MovieTrailerPlayerViewModel : DataContext
    MovieTrailerPlayerViewModel --> ReelRepository : fetches from
    WebScraperBackgroundService --> VideoIngestionService : triggers
    VideoIngestionService --> IWebScraperService : uses
    VideoIngestionService --> ReelRepository : bulk inserts
    ReelRepository --> ReelModel : persists
```
