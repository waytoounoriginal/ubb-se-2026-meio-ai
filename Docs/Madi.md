### 1. Formal Requirements
*   **Requirement 1:** The system must allow an authenticated user to request personality-based matching with other users.
*   **Requirement 2:** The system must retrieve all other users' `UserMoviePreference` ranked lists from the database.
*   **Requirement 3:** The system must calculate a compatibility score between the current user's preference list and each other user's preference list by measuring overlap of top-scored movies.
*   **Requirement 4:** The system must sort and display a top 10 list of the highest-matching users to the current user.
*   **Requirement 5:** The system must allow the user to select and view the engagement details of any matched user from the top 10 list (reading from the matched user's `UserProfile`).
*   **Owner:** Madi
*   **Cross-Team Dependencies:**
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table and the shared `UserMoviePreferenceModel`. Madi reads the scores Bogdan (and others) write to compute preference overlap for matching.
    *   **Tudor:** Tudor owns the `UserProfile` table schema — Madi reads from it to display matched users' engagement stats. Tudor also writes to `UserMoviePreference` (reel likes), which affects matching results.
    *   **Gabi:** Gabi's tournament also writes to `UserMoviePreference`, which affects matching results.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Request Personality Matches`, `View Top 10 Matches`, `View Matched User Details`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It reads from the shared `UserMoviePreference` and `UserProfile` tables.)*
    *   **Shared Table: `UserMoviePreference`** (UserId, MovieId, Score, LastModified) — The source for computing preference overlap between users.
    *   **Shared Table: `UserProfile`** (UserId, TotalLikes, TotalWatchTimeSec, etc.) — Read for displaying matched user engagement details.
    *   *(Note: `MatchResult` is an in-memory DTO, NOT persisted to the database.)*
*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `UserMoviePreferenceModel` (shared), `UserProfileModel` (shared), `MatchResult` (in-memory DTO: User + MatchScore)
    *   *Views:* `MatchListView` (screen showing top 10 matches), `MatchedUserDetailView` (screen showing selected user's traits/stats)
    *   *ViewModels:* `MatchListViewModel` (controls the top 10 list logic), `MatchedUserDetailViewModel` (displays a specific matched user's info)
    *   *Utils/Services:* `PersonalityMatchingService` (contains the algorithm to compute preference overlap and compatibility score)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Use Shared `UserMoviePreferenceModel` Data Class
    *   **Description:** Use the `UserMoviePreferenceModel` class created by **Bogdan**. It mirrors the shared `UserMoviePreference` table. Max 0 mins effort.
*   **Task:** Create `UserProfileModel` Data Class
    *   **Description:** Define the Model class mirroring the shared `UserProfile` table: `UserProfileId`, `UserId`, `TotalLikes`, `TotalWatchTimeSec`, `AvgWatchTimeSec`, `TotalClipsViewed`, `LikeToViewRatio`, `LastUpdated`. Max 30 mins effort.
*   **Task:** Define `MatchResult` DTO
    *   **Description:** Create a non-persistent `MatchResult` model class containing: `MatchedUserId`, `MatchedUsername`, `MatchScore` (float), and a summary of their top preferences. This is computed in-memory, never stored. Max 30 mins effort.
*   **Task:** Create All-Users Preference Retrieval Method
    *   **Description:** Write the repository method to fetch `UserMoviePreference` rows for all users (excluding the current user), grouped by UserId. Return as a dictionary/map of userId → list of (MovieId, Score). Max 30 mins effort.
*   **Task:** Implement User Profile Read Method
    *   **Description:** Write the repository method to fetch a single `UserProfile` row by UserId, for displaying matched user engagement stats on the detail screen. Max 30 mins effort.
*   **Task:** Write Unit Tests for Preference Retrieval
    *   **Description:** Create unit tests using a mocked repository to verify the method returns grouped preferences and excludes the querying user's data. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Define `IPersonalityMatchingService` Interface
    *   **Description:** Create the interface with method: `GetTopMatchesAsync(userId, limit = 10)` returning `List<MatchResult>`. Max 30 mins effort.
*   **Task:** Implement Matching Algorithm — Preference Overlap
    *   **Description:** Implement `GetTopMatchesAsync`. For each other user, compare their top N movies (by Score) against the current user's top N. Compute overlap count or a weighted similarity score. Sort descending, return top 10. Max 30 mins effort.
*   **Task:** Scaffold `MatchListViewModel`
    *   **Description:** Create the ViewModel. Inject `IPersonalityMatchingService`. Define an `ObservableCollection<MatchResult>` and an `IsLoading` boolean. Max 30 mins effort.
*   **Task:** Implement `MatchListViewModel` — Load Command
    *   **Description:** Add `LoadMatchesCommand` that calls the matching service, populates the observable collection, and toggles `IsLoading`. Max 30 mins effort.
*   **Task:** Scaffold `MatchedUserDetailViewModel`
    *   **Description:** Create the ViewModel that accepts a `UserId`, fetches the matched user's `UserProfile` and top `UserMoviePreference` entries, and exposes them as observable properties. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Create `MatchListView` Scaffold & Data Binding
    *   **Description:** Create the UI file. Set DataContext to `MatchListViewModel`. Add a loading spinner bound to `IsLoading`. Max 30 mins effort.
*   **Task:** Design Top 10 Match List Layout
    *   **Description:** Implement a scrollable list component bound to the `MatchResults` observable collection. Max 30 mins effort.
*   **Task:** Design Match Item Data Template
    *   **Description:** Design the UI template for a single match row: matched user's name, their top movie genre, and their match percentage. Max 30 mins effort.
*   **Task:** Create `MatchedUserDetailView` Layout
    *   **Description:** Design the detail screen showing the matched user's top movie preferences, engagement stats, and overall compatibility score. Max 30 mins effort.
