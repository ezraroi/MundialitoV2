namespace Mundialito.DAL.Players;

public class PlayersRepository : GenericRepository<Player>, IPlayersRepository
{

    public PlayersRepository(MundialitoDbContext context)
        : base(context)
    {

    }

    public void DeletePlayer(int playerId)
    {
        Delete(playerId);
    }

    public Player GetPlayer(int playerId)
    {
        return Context.Players.Where(player => player.PlayerId == playerId).SingleOrDefault();
    }

    public IEnumerable<Player> GetPlayers()
    {
        return Get().OrderBy(Player => Player.Name);
    }

    public Player InsertPlayer(Player player)
    {
        return Insert(player);
    }
}
