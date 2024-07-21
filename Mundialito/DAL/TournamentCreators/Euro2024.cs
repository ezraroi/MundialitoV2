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
        var teams = new List<Team>
        {
            CreateTeam("ALBANIA", "ALB", 2),
            CreateTeam("AUSTRIA", "AUT", 8),
            CreateTeam("BELGIUM", "BEL", 13),
            CreateTeam("CROATIA", "CRO", 56370),
            CreateTeam("CZECHIA", "CZE", 58837),
            CreateTeam("DENMARK", "DEN", 35),
            CreateTeam("ENGLAND", "ENG", 39),
            CreateTeam("FRANCE", "FRA", 43),
            CreateTeam("GEORGIA", "GEO", 57157),
            CreateTeam("GERMANY", "GER", 47),
            CreateTeam("HUNGARY", "HUN", 57),
            CreateTeam("ITALY", "ITA", 66),
            CreateTeam("NETHERLANDS", "NED", 95),
            CreateTeam("POLAND", "POL", 109),
            CreateTeam("PORTUGAL", "POR", 110),
            CreateTeam("ROMANIA", "ROU", 113),
            CreateTeam("SCOTLAND", "SCO", 117),
            CreateTeam("SERBIA", "SRB", 147),
            CreateTeam("SLOVAKIA", "SVK", 58836),
            CreateTeam("SLOVENIA", "SVN", 57163),
            CreateTeam("SPAIN", "ESP", 122),
            CreateTeam("SWITZERLAND", "SUI", 128),
            CreateTeam("TÜRKİYE", "TUR", 135),
            CreateTeam("UKRAINE", "UKR", 57166)
        };
        return teams;
    }

    public List<Game> GetGames(Dictionary<String, Stadium> stadiums, Dictionary<String, Team> teams)
    {
        var games = new List<Game>
        {
            new Game
            {
                HomeTeamId = teams[GetTeamName("GERMANY")].TeamId,
                AwayTeamId = teams[GetTeamName("SCOTLAND")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 14, 22, 0, 0)),
                StadiumId = stadiums["Munich Football Arena"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("HUNGARY")].TeamId,
                AwayTeamId = teams[GetTeamName("Switzerland")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 15, 16, 0, 0)),
                StadiumId = stadiums["Cologne Stadium"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Spain")].TeamId,
                AwayTeamId = teams[GetTeamName("Croatia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 15, 19, 0, 0)),
                StadiumId = stadiums["Olympiastadion"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Italy")].TeamId,
                AwayTeamId = teams[GetTeamName("Albania")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 15, 22, 0, 0)),
                StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Poland")].TeamId,
                AwayTeamId = teams[GetTeamName("Netherlands")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 16, 16, 0, 0)),
                StadiumId = stadiums["Volksparkstadion"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Slovenia")].TeamId,
                AwayTeamId = teams[GetTeamName("Denmark")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 16, 19, 0, 0)),
                StadiumId = stadiums["Stuttgart Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Serbia")].TeamId,
                AwayTeamId = teams[GetTeamName("England")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 16, 22, 0, 0)),
                StadiumId = stadiums["Arena AufSchalke"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Romania")].TeamId,
                AwayTeamId = teams[GetTeamName("Ukraine")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 17, 16, 0, 0)),
                StadiumId = stadiums["Munich Football Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Belgium")].TeamId,
                AwayTeamId = teams[GetTeamName("Slovakia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 17, 19, 0, 0)),
                StadiumId = stadiums["Frankfurt Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Austria")].TeamId,
                AwayTeamId = teams[GetTeamName("France")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 17, 22, 0, 0)),
                StadiumId = stadiums["Düsseldorf Arena"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Türki̇ye")].TeamId,
                AwayTeamId = teams[GetTeamName("Georgia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 18, 19, 0, 0)),
                StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Portugal")].TeamId,
                AwayTeamId = teams[GetTeamName("Czechia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 18, 22, 0, 0)),
                StadiumId = stadiums["Leipzig Stadium"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Croatia")].TeamId,
                AwayTeamId = teams[GetTeamName("Albania")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 19, 16, 0, 0)),
                StadiumId = stadiums["Volksparkstadion"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Germany")].TeamId,
                AwayTeamId = teams[GetTeamName("Hungary")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 19, 19, 0, 0)),
                StadiumId = stadiums["Stuttgart Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Scotland")].TeamId,
                AwayTeamId = teams[GetTeamName("Switzerland")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 19, 22, 0, 0)),
                StadiumId = stadiums["Cologne Stadium"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Slovenia")].TeamId,
                AwayTeamId = teams[GetTeamName("Serbia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 20, 16, 0, 0)),
                StadiumId = stadiums["Munich Football Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Denmark")].TeamId,
                AwayTeamId = teams[GetTeamName("England")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 20, 19, 0, 0)),
                StadiumId = stadiums["Frankfurt Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Spain")].TeamId,
                AwayTeamId = teams[GetTeamName("Italy")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 20, 22, 0, 0)),
                StadiumId = stadiums["Arena AufSchalke"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Slovakia")].TeamId,
                AwayTeamId = teams[GetTeamName("Ukraine")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 21, 16, 0, 0)),
                StadiumId = stadiums["Düsseldorf Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Poland")].TeamId,
                AwayTeamId = teams[GetTeamName("Austria")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 21, 19, 0, 0)),
                StadiumId = stadiums["Olympiastadion"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Netherlands")].TeamId,
                AwayTeamId = teams[GetTeamName("France")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 21, 22, 0, 0)),
                StadiumId = stadiums["Leipzig Stadium"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Georgia")].TeamId,
                AwayTeamId = teams[GetTeamName("Czechia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 22, 16, 0, 0)),
                StadiumId = stadiums["Volksparkstadion"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Türki̇ye")].TeamId,
                AwayTeamId = teams[GetTeamName("Portugal")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 22, 19, 0, 0)),
                StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Belgium")].TeamId,
                AwayTeamId = teams[GetTeamName("Romania")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 22, 22, 0, 0)),
                StadiumId = stadiums["Cologne Stadium"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Switzerland")].TeamId,
                AwayTeamId = teams[GetTeamName("Germany")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 23, 22, 0, 0)),
                StadiumId = stadiums["Frankfurt Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Scotland")].TeamId,
                AwayTeamId = teams[GetTeamName("Hungary")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 23, 22, 0, 0)),
                StadiumId = stadiums["Stuttgart Arena"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Albania")].TeamId,
                AwayTeamId = teams[GetTeamName("Spain")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 24, 22, 0, 0)),
                StadiumId = stadiums["Düsseldorf Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Croatia")].TeamId,
                AwayTeamId = teams[GetTeamName("Italy")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 24, 22, 0, 0)),
                StadiumId = stadiums["Leipzig Stadium"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Netherlands")].TeamId,
                AwayTeamId = teams[GetTeamName("Austria")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 25, 19, 0, 0)),
                StadiumId = stadiums["Olympiastadion"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("France")].TeamId,
                AwayTeamId = teams[GetTeamName("Poland")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 25, 19, 0, 0)),
                StadiumId = stadiums["BVB Stadion Dortmund"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("England")].TeamId,
                AwayTeamId = teams[GetTeamName("Slovenia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 25, 22, 0, 0)),
                StadiumId = stadiums["Cologne Stadium"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Denmark")].TeamId,
                AwayTeamId = teams[GetTeamName("Serbia")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 25, 22, 0, 0)),
                StadiumId = stadiums["Munich Football Arena"].StadiumId,
                Type = GameType.Groups
            },

            /* */

            new Game
            {
                HomeTeamId = teams[GetTeamName("Slovakia")].TeamId,
                AwayTeamId = teams[GetTeamName("Romania")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 26, 19, 0, 0)),
                StadiumId = stadiums["Frankfurt Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Ukraine")].TeamId,
                AwayTeamId = teams[GetTeamName("Belgium")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 26, 19, 0, 0)),
                StadiumId = stadiums["Stuttgart Arena"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Georgia")].TeamId,
                AwayTeamId = teams[GetTeamName("Portugal")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 26, 22, 0, 0)),
                StadiumId = stadiums["Arena AufSchalke"].StadiumId,
                Type = GameType.Groups
            },
            new Game
            {
                HomeTeamId = teams[GetTeamName("Czechia")].TeamId,
                AwayTeamId = teams[GetTeamName("Türki̇ye")].TeamId,
                Date = GetFixedDate(new DateTime(2024, 6, 26, 22, 0, 0)),
                StadiumId = stadiums["Volksparkstadion"].StadiumId,
                Type = GameType.Groups
            }
        };

        /* */
        return games;
    }

    public List<Stadium> GetStadiums()
    {
        var stadiums = new List<Stadium>
        {
            new Stadium() { Name = "Munich Football Arena", Capacity = 66000, City = "Munich" },
            new Stadium() { Name = "Olympiastadion", Capacity = 71000, City = "Berlin" },
            new Stadium() { Name = "BVB Stadion Dortmund", Capacity = 62000, City = "Dortmund" },
            new Stadium() { Name = "Stuttgart Arena", Capacity = 51000, City = "Stuttgart" },
            new Stadium() { Name = "Arena AufSchalke", Capacity = 50000, City = "Gelsenkirchen" },
            new Stadium() { Name = "Volksparkstadion", Capacity = 49000, City = "Hamburg" },
            new Stadium() { Name = "Frankfurt Arena", Capacity = 47000, City = "Frankfurt" },
            new Stadium() { Name = "Düsseldorf Arena", Capacity = 47000, City = "Düsseldorf" },
            new Stadium() { Name = "Cologne Stadium", Capacity = 43000, City = "Cologne" },
            new Stadium() { Name = "Leipzig Stadium", Capacity = 40000, City = "Leipzig" }
        };
        return stadiums;
    }

    public List<Player> GetPlayers()
    {
        var players = new List<Player>
        {
            new Player() { Name = "Kylian Mbappe" },
            new Player() { Name = "Harry Kane" },
            new Player() { Name = "Cristiano Ronaldo" },
            new Player() { Name = "Jude Bellingham" },
            new Player() { Name = "Olivier Giroud" },
            new Player() { Name = "Romelu Lukaku" },
            new Player() { Name = "Antoine Griezmann" },
            new Player() { Name = "Alvaro Morata" },
            new Player() { Name = "Bukayo Saka" },
            new Player() { Name = "Kai Havertz" },
            new Player() { Name = "Phil Foden" },
            new Player() { Name = "Diogo Jota" },
            new Player() { Name = "Rasmus Hojlund" },
            new Player() { Name = "Goncalo Ramos" },
            new Player() { Name = "Niclas Fullkrug" },
            new Player() { Name = "Jamal Musiala" },
            new Player() { Name = "Ciro Immobile" },
            new Player() { Name = "Cody Gakpo" },
            new Player() { Name = "Memphis Depay" },
            new Player() { Name = "Marcus Rashford" },
            new Player() { Name = "Marcus Thuram" },
            new Player() { Name = "Lamine Yamal" },
            new Player() { Name = "Donyell Malen" },
            new Player() { Name = "Leroy Sane" },
            new Player() { Name = "Other" }
        };
        return players;
    }

    private string GetTeamName(string team)
    {
        return Strings.UCase(team);
    }
    private DateTime GetFixedDate(DateTime date)
    {
        // return date.Subtract(TimeSpan.FromHours(3));
        return date.Add(TimeSpan.FromDays(56));
    }

    private Team CreateTeam(String name, String shortName, int tournamentTeamId)
    {
        return new Team()
        {
            Name = name,
            ShortName = shortName,
            Flag = string.Format("https://img.uefa.com/imgml/flags/50x50/{0}.png", shortName),
            Logo = string.Format("https://img.uefa.com/imgml/flags/100x100/{0}.png", shortName),
            TournamentTeamId = tournamentTeamId,
            TeamPage = string.Format("https://www.uefa.com/euro2024/teams/{0}--{1}/", tournamentTeamId.ToString(), name.ToLower())
        };
    }
}

