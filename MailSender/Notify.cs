using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.Mail;

namespace Mundialito.Function
{
    public class Notify
    {
        private readonly ILogger _logger;
        private List<Game> openGames;
        private readonly IConfigurationRoot configuration;
        private readonly Config config;
        private readonly IEmailSender emailSender;
        private readonly MundialitoDbContext mundialitoDbContext;

        public Notify(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Notify>();
            configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
            this.config = configuration.GetSection("App").Get<Config>();
            var connectionString = Environment.GetEnvironmentVariable("ConnectionString"); 
            _logger.LogInformation($"Connection string: {connectionString}");
            mundialitoDbContext = new MundialitoDbContext(configuration, connectionString);
            emailSender = new EmailSender(loggerFactory.CreateLogger<EmailSender>(), Options.Create(this.config));
        }

        [Function("Notify")]
        public void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
            openGames = GetOpenGames(_logger);
            for (int i = 0; i < openGames.Count; i++)
            {
                var minutes = openGames[i].Date.ToLocalTime().Subtract(DateTime.Now.ToLocalTime()).TotalMinutes;
                _logger.LogInformation("Game " + openGames[i].GameId + " Minutes is " + minutes);
                if (minutes < 120 && minutes > 35)
                {
                    _logger.LogInformation("Found game that will start @ " + openGames[i].Date.ToLocalTime());
                    _logger.LogInformation(minutes + " Minutes until start time");
                    SendNotifications(openGames[i], _logger);
                }
            }
            _logger.LogInformation("Bye Bye");
        }

        private void SendNotifications(Game game, ILogger log)
        {
            log.LogInformation("***** Sending notifications started *****");
            log.LogInformation("Current Time: " + DateTime.Now.ToLocalTime());
            log.LogInformation(string.Format("Sending notifications on game {0} ", game.GameId));
            List<MundialitoUser> usersToNotify = GetUsersToNotify(game);
            log.LogInformation(string.Format("Sending notifications to {0} users on game {1}", usersToNotify.Count, game.GameId));
            foreach (MundialitoUser user in usersToNotify)
            {
                log.LogInformation(string.Format("Will send notification to {0}", user.Email));
                SendNotification(user, game, log);
            }
            log.LogInformation("***** End of Notification sending *****");
        }

        private void SendNotification(MundialitoUser user, Game game, ILogger log)
        {
            try
            {
                string fromAddress = this.config.FromAddress;
                TimeSpan timeSpan = game.CloseTime - DateTime.UtcNow;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(string.Format("WARNING: The game between {0} and {1}, will be closed in {2} minutes and you havn't placed a bet yet", (object)game.HomeTeam.Name, (object)game.AwayTeam.Name, (object)(int)timeSpan.TotalMinutes));
                stringBuilder.Append(string.Format("Please submit your bet as soon as possible"));
                emailSender.SendEmail(user.Email, string.Format("WARNING: The game between {0} and {1}, will be closed in {2} minutes and you havn't placed a bet yet", (object)game.HomeTeam.Name, (object)game.AwayTeam.Name, (object)(int)timeSpan.TotalMinutes), stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                log.LogError("Failed to send notification. Exception is " + ex.Message);
                if (ex.InnerException != null)
                {
                    log.LogError("Innber excpetion: " + ex.InnerException.Message);
                }
            }
        }

        private List<MundialitoUser> GetUsersToNotify(Game game)
        {
            IEnumerable<MundialitoUser> source = mundialitoDbContext.Users.ToList();
            Dictionary<string, Bet> gameBets = Enumerable.ToDictionary<Bet, string, Bet>(new BetsRepository(mundialitoDbContext).GetGameBets(game.GameId), bet => bet.UserId, bet => bet);
            return Enumerable.ToList<MundialitoUser>(Enumerable.Where<MundialitoUser>(source, user => !gameBets.ContainsKey(user.Id)));
        }
        
        private List<Game> GetOpenGames(ILogger log)
        {
            List<Game> list1 = Enumerable.ToList<Game>(new GamesRepository(mundialitoDbContext).Get(null, null, ""));
            log.LogInformation(string.Format("Got {0} games from database", list1.Count));
            List<Game> list2 = Enumerable.ToList<Game>(Enumerable.Where<Game>((IEnumerable<Game>)Enumerable.OrderBy<Game, DateTime>((IEnumerable<Game>)list1, (Func<Game, DateTime>)(game => game.Date)), (Func<Game, bool>)(game => game.IsOpen())));
            log.LogInformation(string.Format("{0} games still can be notified", list2.Count));
            return list2;
        }

    }
}
