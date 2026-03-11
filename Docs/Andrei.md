### 1. Formal Requirements
*   **Requirement 1:** The system must automatically scrape external web sources to find trailer videos and related content for movies in the external Movie table.
*   **Requirement 2:** The system must store the scraped video data as new rows in the shared `Reel` table with `Source = 'scraped'`, `CreatorUserId = NULL`, and the appropriate `MovieId` foreign key.
*   **Requirement 3:** The system must execute the web scraping and database insertion processes autonomously as a background service, without requiring manual user interaction.
*   **Owner:** Andrei
*   **Cross-Team Dependencies:**
    *   **External Group:** Depends on the other group's `Movie` table to look up movie titles for scraping.
    *   **Alex:** Alex owns the `Reel` table schema — Andrei writes scraped trailers to the same table with `Source = 'scraped'`.
    *   **Tudor:** Scraped reels will appear in Tudor's reels feed — coordinate on `ReelModel` structure.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Automated System (Scraper)
    *   *Use Cases:* `Scrape Movie Trailer Content`, `Store Scraped Trailers as Reels`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It writes to the shared `Reel` table.)*
    *   **Shared Table: `Reel`** — New rows created with `Source = 'scraped'`, `CreatorUserId = NULL`, `MovieId = matched movie`, `VideoUrl = scraped URL`, `Title = scraped title`.
*   **Class Diagram (MVVM) Additions:**
    *   *(Note: The scraper operates on the backend, but the app will display scraped trailers via the existing Reels feed.)*
    *   *Models:* `ReelModel` (shared — scraped trailers use the same model as uploaded reels)
    *   *Views:* `MovieTrailerPlayerView` (optional dedicated screen where users can browse trailers for a specific movie)
    *   *ViewModels:* `MovieTrailerPlayerViewModel` (fetches `Reel` rows filtered by `MovieId` and `Source = 'scraped'`)
    *   *Utils/Services:* `WebScraperBackgroundService`, `VideoIngestionService`

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Create `ReelModel` Data Class
    *   **Description:** Define the Model class mirroring the shared `Reel` table: `ReelId`, `MovieId`, `CreatorUserId`, `VideoUrl`, `ThumbnailUrl`, `Title`, `Caption`, `DurationSeconds`, `Source`, `CreatedAt`. This is the same shared model used by all reel features. Max 30 mins effort.
*   **Task:** Implement Scraped Reel Bulk Insert Method
    *   **Description:** Create a repository method that accepts a list of `ReelModel` objects (with `Source = 'scraped'`) and bulk inserts them into the shared `Reel` table. Avoid duplicates by checking existing `VideoUrl` values. Max 30 mins effort.
*   **Task:** Implement Reel-by-Movie Query Method
    *   **Description:** Create a repository method to query all `Reel` rows matching a specific `MovieId` and `Source = 'scraped'` for display in the trailer player screen. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Define `IWebScraperService` Interface
    *   **Description:** Create the interface with method: `ScrapeVideosForMovieAsync(string movieTitle)` returning a list of raw video URLs and titles. Max 30 mins effort.
*   **Task:** Implement Network HTTP Trigger for Scraper
    *   **Description:** Write the HTTP client code inside the scraper service that queries the target search website using a movie's name. Max 30 mins effort.
*   **Task:** Implement HTML/DOM Parsing Logic
    *   **Description:** Parse the raw HTML response. Extract target video/iframe source URLs accurately. Map each to a potential reel entry. Max 30 mins effort.
*   **Task:** Develop `VideoIngestionService` Coordinator
    *   **Description:** Create a coordinator class that finds movies without scraped trailers, calls the Scraper service, maps found URLs to `ReelModel` objects (with `Source = 'scraped'`), and sends them to the bulk insert repository. Max 30 mins effort.
*   **Task:** Create `MovieTrailerPlayerViewModel`
    *   **Description:** Build the ViewModel that accepts a `MovieId`, fetches scraped `Reel` rows from the repository filtered by `Source = 'scraped'`, and populates an ObservableCollection. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Scaffold `MovieTrailerPlayerView` Layout
    *   **Description:** Create the skeleton UI. Include a placeholder for the embedded video player and a scrollable list for "Other Trailers". Max 30 mins effort.
*   **Task:** Bind UI Video Player to ViewModel Data
    *   **Description:** Set up data binding so the video player's `Source` reflects the `SelectedVideoUrl` from the ViewModel. Max 30 mins effort.
*   **Task:** Implement "No Trailers Found" Empty State
    *   **Description:** Add a responsive text block displaying "No trailers found for this movie", conditionally visible when the ViewModel's list is empty. Max 30 mins effort.
