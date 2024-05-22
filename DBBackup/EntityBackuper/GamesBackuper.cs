using Mundialito.DAL;
using Mundialito.DAL.Games;

namespace DBBackup.EntityBackuper;
public class GamesBackuper : AbstractEntityBackuper<Game>
{
    public GamesBackuper(string directoryName, MundialitoDbContext mundialitoDbContext)
        : base(directoryName, "Games", mundialitoDbContext)
    {

    }

    protected override List<string> GetFieldsToBackup()
    {
        return new List<string>() { "GameId", "HomeTeamId", "AwayTeamId", "Date", "HomeScore", "AwayScore", "CornersMark", "CardsMark", "StadiumId" };
    }

    protected override List<Game> GetAllEntites()
    {
        var repository = new GamesRepository(mundialitoDbContext);
        return repository.GetGames().ToList();
    }
}

