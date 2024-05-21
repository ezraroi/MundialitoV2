using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using System.Diagnostics;

namespace Mundialito.Logic;

public class BetValidator : IBetValidator
{
    private const string ObjectType = "Bet";
    private readonly IGamesRepository gamesRepository;
    private readonly IBetsRepository betsRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger logger;

    public BetValidator(ILogger<BetValidator> logger, IGamesRepository gamesRepository, IBetsRepository betsRepository, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.gamesRepository = gamesRepository;
        this.betsRepository = betsRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogsRepository = actionLogsRepository;
        this.logger = logger;
    }

    public void ValidateNewBet(Bet bet)
    {
        var game = gamesRepository.GetGame(bet.GameId);
        if (game == null)
        {
            AddLog(ActionType.ERROR, string.Format("Game {0} dosen't exist", bet.Game.GameId));
            throw new Exception(string.Format("Game {0} dosen't exist", bet.Game.GameId));
        }
        if (!game.IsOpen(dateTimeProvider.UTCNow))
        {
            AddLog(ActionType.ERROR, string.Format("Game {0} is closed for betting", game.GameId));
            throw new Exception(string.Format("Game {0} is closed for betting", game.GameId));
        }
        if (String.IsNullOrEmpty(bet.UserId))
        {
            AddLog(ActionType.ERROR, "New bet must have an owner");
            throw new Exception("New bet must have an owner");
        }
        if (betsRepository.GetGameBets(game.GameId).Any(b => b.UserId == bet.UserId))
        {
            AddLog(ActionType.ERROR, string.Format("You already have an existing bet on game {0}", game.GameId));
            throw new Exception(string.Format("You already have an existing bet on game {0}", game.GameId));
        }
    }

    public void ValidateUpdateBet(Bet bet)
    {
        var betToUpdate = betsRepository.GetBet(bet.BetId);
        if (betToUpdate == null)
        {
            AddLog(ActionType.ERROR, string.Format("Bet {0} dosen't exist", bet.BetId));
            throw new Exception(string.Format("Bet {0} dosen't exist", bet.BetId));
        }
        if (string.IsNullOrEmpty(bet.UserId))
        {
            AddLog(ActionType.ERROR, string.Format("Updated bet {0} must have user", bet.BetId));
            throw new Exception(string.Format("Updated bet {0} must have user", bet.BetId));
        }
        if (betToUpdate.UserId != bet.UserId)
        {
            AddLog(ActionType.UNAUTHORIZED_ACCESS, "You can't update a bet that is not yours");
            throw new UnauthorizedAccessException("You can't update a bet that is not yours");
        }
        var game = gamesRepository.GetGame(betToUpdate.GameId);
        if (!game.IsOpen(dateTimeProvider.UTCNow))
        {
            AddLog(ActionType.ERROR, string.Format("Game {0} is closed for betting", game.GameId));
            throw new Exception(string.Format("Game {0} is closed for betting", game.GameId));
        }

    }

    public void ValidateDeleteBet(int betId, string userId)
    {
        var betToDelete = betsRepository.GetBet(betId);
        if (betToDelete == null)
        {
            AddLog(ActionType.ERROR, string.Format("Bet {0} dosen't exist", betId));
            throw new Exception(string.Format("Bet {0} dosen't exist", betId));
        }
        if (betToDelete.User.Id != userId)
        {
            AddLog(ActionType.UNAUTHORIZED_ACCESS, "You can't delete a bet that is not yours");
            throw new UnauthorizedAccessException("You can't delete a bet that is not yours");
        }
        var game = gamesRepository.GetGame(betToDelete.GameId);
        if (dateTimeProvider.UTCNow > game.CloseTime)
        {
            AddLog(ActionType.ERROR, string.Format("Game {0} is closed for betting", game.GameId));
            throw new Exception(string.Format("Game {0} is closed for betting", game.GameId));
        }
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
