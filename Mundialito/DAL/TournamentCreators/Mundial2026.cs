using Mundialito.DAL.Games;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Mundialito.DAL.DBCreators;

public class Mundial2026 : ITournamentCreator
{
    public List<Team> GetTeams()
    {
        return new List<Team>
        {
            CreateTeam("ALGERIA", "ALG"),
            CreateTeam("ARGENTINA", "ARG"),
            CreateTeam("AUSTRALIA", "AUS"),
            CreateTeam("AUSTRIA", "AUT"),
            CreateTeam("BELGIUM", "BEL"),
            CreateTeam("BOSNIA AND HERZEGOVINA", "BIH"),
            CreateTeam("BRAZIL", "BRA"),
            CreateTeam("CANADA", "CAN"),
            CreateTeam("CAPE VERDE", "CPV"),
            CreateTeam("COLOMBIA", "COL"),
            CreateTeam("CONGO DR", "COD"),
            CreateTeam("CROATIA", "CRO"),
            CreateTeam("CURACAO", "CUW"),
            CreateTeam("CZECHIA", "CZE"),
            CreateTeam("ECUADOR", "ECU"),
            CreateTeam("EGYPT", "EGY"),
            CreateTeam("ENGLAND", "ENG"),
            CreateTeam("FRANCE", "FRA"),
            CreateTeam("GERMANY", "GER"),
            CreateTeam("GHANA", "GHA"),
            CreateTeam("HAITI", "HAI"),
            CreateTeam("IRAN", "IRN"),
            CreateTeam("IRAQ", "IRQ"),
            CreateTeam("IVORY COAST", "CIV"),
            CreateTeam("JAPAN", "JPN"),
            CreateTeam("JORDAN", "JOR"),
            CreateTeam("KOREA REPUBLIC", "KOR"),
            CreateTeam("MEXICO", "MEX"),
            CreateTeam("MOROCCO", "MAR"),
            CreateTeam("NETHERLANDS", "NED"),
            CreateTeam("NEW ZEALAND", "NZL"),
            CreateTeam("NORWAY", "NOR"),
            CreateTeam("PANAMA", "PAN"),
            CreateTeam("PARAGUAY", "PAR"),
            CreateTeam("PORTUGAL", "POR"),
            CreateTeam("QATAR", "QAT"),
            CreateTeam("SAUDI ARABIA", "KSA"),
            CreateTeam("SENEGAL", "SEN"),
            CreateTeam("SCOTLAND", "SCO"),
            CreateTeam("SOUTH AFRICA", "RSA"),
            CreateTeam("SPAIN", "ESP"),
            CreateTeam("SWEDEN", "SWE"),
            CreateTeam("SWITZERLAND", "SUI"),
            CreateTeam("TUNISIA", "TUN"),
            CreateTeam("TURKIYE", "TUR"),
            CreateTeam("USA", "USA"),
            CreateTeam("URUGUAY", "URU"),
            CreateTeam("UZBEKISTAN", "UZB"),
        };
    }

    public List<Game> GetGames(Dictionary<string, Stadium> stadiums, Dictionary<string, Team> teams)
    {
        return new List<Game>
        {
            GroupGame(teams, stadiums, "MEXICO", "SOUTH AFRICA", IsraelKickoff(2026, 6, 11, 22, 0), "Mexico City Stadium"),
            GroupGame(teams, stadiums, "KOREA REPUBLIC", "CZECHIA", IsraelKickoff(2026, 6, 12, 5, 0), "Estadio Guadalajara"),
            GroupGame(teams, stadiums, "CANADA", "BOSNIA AND HERZEGOVINA", IsraelKickoff(2026, 6, 12, 22, 0), "Toronto Stadium"),
            GroupGame(teams, stadiums, "USA", "PARAGUAY", IsraelKickoff(2026, 6, 13, 4, 0), "Los Angeles Stadium"),
            GroupGame(teams, stadiums, "HAITI", "SCOTLAND", IsraelKickoff(2026, 6, 14, 4, 0), "Boston Stadium"),
            GroupGame(teams, stadiums, "AUSTRALIA", "TURKIYE", IsraelKickoff(2026, 6, 13, 7, 0), "BC Place Vancouver"),
            GroupGame(teams, stadiums, "BRAZIL", "MOROCCO", IsraelKickoff(2026, 6, 14, 1, 0), "New York New Jersey Stadium"),
            GroupGame(teams, stadiums, "QATAR", "SWITZERLAND", IsraelKickoff(2026, 6, 13, 22, 0), "San Francisco Bay Area Stadium"),
            GroupGame(teams, stadiums, "IVORY COAST", "ECUADOR", IsraelKickoff(2026, 6, 15, 2, 0), "Philadelphia Stadium"),
            GroupGame(teams, stadiums, "GERMANY", "CURACAO", IsraelKickoff(2026, 6, 14, 20, 0), "Houston Stadium"),
            GroupGame(teams, stadiums, "NETHERLANDS", "JAPAN", IsraelKickoff(2026, 6, 14, 23, 0), "Dallas Stadium"),
            GroupGame(teams, stadiums, "SWEDEN", "TUNISIA", IsraelKickoff(2026, 6, 15, 5, 0), "Estadio Monterrey"),
            GroupGame(teams, stadiums, "SAUDI ARABIA", "URUGUAY", IsraelKickoff(2026, 6, 16, 1, 0), "Miami Stadium"),
            GroupGame(teams, stadiums, "SPAIN", "CAPE VERDE", IsraelKickoff(2026, 6, 15, 19, 0), "Atlanta Stadium"),
            GroupGame(teams, stadiums, "IRAN", "NEW ZEALAND", IsraelKickoff(2026, 6, 16, 4, 0), "Los Angeles Stadium"),
            GroupGame(teams, stadiums, "BELGIUM", "EGYPT", IsraelKickoff(2026, 6, 15, 22, 0), "Seattle Stadium"),
            GroupGame(teams, stadiums, "FRANCE", "SENEGAL", IsraelKickoff(2026, 6, 16, 22, 0), "New York New Jersey Stadium"),
            GroupGame(teams, stadiums, "IRAQ", "NORWAY", IsraelKickoff(2026, 6, 17, 1, 0), "Boston Stadium"),
            GroupGame(teams, stadiums, "ARGENTINA", "ALGERIA", IsraelKickoff(2026, 6, 17, 4, 0), "Kansas City Stadium"),
            GroupGame(teams, stadiums, "AUSTRIA", "JORDAN", IsraelKickoff(2026, 6, 16, 7, 0), "San Francisco Bay Area Stadium"),
            GroupGame(teams, stadiums, "GHANA", "PANAMA", IsraelKickoff(2026, 6, 18, 2, 0), "Toronto Stadium"),
            GroupGame(teams, stadiums, "ENGLAND", "CROATIA", IsraelKickoff(2026, 6, 17, 23, 0), "Dallas Stadium"),
            GroupGame(teams, stadiums, "PORTUGAL", "CONGO DR", IsraelKickoff(2026, 6, 17, 20, 0), "Houston Stadium"),
            GroupGame(teams, stadiums, "UZBEKISTAN", "COLOMBIA", IsraelKickoff(2026, 6, 18, 5, 0), "Mexico City Stadium"),
            GroupGame(teams, stadiums, "CZECHIA", "SOUTH AFRICA", IsraelKickoff(2026, 6, 18, 19, 0), "Atlanta Stadium"),
            GroupGame(teams, stadiums, "SWITZERLAND", "BOSNIA AND HERZEGOVINA", IsraelKickoff(2026, 6, 18, 22, 0), "Los Angeles Stadium"),
            GroupGame(teams, stadiums, "CANADA", "QATAR", IsraelKickoff(2026, 6, 19, 1, 0), "BC Place Vancouver"),
            GroupGame(teams, stadiums, "MEXICO", "KOREA REPUBLIC", IsraelKickoff(2026, 6, 19, 4, 0), "Estadio Guadalajara"),
            GroupGame(teams, stadiums, "BRAZIL", "HAITI", IsraelKickoff(2026, 6, 20, 3, 30), "Philadelphia Stadium"),
            GroupGame(teams, stadiums, "SCOTLAND", "MOROCCO", IsraelKickoff(2026, 6, 20, 1, 0), "Boston Stadium"),
            GroupGame(teams, stadiums, "TURKIYE", "PARAGUAY", IsraelKickoff(2026, 6, 20, 6, 0), "San Francisco Bay Area Stadium"),
            GroupGame(teams, stadiums, "USA", "AUSTRALIA", IsraelKickoff(2026, 6, 19, 22, 0), "Seattle Stadium"),
            GroupGame(teams, stadiums, "GERMANY", "IVORY COAST", IsraelKickoff(2026, 6, 20, 23, 0), "Toronto Stadium"),
            GroupGame(teams, stadiums, "ECUADOR", "CURACAO", IsraelKickoff(2026, 6, 21, 3, 0), "Kansas City Stadium"),
            GroupGame(teams, stadiums, "NETHERLANDS", "SWEDEN", IsraelKickoff(2026, 6, 20, 20, 0), "Houston Stadium"),
            GroupGame(teams, stadiums, "TUNISIA", "JAPAN", IsraelKickoff(2026, 6, 20, 7, 0), "Estadio Monterrey"),
            GroupGame(teams, stadiums, "URUGUAY", "CAPE VERDE", IsraelKickoff(2026, 6, 22, 1, 0), "Miami Stadium"),
            GroupGame(teams, stadiums, "SPAIN", "SAUDI ARABIA", IsraelKickoff(2026, 6, 21, 19, 0), "Atlanta Stadium"),
            GroupGame(teams, stadiums, "BELGIUM", "IRAN", IsraelKickoff(2026, 6, 21, 22, 0), "Los Angeles Stadium"),
            GroupGame(teams, stadiums, "NEW ZEALAND", "EGYPT", IsraelKickoff(2026, 6, 22, 4, 0), "BC Place Vancouver"),
            GroupGame(teams, stadiums, "NORWAY", "SENEGAL", IsraelKickoff(2026, 6, 23, 3, 0), "New York New Jersey Stadium"),
            GroupGame(teams, stadiums, "FRANCE", "IRAQ", IsraelKickoff(2026, 6, 23, 0, 0), "Philadelphia Stadium"),
            GroupGame(teams, stadiums, "ARGENTINA", "AUSTRIA", IsraelKickoff(2026, 6, 22, 20, 0), "Dallas Stadium"),
            GroupGame(teams, stadiums, "JORDAN", "ALGERIA", IsraelKickoff(2026, 6, 23, 6, 0), "San Francisco Bay Area Stadium"),
            GroupGame(teams, stadiums, "ENGLAND", "GHANA", IsraelKickoff(2026, 6, 23, 23, 0), "Boston Stadium"),
            GroupGame(teams, stadiums, "PANAMA", "CROATIA", IsraelKickoff(2026, 6, 24, 2, 0), "Toronto Stadium"),
            GroupGame(teams, stadiums, "PORTUGAL", "UZBEKISTAN", IsraelKickoff(2026, 6, 23, 20, 0), "Houston Stadium"),
            GroupGame(teams, stadiums, "COLOMBIA", "CONGO DR", IsraelKickoff(2026, 6, 24, 5, 0), "Estadio Guadalajara"),
            GroupGame(teams, stadiums, "SCOTLAND", "BRAZIL", IsraelKickoff(2026, 6, 25, 1, 0), "Miami Stadium"),
            GroupGame(teams, stadiums, "MOROCCO", "HAITI", IsraelKickoff(2026, 6, 25, 1, 0), "Atlanta Stadium"),
            GroupGame(teams, stadiums, "SWITZERLAND", "CANADA", IsraelKickoff(2026, 6, 24, 22, 0), "BC Place Vancouver"),
            GroupGame(teams, stadiums, "BOSNIA AND HERZEGOVINA", "QATAR", IsraelKickoff(2026, 6, 24, 22, 0), "Seattle Stadium"),
            GroupGame(teams, stadiums, "CZECHIA", "MEXICO", IsraelKickoff(2026, 6, 25, 4, 0), "Mexico City Stadium"),
            GroupGame(teams, stadiums, "SOUTH AFRICA", "KOREA REPUBLIC", IsraelKickoff(2026, 6, 25, 4, 0), "Estadio Monterrey"),
            GroupGame(teams, stadiums, "CURACAO", "IVORY COAST", IsraelKickoff(2026, 6, 25, 23, 0), "Philadelphia Stadium"),
            GroupGame(teams, stadiums, "ECUADOR", "GERMANY", IsraelKickoff(2026, 6, 25, 23, 0), "New York New Jersey Stadium"),
            GroupGame(teams, stadiums, "JAPAN", "SWEDEN", IsraelKickoff(2026, 6, 26, 2, 0), "Dallas Stadium"),
            GroupGame(teams, stadiums, "TUNISIA", "NETHERLANDS", IsraelKickoff(2026, 6, 26, 2, 0), "Kansas City Stadium"),
            GroupGame(teams, stadiums, "TURKIYE", "USA", IsraelKickoff(2026, 6, 26, 5, 0), "Los Angeles Stadium"),
            GroupGame(teams, stadiums, "PARAGUAY", "AUSTRALIA", IsraelKickoff(2026, 6, 26, 5, 0), "San Francisco Bay Area Stadium"),
            GroupGame(teams, stadiums, "NORWAY", "FRANCE", IsraelKickoff(2026, 6, 26, 22, 0), "Boston Stadium"),
            GroupGame(teams, stadiums, "SENEGAL", "IRAQ", IsraelKickoff(2026, 6, 26, 22, 0), "Toronto Stadium"),
            GroupGame(teams, stadiums, "EGYPT", "IRAN", IsraelKickoff(2026, 6, 27, 6, 0), "Seattle Stadium"),
            GroupGame(teams, stadiums, "NEW ZEALAND", "BELGIUM", IsraelKickoff(2026, 6, 27, 6, 0), "BC Place Vancouver"),
            GroupGame(teams, stadiums, "CAPE VERDE", "SAUDI ARABIA", IsraelKickoff(2026, 6, 27, 3, 0), "Houston Stadium"),
            GroupGame(teams, stadiums, "URUGUAY", "SPAIN", IsraelKickoff(2026, 6, 27, 3, 0), "Estadio Guadalajara"),
            GroupGame(teams, stadiums, "PANAMA", "ENGLAND", IsraelKickoff(2026, 6, 28, 0, 0), "New York New Jersey Stadium"),
            GroupGame(teams, stadiums, "CROATIA", "GHANA", IsraelKickoff(2026, 6, 28, 0, 0), "Philadelphia Stadium"),
            GroupGame(teams, stadiums, "ALGERIA", "AUSTRIA", IsraelKickoff(2026, 6, 28, 5, 0), "Kansas City Stadium"),
            GroupGame(teams, stadiums, "JORDAN", "ARGENTINA", IsraelKickoff(2026, 6, 28, 5, 0), "Dallas Stadium"),
            GroupGame(teams, stadiums, "COLOMBIA", "PORTUGAL", IsraelKickoff(2026, 6, 28, 2, 30), "Miami Stadium"),
            GroupGame(teams, stadiums, "CONGO DR", "UZBEKISTAN", IsraelKickoff(2026, 6, 28, 2, 30), "Atlanta Stadium"),

        };
    }

    public List<Stadium> GetStadiums()
    {
        return new List<Stadium>
        {
            new Stadium { Name = "Mexico City Stadium", Capacity = 87500, City = "Mexico City" },
            new Stadium { Name = "Estadio Guadalajara", Capacity = 48300, City = "Guadalajara" },
            new Stadium { Name = "Estadio Monterrey", Capacity = 53500, City = "Monterrey" },
            new Stadium { Name = "Toronto Stadium", Capacity = 45736, City = "Toronto" },
            new Stadium { Name = "BC Place Vancouver", Capacity = 54500, City = "Vancouver" },
            new Stadium { Name = "Los Angeles Stadium", Capacity = 70240, City = "Inglewood" },
            new Stadium { Name = "San Francisco Bay Area Stadium", Capacity = 71500, City = "Santa Clara" },
            new Stadium { Name = "Seattle Stadium", Capacity = 69000, City = "Seattle" },
            new Stadium { Name = "Boston Stadium", Capacity = 65878, City = "Foxborough" },
            new Stadium { Name = "New York New Jersey Stadium", Capacity = 82500, City = "East Rutherford" },
            new Stadium { Name = "Philadelphia Stadium", Capacity = 69796, City = "Philadelphia" },
            new Stadium { Name = "Houston Stadium", Capacity = 62000, City = "Houston" },
            new Stadium { Name = "Dallas Stadium", Capacity = 80000, City = "Arlington" },
            new Stadium { Name = "Kansas City Stadium", Capacity = 76416, City = "Kansas City" },
            new Stadium { Name = "Miami Stadium", Capacity = 65326, City = "Miami Gardens" },
            new Stadium { Name = "Atlanta Stadium", Capacity = 71000, City = "Atlanta" },
        };
    }

    public List<Player> GetPlayers()
    {
        return new List<Player>
        {
            new Player { Name = "Kylian Mbappe" },
            new Player { Name = "Harry Kane" },
            new Player { Name = "Lionel Messi" },
            new Player { Name = "Erling Haaland" },
            new Player { Name = "Lamine Yamal" },
            new Player { Name = "Cristiano Ronaldo" },
            new Player { Name = "Ousmane Dembele" },
            new Player { Name = "Lautaro Martinez" },
            new Player { Name = "Vinicius Junior" },
            new Player { Name = "Julian Alvarez" },
            new Player { Name = "Antoine Griezmann" },
            new Player { Name = "Robert Lewandowski" },
            new Player { Name = "Jude Bellingham" },
            new Player { Name = "Florian Wirtz" },
            new Player { Name = "Mohamed Salah" },
            new Player { Name = "Victor Osimhen" },
            new Player { Name = "Raphinha" },
            new Player { Name = "Marcus Rashford" },
            new Player { Name = "Cole Palmer" },
            new Player { Name = "Other" },
        };
    }

    /// <summary>Kickoff in Israel local time (UTC+3). Values are pre-converted from the official FIFA schedule (US Eastern).</summary>
    private static DateTime IsraelKickoff(int year, int month, int day, int hour, int minute = 0) =>
        new DateTime(year, month, day, hour, minute, 0);

    private static Game GroupGame(
        Dictionary<string, Team> teams,
        Dictionary<string, Stadium> stadiums,
        string home,
        string away,
        DateTime kickoff,
        string stadiumName) =>
        new Game
        {
            HomeTeamId = teams[home].TeamId,
            AwayTeamId = teams[away].TeamId,
            Date = kickoff,
            StadiumId = stadiums[stadiumName].StadiumId,
            Type = GameType.Groups,
        };

    private static Team CreateTeam(string name, string shortName) =>
        new Team
        {
            Name = name,
            ShortName = shortName,
            Flag = string.Format("https://api.fifa.com/api/v3/picture/flags-sq-4/{0}", shortName),
            Logo = string.Format("https://api.fifa.com/api/v3/picture/flags-sq-4/{0}", shortName),
            TeamPage = string.Empty,
        };
}
