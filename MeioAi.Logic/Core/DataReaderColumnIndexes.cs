namespace ubb_se_2026_meio_ai.Core
{
    /// <summary>
    /// Constants for mapping SQL data reader column indices to properties.
    /// These indices correspond to the SELECT column order in repository queries.
    /// </summary>
    public static class DataReaderColumnIndexes
    {
        /// <summary>Column indices for ReelModel mapping.</summary>
        public static class ReelModel
        {
            public const int ReelId = 0;
            public const int MovieId = 1;
            public const int CreatorUserId = 2;
            public const int VideoUrl = 3;
            public const int ThumbnailUrl = 4;
            public const int Title = 5;
            public const int Caption = 6;
            public const int FeatureDurationSeconds = 7;
            public const int CropDataJson = 8;
            public const int BackgroundMusicId = 9;
            public const int Source = 10;
            public const int CreatedAt = 11;
            public const int LastEditedAt = 12;
            public const int PrimaryGenre = 13;
        }

        /// <summary>Column indices for UserMoviePreference mapping.</summary>
        public static class UserMoviePreference
        {
            public const int MovieId = 0;
            public const int Score = 1;
        }

        /// <summary>Column indices for UserReelInteraction mapping (LikeCount query).</summary>
        public static class UserReelInteractionLike
        {
            public const int ReelId = 0;
            public const int LikeCount = 1;
        }

        /// <summary>Column indices for UserReelInteraction mapping (Detail query).</summary>
        public static class UserReelInteractionDetail
        {
            public const int UserId = 0;
            public const int ReelId = 1;
            public const int IsLiked = 2;
            public const int ViewedAt = 3;
        }

        /// <summary>Column indices for UserReelInteractionModel mapping.</summary>
        public static class UserReelInteractionModel
        {
            public const int InteractionId = 0;
            public const int UserId = 1;
            public const int ReelId = 2;
            public const int IsLiked = 3;
            public const int WatchDurationSec = 4;
            public const int WatchPercentage = 5;
            public const int ViewedAt = 6;
        }

        /// <summary>Column indices for UserProfileModel mapping.</summary>
        public static class UserProfileModel
        {
            public const int UserProfileId = 0;
            public const int UserId = 1;
            public const int TotalLikes = 2;
            public const int TotalWatchTimeSec = 3;
            public const int AvgWatchTimeSec = 4;
            public const int TotalClipsViewed = 5;
            public const int LikeToViewRatio = 6;
            public const int LastUpdated = 7;
        }

        /// <summary>Column indices for UserReelInteraction aggregate mapping.</summary>
        public static class UserReelInteractionAggregate
        {
            public const int TotalLikes = 0;
            public const int TotalWatchTimeSec = 1;
            public const int AvgWatchTimeSec = 2;
            public const int TotalClipsViewed = 3;
        }
    }
}
