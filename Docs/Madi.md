### 1. Formal Requirements
*   **Requirement[s]:** 
    *   The system must allow an authenticated user to request to be matched with other personality profiles.
    *   The system must retrieve a pool of existing user personality profiles from the database.
    *   The system must calculate a compatibility score between the current user's personality profile and the retrieved profiles.
    *   The system must sort and display a top 10 list of the highest-matching personality profiles to the user.
    *   The system must allow the user to select and view the details of any matched profile from the initial top 10 list without persisting this selection to the database.
*   **Owner:** [Unassigned / Student Name]
*   **Cross-Team Dependencies:** Database Team (for broad querying of existing profiles), Backend Logic Team (for designing the mathematical matching algorithm), UI Team (for displaying list data and profile detail layouts).

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* Request Personality Matches, View Top 10 Matches, View Matched Profile Details
*   **Database Schema Additions:**
    *(Note: Since no relational data is created after the action, no new tables are required. We rely on existing entity schemas).*
    *   *Relevant Tables:* `Users`, `PersonalityProfiles`
    *   *Columns:* `Users` (UserID, Username), `PersonalityProfiles` (ProfileID, UserID, [TraitScore Columns])
    *   *Relationships:* 1-to-1 relationship between `Users` and `PersonalityProfiles`. 
*   **Class Diagram (MVVM) Additions:** 
    *   *Models:* `User`, `PersonalityProfile`, `MatchResult` (A new DTO combining a User, their Profile, and their computed Match Score).
    *   *Views:* `MatchListView` (Screen showing top 10), `MatchedProfileDetailView` (Screen showing the selected person's traits).
    *   *ViewModels:* `MatchListViewModel` (Controls the top 10 list logic), `MatchedProfileDetailViewModel` (Controls displaying a specific user's info).
    *   *Utils/Services:* `PersonalityMatchingService` (Contains the mathematical algorithm to calculate trait differences/similarity).

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Define MatchResult Data Transfer Object (DTO)
    *   **Description:** Create a non-persistent `MatchResult` model class containing properties for a `User` object, a `PersonalityProfile` object, and a computed numerical `MatchScore`. Timebox: 15 mins.
*   **Task:** Create Profile Pool Retrieval Method
    *   **Description:** Write the data access logic (e.g., in `ProfileRepository`) to fetch a list of `PersonalityProfile` records (along with their associated Users) from the database, strictly excluding the active user's profile. Timebox: 25 mins.
*   **Task:** Write Unit Tests for Profile Retrieval
    *   **Description:** Create basic unit tests using a mocked database context/interface to verify the repository method returns profiles and successfully excludes the querying user. Timebox: 30 mins.

**Backend Services & ViewModels**
*   **Task:** Define PersonalityMatchingService Interface
    *   **Description:** Create an `IPersonalityMatchingService` interface. Define a single method signature: `List<MatchResult> GetTopMatches(PersonalityProfile currentUser, List<PersonalityProfile> pool, int limit = 10)`. Timebox: 10 mins.
*   **Task:** Implement Matching Algorithm Math Logic
    *   **Description:** Implement the `GetTopMatches` method in the service. Write a simple algorithm (like Euclidean distance or absolute difference of trait scores) to calculate compatibility, sort the results descending by score, and return the top 10. Timebox: 30 mins.
*   **Task:** Setup MatchListViewModel Properties
    *   **Description:** Create the `MatchListViewModel`. Inject `IPersonalityMatchingService` and the data repository via constructor. Define an `ObservableCollection<MatchResult>` or equivalent reactive property to hold the data for the UI. Timebox: 20 mins.
*   **Task:** Implement MatchListViewModel Load Command
    *   **Description:** Add an asynchronous `LoadMatchesCommand` to the VM that triggers the repository fetch, passes the data to the matching service, populates the observable collection, and toggles an `IsLoading` boolean flag. Timebox: 30 mins.

**GUI (Views)**
*   **Task:** Create MatchListView Scaffold & Data Binding
    *   **Description:** Create the UI file for `MatchListView`. Set the DataContext/BindingContext to `MatchListViewModel`. Add a loading spinner bound to the `IsLoading` property. Timebox: 15 mins.
*   **Task:** Design Top 10 Match List Container Layout
    *   **Description:** Implement a scrollable List/RecyclerView component in the UI. Bind its item source directly to the `MatchResults` observable collection. Timebox: 20 mins.
*   **Task:** Design Match Item Data Template
    *   **Description:** Design the UI template/cell for a single matched row. It must map the data to display the matched user's name/avatar, their primary personality type, and their numerical match percentage. Timebox: 30 mins.
