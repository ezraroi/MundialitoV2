namespace Mundialito.DAL.GeneralBets;

public interface IGeneralBetsRepository
{
    IEnumerable<GeneralBet> GetGeneralBets();

    GeneralBet GetGeneralBet(int betId);

    GeneralBet GetUserGeneralBet(string username);

    bool IsGeneralBetExists(string userId);

    GeneralBet InsertGeneralBet(GeneralBet bet);

    void DeleteGeneralBet(int betId);

    void UpdateGeneralBet(GeneralBet bet);

    void Save(); 
}

