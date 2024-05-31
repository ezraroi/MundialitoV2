using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;

namespace Mundialito.Logic;

public class BetsResolver : IBetsResolver
{
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ILogger logger;

    public BetsResolver(ILogger<BetsResolver> logger, IDateTimeProvider dateTimeProvider)
    {
        this.dateTimeProvider = dateTimeProvider;
        this.logger = logger;
    }

    public void ResolveBets(Game game, IEnumerable<Bet> bets)
    {
        if (!game.IsBetResolved(dateTimeProvider.UTCNow))
            throw new ArgumentException(string.Format("Game {0} is not resolved yet", game.GameId));
        foreach (Bet bet in bets)
        {
            var points = 0;
            if (bet.Mark() == game.Mark(dateTimeProvider.UTCNow))
            {
                points += 3;
                bet.GameMarkWin = true;
            }
            else
                bet.GameMarkWin = false;
            if ((bet.HomeScore == game.HomeScore) && (bet.AwayScore == game.AwayScore))
            {
                points += 2;
                bet.ResultWin = true;
            }
            else
                bet.ResultWin = false;
            if (game.CardsMark == bet.CardsMark)
            {
                points += 1;
                bet.CardsWin = true;
            }
            else
                bet.CardsWin = false;
            if (game.CornersMark == bet.CornersMark)
            {
                points += 1;
                bet.CornersWin = true;
            }
            else
                bet.CornersWin = false;
            bet.Points = points;
            logger.LogInformation("{0} of {1} got {2} points", bet.BetId, game.GameId, points);
        }
    }
}
