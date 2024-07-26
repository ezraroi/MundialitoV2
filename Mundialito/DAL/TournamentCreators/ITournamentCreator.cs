using Mundialito.DAL.Games;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Mundialito.DAL.DBCreators;

public interface ITournamentCreator
{
    List<Team> GetTeams();
    List<Game> GetGames(Dictionary<String, Stadium> stadiums, Dictionary<string, Team> teams);
    List<Stadium> GetStadiums();
    List<Player> GetPlayers();
}

