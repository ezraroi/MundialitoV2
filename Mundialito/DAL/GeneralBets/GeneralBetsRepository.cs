
using Microsoft.EntityFrameworkCore;

namespace Mundialito.DAL.GeneralBets;

public class GeneralBetsRepository : GenericRepository<GeneralBet>, IGeneralBetsRepository
{
    public GeneralBetsRepository(MundialitoDbContext context)
        : base(context)
    {

    }

    public IEnumerable<GeneralBet> GetGeneralBets()
    {
        return Context.GeneralBets.Include(bet => bet.User).Include(bet => bet.WinningTeam).Include(bet => bet.GoldBootPlayer);
    }

    public GeneralBet GetUserGeneralBet(string username)
    {
        return Context.GeneralBets.Include(bet => bet.User).Include(bet => bet.WinningTeam).Include(bet => bet.GoldBootPlayer).SingleOrDefault(bet => bet.User.UserName == username);
    }

    public bool IsGeneralBetExists(string username)
    {
        return Context.GeneralBets.Any(bet => bet.User.UserName == username);
    }
            
    public GeneralBet GetGeneralBet(int betId)
    {
        return Context.GeneralBets.Include(bet => bet.User).Include(bet => bet.WinningTeam).Include(bet => bet.GoldBootPlayer).SingleOrDefault(bet => bet.GeneralBetId == betId);
    }

    public GeneralBet InsertGeneralBet(GeneralBet bet)
    {
        Context.Users.Attach(bet.User);
        return Insert(bet);
    }

    public void DeleteGeneralBet(int betId)
    {
        Delete(betId);
    }

    public void UpdateGeneralBet(GeneralBet bet)
    {
        Update(bet);
    }
}
