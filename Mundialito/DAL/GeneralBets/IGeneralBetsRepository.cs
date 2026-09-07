namespace Mundialito.DAL.GeneralBets;

public interface IGeneralBetsRepository
{
    IEnumerable<GeneralBet> GetGeneralBets();

    GeneralBet GetGeneralBet(int betId);

    GeneralBet GetUserGeneralBet(string username);

    bool IsGeneralBetExists(string userId);

    /// <summary>How many general bets picked this player for the golden boot. The FK to
    /// Players cascades, so deleting a player without asking this first would delete those
    /// bets rather than fail.</summary>
    int CountGeneralBetsOnPlayer(int playerId);

    GeneralBet InsertGeneralBet(GeneralBet bet);

    void DeleteGeneralBet(int betId);

    void UpdateGeneralBet(GeneralBet bet);

    void Save(); 
}

