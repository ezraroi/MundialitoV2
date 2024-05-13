using Mundialito.DAL.Bets;

namespace Mundialito.Logic;

public interface IBetValidator
{
    void ValidateNewBet(Bet bet);
    void ValidateUpdateBet(Bet bet);
    void ValidateDeleteBet(int betId, string userId);
}

