using Mundialito.DAL.Bets;
using Mundialito.Models;
using Mundialito.Logic;
using Mundialito.DAL.ActionLogs;
using System.Text;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.Configuration;
using Microsoft.Extensions.Options;
using Mundialito.Mail;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BetsController : ControllerBase
{
    private const string ObjectType = "Bet";
    private readonly IBetsRepository betsRepository;
    private readonly IGamesRepository gamesRepository;
    private readonly IBetValidator betValidator;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly Config config;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IEmailSender emailSender;
    private readonly ILogger logger;

    public BetsController(ILogger<BetsController> logger, IBetsRepository betsRepository, IBetValidator betValidator, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IGamesRepository gamesRepository, UserManager<MundialitoUser> userManager, IHttpContextAccessor httpContextAccessor, IOptions<Config> config, IEmailSender emailSender)
    {
        this.config = config.Value;
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
        this.gamesRepository = gamesRepository;
        this.betsRepository = betsRepository;
        this.betValidator = betValidator;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogsRepository = actionLogsRepository;
        this.emailSender = emailSender;
        this.logger = logger;
    }

    [HttpGet]
    public IEnumerable<BetViewModel> GetAllBets()
    {
        return betsRepository.GetBets().Select(item => new BetViewModel(item, dateTimeProvider.UTCNow));
    }

    [HttpGet("{id}")]
    public ActionResult<BetViewModel> GetBetById(int id)
    {
        var item = betsRepository.GetBet(id);
        if (item == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Bet with id '{0}' not found", id)});
        return Ok(new BetViewModel(item, dateTimeProvider.UTCNow));
    }

    [HttpGet("user/{username}")]
    public IEnumerable<BetViewModel> GetUserBets(string username)
    {
        var bets = betsRepository.GetUserBets(username).ToList();
        return bets.Where(bet => !bet.IsOpenForBetting(dateTimeProvider.UTCNow) || httpContextAccessor.HttpContext?.User.Identity.Name == username).Select(bet => new BetViewModel(bet, dateTimeProvider.UTCNow));
    }

    [HttpPost]
    [Authorize(Roles = "Active,Admin")]
    public async Task<ActionResult<BetViewModel>> PostBet(NewBetModel bet)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var newBet = new Bet
        {
            UserId = user.Id,
            GameId = bet.GameId,
            HomeScore = bet.HomeScore,
            AwayScore = bet.AwayScore,
            CardsMark = bet.CardsMark,
            CornersMark = bet.CornersMark
        };
        try
        {
            betValidator.ValidateNewBet(newBet);
        }
        catch (Exception e)
        {
            return BadRequest(new ErrorMessage{ Message = e.Message});
        }
        var res = betsRepository.InsertBet(newBet);
        logger.LogInformation("Posting new Bet from {}", user.UserName);
        betsRepository.Save();
        bet.BetId = res.BetId;
        AddLog(ActionType.CREATE, string.Format("Posting new Bet: {0}", res));
        if (ShouldSendMail())
            SendBetMail(newBet, user);
        logger.LogInformation("Bet os user {} was saved", user.UserName);
        return Ok(new BetViewModel(res, dateTimeProvider.UTCNow));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Active,Admin")]
    public async Task<ActionResult<BetViewModel>> UpdateBet(int id, UpdateBetModel bet)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
        {
            return Unauthorized();
        }
        var betToUpdate = betsRepository.GetBet(id);
        betToUpdate.BetId = id;
        betToUpdate.HomeScore = bet.HomeScore;
        betToUpdate.AwayScore = bet.AwayScore;
        betToUpdate.CornersMark = bet.CornersMark;
        betToUpdate.CardsMark = bet.CardsMark;
        betToUpdate.GameId = bet.GameId;
        betToUpdate.UserId = user.Id;
        try {
            betValidator.ValidateUpdateBet(betToUpdate);
        } catch (UnauthorizedAccessException e) {
            return Unauthorized(new ErrorMessage{ Message = e.Message});
        } catch (Exception e) {
            return BadRequest(new ErrorMessage{ Message = e.Message});
        }
        logger.LogInformation("Updating bet from {}", user.UserName);
        betsRepository.Save();
        AddLog(ActionType.UPDATE, string.Format("Updating Bet: {0}", betToUpdate));
        if (ShouldSendMail())
            SendBetMail(betToUpdate, user);
        logger.LogInformation("Bet {} of {} was updated", id, user.UserName);
        return Ok(new BetViewModel(betToUpdate, dateTimeProvider.UTCNow));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Active,Admin")]
    public async Task<IActionResult> DeleteBet(int id)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        try {
            betValidator.ValidateDeleteBet(id, user.Id);
        } catch (UnauthorizedAccessException e) {
            return Unauthorized(e.Message);
        } catch (Exception e) {
            return BadRequest(new ErrorMessage{ Message = e.Message});
        }
        logger.LogInformation("Deleting bet {} of {}", id, user.UserName);
        betsRepository.DeleteBet(id);
        betsRepository.Save();
        AddLog(ActionType.DELETE, string.Format("Deleting Bet: {0}", id));
        logger.LogInformation("Bet {} of {} was deleted", id, user.UserName);
        return Ok();
    }

    private void AddLog(ActionType actionType, string message)
    {
        try
        {
            actionLogsRepository.InsertLogAction(ActionLog.Create(actionType, ObjectType, message, httpContextAccessor.HttpContext?.User.Identity.Name));
            actionLogsRepository.Save();
        }
        catch (Exception e)
        {
            logger.LogError("Exception during log. Exception: {0}", e.Message);
        }
    }

    private bool ShouldSendMail()
    {
        return config.SendBetMail;
    }

    private void SendBetMail(Bet bet, MundialitoUser user)
    {
        try
        {
            Game game = gamesRepository.GetGame(bet.GameId);
            string linkAddress = config.LinkAddress;
            string fromAddress = config.FromAddress;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("Result: {0} {1} - {2} {3}", game.HomeTeam.Name, bet.HomeScore, game.AwayTeam.Name, bet.AwayScore));
            builder.AppendLine(string.Format("Corners: {0}", bet.CornersMark));
            builder.AppendLine(string.Format("Yellow Cards: {0}", bet.CardsMark));
            emailSender.SendEmail(user.Email, string.Format("{0} Bet Update: You placed a bet on {1} - {2}", config.ApplicationName, game.HomeTeam.Name,
                game.AwayTeam.Name), builder.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError("Exception during mail sending. Exception: {0}", ex.Message);
        }
    }
}

