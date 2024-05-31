using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;

namespace Mundialito.Logic;

public interface IBetsResolver
{
    void ResolveBets(Game game, IEnumerable<Bet> bets);
}

