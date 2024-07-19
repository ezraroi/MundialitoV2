using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.GeneralBets;
using Mundialito.Logic;
using Mundialito.Models;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private const string ObjectType = "User";
    private readonly IBetsRepository betsRepository;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly TournamentTimesUtils tournamentTimesUtils;
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly TableBuilder tableBuilder;
    private readonly MundialitoDbContext mundialitoDbContext;
    private readonly ILogger logger;

    public UsersController(ILogger<UsersController> logger, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor, UserManager<MundialitoUser> userManager, IBetsRepository betsRepository, IDateTimeProvider dateTimeProvider, TournamentTimesUtils tournamentTimesUtils, IGeneralBetsRepository generalBetsRepository, TableBuilder tableBuilder, MundialitoDbContext mundialitoDbContext)
    {
        this.actionLogsRepository = actionLogsRepository;
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
        this.betsRepository = betsRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.tournamentTimesUtils = tournamentTimesUtils;
        this.generalBetsRepository = generalBetsRepository;
        this.logger = logger;
        this.tableBuilder = tableBuilder;
        this.mundialitoDbContext = mundialitoDbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserWithPointsModel>> GetAllUsers()
    {
        var res = GetTableDetails(userManager.Users.ToList()).ToList();
        res.ForEach(user => IsAdmin(user));
        return Ok(res);
    }

    [HttpPost("follow/{username}")]
    [Authorize]
    public async Task<ActionResult> FollowUserAsync(string username)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var followee = await userManager.FindByNameAsync(username);
        if (followee == null)
            return NotFound(new ErrorMessage { Message = string.Format("No such user {}", username) });
        if (user.Id == followee.Id)
            return BadRequest(new ErrorMessage { Message = "You can't follow youself" });
        var userFollow = new UserFollow
        {
            FollowerId = user.Id,
            FolloweeId = followee.Id
        };
        logger.LogInformation("User {} added {} as follower", user.UserName, followee.UserName);
        mundialitoDbContext.UserFollows.Add(userFollow);
        mundialitoDbContext.SaveChanges();
        return Ok();
    }

    [HttpDelete("follow/{username}")]
    [Authorize]
    public async Task<ActionResult> UnfollowUserAsync(string username)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized();
        var followee = await userManager.FindByNameAsync(username);
        if (followee == null)
            return NotFound(new ErrorMessage { Message = string.Format("No such user {}", username) });
        var userFollow = await mundialitoDbContext.UserFollows
                .FirstOrDefaultAsync(uf => uf.FollowerId == user.Id && uf.Followee.UserName == username);
        if (userFollow != null)
        {
            mundialitoDbContext.UserFollows.Remove(userFollow);
            mundialitoDbContext.SaveChanges();
            return Ok();
        }
        else
            return NotFound(new ErrorMessage { Message = string.Format("You do not follow {}", followee.UserName) });
    }

    [HttpGet("me/followees")]
    [Authorize]
    public async Task<IList<UserModel>> GetFolloweesAsync()
    {
        return await GetFolloweesAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
    }

    [HttpGet("{username}/followees")]
    [Authorize]
    public async Task<IList<UserModel>> GetFolloweesAsync(string username)
    {
        return await mundialitoDbContext.UserFollows
            .Where(uf => uf.Follower.UserName == username)
            .Select(uf => uf.Followee)
            .Select(item => new UserModel(item))
            .ToListAsync();
    }

    [HttpGet("{username}/followers")]
    [Authorize]
    public async Task<IList<UserModel>> GetFollowersAsync(string username)
    {
        return await mundialitoDbContext.UserFollows
            .Where(uf => uf.Followee.UserName == username)
            .Select(uf => uf.Follower)
            .Select(item => new UserModel(item))
            .ToListAsync();
    }

    [HttpGet("me/followers")]
    [Authorize]
    public async Task<IList<UserModel>> GetFollowersAsync()
    {
        return await GetFollowersAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserModel>> GetUserByUsername(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
            return NotFound(new ErrorMessage { Message = string.Format("No such user '{0}'", username) });
        var userModel = new UserModel(user);
        betsRepository.GetUserBets(user.UserName).Where(bet => httpContextAccessor.HttpContext?.User.Identity.Name == username || !bet.IsOpenForBetting(dateTimeProvider.UTCNow)).ToList().ForEach(bet => userModel.AddBet(new BetViewModel(bet, dateTimeProvider.UTCNow)));
        var generalBet = generalBetsRepository.GetUserGeneralBet(username);
        if (generalBet != null)
            userModel.SetGeneralBet(new GeneralBetViewModel(generalBet, tournamentTimesUtils.GetGeneralBetsCloseTime()));
        return await IsAdmin(userModel);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserModel>> GetMe()
    {
        return await GetUserByUsername(httpContextAccessor.HttpContext?.User.Identity.Name);
    }

    [HttpGet("me/progress")]
    public async Task<ActionResult<IEnumerable<UserCompareModel>>> Progress()
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
            return Unauthorized(new ErrorMessage { Message = "User not found" });
        return Ok(CompareUsers(new List<MundialitoUser> { user }));
    }

    [HttpPost("MakeAdmin/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MakeAdmin(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorMessage { Message = "User not found" });
        user.Role = Role.Admin;
        logger.LogInformation("Making user {} admin", user.UserName);
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot remove user existing roles");
            return BadRequest(ModelState);
        }
        AddLog(ActionType.UPDATE, string.Format("Made user {0} admin", id));
        logger.LogInformation("User {} is now admin", user.UserName);
        return Ok();
    }


    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorMessage { Message = "User not found" });
        if (user.Role == Role.Active || user.Role == Role.Admin)
            return Ok();
        user.Role = Role.Active;
        logger.LogInformation("Activating user {}", user.UserName);
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to activate user {}", user.UserName);
            result.Errors.ToList().ForEach(error => logger.LogError("Error: {0}", error.Description));
            ModelState.AddModelError("", "Failed to activate user");
            return BadRequest(ModelState);
        }
        AddLog(ActionType.UPDATE, string.Format("Made user {0} active", id));
        logger.LogInformation("User {} is now active", user.UserName);
        return Ok();
    }

    [HttpDelete("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeActivate(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorMessage { Message = "User not found" });
        if (user.Role == Role.Disabled)
            return Ok();
        user.Role = Role.Disabled;
        logger.LogInformation("Disabling user {}", user.UserName);
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to disable user {}", user.UserName);
            result.Errors.ToList().ForEach(error => logger.LogError("Error: {0}", error.Description));
            ModelState.AddModelError("", "Failed to disable user");
            return BadRequest(ModelState);
        }
        AddLog(ActionType.UPDATE, string.Format("Made user {0} disabled", id));
        logger.LogInformation("User {} is now disabled", user.UserName);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorMessage { Message = "User not found" });
        logger.LogInformation("Deleting user {0} by {1}", id, user.UserName);
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot delete user");
            return BadRequest(ModelState);
        }
        logger.LogInformation("Deleted user {0}", id);
        AddLog(ActionType.DELETE, string.Format("Deleted user: {0}", id));
        return Ok();
    }

    [HttpGet("compare/{usera}/{userb}")]
    public async Task<ActionResult<IEnumerable<UserCompareModel>>> CompareUsers(string usera, string userb)
    {
        var userA = await userManager.FindByNameAsync(usera);
        var userB = await userManager.FindByNameAsync(userb);
        if (userA == null || userB == null)
            return NotFound(new ErrorMessage { Message = "User not found" });
        return Ok(CompareUsers(new List<MundialitoUser> { userA, userB }));
    }

    private IEnumerable<UserCompareModel> CompareUsers(List<MundialitoUser> users)
    {
        var allUsers = userManager.Users.Select((user) => new UserWithPointsModel(user)).ToList();
        var allBets = betsRepository.GetBets().Where(bet => bet.IsResolved()).ToList();
        var betsByDate = allBets.GroupBy(bet => bet.Game.Date.DayOfYear).ToDictionary(group => group.Key, group => group.ToList()).OrderBy(bet => bet.Key);
        var resEntries = new List<UserCompareModel>();
        foreach (var bets in betsByDate)
        {
            allUsers = tableBuilder.GetTable(allUsers, bets.Value, []).ToList();
            var userEntries = users.Select(user => allUsers.FirstOrDefault(tableUser => tableUser.Id == user.Id)).Where(entry => entry != null).ToList();
            if (userEntries.Count > 0)
            {
                logger.LogInformation("Adding compare entry for date {} with date {}", bets.Key, new DateTime(DateTime.Now.Year, 1, 1).AddDays(bets.Key - 1));
                var compareModel = new UserCompareModel()
                {
                    Date = new DateTime(DateTime.Now.Year, 1, 1).AddDays(bets.Key - 1),
                    Entries = userEntries.Select(userEntry => new CompareEntry()
                    {
                        Name = userEntry.Name,
                        Place = int.Parse(userEntry.Place)
                    }).ToList()
                };
                resEntries.Add(compareModel);
            }
        }
        if (betsByDate.Count() > 0)
        {
            var generalBets = generalBetsRepository.GetGeneralBets();
            allUsers = tableBuilder.GetTable(allUsers, [], generalBets.Where(bet => users.Select(user => user.Id).Contains(bet.User.Id)).ToList()).ToList();
            var finalTable = tableBuilder.GetTable(allUsers, [], generalBets);
            var finalUserEntries = users.Select(user => finalTable.FirstOrDefault(tableUser => tableUser.Id == user.Id)).Where(entry => entry != null).ToList();
            if (finalUserEntries.Count > 0)
            {
                var finalCompareModel = new UserCompareModel()
                {
                    Date = new DateTime(DateTime.Now.Year, 1, 1).AddDays(betsByDate.Last().Key),
                    Entries = finalUserEntries.Select(userEntry => new CompareEntry()
                    {
                        Name = userEntry.Name,
                        Place = int.Parse(userEntry.Place)
                    }).ToList()
                };
                resEntries.Add(finalCompareModel);
            }

        }
        return resEntries;
    }

    private async Task<ActionResult<UserModel>> IsAdmin(UserModel param)
    {
        var user = await userManager.FindByIdAsync(param.Id);
        if (user == null)
            return NotFound();
        param.IsAdmin = user.Role == Role.Admin;
        return Ok(param);
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
            logger.LogInformation("Exception during log. Exception: {0}", e.Message);
        }
    }

    private IEnumerable<UserWithPointsModel> GetTableDetails(IEnumerable<MundialitoUser> users)
    {
        return tableBuilder.GetTable(users.Select((user) => new UserWithPointsModel(user)), betsRepository.GetBets().ToList(), generalBetsRepository.GetGeneralBets().Where(bet => users.Select(user => user.Id).Contains(bet.User.Id)).ToList());
    }
}

