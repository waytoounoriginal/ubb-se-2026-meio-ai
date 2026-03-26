## Personality Matching

*   **Owner:** Madi

The system should provide a user matching and discovery screen where, upon access, the user is presented with a list of up to 10 users whose personalities best match theirs based on movie preferences, with each user displayed alongside a visible match percentage represented as a sidebar or progress indicator on the right of their name; if no matches are found, the system must display the message "No match" followed by a section titled "Maybe you like:" containing 10 randomly selected users, and in all cases, the user can click on any listed profile to view detailed information without affecting their own preferences or matching results, while each user profile may optionally include a facebookAccount field representing their Facebook nickname, allowing others to identify and connect with them externally on Facebook.

### 1. Formal Requirements
*   **Requirement 1:** The system must allow an authenticated user to request personality-based matching with other users through a dedicated matching and discovery screen, where matching is based on similarities in movie preferences.
*   **Requirement 2:** The system must retrieve all other users' `UserMoviePreference` ranked lists from the database, excluding the current user and considering only valid preference data.
*   **Requirement 3:** The system must calculate a compatibility score between the current user's preference list and each other user's preference list by measuring the overlap and ranking similarity of top-scored movies, and normalize this score into a percentage value.
*   **Requirement 4:** The system must sort users in descending order based on their compatibility score and display a list of up to 10 users whose personalities best match the current user, with each user displayed alongside their name and a visible match percentage represented as a sidebar or progress indicator on the right side.
*   **Requirement 5:** If no matching users are found, the system must display the message "No match" and provide an alternative section titled "Maybe you like:" containing up to 10 randomly selected users.
*   **Requirement 6:** The system must allow the user to select and view the details of any listed user (matched or recommended) by accessing that user's `UserProfile`, without affecting the current user's preferences or future matching results.
*   **Requirement 7:** Each user profile may optionally include a `facebookAccount` field representing their Facebook nickname, allowing other users to identify and interact with them externally on Facebook. *(Note: For now this field is hardcoded in the model and NOT stored in the database to avoid schema changes.)*
*   **Requirement 8:** If the user closes the application while matches are being calculated or while viewing the match list, the in-memory match results are discarded, no persistent data is lost, and the user must request matches again upon reopening.

*   **Cross-Team Dependencies:**
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table and the shared `UserMoviePreferenceModel`. Madi reads the scores Bogdan (and others) write to compute preference overlap for matching.
    *   **Tudor:** Tudor owns the `UserProfile` table schema — Madi reads from it to display matched users' engagement stats. Tudor also writes to `UserMoviePreference` (reel likes), which affects matching results.
    *   **Gabi:** Gabi's tournament also writes to `UserMoviePreference`, which affects matching results.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Request Personality Matches`, `View Match List (up to 10)`, `View "Maybe you like" fallback list`, `View User Details`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It reads from the shared `UserMoviePreference` and `UserProfile` tables.)*
    *   **Shared Table: `UserMoviePreference`** (UserId, MovieId, Score, LastModified) — The source for computing preference overlap between users.
    *   **Shared Table: `UserProfile`** (UserId, TotalLikes, TotalWatchTimeSec, etc.) — Read for displaying matched user engagement details.
    *   *(Note: `MatchResult` is an in-memory DTO, NOT persisted to the database.)*
    *   *(Note: `facebookAccount` is hardcoded in the model for now, NOT a database column.)*
*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `UserMoviePreferenceModel` (shared), `UserProfileModel` (shared), `MatchResult` (in-memory DTO: User + MatchScore percentage)
    *   *Views:* `MatchListView` (screen showing up to 10 matches with percentage bar, or "No match" + "Maybe you like" fallback), `MatchedUserDetailView` (screen showing selected user's traits/stats/facebookAccount)
    *   *ViewModels:* `MatchListViewModel` (controls the match list logic + fallback logic), `MatchedUserDetailViewModel` (displays a specific user's info)
    *   *Utils/Services:* `PersonalityMatchingService` (contains the algorithm to compute preference overlap and compatibility percentage)

### 3. Project Management Tasks

**Database & Models**
*   **Task:** Implement Shared Data Models and DTOs
    *   **Description:** Utilize the shared `UserMoviePreferenceModel`, create the `UserProfileModel` mapping to the `UserProfile` table (with a hardcoded `FacebookAccount` property), and define the in-memory `MatchResult` DTO for holding matched user data with a percentage score.
*   **Task:** Implement and Test Database Retrieval Methods
    *   **Description:** Write repository methods to fetch all users' `UserMoviePreference` rows (excluding current user), a single `UserProfile` row by UserId, and a method to fetch N random users for the "Maybe you like" fallback. Include unit tests to verify retrieval logic.

**Backend Services & ViewModels**
*   **Task:** Implement Personality Matching Service
    *   **Description:** Define the `IPersonalityMatchingService` interface and implement the `GetTopMatchesAsync` algorithm to compute preference overlap, normalize to a percentage, and return the top 10 ranked matches. Include a `GetRandomUsersAsync` fallback method for when no matches are found.
*   **Task:** Develop ViewModels for Matching Feature
    *   **Description:** Scaffold and implement `MatchListViewModel` (with loading indicator, state management, match list population, and "No match" / "Maybe you like" fallback state) and `MatchedUserDetailViewModel` (to fetch and expose selected user details, engagements, and facebook account).

**GUI (Views)**
*   **Task:** Design Match List View and Components
    *   **Description:** Create the `MatchListView` UI with a scrollable list showing up to 10 matches, each row displaying the user name and a match percentage sidebar/progress indicator on the right. Implement the "No match" + "Maybe you like:" fallback section with 10 random users.
*   **Task:** Create Matched User Detail View
    *   **Description:** Design the `MatchedUserDetailView` screen layout to display the selected user's top preferences, engagement stats, overall compatibility percentage, and Facebook nickname.
