using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mundialito.DAL.Bets;

public class BetsRepository : GenericRepository<Bet>, IBetsRepository
{

    public BetsRepository(MundialitoDbContext context)
        : base(context)
    {
        
    }

    public IEnumerable<Bet> GetBets()
    {
        return Context.Bets.Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.Game.AwayTeam).Include(bet => bet.Game.HomeTeam);
    }

    public IEnumerable<Bet> GetUserBets(string username)
    {
        return Context.Bets.Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.Game.AwayTeam).Include(bet => bet.Game.HomeTeam).Where(bet => bet.User.UserName == username);
    }

    public Bet GetUserBetOnGame(string username, int gameId)
    {
        return Context.Bets.Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.Game.AwayTeam).Include(bet => bet.Game.HomeTeam).SingleOrDefault(bet => bet.User.UserName == username && bet.GameId == gameId);
    }

    public IEnumerable<Bet> GetGameBets(int gameId)
    {
        return Context.Bets.Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.Game.AwayTeam).Include(bet => bet.Game.HomeTeam).Where(bet => bet.Game.GameId == gameId);
    }

    public Bet GetBet(int betId)
    {
        return Get().Include(bet => bet.User).Include(bet => bet.Game).Include(bet => bet.Game.AwayTeam).Include(bet => bet.Game.HomeTeam).SingleOrDefault(bet => bet.BetId == betId);
    }

    public Bet InsertBet(Bet bet)
    {
        return Insert(bet);
    }

    public void DeleteBet(int betId)
    {
        Delete(betId);
    }

    public void UpdateBet(Bet bet)
    {
        Update(bet);
    }

    /// <summary>
    /// Turns the driver's unique_violation on IX_Bets_UserId_GameId into a named domain
    /// failure. Matched on SQLSTATE rather than on the index name, which is not part of any
    /// contract. Two concurrent first saves of the same bet are the shape that hits this.
    /// </summary>
    public override void Save()
    {
        try
        {
            base.Save();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new DuplicateBetException("This user already has a bet on this game", e);
        }
    }
}
