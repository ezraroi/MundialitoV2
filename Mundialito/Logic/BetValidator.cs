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

    public void ValidateBetUpsert(Game game)
    {
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
