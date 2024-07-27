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
            bet.MaxPoints = false;
            if (bet.Mark() == game.Mark(dateTimeProvider.UTCNow))
            {
                points += game.MarkPoints();
                bet.GameMarkWin = true;
            }
            else
                bet.GameMarkWin = false;
            if ((bet.HomeScore == game.HomeScore) && (bet.AwayScore == game.AwayScore))
            {
                points += game.ResultPoints();
                bet.ResultWin = true;
            }
            else
                bet.ResultWin = false;
            if (game.CardsMark == bet.CardsMark)
            {
                points += game.CardsPoints();
                bet.CardsWin = true;
            }
            else
                bet.CardsWin = false;
            if (game.CornersMark == bet.CornersMark)
            {
                points += game.CornersPoints();
                bet.CornersWin = true;
            }
            else
                bet.CornersWin = false;
            if (points == game.MaxPoints()) 
            {
                bet.MaxPoints = true;
                points += game.BingoBonusPoints();
            }
            bet.Points = points;
            logger.LogInformation("{0} of {1} ({2}) got {3} points", bet.BetId, game.GameId, game.Type, points);
        }
    }
}
