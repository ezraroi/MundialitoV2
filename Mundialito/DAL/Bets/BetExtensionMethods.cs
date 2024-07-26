using Mundialito.DAL.Games;

namespace Mundialito.DAL.Bets;

public static class BetExtensionMethods
{

    public static bool IsOpenForBetting(this Bet bet)
    {
        return bet.IsOpenForBetting(DateTime.UtcNow);
    }

    public static bool IsOpenForBetting(this Bet bet, DateTime now)
    {
        return bet.Game.IsOpen(now);
    }

    public static bool IsResolved(this Bet bet)
    {
        return bet.IsResolved(DateTime.UtcNow);
    }

    public static bool IsResolved(this Bet bet, DateTime now)
    {

        return !bet.IsOpenForBetting(now) && !bet.Game.IsPendingUpdate(now);
    }

    // TODO - Should remove this
    public static string Mark(this Bet bet)
    {
        if (bet.HomeScore == bet.AwayScore) return "X";
        return bet.HomeScore > bet.AwayScore ? "1" : "2";
    }

}
