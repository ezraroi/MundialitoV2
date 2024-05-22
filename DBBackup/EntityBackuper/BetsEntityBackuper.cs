using Mundialito.DAL;
using Mundialito.DAL.Bets;

namespace DBBackup.EntityBackuper;
public class BetsEntityBackuper : AbstractEntityBackuper<Bet>
{
    public BetsEntityBackuper(string directoryName, MundialitoDbContext mundialitoDbContext)
        : base(directoryName, "Bets", mundialitoDbContext)
    {

    }

    protected override List<string> GetFieldsToBackup()
    {
        return new List<string>() { "BetId", "UserId", "GameId", "HomeScore", "AwayScore", "CornersMark", "CardsMark", "Points", "CornersWin", "GameMarkWin", "ResultWin", "CardsWin" };
    }

    protected override List<Bet> GetAllEntites()
    {
        var repository = new BetsRepository(mundialitoDbContext);
        return repository.GetBets().ToList();
    }
}

