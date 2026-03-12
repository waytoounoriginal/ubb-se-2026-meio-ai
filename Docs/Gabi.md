### 1. Choose a movie tournament style
*   **Requirement 1:** The application allows the user to input a valid numeric value greater than 2 and smaller than the total number of liked movies to determine the initial pool size for the movie tournament.The system must fetch  set of movies whose's score was recently increased (matching the pool size) from the external Movie table and pair them for head-to-head matchups.
*   **Requirement 2:** The system must present the user with a side-by-side selection of two movies, displaying each movie's title and poster.The system must advance the user's selected movie from each pair to the next round, discarding the unselected movie.
*   **Requirement 3:** The system must repeatedly present winning movies in pairs until only one final winner remains.
*   **Requirement 4:** The system must boost the winning movie's score by two points in the shared `UserMoviePreference` table and present to the user a victory view with the results.
*   **Requirement 5:** Tournament progress is maintained during active sessions, allowing users to navigate away and return without interruption. However if the user closes  the application, it will result in a reset of all current tournament data.
 *   **Requirement 6:** User-flow:User will choose the number of movies to be included in the tournament. After doing so, the view will be changed to a new view with two movies side by side from which he will have to choose one.  When a movie wins it gets to the next phase where it gets matched against another movie that won. The user does this until there is only one movie left and the user gets a view of the results.
*   **Owner:** Gabi
*   **Cross-Team Dependencies:**
    *   **External Group:** Depends on Lucas's team's  `Movie` table for fetching the tournament movie pool.
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table schema — Gabi also writes to it (boosting winner score). Coordinate on shared upsert logic.


### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Start Movie Tournament`, `Select Movie Pair Winner`, `View Tournament Result`, `Boost Winner Preference Score`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It reads from the external `Movie` table and writes to the shared `UserMoviePreference` table.)*
    *   **External Table: `Movie`** — Read-only source of movie pool for tournament generation.
    *   **Shared Table: `UserMoviePreference`** — The tournament winner's Score is boosted for the current user. If no row exists, one is created with an initial boosted score.
    *   *(Note: Tournament bracket state — `TournamentState`, `Matchup` — is managed entirely in-memory and is NOT persisted to the database.)*
*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `MovieModel` (projection from external Movie table),`UserMoviePreferenceModel`,`Matchup`, `TournamentState` (in-memory), `Matchup` (in-memory)
    *   *Views:* `TournamentSetupView` (pool size input), `TournamentMatchView` (pairwise selection), `TournamentResultView` (winner display)
    *   *ViewModels:* `TournamentSetupViewModel`, `TournamentMatchViewModel`, `TournamentResultViewModel`
    *   *Utils/Services:* `TournamentLogicService` (handles bracket generation and advancement), `MovieTournamentRepository` (fetches random movies from external table)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Create `MovieModel` and `Matchup` Data Class
    *   **Description:** Define the Model class representing a movie card: `MovieId`, `Title`, `PosterUrl`.Define a  model holding `MovieA`, `MovieB`, and `WinnerId` Use the `UserMoviePreferenceModel` class created by **Bogdan**. It mirrors the shared `UserMoviePreference` table.
*   **Task:** Create `TournamentState` In-Memory Model
    *   **Description:** Create a model class representing the current state of a tournament: list of pending matches, completed matches, current round number. Not persisted. Tournament progress is maintained during active sessions, allowing users to navigate away and return without interruption. However if the user closes  the application, it will result in a reset of all current tournament data.
*   **Task:** Create `Matchup` In-Memory Model
    *   **Description:** Define a  model holding `MovieA`, `MovieB`, and `WinnerId`. Used to track individual tournament pairings in memory. 
*   **Task:** Implement Preference Score Boost for Winner
    *   **Description:** Write a repository method that updates a `UserMoviePreference` row for the (UserId, winning MovieId) pair, boosting the Score by a 2 points. 

**Backend Services & ViewModels**
*   **Task:** Implement `MovieRepository` — Fetch Random Movies
    *   **Description:** Create a repository method that queries the external Movie table and returns a subset of N movies who recently got their score increased for tournament pool generation. 
*   **Task:** Implement Initial Bracket Generation Logic
    *   **Description:** Write an algorithm in `TournamentLogicService` that takes a list of N movies with recently increased scores and constructs initial pairs. Handle odd numbers (e.g., give one movie a bye to the next round).
*   **Task:** Implement Match Advancement Logic
    *   **Description:** Add a method to `TournamentLogicService` that accepts a winner, updates the `TournamentState`, and generates the next round of matchups.
*   **Task:** Create `TournamentSetupViewModel`
    *   **Description:** Implement the ViewModel with an integer property for pool size and do validation ( > 2 and < than the total number of movies who got a score increase) and a command to initialize the tournament with the movies. 
*   **Task:** Create `TournamentMatchViewModel`
    *   **Description:** Implement the ViewModel with `MovieOptionA` and `MovieOptionB` properties. Add `SelectMovieCommand` triggered when user clicks a side. 
*   **Task:** Connect MatchViewModel to Tournament Logic
    *   **Description:** Wire `SelectMovieCommand` to call `TournamentLogicService`, then update `MovieA`/`MovieB` to the next pair. When the final winner is decided, navigate to result screen and boost the score by two points.

**GUI (Views)**
*   **Task:** Lay out `TournamentSetupView` UI
    *   **Description:** Create a layout with where the user can select the number of movies through a slider or manually input it. Bind to `TournamentSetupViewModel`. 
*   **Task:** Lay out `TournamentMatchView` 
    *   **Description:** Design the split-screen layout with two distinct clickable options (left vs right).Populate the split areas with image components and text for titles. Bind to the `TournamentMatchViewModel`.
*   **Task:** Lay out `TournamentResultView`
    *   **Description:** Create the "Winner" screen displaying the winning movie's poster in large format, with visual confirmation that the preference score was boosted.
