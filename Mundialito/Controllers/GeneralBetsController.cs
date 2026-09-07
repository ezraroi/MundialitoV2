using Microsoft.AspNetCore.Authorization;
using Mundialito.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Players;
using Mundialito.DAL.Teams;
using Mundialito.Logic;
using Mundialito.Models;
using Mundialito.Auth.Authorization;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GeneralBetsController : ControllerBase
{
    private const string ObjectType = "GeneralBet";
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogger actionLogger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly TournamentTimesUtils tournamentTimesUtils;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly ITeamsRepository teamsRepository;
    private readonly IPlayersRepository playersRepository;
    private readonly GeneralBetsService generalBetsService;
    private readonly ICurrentUserRoleProvider currentUserRoleProvider;
    private readonly ILogger logger;
    private readonly ICurrentUser currentUser;

    public GeneralBetsController(ILogger<GeneralBetsController> logger, IGeneralBetsRepository generalBetsRepository, IDateTimeProvider dateTimeProvider, IActionLogger actionLogger, IHttpContextAccessor httpContextAccessor, TournamentTimesUtils tournamentTimesUtils, UserManager<MundialitoUser> userManager, ITeamsRepository teamsRepository, IPlayersRepository playersRepository, GeneralBetsService generalBetsService, ICurrentUserRoleProvider currentUserRoleProvider, ICurrentUser currentUser)
    {
        this.generalBetsRepository = generalBetsRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogger = actionLogger;
        this.httpContextAccessor = httpContextAccessor;
        this.tournamentTimesUtils = tournamentTimesUtils;
        this.userManager = userManager;
        this.teamsRepository = teamsRepository;
        this.playersRepository = playersRepository;
        this.generalBetsService = generalBetsService;
        this.currentUserRoleProvider = currentUserRoleProvider;
        this.logger = logger;
        this.currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GeneralBetViewModel>>> GetAllGeneralBets()
    {
        // Admins may look before the deadline. The role has to come from the database:
        // User.IsInRole would read the snapshot baked into the caller's 60 day token.
        var isAdmin = await currentUserRoleProvider.GetCurrentRoleAsync() == Role.Admin;
        if (dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime() && !isAdmin)
        {
            return BadRequest(new ErrorMessage { Message = "General bets are still open for betting, you can't see other users bets yet" });
        }
        return Ok(generalBetsService.GetGeneralBets().Select(bet =>
            new GeneralBetViewModel(bet, tournamentTimesUtils.GetGeneralBetsCloseTime())).OrderBy(bet => bet.OwnerName));
    }

    [HttpGet("has-bet/{username}")]
    public bool HasBet(string username)
    {
        return generalBetsRepository.IsGeneralBetExists(username);
    }

    [HttpGet("CanSubmitBets")]
    public bool CanSubmitBets()
    {
        return dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime();
    }

    [HttpGet("user/{username}")]
    public ActionResult<GeneralBetViewModel> GetUserGeneralBet(string username)
    {
        if (httpContextAccessor.HttpContext?.User.Identity.Name != username && dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime())
            return BadRequest(new ErrorMessage { Message = "General bets are still open for betting, you can't see other users bets yet" });
        var item = generalBetsService.GetUserGeneralBet(username);
        if (item == null)
            return NotFound(string.Format("User '{0}' dosen't have a general bet yet", username));
        return Ok(new GeneralBetViewModel(item, tournamentTimesUtils.GetGeneralBetsCloseTime()));
    }

    [HttpGet("{id}")]
    public ActionResult<GeneralBetViewModel> GetGeneralBetById(int id)
    {
        var item = generalBetsRepository.GetGeneralBet(id);
        if (item == null)
            return NotFound(new ErrorMessage { Message = string.Format("General Bet with id '{0}' not found", id) });
        return Ok(new GeneralBetViewModel(item, tournamentTimesUtils.GetGeneralBetsCloseTime()));
    }

    [HttpPost]
    [Authorize(Policy = Policies.ActiveOrAdmin)]
    public async Task<ActionResult<NewGeneralBetModel>> PostBet(NewGeneralBetModel newBet)
    {
        if (generalBetsRepository.IsGeneralBetExists(httpContextAccessor.HttpContext?.User.Identity.Name))
            return BadRequest(new ErrorMessage { Message = "You have already submitted your general bet, only update is permitted" });
        var validate = Validate();
        if (!string.IsNullOrEmpty(validate))
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, validate);
            return BadRequest(new ErrorMessage { Message = validate });
        }
        var user = await currentUser.GetAsync();
        if (user == null)
            return Unauthorized();
        var winningTeam = teamsRepository.GetTeam(newBet.WinningTeam.TeamId);
        if (winningTeam == null)
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, string.Format("Team with id '{0}' dosen't exits", newBet.WinningTeam.TeamId));
            return NotFound(new ErrorMessage { Message = string.Format("Team with id '{0}' dosen't exits", newBet.WinningTeam.TeamId) });
        }
        var goldenBootPlayer = playersRepository.GetPlayer(newBet.GoldenBootPlayer.PlayerId);
        if (goldenBootPlayer == null)
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, string.Format("Player with id '{0}' dosen't exits", newBet.GoldenBootPlayer.PlayerId));
            return NotFound(new ErrorMessage { Message = string.Format("Player with id '{0}' dosen't exits", newBet.GoldenBootPlayer.PlayerId) });
        }
        var generalBet = new GeneralBet
        {
            User = user,
            WinningTeam = winningTeam,
            GoldBootPlayer = goldenBootPlayer
        };
        var res = generalBetsRepository.InsertGeneralBet(generalBet);
        logger.LogInformation("Posting new general bet {} from {}", generalBet, user.UserName);
        generalBetsRepository.Save();
        newBet.GeneralBetId = res.GeneralBetId;
        actionLogger.Log(ActionType.CREATE, ObjectType, string.Format("Posting new Generel Bet: {0}", res));
        logger.LogInformation("Saved general bet of {}", user.UserName);
        return Ok(newBet);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ActiveOrAdmin)]
    public async Task<ActionResult<UpdateGenralBetModel>> UpdateBet(int id, UpdateGenralBetModel bet)
    {
        var validate = Validate();
        if (!string.IsNullOrEmpty(validate))
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, validate);
            return BadRequest(new ErrorMessage { Message = validate });
        }
        var user = await currentUser.GetAsync();
        if (user == null)
            return Unauthorized();
        var betToUpdate = generalBetsRepository.GetGeneralBet(id);
        if (betToUpdate == null)
            return NotFound(new ErrorMessage { Message = string.Format("General Bet '{0}' dosen't exits", id) });
        if (betToUpdate.User.Id != user.Id)
        {
            actionLogger.Log(ActionType.UNAUTHORIZED_ACCESS, ObjectType, "You can't update a bet that is not yours");
            return Unauthorized(new ErrorMessage { Message = "You can't update a bet that is not yours" });
        }
        var winningTeam = teamsRepository.GetTeam(bet.WinningTeam.TeamId);
        if (winningTeam == null)
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, string.Format("Team with id '{0}' dosen't exits", bet.WinningTeam.TeamId));
            return NotFound(new ErrorMessage { Message = string.Format("Team with id '{0}' dosen't exits", bet.WinningTeam.TeamId) });
        }
        var goldenBootPlayer = playersRepository.GetPlayer(bet.GoldenBootPlayer.PlayerId);
        if (goldenBootPlayer == null)
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, string.Format("Player with id '{0}' dosen't exits", bet.GoldenBootPlayer.PlayerId));
            return NotFound(new ErrorMessage { Message = string.Format("Player with id '{0}' dosen't exits", bet.GoldenBootPlayer.PlayerId) });
        }
        betToUpdate.WinningTeamId = bet.WinningTeam.TeamId;
        betToUpdate.GoldBootPlayerId = bet.GoldenBootPlayer.PlayerId;
        generalBetsRepository.Save();
        logger.LogInformation("Updated general bet of {}", user.UserName);
        return bet;
    }

    [HttpPut("{id}/resolve")]
    [Authorize(Policy = Policies.AdminOnly)]
    public IActionResult ResolveGeneralBet(int id, ResolveGeneralBetModel resolvedBet)
    {
        if (dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsResolveTime())
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, "General bets are not closed for betting yet");
            return BadRequest(new ErrorMessage { Message = "General bets are not closed for betting yet" });
        }
        var item = generalBetsRepository.GetGeneralBet(id);
        if (item == null)
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, string.Format("General Bet '{0}' dosen't exits", id));
            return NotFound(new ErrorMessage { Message = string.Format("General Bet '{0}' dosen't exits", id) });
        }
        logger.LogInformation("Resolving general bet {0} with data: {1}", id, resolvedBet);
        item.Resolve(resolvedBet.PlayerIsRight, resolvedBet.TeamIsRight);
        generalBetsRepository.Save();
        logger.LogInformation("Resolved general bet {0}", id);
        actionLogger.Log(ActionType.UPDATE, ObjectType, string.Format("Resolved Generel Bet: {0}", item));
        return Ok();
    }

    private string Validate()
    {
        if (dateTimeProvider.UTCNow > tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            actionLogger.Log(ActionType.ERROR, ObjectType, "General bets are already closed for betting");
            return "General bets are already closed for betting";
        }
        return string.Empty;
    }

}

