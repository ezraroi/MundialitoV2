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
                if (!string.IsNullOrEmpty(config.TournamentDBCreatorName))
                {
                    Type t = Type.GetType("Mundialito.DAL.DBCreators." + config.TournamentDBCreatorName);
                    ITournamentCreator tournamentCreator = Activator.CreateInstance(t) as ITournamentCreator;
                    logger.LogInformation("Using tournament creator {0}", config.TournamentDBCreatorName);
                    SetupTeams(context, tournamentCreator);
                    SetupStadiums(context, tournamentCreator);
                    SetupPlayers(context, tournamentCreator);
                    SetupGames(context, tournamentCreator, config, userManager);
                }
            }
        }
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
        if (!string.IsNullOrEmpty(config.MonkeyUserName))
        {
            var monkey = new MundialitoUser
            {
                UserName = config.MonkeyUserName,
                FirstName = "Monkey",
                LastName = "Monk",
                Email = "monkey@zoo.com",
                Role = Role.Active,
                ProfilePicture = "../../../icons/monkey.png"
            };
            logger.LogInformation("Creating user {0}", monkey);
            userManager.CreateAsync(monkey, "monkey").Wait();
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