using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Services
{
    /// <summary>
    /// Implements personality matching using cosine similarity on movie score vectors.
    /// Owner: Madi
    /// </summary>
    public class PersonalityMatchingService : IPersonalityMatchingService
    {
        private readonly IPersonalityMatchRepository _repository;

        private static readonly Dictionary<int, string> HardcodedUsernames = new()
        {
            [1]  = "Alex Carter",
            [2]  = "Alice Rivers",
            [3]  = "Bob Chen",
            [4]  = "Carol Hayes",
            [5]  = "Dave Morris",
            [6]  = "Eve Santos",
            [7]  = "James Park",
            [8]  = "Luna Kim",
            [9]  = "Sam Taylor",
            [10] = "Nina Reeves",
            [11] = "Tom Walsh",
            [12] = "Zara Foster",
            [13] = "Kai Rivera",
        };


        private static readonly Dictionary<int, string> HardcodedFacebookAccounts = new()
        {
            [1]  = "fb_alex_carter",
            [2]  = "fb_alice_rivers",
            [3]  = "fb_bob_chen",
            [4]  = "fb_carol_hayes",
            [5]  = "fb_dave_morris",
            [6]  = "fb_eve_santos",
            [7]  = "fb_james_park",
            [8]  = "fb_luna_kim",
            [9]  = "fb_sam_taylor",
            [10] = "fb_nina_reeves",
            [11] = "fb_tom_walsh",
            [12] = "fb_zara_foster",
            [13] = "fb_kai_rivera",
        };

        public PersonalityMatchingService(IPersonalityMatchRepository repository)
        {
            _repository = repository;
        }

    
        public async Task<List<MatchResult>> GetTopMatchesAsync(int userId, int count)
        {
            List<UserMoviePreferenceModel> currentUserPrefs =
                await _repository.GetCurrentUserPreferencesAsync(userId);

            if (currentUserPrefs.Count == 0)
                return new List<MatchResult>();

            Dictionary<int, double> currentVector = currentUserPrefs
                .ToDictionary(p => p.MovieId, p => p.Score);

            Dictionary<int, List<UserMoviePreferenceModel>> othersPrefs =
                await _repository.GetAllPreferencesExceptUserAsync(userId);

            var scored = new List<(int OtherUserId, double Similarity)>();

            foreach (var kvp in othersPrefs)
            {
                int otherUserId = kvp.Key;
                Dictionary<int, double> otherVector = kvp.Value
                    .ToDictionary(p => p.MovieId, p => p.Score);

                double similarity = ComputeCosineSimilarity(currentVector, otherVector);
                double percentage = Math.Round(similarity * 100.0, 1);

                if (percentage > 0)
                    scored.Add((otherUserId, percentage));
            }

            scored.Sort((a, b) => b.Similarity.CompareTo(a.Similarity));

            var results = new List<MatchResult>();
            foreach (var (otherUserId, similarity) in scored.Take(count))
            {

                results.Add(new MatchResult
                {
                    MatchedUserId = otherUserId,
                    MatchedUsername = GetHardcodedUsername(otherUserId),
                    MatchScore = similarity,
                    FacebookAccount = GetFacebookAccount(otherUserId),
                });
            }

            return results;
        }


        public async Task<List<MatchResult>> GetRandomUsersAsync(int userId, int count)
        {
            List<int> randomIds = await _repository.GetRandomUserIdsAsync(userId, count);

            var results = new List<MatchResult>();
            foreach (int id in randomIds)
            {

                results.Add(new MatchResult
                {
                    MatchedUserId = id,
                    MatchedUsername = GetHardcodedUsername(id),
                    MatchScore = 0,
                    FacebookAccount = GetFacebookAccount(id),
                });
            }

            return results;
        }

        private static double ComputeCosineSimilarity(
            Dictionary<int, double> vectorA,
            Dictionary<int, double> vectorB)
        {
            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            foreach (var kvp in vectorA)
            {
                magnitudeA += kvp.Value * kvp.Value;
                if (vectorB.TryGetValue(kvp.Key, out double bScore))
                    dotProduct += kvp.Value * bScore;
            }

            foreach (var kvp in vectorB)
                magnitudeB += kvp.Value * kvp.Value;

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        private static string GetHardcodedUsername(int userId)
        {
            return HardcodedUsernames.TryGetValue(userId, out string? name) ? name : $"User {userId}";
        }

        private static string GetFacebookAccount(int userId)
        {
            if (HardcodedFacebookAccounts.TryGetValue(userId, out string? fb))
                return fb;
            return $"fb_user_{userId}";
        }
    }
}
