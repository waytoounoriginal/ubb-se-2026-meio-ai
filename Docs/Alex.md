### 1. Formal Requirements
*   **Requirement 1:** The system must allow an authenticated user to select and upload a short-format video file (reel) from their device.
*   **Requirement 2:** The system must validate the uploaded video to ensure it meets format (e.g., MP4) and duration (e.g., under 60 seconds) constraints.
*   **Requirement 3:** The system must persistently store the video file (e.g., in blob storage) and record its metadata (uploader ID, storage URL, timestamp) in the database.
*   **Requirement 4:** The system must provide a viewable feed that retrieves and plays uploaded movie reels for other users to watch.
*   **Owner:** Student / Pitch Submitter
*   **Cross-Team Dependencies:** 
    *   *Database Team:* Needs to set up file blob storage (or integration like AWS S3/Firebase) and relational tables for reel metadata.
    *   *UI Team:* Needs to design an intuitive, full-screen video playback UI and a seamless file-picker UI for uploading.
    *   *Backend Logic Team:* Needs to implement secure chunked file uploading and streaming/fetching logic for the feed.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:** 
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Upload Movie Reel`, `View Reel Feed`, `Validate Video Format`
*   **Database Schema Additions:** 
    *   **Table: `Reels`**
        *   `ReelID` (Primary Key, UUID)
        *   `UploaderID` (Foreign Key -> `Users.UserID`)
        *   `VideoStorageURL` (String)
        *   `CreatedAt` (Timestamp)
        *   `DurationSeconds` (Integer)
        *   `Caption` (String)
    *   **Table: `ReelViews`** (To track who watched what for personality/algorithm purposes)
        *   `ViewID` (Primary Key, UUID)
        *   `ReelID` (Foreign Key -> `Reels.ReelID`)
        *   `ViewerID` (Foreign Key -> `Users.UserID`)
        *   `WatchedAt` (Timestamp)
*   **Class Diagram (MVVM) Additions:** 
    *   *Models:* `Reel`, `ReelUploadRequest`
    *   *Views:* `ReelUploadView`, `ReelFeedView`
    *   *ViewModels:* `ReelUploadViewModel`, `ReelFeedViewModel`
    *   *Utils/Services:* `VideoStorageService`, `ReelPlaybackService`

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Define `Reel` and `ReelUploadRequest` Model Classes
    *   **Description:** Create the basic OOP classes (`Reel` and `ReelUploadRequest`) with appropriate properties (ID, UploaderID, VideoURL, Duration) to represent a video reel in the application layer. No backend logic, just class structures.
*   **Task:** Create `Reels` Database Table Migration/Script
    *   **Description:** Write the exact SQL script or ORM migration to create the `Reels` table with `ReelID`, `UploaderID`, `VideoStorageURL`, `CreatedAt`, and `Caption`.
*   **Task:** Define `IVideoStorageService` Interface
    *   **Description:** Create an interface for the repository/service that will handle saving the video file and saving the database record. Define methods like `UploadVideoAsync()` and `GetReelFeedAsync()`.

**Backend Services & ViewModels**
*   **Task:** Implement `ReelUploadViewModel` - File Picker Logic
    *   **Description:** Add commands and properties to the `ReelUploadViewModel` to trigger the OS-level file picker and capture the selected file path/stream in a local variable.
*   **Task:** Implement `ReelUploadViewModel` - Validation Logic
    *   **Description:** Add logic to the ViewModel to verify the selected file has a valid video extension (e.g., .mp4, .mov) and is under the maximum file size limit before allowing the upload command to execute. Update a `StatusMessage` binding for the UI.
*   **Task:** Implement `ReelUploadViewModel` - Upload Command
    *   **Description:** Wire up the `SubmitUploadCommand` to pass the validated file stream and user ID to the `IVideoStorageService`. Ensure a loading boolean property is toggled during the operation.
*   **Task:** Implement `ReelFeedViewModel` - Fetching Setup
    *   **Description:** Create the `ReelFeedViewModel` with an `ObservableCollection<Reel>` property. Implement the `LoadNextReelsCommand` to fetch the first batch of reels from the service and populate the collection.

**GUI (Views)**
*   **Task:** Create `ReelUploadView` - Layout & Select Button
    *   **Description:** Create the basic XML/XAML/HTML layout constraint for the upload screen. Add a stylized "Select Video" button bound to the ViewModel's file picker command.
*   **Task:** Create `ReelUploadView` - Progress & Submit UI
    *   **Description:** Add a "Submit" button (disabled if no valid file is selected) and a loading spinner/progress bar bound to the ViewModel's loading state. Add a text field for a caption.
*   **Task:** Create `ReelFeedView` - Video Player Skeleton
    *   **Description:** Setup the full-screen layout for the reel feed. Place a mock/placeholder video player control on the screen that fills the available space, centered. Bind its source URL property to the current item in the ViewModel's reel collection.
