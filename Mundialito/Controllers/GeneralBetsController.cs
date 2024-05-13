using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.GeneralBets;
using Mundialito.Logic;
using Mundialito.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GeneralBetsController : ControllerBase
{
    private const String ObjectType = "GeneralBet";
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly ILoggedUserProvider userProivider;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly TournamentTimesUtils tournamentTimesUtils;

    public GeneralBetsController(IGeneralBetsRepository generalBetsRepository, ILoggedUserProvider userProivider, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor, TournamentTimesUtils tournamentTimesUtils)    {
        this.httpContextAccessor = httpContextAccessor;
        if (generalBetsRepository == null)
            throw new ArgumentNullException("generalBetsRepository");
        if (userProivider == null)
            throw new ArgumentNullException("userProivider");
        if (dateTimeProvider == null)
            throw new ArgumentNullException("dateTimeProvider");
        if (actionLogsRepository == null)
            throw new ArgumentNullException("actionLogsRepository");

        this.dateTimeProvider = dateTimeProvider;
        this.generalBetsRepository = generalBetsRepository;
        this.userProivider = userProivider;
        this.actionLogsRepository = actionLogsRepository;
        this.tournamentTimesUtils = tournamentTimesUtils;
    }
    
    [HttpGet]
    public IEnumerable<GeneralBetViewModel> GetAllGeneralBets()
    {
        if (!User.IsInRole("Admin") && dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            throw new ArgumentException("General bets are still open for betting, you can't see other users bets yet");
        }
        return generalBetsRepository.GetGeneralBets().Select(bet => new GeneralBetViewModel(bet, tournamentTimesUtils.GetGeneralBetsCloseTime())).OrderBy(bet => bet.OwnerName);
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
    public GeneralBetViewModel GetUserGeneralBet(string username)
    {
        if (userProivider.UserName != username && dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsCloseTime())
            throw new ArgumentException("General bets are still open for betting, you can't see other users bets yet");

        var item = generalBetsRepository.GetUserGeneralBet(username);

        if (item == null)
            throw new ObjectNotFoundException(string.Format("User '{0}' dosen't have a general bet yet", username));

        return new GeneralBetViewModel(item, tournamentTimesUtils.GetGeneralBetsCloseTime());
    }

    [HttpGet("{id}")]
    public GeneralBetViewModel GetGeneralBetById(int id)
    {
        var item = generalBetsRepository.GetGeneralBet(id);

        if (item == null)
            throw new ObjectNotFoundException(string.Format("General Bet with id '{0}' not found", id));

        return new GeneralBetViewModel(item, tournamentTimesUtils.GetGeneralBetsCloseTime());
    }

    [HttpPost]
    public NewGeneralBetModel PostBet(NewGeneralBetModel newBet)
    {
        Validate();
        var generalBet = new GeneralBet();
        generalBet.User = new MundialitoUser();
        generalBet.User.Id = userProivider.UserId;
        generalBet.WinningTeamId = newBet.WinningTeamId;
        generalBet.GoldBootPlayerId = newBet.GoldenBootPlayerId;
        var res = generalBetsRepository.InsertGeneralBet(generalBet);
        Trace.TraceInformation("Posting new General Bet: {0}", generalBet);
        generalBetsRepository.Save();
        newBet.GeneralBetId = res.GeneralBetId;
        AddLog(ActionType.CREATE, String.Format("Posting new Generel Bet: {0}", res));
        return newBet;
    }

    [HttpPut]
    public UpdateGenralBetModel UpdateBet(int id, UpdateGenralBetModel bet)
    {
        if (dateTimeProvider.UTCNow > tournamentTimesUtils.GetGeneralBetsCloseTime())
            throw new ArgumentException("General bets are already closed for betting");
        var betToUpdate = new GeneralBet();
        betToUpdate.GeneralBetId = id;
        betToUpdate.WinningTeamId = bet.WinningTeamId;
        betToUpdate.GoldBootPlayerId = bet.GoldenBootPlayerId;
        betToUpdate.User = new MundialitoUser();
        betToUpdate.User.Id = userProivider.UserId;
        generalBetsRepository.UpdateGeneralBet(betToUpdate);
        generalBetsRepository.Save();
        Trace.TraceInformation("Updating General Bet: {0}", betToUpdate);
        AddLog(ActionType.UPDATE, String.Format("Updating new Generel Bet: {0}", betToUpdate));
        return bet;
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    [Route("{id}/resolve")]
    public void ResolveGeneralBet(int id, ResolveGeneralBetModel resolvedBet)
    {
        if (dateTimeProvider.UTCNow < tournamentTimesUtils.GetGeneralBetsResolveTime())
        {
            AddLog(ActionType.ERROR, "General bets are not closed for betting yet");
            throw new ArgumentException("General bets are not closed for betting yet");
        }

        var item = generalBetsRepository.GetGeneralBet(id);
        if (item == null)
        {
            AddLog(ActionType.ERROR, string.Format("General Bet '{0}' dosen't exits", id));
            throw new ObjectNotFoundException(string.Format("General Bet '{0}' dosen't exits", id));
        }

        Trace.TraceInformation("Resolved General Bet '{0}' with data: {1}", id, resolvedBet);
        item.Resolve(resolvedBet.PlayerIsRight, resolvedBet.TeamIsRight);
        generalBetsRepository.UpdateGeneralBet(item);
        generalBetsRepository.Save();
        AddLog(ActionType.UPDATE, String.Format("Resolved Generel Bet: {0}", item));
    }

    private void Validate()
    {
        if (generalBetsRepository.IsGeneralBetExists(userProivider.UserName))
        {
            AddLog(ActionType.ERROR, "You have already submitted your general bet, only update is permitted");
            throw new ArgumentException("You have already submitted your general bet, only update is permitted");
        }
        if (dateTimeProvider.UTCNow > tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            AddLog(ActionType.ERROR, "General bets are already closed for betting");
            throw new ArgumentException("General bets are already closed for betting");
        }
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
}

