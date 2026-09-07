using Mundialito.DAL.Bets;

namespace Mundialito.Logic;

public interface IBetValidator
{
    void ValidateNewBet(Bet bet);

    /// <param name="persisted">The bet as it is stored - never a copy the caller supplied.</param>
    /// <param name="callerUserId">The authenticated caller. A separate parameter on purpose:
    /// comparing two fields of one object is how the ownership check came to compare a field
    /// to itself, since EF hands back the same instance on every read.</param>
    void ValidateUpdateBet(Bet persisted, string callerUserId);

    void ValidateDeleteBet(int betId, string userId);
}
