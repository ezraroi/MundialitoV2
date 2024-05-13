using Mundialito.DAL.Bets;
using Mundialito.Models;
using Mundialito.Logic;
using System.Diagnostics;
using Mundialito.DAL.ActionLogs;
using System.Net.Mail;
using System.Net;
using System.Text;
using Mundialito.DAL.Accounts;
using Mundialito.DAL;
using Mundialito.DAL.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.Configuration;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BetsController : ControllerBase
{
    private const String ObjectType = "Bet";
    private readonly IBetsRepository betsRepository;
    private readonly IGamesRepository gamesRepository;
    private readonly IBetValidator betValidator;
    private readonly ILoggedUserProvider userProivider;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private UserManager<MundialitoUser> userManager;
    private readonly Config config;
    private readonly IHttpContextAccessor httpContextAccessor;

    public BetsController(IBetsRepository betsRepository, IBetValidator betValidator, ILoggedUserProvider userProivider, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IGamesRepository gamesRepository, UserManager<MundialitoUser> userManager, IHttpContextAccessor httpContextAccessor, IOptions<Config> config)
    {
        this.config = config.Value;
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
        if (gamesRepository == null)
            throw new ArgumentNullException("gamesRepository");
        this.gamesRepository = gamesRepository;

        if (betsRepository == null)
            throw new ArgumentNullException("betsRepository");
        this.betsRepository = betsRepository;

        if (betValidator == null)
            throw new ArgumentNullException("betValidator");
        this.betValidator = betValidator;

        if (userProivider == null)
            throw new ArgumentNullException("userProivider");
        this.userProivider = userProivider;

        if (dateTimeProvider == null)
            throw new ArgumentNullException("dateTimeProvider");
        this.dateTimeProvider = dateTimeProvider;

        if (actionLogsRepository == null)
            throw new ArgumentNullException("actionLogsRepository");
        this.actionLogsRepository = actionLogsRepository;
    }

    [HttpGet]
    public IEnumerable<BetViewModel> GetAllBets()
    {
        return betsRepository.GetBets().Select(item => new BetViewModel(item, dateTimeProvider.UTCNow));
    }

    [HttpGet("{id}")]
    public BetViewModel GetBetById(int id)
    {
        var item = betsRepository.GetBet(id);

        if (item == null)
            throw new ObjectNotFoundException(string.Format("Bet with id '{0}' not found", id));

        return new BetViewModel(item, dateTimeProvider.UTCNow);
    }

    [HttpGet("user/{username}")]
    public IEnumerable<BetViewModel> GetUserBets(string username)
    {
        var bets = betsRepository.GetUserBets(username).ToList();
        return bets.Where(bet => !bet.IsOpenForBetting(dateTimeProvider.UTCNow) || userProivider.UserName == username).Select(bet => new BetViewModel(bet, dateTimeProvider.UTCNow));
    }

    [HttpPost]
    public NewBetModel PostBet(NewBetModel bet)
    {
        var newBet = new Bet();
        newBet.UserId = userProivider.UserId;
        newBet.GameId = bet.GameId;
        newBet.HomeScore = bet.HomeScore;
        newBet.AwayScore = bet.AwayScore;
        newBet.CardsMark = bet.CardsMark;
        newBet.CornersMark = bet.CornersMark;
        betValidator.ValidateNewBet(newBet);
        var res = betsRepository.InsertBet(newBet);
        Trace.TraceInformation("Posting new Bet: {0}", newBet);
        betsRepository.Save();
        bet.BetId = res.BetId;
        AddLog(ActionType.CREATE, string.Format("Posting new Bet: {0}", res));
        if (ShouldSendMail())
        {
            SendBetMail(newBet);
        }
        return bet;
    }

    [HttpPut("{id}")]
    public NewBetModel UpdateBet(int id, UpdateBetModel bet)
    {
        var betToUpdate = new Bet();
        betToUpdate.BetId = id;
        betToUpdate.HomeScore = bet.HomeScore;
        betToUpdate.AwayScore = bet.AwayScore;
        betToUpdate.CornersMark = bet.CornersMark;
        betToUpdate.CardsMark = bet.CardsMark;
        betToUpdate.GameId = bet.GameId;
        betToUpdate.UserId = userProivider.UserId;
        betValidator.ValidateUpdateBet(betToUpdate);
        betsRepository.UpdateBet(betToUpdate);
        betsRepository.Save();
        Trace.TraceInformation("Updating Bet: {0}", betToUpdate);
        AddLog(ActionType.UPDATE, string.Format("Updating Bet: {0}", betToUpdate));
        if (ShouldSendMail())
        {
            SendBetMail(betToUpdate);
        }
        return new NewBetModel(id, bet);
    }

    [HttpDelete("{id}")]
    public void DeleteBet(int id)
    {
        betValidator.ValidateDeleteBet(id, userProivider.UserId);
        betsRepository.DeleteBet(id);
        betsRepository.Save();
        Trace.TraceInformation("Deleting Bet {0}", id);
        AddLog(ActionType.DELETE, string.Format("Deleting Bet: {0}", id));
    }

    private void AddLog(ActionType actionType, String message)
    {
        try
        {
            actionLogsRepository.InsertLogAction(ActionLog.Create(actionType, ObjectType, message, httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value));
            actionLogsRepository.Save();
        }
        catch (Exception e)
        {
            Trace.TraceError("Exception during log. Exception: {0}", e.Message);
        }
    }

    private bool ShouldSendMail()
    {
        return config.SendBetMail;
    }

    private void SendBetMail(Bet bet)
    {
        try
        {
            // MundialitoUser user = userManager.FindById(userProivider.UserId);
            MundialitoUser user = new MundialitoUser();
            Game game = gamesRepository.GetGame(bet.GameId);
            string sendGridUsername = config.SendGridUserName;
            string sendGridPassword = config.SendGridPassword;
            string linkAddress = config.LinkAddress;
            string fromAddress = config.FromAddress;
            MailMessage message = new MailMessage();
            message.To.Add(new MailAddress(user.Email, user.FirstName + " " + user.LastName));
            message.Subject = string.Format("{0} Bet Update: You placed a bet on {1} - {2}", config.ApplicationName, game.HomeTeam.Name,
                game.AwayTeam.Name);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("Result: {0} {1} - {2} {3}", game.HomeTeam.Name, bet.HomeScore, game.AwayTeam.Name, bet.AwayScore));
            builder.AppendLine(string.Format("Corners: {0}", bet.CornersMark));
            builder.AppendLine(string.Format("Yellow Cards: {0}", bet.CardsMark));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(builder.ToString(), null, "text/plain"));
            SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
            NetworkCredential networkCredential = new NetworkCredential(sendGridUsername, sendGridPassword);
            smtpClient.Credentials = (ICredentialsByHost)networkCredential;
            smtpClient.Send(message);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Exception during mail sending. Exception: {0}", ex.Message);
        }
    }
}

