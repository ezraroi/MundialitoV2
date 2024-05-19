using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.GeneralBets;
using Mundialito.Logic;
using Mundialito.Models;
using System.Diagnostics;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private const String ObjectType = "User";
    private readonly IBetsRepository betsRepository;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly TournamentTimesUtils tournamentTimesUtils;
    private readonly IGeneralBetsRepository generalBetsRepository;

    public UsersController(IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor, UserManager<MundialitoUser> userManager, IBetsRepository betsRepository, IDateTimeProvider dateTimeProvider, TournamentTimesUtils tournamentTimesUtils, IGeneralBetsRepository generalBetsRepository)
    {
        this.actionLogsRepository = actionLogsRepository;
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
        this.betsRepository = betsRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.tournamentTimesUtils = tournamentTimesUtils;
        this.generalBetsRepository = generalBetsRepository;
    }

    [HttpGet]
    public IEnumerable<UserModel> GetAllUsers()
    {
        var res = GetTableDetails().ToList();
        res.ForEach(user => IsAdmin(user));
        return res;
    }

    [HttpGet("table")]
    public IEnumerable<UserModel> GetTable()
    {
        return GetTableDetails();
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserModel>> GetUserByUsername(String username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            return NotFound(new ErrorMessage { Message = string.Format("No such user '{0}'", username) });
        }
        var userModel = new UserModel(user);
        betsRepository.GetUserBets(user.UserName).Where(bet => httpContextAccessor.HttpContext?.User.Identity.Name == username || !bet.IsOpenForBetting(dateTimeProvider.UTCNow)).ToList().ForEach(bet => userModel.AddBet(new BetViewModel(bet, dateTimeProvider.UTCNow)));
        var generalBet = generalBetsRepository.GetUserGeneralBet(username);
        if (generalBet != null)
        {
            userModel.SetGeneralBet(new GeneralBetViewModel(generalBet, tournamentTimesUtils.GetGeneralBetsCloseTime()));
        }
        return await IsAdmin(userModel);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserModel>> GetMe()
    {
        return await GetUserByUsername(httpContextAccessor.HttpContext?.User.Identity.Name);
    }

    [HttpGet("GeneratePrivateKey/{email}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GeneratePrivateKey(string email)
    {
        return Ok(PrivateKeyValidator.GeneratePrivateKey(email));
    }

    [HttpPost("MakeAdmin/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MakeAdmin(String id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new ErrorMessage { Message = "User not found" });
        }
        user.Role = Role.Admin;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot remove user existing roles");
            return BadRequest(ModelState);
        }
        AddLog(ActionType.UPDATE, string.Format("Made  user {0} admin", id));
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(String id)
    {
        Trace.TraceInformation("Deleting user {0} by {1}", id, httpContextAccessor.HttpContext?.User.Identity.Name);
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new ErrorMessage { Message = "User not found" });
        }
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot delete user");
            return BadRequest(ModelState);
        }
        AddLog(ActionType.DELETE, string.Format("Deleted user: {0}", id));
        return Ok();
    }

    private async Task<ActionResult<UserModel>> IsAdmin(UserModel param)
    {
        var user = await userManager.FindByIdAsync(param.Id);
        if (user == null)
        {
            return NotFound();
        }
        param.IsAdmin = user.Role == Role.Admin;
        return Ok(param);
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
            Trace.TraceError("Exception during log. Exception: {0}", e.Message);
        }
    }

    private IEnumerable<UserModel> GetTableDetails()
    {
        var users = userManager.Users.ToDictionary(user => user.Id, user => new UserModel(user));
        var yesterdayPlaces = new Dictionary<string, int>(users.Count);
        var allBets = betsRepository.GetBets();
        allBets.Where(bet => users.ContainsKey(bet.User.Id)).Where(bet => !bet.IsOpenForBetting(dateTimeProvider.UTCNow)).ToList().ForEach(bet => users[bet.User.Id].AddBet(new BetViewModel(bet, dateTimeProvider.UTCNow)));
        allBets.Where(bet => users.ContainsKey(bet.User.Id)).Where(bet => !bet.IsOpenForBetting(dateTimeProvider.UTCNow)).Where(bet => bet.Game.Date < dateTimeProvider.UTCNow.Subtract(TimeSpan.FromDays(1))).ToList().ForEach(bet => users[bet.User.Id].YesterdayPoints += bet.Points.HasValue ? bet.Points.Value : 0);
        generalBetsRepository.GetGeneralBets().ToList().ForEach(generalBet =>
            {
                users[generalBet.User.Id].SetGeneralBet(new GeneralBetViewModel(generalBet, tournamentTimesUtils.GetGeneralBetsCloseTime()));
            });
        var res = users.Values.ToList().OrderByDescending(user => user.YesterdayPoints).ToList();
        for (int i = 0; i < res.Count; i++)
        {
            yesterdayPlaces.Add(res[i].Id, i + 1);
        }
        res = res.OrderByDescending(user => user.Points).ToList();
        for (int i = 0; i < res.Count; i++)
        {
            res[i].Place = (i + 1).ToString();
            var diff = yesterdayPlaces[res[i].Id] - (i + 1);
            res[i].PlaceDiff = string.Format("{0}{1}", diff > 0 ? "+" : string.Empty, diff);
            res[i].TotalMarks = res[i].Marks + res[i].Results;
        }
        return res;
    }
}

