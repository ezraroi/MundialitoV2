namespace Mundialito.DAL.Games;

public interface IGamesRepository : IDisposable
{
    IEnumerable<Game> GetGames();
    Game GetGame(int gameId);

    /// <summary>Detached from the change tracker, for callers that mutate a game only to
    /// compute something and must not be able to persist it - see GamesController.SimulateGame.</summary>
    Game GetGameNoTracking(int gameId);
    Game InsertGame(Game game);
    void DeleteGame(int gameId);
    void UpdateGame(Game game);
    IEnumerable<Game> GetStadiumGames(int id);
    void Save();
    
}