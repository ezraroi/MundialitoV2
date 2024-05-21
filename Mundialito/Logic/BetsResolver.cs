using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using System.Diagnostics;

namespace Mundialito.Logic;

public class BetsResolver : IBetsResolver
{
    private const string ObjectType = "Bet";
    private readonly IBetsRepository betsRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger logger;

    public BetsResolver(ILogger<BetsResolver> logger, IBetsRepository betsRepository, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor= httpContextAccessor;
        this.betsRepository = betsRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogsRepository = actionLogsRepository;
        this.logger = logger;
    }

    public void ResolveBets(Game game)
    {
        if (!game.IsBetResolved(dateTimeProvider.UTCNow))
            throw new ArgumentException(string.Format("Game {0} is not resolved yet", game.GameId));
        var bets = betsRepository.GetGameBets(game.GameId);
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
            AddLog(ActionType.UPDATE, String.Format("Resolved bet {0} with points {1}", bet, points));
        }
        if (bets.Count() > 0)
            betsRepository.Save();
    }

    private void AddLog(ActionType actionType, String message)
    {
        try
        {
            
            actionLogsRepository.InsertLogAction(ActionLog.Create(actionType, ObjectType, message, httpContextAccessor.HttpContext?.User.Identity.Name));
            actionLogsRepository.Save();
        }
        catch (Exception e)
        {
            logger.LogError("Exception during log. Exception: {0}", e.Message);
        }
    }
}
