using Mundialito.DAL;
using Mundialito.DAL.Teams;

namespace DBBackup.EntityBackuper;

public class TeamsEntityBackuper : AbstractEntityBackuper<Team>
{
    public TeamsEntityBackuper(string directoryName, MundialitoDbContext mundialitoDbContext)
        : base(directoryName, "Teams", mundialitoDbContext)
    {

    }

    protected override List<string> GetFieldsToBackup()
    {
        return new List<string>() { "TeamId", "Name", "Flag", "Logo", "ShortName" };
    }

    protected override List<Team> GetAllEntites()
    {
        var repository = new TeamsRepository(mundialitoDbContext);
        return repository.GetTeams().ToList();
    }
}

