### 1. Formal Requirements
*   **Requirement 1:** The system must present a sequential, interactive stack of movie cards or movie genre cards to the authenticated user.
*   **Requirement 2:** The system must allow the user to input their preference by swiping a card right to "like" the movie/genre, or swiping left to "dislike/skip" it.
*   **Requirement 3:** The system must continuously record and save every swipe action (positive and negative) mapped to the specific user in a long-term database storage system.
*   **Requirement 4:** The system must calculate a "Personality Profile" for the user by analyzing their historical swipe data and aggregating their top 10 most liked movie genres.
*   **Requirement 5:** The system must dynamically recalculate or update the user's top 10 liked genres upon the completion of new consecutive swipe actions.
*   **Owner:** Bogdan
*   **Cross-Team Dependencies:** 
    *   **UI Team:** Responsible for implementing the complex card swiping gestures, physics, and visual overlays.
    *   **Backend Logic Team:** Responsible for the algorithm that parses raw swipe data and aggregates the correct ranking of the Top 10 genres.
    *   **Database Team:** Responsible for creating the schema mapping Users, Movies/Genres, and their historical Swipe Actions.

---

### 2. Diagram Blueprint

*   **Use Case Diagram Additions:**
    *   **Actor:** Authenticated User
    *   **Use Cases:** `Swipe on Movie Card`, `Record Preference Data`, `Calculate Personality Profile`, `View Top 10 Movie Genres`.

*   **Database Schema Additions:**
    *   **Table: `UserSwipeAction`**
        *   `SwipeId` (Primary Key)
        *   `UserId` (Foreign Key -> User)
        *   `MovieOrGenreId` (Foreign Key -> Movie/Genre)
        *   `IsLiked` (Boolean)
        *   `Timestamp` (DateTime)
    *   **Table: `UserPersonalityProfile`**
        *   `ProfileId` (Primary Key)
        *   `UserId` (Foreign Key -> User, Unique)
        *   `TopGenresList` (JSON or Foreign Keys mapping to a cross-reference table of 10 genres)
        *   `LastRecalculated` (DateTime)

*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `MovieCardModel`, `SwipeActionModel`, `PersonalityProfileModel`, `GenreRankModel`.
    *   *Views:* `MovieSwipeView`, `PersonalityProfileResultView`.
    *   *ViewModels:* `MovieSwipeViewModel` (Controls the card deck and swipe state), `PersonalityProfileResultViewModel` (Controls the display sequence of the Top 10 list).
    *   *Utils/Services:* `SwipeDataService` (Handles transferring swipe actions to the database via API/ORM), `PersonalityProfilerService` (Calculates the genre rankings).

---

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Define `UserSwipeAction` Table Schema
    *   **Description:** Draft the precise database table blueprint or SQL `CREATE TABLE` statement for logging individual swipes (SwipeId, UserId, EntityType, EntityId, IsLiked, Timestamp). Max 30 mins effort.
*   **Task:** Define `UserPersonality` Table Schema
    *   **Description:** Draft the database table schema defining how a user's calculated top 10 genres will be stored for quick read-access on their profile. Max 30 mins effort.
*   **Task:** Create `SwipeActionModel` Data Class
    *   **Description:** Code the plain POJO/POCO Model class for `SwipeActionModel` to mirror the database table. Add necessary getter/setters. Max 30 mins effort.
*   **Task:** Create `MovieCardModel` Data Class
    *   **Description:** Code the Model class representing the data passed to the UI for the swipe-card (e.g., CoverImageURL, Title, PrimaryGenre). Max 30 mins effort.
*   **Task:** Create `GenreRankModel` Data Class
    *   **Description:** Code a simple data structure holding a Genre Name and its calculated Rank (1 through 10) for the profile screen. Max 30 mins effort.
*   **Task:** Write Swipe Data Insertion Query Structure
    *   **Description:** Write the Mock SQL INSERT query or setup the ORM endpoint required to safely write a new swipe transaction to the database. Max 30 mins effort.
*   **Task:** Write Genre Aggregation SQL / Query Architecture 
    *   **Description:** Formulate the SQL `GROUP BY` and `ORDER BY count` statements used to extract the Top 10 most "liked" genres from raw swipe history. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Scaffold `MovieSwipeViewModel`
    *   **Description:** Create the `MovieSwipeViewModel` class structure. Inherit from base ViewModel, implement Observer patterns (e.g. `INotifyPropertyChanged`), and define constructor. Max 30 mins effort.
*   **Task:** Implement `SwipeRightCommand`
    *   **Description:** Create the explicit ViewModel Command for swiping right. Have it take the `CurrentCard`, log state as 'Liked', and trigger the datastore service. Max 30 mins effort.
*   **Task:** Implement `SwipeLeftCommand`
    *   **Description:** Create the explicit ViewModel Command for swiping left. Have it log the `CurrentCard` state as 'Disliked' and trigger the datastore service. Max 30 mins effort.
*   **Task:** Implement ViewModel Card Queue Algorithm
    *   **Description:** Write logic inside `MovieSwipeViewModel` to hold an array/queue of the next 5 cards to display, automatically requesting more cards when the queue drops to 2. Max 30 mins effort.
*   **Task:** Define `ISwipeDataService` Interface
    *   **Description:** Create the service interface detailing the methods `RecordSwipeAsync()` and `FetchNextCardsAsync()`. Max 30 mins effort.
*   **Task:** Define `IPersonalityProfilerService` Interface
    *   **Description:** Create the Service interface with the necessary contract method: `CalculateTop10GenresAsync(int userId)`. Max 30 mins effort.
*   **Task:** Service Logic implementation: Calculate Profile
    *   **Description:** Implement the `CalculateTop10GenresAsync` logic inside the concrete service, fetching the aggregated data and mapping it to a `PersonalityProfileModel`. Max 30 mins effort.
*   **Task:** Scaffold `PersonalityProfileResultViewModel`
    *   **Description:** Create the ViewModel for the profile screen. Instantiate the property that will hold an observable list of `GenreRankModel` objects. Max 30 mins effort.
*   **Task:** Implement ViewModel Error Handling
    *   **Description:** Add global try-catch and logging mechanisms inside the ViewModel service calls so database failures don't crash the swiping UI. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Create `MovieSwipeView` Skeleton Layout
    *   **Description:** Create the base UI layout file (XML/XAML/HTML). Set up the main structural container that will center the movie cards. Max 30 mins effort.
*   **Task:** Design Static `MovieCard` UI Component
    *   **Description:** Design the visual aesthetics of a single, standalone movie card (e.g., Image taking up 70%, Title/Genre taking up 30%, rounded corners). Max 30 mins effort.
*   **Task:** Wire Data Binding for `MovieSwipeView`
    *   **Description:** Connect `MovieSwipeView` to its ViewModel. Bind the top card UI element to the ViewModel's `CurrentCard` property. Max 30 mins effort.
*   **Task:** Implement Swipe Event Listeners
    *   **Description:** Add touch-and-drag X/Y coordinate event listeners onto the `MovieCard` UI component in the view layer. Max 30 mins effort.
*   **Task:** Implement Drag-Threshold Logic
    *   **Description:** Write calculations in the View to determine if a drag event exceeded the "Swipe Decision Margin" (e.g. dragged > 30% off-screen), and execute the correct ViewModel Command upon release. Max 30 mins effort.
*   **Task:** Bind Visual Overlay Opacity ("Like" / "Nope")
    *   **Description:** Add Green 'LIKE' and Red 'NOPE' text overlays to the card design. Bind their visual opacity to the distance dragged from center. Max 30 mins effort.
*   **Task:** Create Layout Base for `PersonalityProfileResultView`
    *   **Description:** Build the UI skeleton for the "Top 10 Generas" screen, adding a Screen Title and an empty Data List/Recycler View container. Max 30 mins effort.
*   **Task:** Design Top 10 Genre List-Item Layout
    *   **Description:** Design the visual row template for a single Top 10 rank item (e.g., emphasizing the #1 Rank with a larger font or gold icon). Max 30 mins effort.
*   **Task:** Wire Data Binding for Profile Results View
    *   **Description:** Bind the View's List container to the observable list belonging to `PersonalityProfileResultViewModel` to render the Top 10 ranks onto the screen. Max 30 mins effort.
