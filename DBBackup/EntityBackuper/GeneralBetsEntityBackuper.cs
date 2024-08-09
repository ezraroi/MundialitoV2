using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.GeneralBets;

namespace DBBackup.EntityBackuper;

public class GeneralBetsEntityBackuper : AbstractEntityBackuper<GeneralBet>
{

    public GeneralBetsEntityBackuper(string directoryName, MundialitoDbContext mundialitoDbContext)
        : base(directoryName, "Genenral Bets", mundialitoDbContext)
    {

    }

    protected override List<string> GetFieldsToBackup()
    {
        return new List<string>() { "GeneralBetId", "WinningTeam", "GoldBootPlayer", "TeamPoints", "PlayerPoints", "User" };
    }

    protected override List<GeneralBet> GetAllEntites()
    {
        return new GeneralBetsRepository(mundialitoDbContext).GetGeneralBets().ToList();
    }

    protected override object ProcessValue(object obj, string propName)
    {
        if (obj is MundialitoUser)
            return ((MundialitoUser)obj).UserName;
        return obj;
    }
}

