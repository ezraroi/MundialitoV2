using Mundialito.DAL;
using Mundialito.DAL.Stadiums;

namespace DBBackup.EntityBackuper;
public class StadiumsEntityBackuper : AbstractEntityBackuper<Stadium>
{
    public StadiumsEntityBackuper(string directoryName, MundialitoDbContext mundialitoDbContext)
        : base(directoryName, "Stadiums", mundialitoDbContext)
    {

    }

    protected override List<string> GetFieldsToBackup()
    {
        return new List<string>() { "StadiumId", "Name", "City", "Capacity" };
    }

    protected override List<Stadium> GetAllEntites()
    {
        var repository = new StadiumsRepository(mundialitoDbContext);
        return repository.GetStadiums().ToList();
    }
}

