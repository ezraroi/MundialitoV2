﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public IEnumerable<UserModel> GetAllUsers()
    {
        var res = GetTableDetails().ToList();
        res.ForEach(user => IsAdmin(user));
        return res;
    }

    [HttpGet("table")]
    public ActionResult<IEnumerable<UserModel>> GetTable()
    {
        return Ok(GetTableDetails());
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

    [HttpGet("GeneratePrivateKey/{email}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GeneratePrivateKey(string email)
    {
        logger.LogInformation("Generating private key for {}", email);
        return Ok(PrivateKeyValidator.GeneratePrivateKey(email));
    }

    [HttpPost("MakeAdmin/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MakeAdmin(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorMessage { Message = "User not found" });
        user.Role = Role.Admin;
        logger.LogInformation("Makeing user {} admin", user.UserName);
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot remove user existing roles");
            return BadRequest(ModelState);
        }
        AddLog(ActionType.UPDATE, string.Format("Made  user {0} admin", id));
        logger.LogInformation("User {} is now admin", user.UserName);
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

    private async Task<ActionResult<UserModel>> IsAdmin(UserModel param)
    {
        var user = await userManager.FindByIdAsync(param.Id);
        if (user == null)
            return NotFound();
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
            logger.LogInformation("Exception during log. Exception: {0}", e.Message);
        }
    }

    private IEnumerable<UserModel> GetTableDetails()
    {
        return tableBuilder.GetTable(userManager.Users, betsRepository.GetBets(), generalBetsRepository.GetGeneralBets());
    }
}

