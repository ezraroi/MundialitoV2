using Microsoft.VisualBasic;
using Mundialito.DAL.Games;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Mundialito.DAL.DBCreators;
public class Euro2024 : ITournamentCreator
{
    public List<Team> GetTeams()
    {
        var teams = new List<Team>();
        teams.Add(CreateTeam("ALBANIA", "ALB"));
        teams.Add(CreateTeam("AUSTRIA", "AUT"));
        teams.Add(CreateTeam("BELGIUM", "BEL"));
        teams.Add(CreateTeam("CROATIA", "CRO"));
        teams.Add(CreateTeam("CZECHIA", "CZE"));
        teams.Add(CreateTeam("DENMARK", "DEN"));
        teams.Add(CreateTeam("ENGLAND", "ENG"));
        teams.Add(CreateTeam("FRANCE", "FRA"));
        teams.Add(CreateTeam("GEORGIA", "GEO"));
        teams.Add(CreateTeam("GERMANY", "GER"));
        teams.Add(CreateTeam("HUNGARY", "HUN"));
        teams.Add(CreateTeam("ITALY", "ITA"));
        teams.Add(CreateTeam("NETHERLANDS", "NED"));
        teams.Add(CreateTeam("POLAND", "POL"));
        teams.Add(CreateTeam("PORTUGAL", "POR"));
        teams.Add(CreateTeam("ROMANIA", "ROU"));
        teams.Add(CreateTeam("SCOTLAND", "SCO"));
        teams.Add(CreateTeam("SERBIA", "SRB"));
        teams.Add(CreateTeam("SLOVAKIA", "SVK"));
        teams.Add(CreateTeam("SLOVENIA", "SVN"));
        teams.Add(CreateTeam("SPAIN", "ESP"));
        teams.Add(CreateTeam("SWITZERLAND", "SUI"));
        teams.Add(CreateTeam("TÜRKİYE", "TUR"));
        teams.Add(CreateTeam("UKRAINE", "UKR"));
        return teams;
    }

    public List<Game> GetGames(Dictionary<String, Stadium> stadiums, Dictionary<String, Team> teams)
    {
        var games = new List<Game>();
        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("GERMANY")].TeamId,
            AwayTeamId = teams[GetTeamName("SCOTLAND")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 14, 22, 0, 0)),
            StadiumId = stadiums["Munich Football Arena"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("HUNGARY")].TeamId,
            AwayTeamId = teams[GetTeamName("Switzerland")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 15, 16, 0, 0)),
            StadiumId = stadiums["Cologne Stadium"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Spain")].TeamId,
            AwayTeamId = teams[GetTeamName("Croatia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 15, 19, 0, 0)),
            StadiumId = stadiums["Olympiastadion"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Italy")].TeamId,
            AwayTeamId = teams[GetTeamName("Albania")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 15, 22, 0, 0)),
            StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Poland")].TeamId,
            AwayTeamId = teams[GetTeamName("Netherlands")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 16, 16, 0, 0)),
            StadiumId = stadiums["Volksparkstadion"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Slovenia")].TeamId,
            AwayTeamId = teams[GetTeamName("Denmark")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 16, 19, 0, 0)),
            StadiumId = stadiums["Stuttgart Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Serbia")].TeamId,
            AwayTeamId = teams[GetTeamName("England")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 16, 22, 0, 0)),
            StadiumId = stadiums["Arena AufSchalke"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Romania")].TeamId,
            AwayTeamId = teams[GetTeamName("Ukraine")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 17, 16, 0, 0)),
            StadiumId = stadiums["Munich Football Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Belgium")].TeamId,
            AwayTeamId = teams[GetTeamName("Slovakia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 17, 19, 0, 0)),
            StadiumId = stadiums["Frankfurt Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Austria")].TeamId,
            AwayTeamId = teams[GetTeamName("France")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 17, 22, 0, 0)),
            StadiumId = stadiums["Düsseldorf Arena"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Türki̇ye")].TeamId,
            AwayTeamId = teams[GetTeamName("Georgia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 18, 19, 0, 0)),
            StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Portugal")].TeamId,
            AwayTeamId = teams[GetTeamName("Czechia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 18, 22, 0, 0)),
            StadiumId = stadiums["Leipzig Stadium"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Croatia")].TeamId,
            AwayTeamId = teams[GetTeamName("Albania")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 19, 16, 0, 0)),
            StadiumId = stadiums["Volksparkstadion"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Germany")].TeamId,
            AwayTeamId = teams[GetTeamName("Hungary")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 19, 19, 0, 0)),
            StadiumId = stadiums["Stuttgart Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Scotland")].TeamId,
            AwayTeamId = teams[GetTeamName("Switzerland")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 19, 22, 0, 0)),
            StadiumId = stadiums["Cologne Stadium"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Slovenia")].TeamId,
            AwayTeamId = teams[GetTeamName("Serbia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 20, 16, 0, 0)),
            StadiumId = stadiums["Munich Football Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Denmark")].TeamId,
            AwayTeamId = teams[GetTeamName("England")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 20, 19, 0, 0)),
            StadiumId = stadiums["Frankfurt Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Spain")].TeamId,
            AwayTeamId = teams[GetTeamName("Italy")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 20, 22, 0, 0)),
            StadiumId = stadiums["Arena AufSchalke"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Slovakia")].TeamId,
            AwayTeamId = teams[GetTeamName("Ukraine")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 21, 16, 0, 0)),
            StadiumId = stadiums["Düsseldorf Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Poland")].TeamId,
            AwayTeamId = teams[GetTeamName("Austria")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 21, 19, 0, 0)),
            StadiumId = stadiums["Olympiastadion"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Netherlands")].TeamId,
            AwayTeamId = teams[GetTeamName("France")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 21, 22, 0, 0)),
            StadiumId = stadiums["Leipzig Stadium"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Georgia")].TeamId,
            AwayTeamId = teams[GetTeamName("Czechia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 22, 16, 0, 0)),
            StadiumId = stadiums["Volksparkstadion"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Türki̇ye")].TeamId,
            AwayTeamId = teams[GetTeamName("Portugal")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 22, 19, 0, 0)),
            StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Belgium")].TeamId,
            AwayTeamId = teams[GetTeamName("Romania")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 22, 22, 0, 0)),
            StadiumId = stadiums["Cologne Stadium"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Switzerland")].TeamId,
            AwayTeamId = teams[GetTeamName("Germany")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 23, 22, 0, 0)),
            StadiumId = stadiums["Frankfurt Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Scotland")].TeamId,
            AwayTeamId = teams[GetTeamName("Hungary")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 23, 22, 0, 0)),
            StadiumId = stadiums["Stuttgart Arena"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Albania")].TeamId,
            AwayTeamId = teams[GetTeamName("Spain")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 24, 22, 0, 0)),
            StadiumId = stadiums["Düsseldorf Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Croatia")].TeamId,
            AwayTeamId = teams[GetTeamName("Italy")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 24, 22, 0, 0)),
            StadiumId = stadiums["Leipzig Stadium"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Netherlands")].TeamId,
            AwayTeamId = teams[GetTeamName("Austria")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 25, 19, 0, 0)),
            StadiumId = stadiums["Olympiastadion"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("France")].TeamId,
            AwayTeamId = teams[GetTeamName("Poland")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 25, 19, 0, 0)),
            StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("England")].TeamId,
            AwayTeamId = teams[GetTeamName("Slovenia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 25, 22, 0, 0)),
            StadiumId = stadiums["Cologne Stadium"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Denmark")].TeamId,
            AwayTeamId = teams[GetTeamName("Serbia")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 25, 22, 0, 0)),
            StadiumId = stadiums["Munich Football Arena"].StadiumId
        });

        /* */

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Slovakia")].TeamId,
            AwayTeamId = teams[GetTeamName("Romania")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 26, 19, 0, 0)),
            StadiumId = stadiums["Frankfurt Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Ukraine")].TeamId,
            AwayTeamId = teams[GetTeamName("Belgium")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 26, 19, 0, 0)),
            StadiumId = stadiums["Stuttgart Arena"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Georgia")].TeamId,
            AwayTeamId = teams[GetTeamName("Portugal")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 26, 22, 0, 0)),
            StadiumId = stadiums["Arena AufSchalke"].StadiumId
        });

        games.Add(new Game
        {
            HomeTeamId = teams[GetTeamName("Czechia")].TeamId,
            AwayTeamId = teams[GetTeamName("Türki̇ye")].TeamId,
            Date = GetFixedDate(new DateTime(2024, 6, 26, 22, 0, 0)),
            StadiumId = stadiums["Volksparkstadion"].StadiumId
        });

        /* */
        return games;
    }

    public List<Stadium> GetStadiums()
    {
        var stadiums = new List<Stadium>();
        stadiums.Add(new Stadium() { Name = "Munich Football Arena", Capacity = 66000, City = "Munich" });
        stadiums.Add(new Stadium() { Name = "Olympiastadion", Capacity = 71000, City = "Berlin" });
        stadiums.Add(new Stadium() { Name = "BVB Stadion Dortmund", Capacity = 62000, City = "Dortmund" });
        stadiums.Add(new Stadium() { Name = "Stuttgart Arena", Capacity = 51000, City = "Stuttgart" });
        stadiums.Add(new Stadium() { Name = "Arena AufSchalke", Capacity = 50000, City = "Gelsenkirchen" });
        stadiums.Add(new Stadium() { Name = "Volksparkstadion", Capacity = 49000, City = "Hamburg" });
        stadiums.Add(new Stadium() { Name = "Frankfurt Arena", Capacity = 47000, City = "Frankfurt" });
        stadiums.Add(new Stadium() { Name = "Düsseldorf Arena", Capacity = 47000, City = "Düsseldorf" });
        stadiums.Add(new Stadium() { Name = "Cologne Stadium", Capacity = 43000, City = "Cologne" });
        stadiums.Add(new Stadium() { Name = "Leipzig Stadium", Capacity = 40000, City = "Leipzig" });
        return stadiums;
    }

    public List<Player> GetPlayers()
    {
        var players = new List<Player>();
        players.Add(new Player() { Name = "Kylian Mbappe" });
        players.Add(new Player() { Name = "Harry Kane" });
        players.Add(new Player() { Name = "Cristiano Ronaldo" });
        players.Add(new Player() { Name = "Jude Bellingham" });
        players.Add(new Player() { Name = "Olivier Giroud" });
        players.Add(new Player() { Name = "Romelu Lukaku" });
        players.Add(new Player() { Name = "Antoine Griezmann" });
        players.Add(new Player() { Name = "Alvaro Morata" });
        players.Add(new Player() { Name = "Bukayo Saka" });
        players.Add(new Player() { Name = "Kai Havertz" });
        players.Add(new Player() { Name = "Phil Foden" });
        players.Add(new Player() { Name = "Diogo Jota" });
        players.Add(new Player() { Name = "Rasmus Hojlund" });
        players.Add(new Player() { Name = "Goncalo Ramos" });
        players.Add(new Player() { Name = "Niclas Fullkrug" });
        players.Add(new Player() { Name = "Jamal Musiala" });
        players.Add(new Player() { Name = "Ciro Immobile" });
        players.Add(new Player() { Name = "Cody Gakpo" });
        players.Add(new Player() { Name = "Memphis Depay" });
        players.Add(new Player() { Name = "Marcus Rashford" });
        players.Add(new Player() { Name = "Marcus Thuram" });
        players.Add(new Player() { Name = "Lamine Yamal" });
        players.Add(new Player() { Name = "Donyell Malen" });
        players.Add(new Player() { Name = "Leroy Sane" });
        players.Add(new Player() { Name = "Other" });
        return players;
    }

    private string GetTeamName(string team) {
        return Strings.UCase(team);
    }
    private DateTime GetFixedDate(DateTime date)
    {
        return date;
        //  return date.Subtract(TimeSpan.FromDays(56));
        // return date.Se(65);
    }

    private Team CreateTeam(String name, String shortName)
    {
        return new Team() { Name = name, ShortName = shortName, Flag = string.Format("https://img.uefa.com/imgml/flags/50x50/{0}.png", shortName), Logo = string.Format("https://img.uefa.com/imgml/flags/100x100/{0}.png", shortName) };
    }
}

