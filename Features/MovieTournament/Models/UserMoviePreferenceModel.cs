using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Models
{
    internal class UserMoviePreferenceModel
    {
        public int UserMoviePreferenceId { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public float Score { get; set; }
        public DateTime? LastModified { get; set; }
        public int? ChangeFromPreviousValue { get; set; }

        public UserMoviePreferenceModel(int userMoviePreferenceId, int userId, int movieId, float score, 
            DateTime? lastModified, int? changeFromPreviousValue)
        {
            UserMoviePreferenceId = userMoviePreferenceId;
            UserId = userId;
            MovieId = movieId;
            Score = score;
            LastModified = lastModified;
            ChangeFromPreviousValue = changeFromPreviousValue;
        }
    }
}
