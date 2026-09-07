using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;

namespace Mundialito.Logic;

/// <summary>
/// Pure: it throws, it does not write. Every repository in a request shares one
/// <c>MundialitoDbContext</c>, so a Save() from here - the audit log this class used to
/// write on its rejection paths - flushed whatever the caller had already put on the
/// tracked entity, committing the very update it was about to refuse. Callers log.
/// </summary>
public class BetValidator : IBetValidator
{
    private readonly IGamesRepository gamesRepository;
    private readonly IBetsRepository betsRepository;
    private readonly IDateTimeProvider dateTimeProvider;

    public BetValidator(IGamesRepository gamesRepository, IBetsRepository betsRepository, IDateTimeProvider dateTimeProvider)
    {
        this.gamesRepository = gamesRepository;
        this.betsRepository = betsRepository;
        this.dateTimeProvider = dateTimeProvider;
    }

    public void ValidateNewBet(Bet bet)
    {
        var game = gamesRepository.GetGame(bet.GameId);
        if (game == null)
            throw new BetValidationException(string.Format("Game {0} dosen't exist", bet.GameId));
        if (!game.IsOpen(dateTimeProvider.UTCNow))
            throw new GameClosedForBettingException(string.Format("Game {0} is closed for betting", game.GameId));
        if (string.IsNullOrEmpty(bet.UserId))
            throw new BetValidationException("New bet must have an owner");
        if (betsRepository.GetGameBets(game.GameId).Any(b => b.UserId == bet.UserId))
            throw new BetValidationException(string.Format("You already have an existing bet on game {0}", game.GameId));
    }

    public void ValidateUpdateBet(Bet persisted, string callerUserId)
    {
        if (persisted == null)
            throw new BetValidationException("Bet dosen't exist");
        if (string.IsNullOrEmpty(callerUserId))
            throw new BetValidationException(string.Format("Updated bet {0} must have user", persisted.BetId));
        if (persisted.UserId != callerUserId)
            throw new BetForbiddenException("You can't update a bet that is not yours");
        // The bet's own game, never one named by the request: a bet's game is fixed at creation.
        var game = gamesRepository.GetGame(persisted.GameId);
        if (game == null)
            throw new BetValidationException(string.Format("Game {0} dosen't exist", persisted.GameId));
        if (!game.IsOpen(dateTimeProvider.UTCNow))
            throw new GameClosedForBettingException(string.Format("Game {0} is closed for betting", game.GameId));
    }

    public void ValidateDeleteBet(int betId, string userId)
    {
        var betToDelete = betsRepository.GetBet(betId);
        if (betToDelete == null)
            throw new BetValidationException(string.Format("Bet {0} dosen't exist", betId));
        if (betToDelete.User.Id != userId)
            throw new BetForbiddenException("You can't delete a bet that is not yours");
        var game = gamesRepository.GetGame(betToDelete.GameId);
        if (dateTimeProvider.UTCNow > game.CloseTime)
            throw new GameClosedForBettingException(string.Format("Game {0} is closed for betting", game.GameId));
    }
}
