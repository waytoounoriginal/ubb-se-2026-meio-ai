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

        // Hardcoded Facebook accounts for demo — will move to DB later
        private static readonly Dictionary<int, string> HardcodedFacebookAccounts = new()
        {
            [1] = "fb_currentuser",
            [2] = "fb_alice_movies",
            [3] = "fb_bob_cinephile",
            [4] = "fb_carol_films",
            [5] = "fb_dave_reels",
            [6] = "fb_eve_cinema",
        };

        public PersonalityMatchingService(IPersonalityMatchRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task<List<MatchResult>> GetTopMatchesAsync(int userId, int count)
        {
            // 1. Load current user's preferences as a score vector keyed by MovieId
            List<UserMoviePreferenceModel> currentUserPrefs =
                await _repository.GetCurrentUserPreferencesAsync(userId);

            if (currentUserPrefs.Count == 0)
            {
                // No preferences → can't compute matches
                return new List<MatchResult>();
            }

            Dictionary<int, double> currentVector = currentUserPrefs
                .ToDictionary(p => p.MovieId, p => p.Score);

            // 2. Load all other users' preferences
            Dictionary<int, List<UserMoviePreferenceModel>> othersPrefs =
                await _repository.GetAllPreferencesExceptUserAsync(userId);

            // 3. Compute cosine similarity for each other user
            var scored = new List<(int OtherUserId, double Similarity)>();

            foreach (var kvp in othersPrefs)
            {
                int otherUserId = kvp.Key;
                List<UserMoviePreferenceModel> otherPrefs = kvp.Value;

                Dictionary<int, double> otherVector = otherPrefs
                    .ToDictionary(p => p.MovieId, p => p.Score);

                double similarity = ComputeCosineSimilarity(currentVector, otherVector);

                // Normalize to 0–100%
                double percentage = Math.Round(similarity * 100.0, 1);

                if (percentage > 0)
                {
                    scored.Add((otherUserId, percentage));
                }
            }

            // 4. Sort descending and take top N
            scored.Sort((a, b) => b.Similarity.CompareTo(a.Similarity));

            var topMatches = scored.Take(count).ToList();

            // 5. Build MatchResult list with usernames and facebook accounts
            var results = new List<MatchResult>();
            foreach (var (otherUserId, similarity) in topMatches)
            {
                string username = await _repository.GetUsernameAsync(otherUserId);
                string facebook = GetFacebookAccount(otherUserId, username);

                results.Add(new MatchResult
                {
                    MatchedUserId = otherUserId,
                    MatchedUsername = username,
                    MatchScore = similarity,
                    FacebookAccount = facebook,
                });
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<List<MatchResult>> GetRandomUsersAsync(int userId, int count)
        {
            List<int> randomIds = await _repository.GetRandomUserIdsAsync(userId, count);

            var results = new List<MatchResult>();
            foreach (int id in randomIds)
            {
                string username = await _repository.GetUsernameAsync(id);
                string facebook = GetFacebookAccount(id, username);

                results.Add(new MatchResult
                {
                    MatchedUserId = id,
                    MatchedUsername = username,
                    MatchScore = 0,
                    FacebookAccount = facebook,
                });
            }

            return results;
        }

        /// <summary>
        /// Computes cosine similarity between two sparse score vectors.
        /// Only movies present in BOTH vectors contribute to the dot product.
        /// Returns a value between 0 and 1.
        /// </summary>
        private static double ComputeCosineSimilarity(
            Dictionary<int, double> vectorA,
            Dictionary<int, double> vectorB)
        {
            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            // Compute dot product over shared movie IDs
            foreach (var kvp in vectorA)
            {
                magnitudeA += kvp.Value * kvp.Value;

                if (vectorB.TryGetValue(kvp.Key, out double bScore))
                {
                    dotProduct += kvp.Value * bScore;
                }
            }

            foreach (var kvp in vectorB)
            {
                magnitudeB += kvp.Value * kvp.Value;
            }

            if (magnitudeA == 0 || magnitudeB == 0)
            {
                return 0;
            }

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        /// <summary>
        /// Returns the hardcoded Facebook account for a user, or generates one from username.
        /// </summary>
        private static string GetFacebookAccount(int userId, string username)
        {
            if (HardcodedFacebookAccounts.TryGetValue(userId, out string? fb))
            {
                return fb;
            }

            // Generate a plausible facebook handle for any user not in the hardcoded map
            return $"fb_{username.ToLowerInvariant().Replace(' ', '_')}";
        }
    }
}
