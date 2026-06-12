using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Bets;
using Mundialito.DAL.DBCreators;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using Mundialito.Logic;

namespace Mundialito.DAL;

public class DatabaseInitilaizer
{

    private static Dictionary<string, Stadium> stadiumsDic = new Dictionary<string, Stadium>();
    private static Dictionary<string, Team> teamsDic = new Dictionary<string, Team>();
    private static Dictionary<string, Player> playersDic = new Dictionary<string, Player>();

    public static void Seed(IApplicationBuilder applicationBuilder)
    {
        using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
        {
            var logger = applicationBuilder.ApplicationServices.GetService<ILogger<DatabaseInitilaizer>>();
            var context = serviceScope.ServiceProvider.GetService<MundialitoDbContext>();
            var userManager = serviceScope.ServiceProvider.GetService<UserManager<MundialitoUser>>();
            var config = serviceScope.ServiceProvider.GetService<IOptions<Config>>().Value;
            if (context.Users.Count() == 0)
            {
                logger.LogInformation("No users found. will populate the database");
                CreateFirstUsers(config, userManager, logger);
            }

            EnsureMonkeyUser(context, config, userManager, logger);

            if (context.Teams.Count() == 0)
            {
                SeedTournamentData(context, config, userManager, logger);
            }

            EnsureMonkeyBets(context, config, logger);
        }
    }

    private static void SeedTournamentData(
        MundialitoDbContext context,
        Config config,
        UserManager<MundialitoUser> userManager,
        ILogger<DatabaseInitilaizer> logger)
    {
        if (string.IsNullOrEmpty(config.TournamentDBCreatorName))
        {
            logger.LogWarning("Tournament seed skipped: App:TournamentDBCreatorName is not set");
            return;
        }

        var typeName = "Mundialito.DAL.DBCreators." + config.TournamentDBCreatorName;
        var creatorType = typeof(ITournamentCreator).Assembly.GetType(typeName);
        if (creatorType == null)
        {
            logger.LogError("Tournament creator type not found: {TypeName}", typeName);
            return;
        }

        var tournamentCreator = Activator.CreateInstance(creatorType) as ITournamentCreator;
        if (tournamentCreator == null)
        {
            logger.LogError("Failed to create tournament creator: {TypeName}", typeName);
            return;
        }

        logger.LogInformation("Using tournament creator {Creator}", config.TournamentDBCreatorName);
        SetupTeams(context, tournamentCreator);
        SetupStadiums(context, tournamentCreator);
        SetupPlayers(context, tournamentCreator);
        SetupGames(context, tournamentCreator, config, userManager);
    }

    private static void CreateFirstUsers(Config config, UserManager<MundialitoUser> userManager, ILogger<DatabaseInitilaizer> logger)
    {
        if (string.IsNullOrEmpty(config.GoogleClientId))
        {
            var user = new MundialitoUser
            {
                UserName = config.AdminUserName,
                Email = config.AdminEmail,
                LastName = config.AdminLastName,
                FirstName = config.AdminFirstName,
                Role = Role.Admin,
            };
            logger.LogInformation("Creating admin user {0}", user);
            userManager.CreateAsync(user, "123456").Wait();
        }
    }

    private static void EnsureMonkeyUser(
        MundialitoDbContext context,
        Config config,
        UserManager<MundialitoUser> userManager,
        ILogger<DatabaseInitilaizer> logger)
    {
        if (string.IsNullOrEmpty(config.MonkeyUserName))
        {
            return;
        }

        if (context.Users.Any(u => u.UserName == config.MonkeyUserName))
        {
            return;
        }

        var monkey = new MundialitoUser
        {
            UserName = config.MonkeyUserName,
            FirstName = "Monkey",
            LastName = "Monk",
            Email = "monkey@zoo.com",
            Role = Role.Active,
            ProfilePicture = "../../../icons/monkey.png"
        };
        logger.LogInformation("Creating missing monkey user {UserName}", config.MonkeyUserName);
        userManager.CreateAsync(monkey, "monkey").Wait();
    }

    // Idempotent backfill: guarantees the monkey has a random bet on every existing game and a
    // general bet. Normally SetupGames seeds these, but if the tournament data was seeded while the
    // monkey user did not yet exist, SetupGames skipped them and the one-shot Teams gate never re-runs.
    // On a healthy database this is a no-op. Games added later via the API are covered by AddMonkeyBet.
    private static void EnsureMonkeyBets(
        MundialitoDbContext context,
        Config config,
        ILogger<DatabaseInitilaizer> logger)
    {
        if (string.IsNullOrEmpty(config.MonkeyUserName))
        {
            return;
        }

        // Best-effort: Seed() is called unguarded in Program.cs, so a throw here would abort startup.
        // The backfill must never crash the app; on failure we log and let the app start normally.
        try
        {
            var monkey = context.Users.FirstOrDefault(u => u.UserName == config.MonkeyUserName);
            if (monkey == null)
            {
                logger.LogWarning("Monkey user {UserName} not found, skipping monkey bets backfill", config.MonkeyUserName);
                return;
            }

            var randomResults = new RandomResults();

            // A random, unscored bet for every game the monkey is missing. Already-resolved games get an
            // unscored bet too; re-saving that game's result re-runs BetsResolver and scores the monkey's
            // bet without changing any other user's points. The (UserId, GameId) unique index makes this safe.
            var existingBetGameIds = context.Bets
                .Where(bet => bet.UserId == monkey.Id)
                .Select(bet => bet.GameId)
                .ToHashSet();

            var addedBets = 0;
            foreach (var game in context.Games.ToList())
            {
                if (existingBetGameIds.Contains(game.GameId))
                {
                    continue;
                }
                var result = randomResults.GetRandomResult();
                context.Bets.Add(new Bet
                {
                    UserId = monkey.Id,
                    GameId = game.GameId,
                    HomeScore = result.Key,
                    AwayScore = result.Value,
                    CardsMark = randomResults.GetRandomMark(),
                    CornersMark = randomResults.GetRandomMark()
                });
                addedBets++;
            }

            // General bet: there is no DB uniqueness on GeneralBets.UserId, so this guard is the only thing
            // preventing a duplicate monkey general bet on every startup.
            var addedGeneralBet = false;
            if (!context.GeneralBets.Any(generalBet => generalBet.User.Id == monkey.Id))
            {
                var teams = context.Teams.ToList();
                var players = context.Players.ToList();
                if (teams.Count > 0 && players.Count > 0)
                {
                    var rnd = new Random();
                    context.GeneralBets.Add(new GeneralBet
                    {
                        User = monkey,
                        WinningTeam = teams[rnd.Next(teams.Count)],
                        GoldBootPlayer = players[rnd.Next(players.Count)],
                        IsResolved = false
                    });
                    addedGeneralBet = true;
                }
                else
                {
                    logger.LogWarning("Cannot backfill monkey general bet: teams or players are missing");
                }
            }

            if (addedBets > 0 || addedGeneralBet)
            {
                logger.LogInformation("Monkey backfill: adding {BetCount} missing bet(s){GeneralBet}", addedBets, addedGeneralBet ? " and a general bet" : string.Empty);
                context.SaveChanges();
            }
        }
        catch (Exception e)
        {
            logger.LogError("Monkey bets backfill failed, continuing startup: {0}", e.Message);
        }
    }

    private static void SetupPlayers(MundialitoDbContext context, ITournamentCreator tournamentCreator)
    {
        var players = tournamentCreator.GetPlayers();

        players.ForEach(player => context.Players.Add(player));

        context.SaveChanges();
        playersDic = context.Players.ToDictionary(player => player.Name, player => player);
    }

    private static void SetupTeams(MundialitoDbContext context, ITournamentCreator tournamentCreator)
    {
        var teams = tournamentCreator.GetTeams();
        teams.ForEach(team => context.Teams.Add(team));
        context.SaveChanges();
        teamsDic = context.Teams.ToDictionary(team => team.Name, team => team);
    }

    private static void SetupStadiums(MundialitoDbContext context, ITournamentCreator tournamentCreator)
    {
        var stadiums = tournamentCreator.GetStadiums();

        stadiums.ForEach(stadium => context.Stadiums.Add(stadium));

        context.SaveChanges();
        stadiumsDic = context.Stadiums.ToDictionary(stadium => stadium.Name, stadium => stadium);
    }

    private static void SetupGames(MundialitoDbContext context, ITournamentCreator tournamentCreator, Config config, UserManager<MundialitoUser> userManager)
    {
        var games = tournamentCreator.GetGames(stadiumsDic, teamsDic);
        games.ForEach(game => context.Games.Add(game));
        context.SaveChanges();
        if (!string.IsNullOrEmpty(config.MonkeyUserName))
        {
            var monkey = context.Users.FirstOrDefault(u => u.UserName == config.MonkeyUserName);
            if (monkey != null)
            {
                var randomResults = new RandomResults();
                games.ForEach(game =>
                {
                    var result = randomResults.GetRandomResult();
                    var newBet = new Bet
                    {
                        UserId = monkey.Id,
                        GameId = game.GameId,
                        HomeScore = result.Key,
                        AwayScore = result.Value,
                        CardsMark = randomResults.GetRandomMark(),
                        CornersMark = randomResults.GetRandomMark()
                    };
                    context.Bets.Add(newBet);
                });
                Random rnd = new Random();
                var team = teamsDic.Values.ElementAt(rnd.Next(0, teamsDic.Count));
                var player = playersDic.Values.ElementAt(rnd.Next(0, playersDic.Count));

                context.GeneralBets.Add(new GeneralBet
                {
                    GoldBootPlayer = player,
                    WinningTeam = team,
                    User = monkey
                });
                context.SaveChanges();
            }
            else
            {
                return;
            }
        }
    }
}