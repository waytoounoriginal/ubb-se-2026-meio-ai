### 1. Formal Requirements
*   **Requirement 1:** The system must allow the user to input a valid numeric value greater than 2 to determine the initial pool size for the movie tournament.
*   **Requirement 2:** The system must pair the selected movies and present the user with a side-by-side selection of two movies, displaying each movie's title and poster.
*   **Requirement 3:** The system must advance the user's selected movie from each pair to the next round of the tournament, discarding the unselected movie.
*   **Requirement 4:** The system must repeatedly present the winning movies in pairs until only one final winning movie remains.
*   **Requirement 5:** The system must persist the final winning movie in the database, associating it with the current user's profile as a "saved" or "liked" movie.
*   **Owner:** Gabi
*   **Cross-Team Dependencies:** 
    *   *UI Team*: Requires dynamic UI rendering for the swipe/selection interface and tournament bracket.
    *   *Database Team*: Requires database updates to store the winning movie against the user's profile.
    *   *Backend Team*: Requires logic for tournament generation (fetching random movies) and managing bracket state.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:** 
    *   *Actor:* Authenticated User
    *   *Use Cases:* Start Movie Tournament, Select Movie Pair Winner, View Tournament Result, Save Tournament Winner.
*   **Database Schema Additions:** 
    *   *Table `Users`:* (Existing) `UserId` (PK), `Username`.
    *   *Table `Movies`:* `MovieId` (PK), `Title`, `PosterUrl`, `Description`.
    *   *Table `TournamentWinners`:* `WinnerId` (PK), `UserId` (FK to Users), `MovieId` (FK to Movies), `DateWon` (Timestamp).
    *   *Relationships:* One-to-Many between `Users` and `TournamentWinners`. One-to-Many between `Movies` and `TournamentWinners`.
*   **Class Diagram (MVVM) Additions:** 
    *   *Models:* `Movie`, `TournamentState`, `Matchup`, `TournamentResult`
    *   *Views:* `TournamentSetupView` (number input), `TournamentMatchView` (pairwise selection), `TournamentResultView` (winner display)
    *   *ViewModels:* `TournamentSetupViewModel`, `TournamentMatchViewModel`, `TournamentResultViewModel`
    *   *Utils/Services:* `TournamentLogicService` (handles bracket advancement), `MovieRepository` (fetches movies)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Create Movie and Matchup Models
    *   **Description:** Define the `Movie` model (Id, Title, PosterPath) and the `Matchup` model (MovieA, MovieB, WinnerId). Ensure models map cleanly to future database logic. Max 30 mins effort.
*   **Task:** Define TournamentState Model
    *   **Description:** Create a model class representing the current state of a tournament, storing the list of pending matches, completed matches, and the current round number. Max 30 mins effort.
*   **Task:** Update Schema for TournamentWinners
    *   **Description:** Write a database migration script or define the ORM entity mapping to add the `TournamentWinners` table with foreign keys linking to Users and Movies. Max 30 mins effort.
*   **Task:** Implement Database Repository for Saving Winners
    *   **Description:** Create a repository interface and implementation with a `SaveWinner(UserId, MovieId)` method to insert the tournament result into the database. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Implement Initial Bracket Generation Logic
    *   **Description:** Write an algorithm in `TournamentLogicService` that takes a list of $n$ movies and constructs the initial set of pairs (matches). Handle odd numbers if the user inputs a non-power of 2. Max 30 mins effort.
*   **Task:** Implement Match Advancement Logic
    *   **Description:** Add a method to `TournamentLogicService` that takes a winner, updates the tournament state, and generates the next round of match-ups. Max 30 mins effort.
*   **Task:** Create TournamentSetupViewModel
    *   **Description:** Implement `TournamentSetupViewModel` with an integer property for the pool size (validate > 2) and a command to trigger the initialization of the tournament with the chosen size. Max 30 mins effort.
*   **Task:** Create TournamentMatchViewModel Structure
    *   **Description:** Implement `TournamentMatchViewModel` with properties `MovieOptionA` and `MovieOptionB`. Add a `SelectMovieCommand` that triggers when the user clicks a side. Max 30 mins effort.
*   **Task:** Connect MatchViewModel to Tournament Logic
    *   **Description:** Wire the `SelectMovieCommand` in `TournamentMatchViewModel` to call the `TournamentLogicService`, then update the View's `MovieA` and `MovieB` properties to the next pair. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Lay out TournamentSetupView UI
    *   **Description:** Create the skeleton layout for `TournamentSetupView`. Add a number input field (or slider) and a "Start" button. Bind these to the `TournamentSetupViewModel`. Max 30 mins effort.
*   **Task:** Lay out TournamentMatchView Split Screen
    *   **Description:** Design the split-screen layout for `TournamentMatchView` using a grid layout with two distinct clickable halves (left vs right). Max 30 mins effort.
*   **Task:** Bind Posters and Titles in MatchView
    *   **Description:** Populate the split areas in `TournamentMatchView` with Image UI components for the movie posters and Text components for the titles. Bind these to the ViewModel properties. Max 30 mins effort.
*   **Task:** Lay out TournamentResultView 
    *   **Description:** Create the final "Winner" screen UI displaying the winning movie's poster in large format, with a visual confirmation that the movie was saved to the database. Max 30 mins effort.
