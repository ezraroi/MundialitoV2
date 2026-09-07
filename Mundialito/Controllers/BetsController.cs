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
using Mundialito.Auth.Authorization;

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
        var currentUsername = httpContextAccessor.HttpContext?.User.Identity.Name;
        // Bets on games that are still open for betting are private to their owner.
        return betsRepository.GetBets()
            .Where(bet => !bet.IsOpenForBetting(dateTimeProvider.UTCNow) || bet.User.UserName == currentUsername)
            .Select(item => new BetViewModel(item, dateTimeProvider.UTCNow));
    }

    [HttpGet("{id}")]
    public ActionResult<BetViewModel> GetBetById(int id)
    {
        var item = betsRepository.GetBet(id);
        if (item == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Bet with id '{0}' not found", id)});
        // A bet on a game that is still open for betting may only be viewed by its owner.
        // Return NotFound (rather than Forbid) so bet existence cannot be enumerated.
        var currentUsername = httpContextAccessor.HttpContext?.User.Identity.Name;
        if (item.IsOpenForBetting(dateTimeProvider.UTCNow) && item.User.UserName != currentUsername)
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
    [Authorize(Policy = Policies.ActiveOrAdmin)]
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
        catch (BetValidationException e)
        {
            AddLog(ActionType.ERROR, e.Message);
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
    [Authorize(Policy = Policies.ActiveOrAdmin)]
    public async Task<ActionResult<BetViewModel>> UpdateBet(int id, UpdateBetModel bet)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
        {
            return Unauthorized();
        }
        var betToUpdate = betsRepository.GetBet(id);
        if (betToUpdate == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Bet with id '{0}' not found", id)});
        /* Load, authorize, validate - and only then mutate. Every repository in a request
           shares one DbContext, so anything written to the tracked entity before the checks
           pass is committed by the next Save() on any repository, rejection or not. */
        try {
            betValidator.ValidateUpdateBet(betToUpdate, user.Id);
        } catch (BetForbiddenException e) {
            AddLog(ActionType.UNAUTHORIZED_ACCESS, e.Message);
            return Unauthorized(new ErrorMessage{ Message = e.Message});
        } catch (BetValidationException e) {
            AddLog(ActionType.ERROR, e.Message);
            return BadRequest(new ErrorMessage{ Message = e.Message});
        }
        /* GameId and UserId are deliberately not assigned: a bet's game is fixed at creation
           and its owner is whoever created it. Neither is the caller's to change. */
        betToUpdate.HomeScore = bet.HomeScore;
        betToUpdate.AwayScore = bet.AwayScore;
        betToUpdate.CornersMark = bet.CornersMark;
        betToUpdate.CardsMark = bet.CardsMark;
        logger.LogInformation("Updating bet from {}", user.UserName);
        betsRepository.Save();
        AddLog(ActionType.UPDATE, string.Format("Updating Bet: {0}", betToUpdate));
        if (ShouldSendMail())
            SendBetMail(betToUpdate, user);
        logger.LogInformation("Bet {} of {} was updated", id, user.UserName);
        return Ok(new BetViewModel(betToUpdate, dateTimeProvider.UTCNow));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.ActiveOrAdmin)]
    public async Task<IActionResult> DeleteBet(int id)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        try {
            betValidator.ValidateDeleteBet(id, user.Id);
        } catch (BetForbiddenException e) {
            AddLog(ActionType.UNAUTHORIZED_ACCESS, e.Message);
            return Unauthorized(e.Message);
        } catch (BetValidationException e) {
            AddLog(ActionType.ERROR, e.Message);
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

