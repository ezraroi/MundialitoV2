using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Players;
using Mundialito.DAL.Teams;
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
    private readonly IPlayersRepository playersRepository;
    private readonly ITeamsRepository teamsRepository;
    private readonly ILogger logger;

    public GeneralBetsController(ILogger<GeneralBetsController> logger, IGeneralBetsRepository generalBetsRepository, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor, TournamentTimesUtils tournamentTimesUtils, UserManager<MundialitoUser> userManager, IPlayersRepository playersRepository, ITeamsRepository teamsRepository)
    {
        this.generalBetsRepository = generalBetsRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogsRepository = actionLogsRepository;
        this.httpContextAccessor = httpContextAccessor;
        this.tournamentTimesUtils = tournamentTimesUtils;
        this.userManager = userManager;
        this.playersRepository = playersRepository;
        this.teamsRepository = teamsRepository;
        this.logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<GeneralBetViewModel>> GetAllGeneralBets()
    {
        if (!User.IsInRole("Admin") && dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            return BadRequest(new ErrorMessage { Message = "General bets are still open for betting, you can't see other users bets yet"});
        }
        return Ok(generalBetsRepository.GetGeneralBets().Select(bet => 
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
            return BadRequest(new ErrorMessage { Message = "General bets are still open for betting, you can't see other users bets yet"});
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
    [Authorize(Roles = "Active,Admin")]
    public async Task<ActionResult<NewGeneralBetModel>> PostBet(NewGeneralBetModel newBet)
    {
        var validate = Validate();
        if (!string.IsNullOrEmpty(validate))
        {
            AddLog(ActionType.ERROR, validate);
            return BadRequest(new ErrorMessage { Message = validate});
        }
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var team = teamsRepository.GetTeam(newBet.WinningTeam.TeamId);
        if (team == null)
            return BadRequest(new ErrorMessage { Message = string.Format("Team with id '{0}' not found", newBet.WinningTeam.TeamId)});
        var player = playersRepository.GetPlayer(newBet.GoldenBootPlayer.PlayerId);
        if (player == null)
            return BadRequest(new ErrorMessage { Message = string.Format("Player with id '{0}' not found", newBet.GoldenBootPlayer.PlayerId)});
        var generalBet = new GeneralBet
        {
            User = user,
            WinningTeam = team,
            GoldBootPlayer = player,
        };
        var res = generalBetsRepository.InsertGeneralBet(generalBet);
        logger.LogInformation("Posting new general bet {} from {}", generalBet, user.UserName);
        generalBetsRepository.Save();
        newBet.GeneralBetId = res.GeneralBetId;
        AddLog(ActionType.CREATE, String.Format("Posting new Generel Bet: {0}", res));
        logger.LogInformation("Saved general bet of {}", user.UserName);
        return Ok(newBet);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Active,Admin")]
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
        var team = teamsRepository.GetTeam(bet.WinningTeam.TeamId);
        if (team == null)
            return BadRequest(new ErrorMessage { Message = string.Format("Team with id '{0}' not found", bet.WinningTeam.TeamId)});
        var player = playersRepository.GetPlayer(bet.GoldenBootPlayer.PlayerId);
        if (player == null)
            return BadRequest(new ErrorMessage { Message = string.Format("Player with id '{0}' not found", bet.GoldenBootPlayer.PlayerId)});
        betToUpdate.WinningTeam = team;
        betToUpdate.GoldBootPlayer = player;
        logger.LogInformation("Updating general bet of {} with {}", user.UserName, betToUpdate);
        logger.LogInformation("Updated general bet of {}", user.UserName);
        AddLog(ActionType.UPDATE, string.Format("Updating new Generel Bet: {0}", betToUpdate));
        return bet;
    }

    [HttpPut("{id}/resolve")]
    [Authorize(Roles = "Admin")]
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
        AddLog(ActionType.UPDATE, string.Format("Resolved Generel Bet: {0}", item));
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
        return string.Empty;
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
}

