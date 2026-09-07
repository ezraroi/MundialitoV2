using Mundialito.DAL.Games;

namespace Mundialito.DAL.Bets;

public interface IBetsRepository
{
    IEnumerable<Bet> GetBets();

    /// <summary>Detached from the change tracker - see <see cref="IGamesRepository.GetGameNoTracking"/>.</summary>
    IEnumerable<Bet> GetBetsNoTracking();

    IEnumerable<Bet> GetUserBets(string username);

    Bet GetUserBetOnGame(string username, int gameId);

    IEnumerable<Bet> GetGameBets(int gameId);
    
    Bet GetBet(int betId);

    Bet InsertBet(Bet bet);

    void DeleteBet(int betId);

    void UpdateBet(Bet bet);

    void Save(); 
}