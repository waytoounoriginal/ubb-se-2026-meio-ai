using ubb_se_2026_meio_ai.Features.MovieTournament.Models;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    public class TournamentLogicService : ITournamentLogicService
    {
        private readonly IMovieTournamentRepository _repository;
        private readonly Random _random;
        private TournamentState? _state;

        public TournamentState CurrentState => _state ?? throw new InvalidOperationException("No active tournament.");
        public bool IsTournamentActive => _state != null && _state.PendingMatches.Count > 0;

        public TournamentLogicService(IMovieTournamentRepository repository)
        {
            _repository = repository;
            _random = new Random();
        }

        public async Task StartTournamentAsync(int userId, int poolSize)
        {
            if (poolSize < 4)
                throw new ArgumentException("Pool size must be at least 4.");

            var movies = await _repository.GetTournamentPoolAsync(userId, poolSize);
            
            if (movies.Count < poolSize)
                throw new InvalidOperationException($"Not enough movies. Requested {poolSize}, but found {movies.Count}.");

            _state = new TournamentState();

            
            for (int i = movies.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                var temp = movies[i];
                movies[i] = movies[j];
                movies[j] = temp;
            }

            int pairCount = movies.Count / 2;
            bool hasBye = movies.Count % 2 != 0;

            for (int i = 0; i < pairCount; i++)
            {
                _state.PendingMatches.Add(new MatchPair(movies[i * 2], movies[i * 2 + 1]));
            }

            if (hasBye)
            {
                var byeMovie = movies.Last();
                var byePair = new MatchPair(byeMovie, null!) { WinnerId = byeMovie.MovieId };
                _state.CompletedMatches.Add(byePair);
                _state.CurrentRoundWinners.Add(byeMovie);
            }
        }

        public async Task AdvanceWinnerAsync(int userId, int winnerId)
        {
            if (_state == null || _state.PendingMatches.Count == 0)
                throw new InvalidOperationException("No pending matches to advance.");

            var currentMatch = _state.PendingMatches[0];

            if (currentMatch.MovieA.MovieId != winnerId && 
                (currentMatch.MovieB == null || currentMatch.MovieB.MovieId != winnerId))
            {
                throw new ArgumentException("Winner ID does not match any movie in the current pair.");
            }

            currentMatch.WinnerId = winnerId;
            _state.PendingMatches.RemoveAt(0);
            _state.CompletedMatches.Add(currentMatch);

            var winnerMovie = currentMatch.MovieA.MovieId == winnerId ? currentMatch.MovieA : currentMatch.MovieB;
            _state.CurrentRoundWinners.Add(winnerMovie);

       
            if (_state.PendingMatches.Count == 0 && _state.CurrentRoundWinners.Count > 1)
            {
                GenerateNextRound();
            }

            if (IsTournamentComplete())
            {
                var finalWinner = GetFinalWinner();
                await _repository.BoostMovieScoreAsync(userId, finalWinner.MovieId, 2.0);
            }
        }

        private void GenerateNextRound()
        {
            var winners = new List<MovieCardModel>(_state!.CurrentRoundWinners);
            _state.CurrentRoundWinners.Clear();
            _state.CurrentRound++;

      
            for (int i = winners.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                var temp = winners[i];
                winners[i] = winners[j];
                winners[j] = temp;
            }

            int pairCount = winners.Count / 2;
            bool hasBye = winners.Count % 2 != 0;

            for (int i = 0; i < pairCount; i++)
            {
                _state.PendingMatches.Add(new MatchPair(winners[i * 2], winners[i * 2 + 1]));
            }

            if (hasBye)
            {
                var byeMovie = winners.Last();
                var byePair = new MatchPair(byeMovie, null!) { WinnerId = byeMovie.MovieId };
                _state.CompletedMatches.Add(byePair);
                _state.CurrentRoundWinners.Add(byeMovie);
            }
        }

        public MatchPair? GetCurrentMatch()
        {
            return _state?.PendingMatches.FirstOrDefault();
        }

        public bool IsTournamentComplete()
        {
            return _state != null && _state.PendingMatches.Count == 0 && _state.CurrentRoundWinners.Count == 1;
        }

        public MovieCardModel GetFinalWinner()
        {
            if (!IsTournamentComplete())
                throw new InvalidOperationException("Tournament is not yet complete.");
                
            return _state!.CurrentRoundWinners[0];
        }

        public void ResetTournament()
        {
            _state = null;
        }
    }
}
