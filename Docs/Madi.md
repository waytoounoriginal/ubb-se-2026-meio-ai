### 1. Formal Requirements
*   **Requirement 1:** The system must allow an authenticated user to request personality-based matching with other users.
*   **Requirement 2:** The system must retrieve all other users' `UserMoviePreference` ranked lists from the database.
*   **Requirement 3:** The system must calculate a compatibility score between the current user's preference list and each other user's preference list by measuring overlap of top-scored movies.
*   **Requirement 4:** The system must sort and display a top 10 list of the highest-matching users to the current user.
*   **Requirement 5:** The system must allow the user to select and view the engagement details of any matched user from the top 10 list (reading from the matched user's `UserProfile`).
*   **Requirement 6:** User flow = User enters this screen and see a list of 10 users that match his personality based on movies he liked. He can click on any user to see his details.(does not influence user preferences).
*   **Requirement 7:** If the user closes the application while matches are being calculated or while viewing the match list, the in-memory match results are discarded. No persistent data is lost, and the user will simply need to request matches again upon reopening.

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

### 3. Project Management Tasks

**Database & Models**
*   **Task:** Implement Shared Data Models and DTOs
    *   **Description:** Utilize the shared `UserMoviePreferenceModel`, create the `UserProfileModel` mapping to the `UserProfile` table, and define the in-memory `MatchResult` DTO for holding matched user data.
*   **Task:** Implement and Test Database Retrieval Methods
    *   **Description:** Write repository methods to fetch all users' `UserMoviePreference` rows and a single `UserProfile` row by UserId. Include unit tests to verify preference retrieval logic.

**Backend Services & ViewModels**
*   **Task:** Implement Personality Matching Service
    *   **Description:** Define the `IPersonalityMatchingService` interface and implement the `GetTopMatchesAsync` algorithm to compute preference overlap and return the top 10 ranked matches.
*   **Task:** Develop ViewModels for Matching Feature
    *   **Description:** Scaffold and implement `MatchListViewModel` (with loading indicator, state management, and data population) and `MatchedUserDetailViewModel` (to fetch and expose selected user details and engagements).

**GUI (Views)**
*   **Task:** Design Match List View and Components
    *   **Description:** Create the `MatchListView` UI, implement the top 10 scrollable list layout, and design the data template for individual match rows including bindings to `MatchListViewModel`.
*   **Task:** Create Matched User Detail View
    *   **Description:** Design the `MatchedUserDetailView` screen layout to beautifully display the selected user's top preferences, engagement stats, and overall compatibility score.
