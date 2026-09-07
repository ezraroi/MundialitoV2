using Mundialito.DAL.Games;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Mundialito.DAL.DBCreators;

/// <summary>
/// Local development fixtures, generated relative to the moment of seeding.
///
/// Every other creator hard-codes a real tournament's dates, so once that tournament starts
/// there is no game left open for betting and the bet flows cannot be exercised locally at
/// all - which is why running the app from the committed appsettings.Development.json is not
/// enough to test a bet. This one always produces a usable mix: three games already played,
/// one about to close, and four comfortably open.
///
/// Selected only through App:TournamentDBCreatorName, which scripts/dev.sh sets to
/// "LocalDev". It is never referenced by name anywhere else, so it cannot be reached in
/// production.
///
/// Note that the dates are fixed at seed time, not evaluated per request: seeding is gated on
/// Teams.Count() == 0, so once these games age out you need `scripts/dev.sh reset`.
/// </summary>
public class LocalDev : ITournamentCreator
{
    private const string Stadium = "Local Ground";

    /// <summary>Seed-time "now", rounded to the minute so the fixtures read cleanly.</summary>
    private static readonly DateTime Now =
        new DateTime(DateTime.UtcNow.Ticks - (DateTime.UtcNow.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);

    public List<Team> GetTeams()
    {
        return new List<Team>
        {
            CreateTeam("ARSENAL", "ARS", "52280"),
            CreateTeam("BARCELONA", "BAR", "50080"),
            CreateTeam("BAYERN MUNICH", "BAY", "50037"),
            CreateTeam("INTER", "INT", "50138"),
            CreateTeam("LIVERPOOL", "LIV", "7889"),
            CreateTeam("MANCHESTER CITY", "MCI", "52919"),
            CreateTeam("PARIS SAINT-GERMAIN", "PSG", "52747"),
            CreateTeam("REAL MADRID", "RMA", "50051"),
        };
    }

    public List<Stadium> GetStadiums()
    {
        return new List<Stadium>
        {
            new Stadium { Name = Stadium, Capacity = 25000, City = "Localhost" },
        };
    }

    public List<Player> GetPlayers()
    {
        return new List<Player>
        {
            new Player { Name = "Kylian Mbappe" },
            new Player { Name = "Erling Haaland" },
            new Player { Name = "Lamine Yamal" },
            new Player { Name = "Harry Kane" },
            new Player { Name = "Mohamed Salah" },
            new Player { Name = "Other" },
        };
    }

    public List<Game> GetGames(Dictionary<string, Stadium> stadiums, Dictionary<string, Team> teams)
    {
        return new List<Game>
        {
            // Closed - betting is over, results can be entered and bets resolved.
            Game(teams, stadiums, "ARSENAL", "BARCELONA", Now.AddDays(-3)),
            Game(teams, stadiums, "BAYERN MUNICH", "INTER", Now.AddDays(-2)),
            Game(teams, stadiums, "LIVERPOOL", "MANCHESTER CITY", Now.AddDays(-1)),

            // Closes in 5 minutes (CloseTime is kickoff minus 15), for testing the deadline.
            Game(teams, stadiums, "PARIS SAINT-GERMAIN", "REAL MADRID", Now.AddMinutes(20)),

            // Comfortably open for betting.
            Game(teams, stadiums, "BARCELONA", "BAYERN MUNICH", Now.AddHours(3)),
            Game(teams, stadiums, "INTER", "LIVERPOOL", Now.AddDays(1)),
            Game(teams, stadiums, "MANCHESTER CITY", "PARIS SAINT-GERMAIN", Now.AddDays(2)),
            Game(teams, stadiums, "REAL MADRID", "ARSENAL", Now.AddDays(3)),
        };
    }

    private static Game Game(Dictionary<string, Team> teams, Dictionary<string, Stadium> stadiums, string home, string away, DateTime kickoff) =>
        new Game
        {
            HomeTeamId = teams[home].TeamId,
            AwayTeamId = teams[away].TeamId,
            Date = kickoff,
            StadiumId = stadiums[Stadium].StadiumId,
            Type = GameType.Groups,
        };

    /// <summary>Logos come from UEFA's CDN so the UI renders realistically. Lower-case path on
    /// purpose - the client lower-cases Team.Flag/Logo (Client/src/Teams/Team.js).</summary>
    private static Team CreateTeam(string name, string shortName, string uefaId) =>
        new Team
        {
            Name = name,
            ShortName = shortName,
            Flag = string.Format("https://img.uefa.com/imgml/tp/teams/logos/70x70/{0}.png", uefaId),
            Logo = string.Format("https://img.uefa.com/imgml/tp/teams/logos/240x240/{0}.png", uefaId),
        };
}
