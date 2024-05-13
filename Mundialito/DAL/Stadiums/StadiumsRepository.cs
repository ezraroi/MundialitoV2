using System.Collections.Generic;
using System.Linq;
using Mundialito.Models;


namespace Mundialito.DAL.Stadiums;

public class StadiumsRepository : GenericRepository<Stadium>, IStadiumsRepository
{

    public StadiumsRepository()
        : base(new MundialitoDbContext())
    {
    }

    #region Implementation of IStadiumsRepository

    public IEnumerable<Stadium> GetStadiums()
    {
        return Get().OrderBy(stadium => stadium.Name);
    }

    public Stadium GetStadium(int stadiumId)
    {
        return Context.Stadiums.Where(stadium => stadium.StadiumId == stadiumId).SingleOrDefault();
    }

    public Stadium InsertStadium(Stadium stadium)
    {
        return Insert(stadium);
    }

    public void DeleteStadium(int stadiumId)
    {
        Delete(stadiumId);
    }

    public void UpdateStadium(Stadium stadium)
    {
        Update(stadium);
    }

    #endregion
}
