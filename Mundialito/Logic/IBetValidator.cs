using Mundialito.DAL.Games;

namespace Mundialito.Logic;

public interface IBetValidator
{
    /// <summary>
    /// The only rule left on the bet write path. Everything the old ValidateNewBet and
    /// ValidateUpdateBet checked - that the game exists, that the bet has an owner, that
    /// the caller owns it, that there is not already a bet - is unrepresentable once the
    /// request carries no bet id and no game id: the route names the game and the token
    /// names the owner, and (owner, game) is unique in the database.
    /// </summary>
    /// <param name="game">The game as loaded by the caller, never one named by the request.</param>
    void ValidateBetUpsert(Game game);

    void ValidateDeleteBet(int betId, string userId);
}
