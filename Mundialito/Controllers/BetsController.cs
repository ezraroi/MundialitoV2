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

    /// <summary>
    /// The caller's bet on a game, read and written at one address. Absolute routes so the
    /// two halves of the resource live beside the validator, mail and audit dependencies
    /// they need, rather than dragging those into GamesController.
    /// </summary>
    [HttpGet("/api/games/{gameId}/mybet")]
    public async Task<ActionResult<BetViewModel>> GetMyBet(int gameId)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var game = gamesRepository.GetGame(gameId);
        if (game == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Game with id '{0}' not found", gameId)});
        var bet = betsRepository.GetUserBetOnGame(user.UserName, gameId);
        if (bet == null)
        {
            /* "You have not bet on this game" is a successful answer to a legitimate
               question, and the client resolves it in a route resolver where a rejection
               would abort the route change - so 200 with HasBet false, not 404. */
            logger.LogInformation("No bet found for game {} and user {}", gameId, user.UserName);
            return Ok(new BetViewModel
            {
                HasBet = false,
                HomeScore = null,
                AwayScore = null,
                IsOpenForBetting = game.IsOpen(dateTimeProvider.UTCNow),
                IsResolved = false,
                Game = new BetGame(game)
            });
        }
        return Ok(new BetViewModel(bet, dateTimeProvider.UTCNow));
    }

    /// <summary>
    /// Idempotent upsert of the caller's bet on a game, keyed by the (user, game) pair the
    /// database already declares unique. It replaces POST /api/bets + PUT /api/bets/{id}:
    /// with no bet id in the request a caller can only ever address their own bet, and with
    /// no game id in the body a bet cannot be moved to another game. 201 on create, 200 on
    /// update - the only way to observe which branch ran from outside.
    /// </summary>
    [HttpPut("/api/games/{gameId}/mybet")]
    [Authorize(Policy = Policies.ActiveOrAdmin)]
    public async Task<ActionResult<BetViewModel>> PutMyBet(int gameId, SaveBetModel bet)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var game = gamesRepository.GetGame(gameId);
        if (game == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Game with id '{0}' not found", gameId)});
        try
        {
            betValidator.ValidateBetUpsert(game);
        }
        catch (GameClosedForBettingException e)
        {
            /* 409, not 400: "you were too late" is a different thing from "your request was
               malformed" and should not share its Sentry fingerprint. */
            AddLog(ActionType.ERROR, e.Message);
            return Conflict(new ErrorMessage{ Message = e.Message});
        }

        var existing = betsRepository.GetUserBetOnGame(user.UserName, gameId);
        var isCreate = existing == null;
        var target = existing ?? new Bet
        {
            UserId = user.Id,
            User = user,
            GameId = gameId,
            Game = game
        };
        target.HomeScore = bet.HomeScore!.Value;
        target.AwayScore = bet.AwayScore!.Value;
        target.CardsMark = bet.CardsMark;
        target.CornersMark = bet.CornersMark;
        if (isCreate)
            target = betsRepository.InsertBet(target);

        logger.LogInformation("Saving bet of {} on game {}", user.UserName, gameId);
        try
        {
            betsRepository.Save();
        }
        catch (DuplicateBetException e)
        {
            /* Two concurrent first saves both see "no existing bet" and both insert; one
               loses on IX_Bets_UserId_GameId. Answering 409 rather than re-reading and
               retrying keeps this handler free of a detach-and-replay path for a race the
               client's in-flight guard already prevents in practice. Retrying is safe:
               the endpoint is idempotent. */
            logger.LogWarning("Concurrent first bet of {} on game {}: {}", user.UserName, gameId, e.Message);
            return Conflict(new ErrorMessage{ Message = "Your bet on this game was just saved by another request, please reload and try again"});
        }

        /* These two message texts are load-bearing: the ActionLogs query in #171 that tells
           a post-deadline write apart from a rejected create matches on them. */
        AddLog(isCreate ? ActionType.CREATE : ActionType.UPDATE,
            string.Format(isCreate ? "Posting new Bet: {0}" : "Updating Bet: {0}", target));
        if (ShouldSendMail())
            SendBetMail(target, user);
        logger.LogInformation("Bet {} of {} was saved", target.BetId, user.UserName);
        var view = new BetViewModel(target, dateTimeProvider.UTCNow);
        return isCreate ? StatusCode(StatusCodes.Status201Created, view) : Ok(view);
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

