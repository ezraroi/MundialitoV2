using Microsoft.EntityFrameworkCore;

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
    
}
