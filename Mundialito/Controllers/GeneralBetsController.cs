using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.GeneralBets;
using Mundialito.Logic;
using Mundialito.Models;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GeneralBetsController : ControllerBase
{
    private const string ObjectType = "GeneralBet";
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly TournamentTimesUtils tournamentTimesUtils;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly ILogger logger;

    public GeneralBetsController(ILogger<GeneralBetsController> logger, IGeneralBetsRepository generalBetsRepository, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor, TournamentTimesUtils tournamentTimesUtils, UserManager<MundialitoUser> userManager)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.dateTimeProvider = dateTimeProvider;
        this.generalBetsRepository = generalBetsRepository;
        this.actionLogsRepository = actionLogsRepository;
        this.tournamentTimesUtils = tournamentTimesUtils;
        this.userManager = userManager;
        this.logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<GeneralBetViewModel>> GetAllGeneralBets()
    {
        if (!User.IsInRole("Admin") && dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            return BadRequest(new ErrorMessage { Message = "General bets are still open for betting, you can't see other users bets yet"});
        }
        return Ok(generalBetsRepository.GetGeneralBets().Select(bet => new GeneralBetViewModel(bet, tournamentTimesUtils.GetGeneralBetsCloseTime())).OrderBy(bet => bet.OwnerName));
    }

    [HttpGet("has-bet/{username}")]
    public Boolean HasBet(string username)
    {
        return generalBetsRepository.IsGeneralBetExists(username);
    }

    [HttpGet("CanSubmitBets")]
    public Boolean CanSubmitBets()
    {
        return dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime();
    }

    [HttpGet("user/{username}")]
    public ActionResult<GeneralBetViewModel> GetUserGeneralBet(string username)
    {
        if (httpContextAccessor.HttpContext?.User.Identity.Name != username && dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            return BadRequest(new ErrorMessage { Message = "General bets are still open for betting, you can't see other users bets yet"});
        }
        var item = generalBetsRepository.GetUserGeneralBet(username);
        if (item == null)
            return NotFound(string.Format("User '{0}' dosen't have a general bet yet", username));
        return Ok(new GeneralBetViewModel(item, tournamentTimesUtils.GetGeneralBetsCloseTime()));
    }

    [HttpGet("{id}")]
    public ActionResult<GeneralBetViewModel> GetGeneralBetById(int id)
    {
        var item = generalBetsRepository.GetGeneralBet(id);
        if (item == null)
            return NotFound(new ErrorMessage { Message = string.Format("General Bet with id '{0}' not found", id)});
        return Ok(new GeneralBetViewModel(item, tournamentTimesUtils.GetGeneralBetsCloseTime()));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<NewGeneralBetModel>> PostBet(NewGeneralBetModel newBet)
    {
        var validate = Validate();
        if (!String.IsNullOrEmpty(validate))
        {
            AddLog(ActionType.ERROR, validate);
            return BadRequest(new ErrorMessage { Message = validate});
        }
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var generalBet = new GeneralBet
        {
            User = user,
            WinningTeamId = newBet.WinningTeamId,
            GoldBootPlayerId = newBet.GoldenBootPlayerId
        };
        var res = generalBetsRepository.InsertGeneralBet(generalBet);
        logger.LogInformation("Posting new general bet {} from {}", generalBet, user.UserName);
        generalBetsRepository.Save();
        newBet.GeneralBetId = res.GeneralBetId;
        AddLog(ActionType.CREATE, String.Format("Posting new Generel Bet: {0}", res));
        logger.LogInformation("Saved general bet of {}", user.UserName);
        return Ok(newBet);
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult<UpdateGenralBetModel>> UpdateBet(int id, UpdateGenralBetModel bet)
    {
        if (dateTimeProvider.UTCNow > tournamentTimesUtils.GetGeneralBetsCloseTime())
            return BadRequest("General bets are already closed for betting");
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var betToUpdate = generalBetsRepository.GetGeneralBet(id);
        if (betToUpdate.User.Id != user.Id)
        {
            AddLog(ActionType.UNAUTHORIZED_ACCESS, "You can't update a bet that is not yours");
            return Unauthorized(new ErrorMessage { Message = "You can't update a bet that is not yours"});
        }
        betToUpdate.WinningTeamId = bet.WinningTeamId;
        betToUpdate.GoldBootPlayerId = bet.GoldenBootPlayerId;
        logger.LogInformation("Updating general bet of {} with {}", user.UserName, betToUpdate);
        generalBetsRepository.Save();
        logger.LogInformation("Updated general bet of {}", user.UserName);
        AddLog(ActionType.UPDATE, String.Format("Updating new Generel Bet: {0}", betToUpdate));
        return bet;
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    [Route("{id}/resolve")]
    public IActionResult ResolveGeneralBet(int id, ResolveGeneralBetModel resolvedBet)
    {
        if (dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsResolveTime())
        {
            AddLog(ActionType.ERROR, "General bets are not closed for betting yet");
            return BadRequest(new ErrorMessage { Message = "General bets are not closed for betting yet"});
        }
        var item = generalBetsRepository.GetGeneralBet(id);
        if (item == null)
        {
            AddLog(ActionType.ERROR, string.Format("General Bet '{0}' dosen't exits", id));
            return NotFound(new ErrorMessage { Message = string.Format("General Bet '{0}' dosen't exits", id)});
        }
        logger.LogInformation("Resolving general bet {0} with data: {1}", id, resolvedBet);
        item.Resolve(resolvedBet.PlayerIsRight, resolvedBet.TeamIsRight);
        generalBetsRepository.Save();
        logger.LogInformation("Resolved general bet {0}", id);
        AddLog(ActionType.UPDATE, String.Format("Resolved Generel Bet: {0}", item));
        return Ok();
    }

    private string Validate()
    {
        if (generalBetsRepository.IsGeneralBetExists(httpContextAccessor.HttpContext?.User.Identity.Name))
        {
            AddLog(ActionType.ERROR, "You have already submitted your general bet, only update is permitted");
            return "You have already submitted your general bet, only update is permitted";
        }
        if (dateTimeProvider.UTCNow > tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            AddLog(ActionType.ERROR, "General bets are already closed for betting");
            return "General bets are already closed for betting";
        }
        return String.Empty;
    }

    private void AddLog(ActionType actionType, String message)
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
}

