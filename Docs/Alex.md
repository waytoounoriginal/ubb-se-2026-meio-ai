### 1. Formal Requirements
*   **Requirement 1:** The system must allow an authenticated user to select and upload a short-format video file (reel) from their device.
*   **Requirement 2:** The system must validate the uploaded video to ensure it meets format (e.g., MP4) and duration (e.g., under 60 seconds) constraints.
*   **Requirement 3:** The system must persistently store the video file (e.g., in blob storage) and record its metadata as a new row in the shared `Reel` table with `Source = 'upload'`.
*   **Requirement 4:** The system must associate each uploaded reel with the uploader's user ID and with a movie from the external Movie table (does not influence user preferences).
*   **Requirement 5:** User flow: logged-in user goes to the 'Upload reel' view, selects the file they want to upload, writes some details regarding the reel (eg: title, movie etc) and clicks the upload button. The user will be shown a confirmation text or an error if the validation fails
*   **Requirement 6:** If the user closes the application while it is uploading the reel the progress and unsaved data will be lost
*   **Owner:** Alex
*   **Cross-Team Dependencies:**
    *   **External Group:** Depends on the other group's `Movie` table for optional reel-to-movie association.
    *   **Beatrice:** The `Reel` table includes `CropDataJson` and `BackgroundMusicId` columns used by Beatrice's editing feature — coordinate on the shared schema.
    *   **Tudor:** Tudor's reels feed reads from the `Reel` table that Alex populates — uploaded reels will appear in the feed.
    *   **Andrei:** Andrei also writes to the `Reel` table (scraped trailers) — coordinate to avoid schema conflicts.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Upload Movie Reel`, `Validate Video Format`, `Associate Reel with Movie`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It writes to the shared `Reel` table.)*
    *   **Shared Table: `Reel`** — New rows created with `Source = 'upload'`, `CreatorUserId = current user`, `MovieId = selected movie (nullable)`.
*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `ReelModel`, `ReelUploadRequest` (DTO for the upload payload)
    *   *Views:* `ReelUploadView`
    *   *ViewModels:* `ReelUploadViewModel`
    *   *Utils/Services:* `VideoStorageService` (handles file upload to blob storage and `Reel` row insertion)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Define `Reel` Table Schema
    *   **Description:** Design and create the database migration for the `Reel` table with columns: `ReelId` (PK), `MovieId` (FK → Movie, nullable), `CreatorUserId` (FK → User, nullable), `VideoUrl`, `ThumbnailUrl`, `Title`, `Caption`, `DurationSeconds`, `CropDataJson` (nullable), `BackgroundMusicId` (FK → MusicTrack, nullable), `Source` (enum: upload/scraped), `CreatedAt`, `LastEditedAt`. Define all constraints and foreign keys. Max 30 mins effort.
*   **Task:** Create `ReelModel` Data Class
    *   **Description:** Define the Model class mirroring the `Reel` table: `ReelId`, `MovieId`, `CreatorUserId`, `VideoUrl`, `ThumbnailUrl`, `Title`, `Caption`, `DurationSeconds`, `CropDataJson`, `BackgroundMusicId`, `Source`, `CreatedAt`, `LastEditedAt`. Max 30 mins effort.
*   **Task:** Create `ReelUploadRequest` DTO
    *   **Description:** Define a data transfer object with properties: `VideoFileStream`, `FileName`, `UploaderUserId`, `MovieId` (nullable), `Caption` (nullable). This is used to pass upload data from the ViewModel to the service. Max 30 mins effort.
*   **Task:** Implement Reel Insert Repository Method
    *   **Description:** Write the repository method to insert a new `Reel` row with `Source = 'upload'`, `CreatorUserId`, `VideoUrl` (from blob storage), `DurationSeconds`, `MovieId`, `Caption`, and `CreatedAt`. Max 30 mins effort.
*   **Task:** Define `IVideoStorageService` Interface
    *   **Description:** Create the interface with methods: `UploadVideoAsync(ReelUploadRequest)` returning the stored `ReelModel`, and `ValidateVideoAsync(fileStream)` returning validation results. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Implement `ReelUploadViewModel` — File Picker Logic
    *   **Description:** Add commands and properties to trigger the OS-level file picker and capture the selected file path/stream in a local variable. Max 30 mins effort.
*   **Task:** Implement `ReelUploadViewModel` — Validation Logic
    *   **Description:** Add logic to verify the selected file has a valid video extension (e.g., .mp4, .mov) and is under the maximum file size/duration limit. Update a `StatusMessage` binding. Max 30 mins effort.
*   **Task:** Implement `ReelUploadViewModel` — Upload Command
    *   **Description:** Wire up `SubmitUploadCommand` to pass the validated file and metadata to `IVideoStorageService`. Toggle an `IsUploading` boolean during the operation. Max 30 mins effort.
*   **Task:** Implement `ReelUploadViewModel` — Movie Association Picker
    *   **Description:** Add an optional movie-selection step where the user can search/select a movie from the external Movie table to link to their reel. Bind selected movie to `SelectedMovieId`. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Create `ReelUploadView` — Layout & Select Button
    *   **Description:** Create the base layout for the upload screen. Add a stylized "Select Video" button bound to the file picker command. Max 30 mins effort.
*   **Task:** Create `ReelUploadView` — Progress & Submit UI
    *   **Description:** Add a "Submit" button (disabled if no valid file), a loading spinner bound to `IsUploading`, and a text field for a caption. Max 30 mins effort.
*   **Task:** Create `ReelUploadView` — Movie Selector Dropdown
    *   **Description:** Add a searchable dropdown or autocomplete field for linking the reel to a movie. Bind to the ViewModel's movie selection property. Max 30 mins effort.
