using Mundialito.DAL.Games;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using Mundialito.Logic;

namespace Mundialito.DAL.DBCreators;

/// <summary>
/// UEFA Champions League 2026/27 league phase. Seeds the 36 clubs and matchdays 1-2 only;
/// matchdays 3-8 are added later through the admin UI (POST /api/games).
/// All games share a single placeholder stadium.
/// Fixtures generated from UEFA's match API:
/// https://match.uefa.com/v5/matches?competitionId=1&amp;seasonYear=2027&amp;phase=TOURNAMENT&amp;offset=0&amp;limit=50
/// (paged; 144 matches, matchday in matchday.sequenceNumber, kick-off in kickOffTime.dateTime as UTC).
/// </summary>
public class ChampionsLeague2027 : ITournamentCreator
{
    private const string Stadium = "Europe";

    public List<Team> GetTeams()
    {
        return new List<Team>
        {
            CreateTeam("AEK ATHENS", "AEK", "50129"),
            CreateTeam("ARSENAL", "ARS", "52280"),
            CreateTeam("ASTON VILLA", "AVL", "52683"),
            CreateTeam("ATLETICO MADRID", "ATM", "50124"),
            CreateTeam("BARCELONA", "BAR", "50080"),
            CreateTeam("BAYERN MUNICH", "BAY", "50037"),
            CreateTeam("BODO/GLIMT", "BOD", "59333"),
            CreateTeam("BORUSSIA DORTMUND", "BVB", "52758"),
            CreateTeam("CLUB BRUGGE", "BRU", "50043"),
            CreateTeam("COMO", "COM", "79946"),
            CreateTeam("FENERBAHCE", "FEN", "52692"),
            CreateTeam("FEYENOORD", "FEY", "52749"),
            CreateTeam("GALATASARAY", "GAL", "50067"),
            CreateTeam("INTER", "INT", "50138"),
            CreateTeam("LASK", "LAS", "63405"),
            CreateTeam("LEIPZIG", "RBL", "2603790"),
            CreateTeam("LENS", "LEN", "52277"),
            CreateTeam("LILLE", "LIL", "75797"),
            CreateTeam("LIVERPOOL", "LIV", "7889"),
            CreateTeam("MANCHESTER CITY", "MCI", "52919"),
            CreateTeam("MANCHESTER UNITED", "MUN", "52682"),
            CreateTeam("NAPOLI", "NAP", "50136"),
            CreateTeam("PARIS SAINT-GERMAIN", "PSG", "52747"),
            CreateTeam("PORTO", "POR", "50064"),
            CreateTeam("PSV EINDHOVEN", "PSV", "50062"),
            CreateTeam("REAL BETIS", "BET", "52265"),
            CreateTeam("REAL MADRID", "RMA", "50051"),
            CreateTeam("ROMA", "ROM", "50137"),
            CreateTeam("SABAH", "SAB", "2609356"),
            CreateTeam("SHAKHTAR DONETSK", "SHK", "52707"),
            CreateTeam("SLAVIA PRAHA", "SLA", "52498"),
            CreateTeam("SLOVAN BRATISLAVA", "SBR", "52797"),
            CreateTeam("SPORTING CP", "SPO", "50149"),
            CreateTeam("STUTTGART", "STU", "50107"),
            CreateTeam("VIKING", "VFK", "52319"),
            CreateTeam("VILLARREAL", "VIL", "70691"),
        };
    }

    public List<Game> GetGames(Dictionary<string, Stadium> stadiums, Dictionary<string, Team> teams)
    {
        return new List<Game>
        {
            // ---- Matchday 1 ----
            LeagueGame(teams, stadiums, "AEK ATHENS", "LASK", IsraelKickoff(2026, 9, 8, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "CLUB BRUGGE", "ASTON VILLA", IsraelKickoff(2026, 9, 8, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "BORUSSIA DORTMUND", "VILLARREAL", IsraelKickoff(2026, 9, 8, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "LILLE", "REAL BETIS", IsraelKickoff(2026, 9, 8, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "PORTO", "MANCHESTER CITY", IsraelKickoff(2026, 9, 8, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "REAL MADRID", "INTER", IsraelKickoff(2026, 9, 8, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "BARCELONA", "FEYENOORD", IsraelKickoff(2026, 9, 9, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "STUTTGART", "VIKING", IsraelKickoff(2026, 9, 9, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "LIVERPOOL", "ATLETICO MADRID", IsraelKickoff(2026, 9, 9, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "NAPOLI", "ARSENAL", IsraelKickoff(2026, 9, 9, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "PARIS SAINT-GERMAIN", "SLOVAN BRATISLAVA", IsraelKickoff(2026, 9, 9, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "SPORTING CP", "GALATASARAY", IsraelKickoff(2026, 9, 9, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "FENERBAHCE", "ROMA", IsraelKickoff(2026, 9, 10, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "PSV EINDHOVEN", "SHAKHTAR DONETSK", IsraelKickoff(2026, 9, 10, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "BAYERN MUNICH", "BODO/GLIMT", IsraelKickoff(2026, 9, 10, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "COMO", "LEIPZIG", IsraelKickoff(2026, 9, 10, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "MANCHESTER UNITED", "SABAH", IsraelKickoff(2026, 9, 10, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "SLAVIA PRAHA", "LENS", IsraelKickoff(2026, 9, 10, 22, 0), Stadium),

            // ---- Matchday 2 ----
            LeagueGame(teams, stadiums, "LENS", "SPORTING CP", IsraelKickoff(2026, 10, 13, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "SABAH", "SLAVIA PRAHA", IsraelKickoff(2026, 10, 13, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "ARSENAL", "LILLE", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "ATLETICO MADRID", "MANCHESTER UNITED", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "GALATASARAY", "BARCELONA", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "INTER", "CLUB BRUGGE", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "LEIPZIG", "PSV EINDHOVEN", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "VIKING", "BAYERN MUNICH", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "VILLARREAL", "NAPOLI", IsraelKickoff(2026, 10, 13, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "FEYENOORD", "COMO", IsraelKickoff(2026, 10, 14, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "LASK", "LIVERPOOL", IsraelKickoff(2026, 10, 14, 19, 45), Stadium),
            LeagueGame(teams, stadiums, "ASTON VILLA", "FENERBAHCE", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "BODO/GLIMT", "BORUSSIA DORTMUND", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "MANCHESTER CITY", "PARIS SAINT-GERMAIN", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "REAL BETIS", "PORTO", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "ROMA", "REAL MADRID", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "SHAKHTAR DONETSK", "AEK ATHENS", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
            LeagueGame(teams, stadiums, "SLOVAN BRATISLAVA", "STUTTGART", IsraelKickoff(2026, 10, 14, 22, 0), Stadium),
        };
    }

    public List<Stadium> GetStadiums()
    {
        return new List<Stadium>
        {
            new Stadium { Name = Stadium, Capacity = 50000, City = "Europe" },
        };
    }

    public List<Player> GetPlayers()
    {
        return new List<Player>
        {
            new Player { Name = "Kylian Mbappe" },
            new Player { Name = "Harry Kane" },
            new Player { Name = "Erling Haaland" },
            new Player { Name = "Ousmane Dembele" },
            new Player { Name = "Jude Bellingham" },
            new Player { Name = "Raphinha" },
            new Player { Name = "Vinicius Junior" },
            new Player { Name = "Lamine Yamal" },
            new Player { Name = "Khvicha Kvaratskhelia" },
            new Player { Name = "Julian Alvarez" },
            new Player { Name = "Lautaro Martinez" },
            new Player { Name = "Kai Havertz" },
            new Player { Name = "Victor Osimhen" },
            new Player { Name = "Viktor Gyokeres" },
            new Player { Name = "Ferran Torres" },
            new Player { Name = "Marcus Rashford" },
            new Player { Name = "Desire Doue" },
            new Player { Name = "Mohamed Salah" },
            new Player { Name = "Alexander Isak" },
            new Player { Name = "Other" },
        };
    }

    /// <summary>Israel local kickoff from the UEFA schedule (18:45 CEST = 19:45, 21:00 CEST = 22:00), stored as UTC.</summary>
    private static DateTime IsraelKickoff(int year, int month, int day, int hour, int minute = 0) =>
        GameDateTime.FromIsraelLocal(year, month, day, hour, minute);

    private static Game LeagueGame(
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

    /// <summary>uefaId is the club id in UEFA's media CDN. Path is lower-case on purpose: the
    /// client lower-cases Team.Flag/Logo (Client/src/Teams/Team.js), which would break "/TP/".</summary>
    private static Team CreateTeam(string name, string shortName, string uefaId) =>
        new Team
        {
            Name = name,
            ShortName = shortName,
            Flag = string.Format("https://img.uefa.com/imgml/tp/teams/logos/70x70/{0}.png", uefaId),
            Logo = string.Format("https://img.uefa.com/imgml/tp/teams/logos/240x240/{0}.png", uefaId),
        };
}
