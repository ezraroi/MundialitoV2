using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.Logic;
using Mundialito.Models;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private const String ObjectType = "User";
    private readonly IUsersRetriver usersRetriver;
    private readonly ILoggedUserProvider loggedUserProvider;
    private readonly IUsersRepository usersRepository;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<MundialitoUser> userManager;

    public UsersController(IUsersRetriver usersRetriver, ILoggedUserProvider loggedUserProvider, IUsersRepository usersRepository, IActionLogsRepository actionLogsRepository, IHttpContextAccessor httpContextAccessor, UserManager<MundialitoUser> userManager)
    {
        this.usersRetriver = usersRetriver;
        this.loggedUserProvider = loggedUserProvider;
        this.usersRepository = usersRepository;
        this.actionLogsRepository = actionLogsRepository;
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
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
    public Task<ActionResult<UserModel>> GetUserByUsername(String username)
    {
        var res = usersRetriver.GetUser(username, loggedUserProvider.UserName == username);
        return IsAdmin(res);
    }

    [HttpGet("me")]
    public Task<ActionResult<UserModel>> GetMe()
    {
        return GetUserByUsername(loggedUserProvider.UserName);
    }

    [HttpGet("GeneratePrivateKey/{email}")]
    [Authorize(Roles = "Admin")]
    public HttpResponseMessage GeneratePrivateKey(string email)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(PrivateKeyValidator.GeneratePrivateKey(email), Encoding.UTF8, "text/plain")
        };
        return response;
    }

    [HttpPost("MakeAdmin/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MakeAdmin(String id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("User not found");
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
    public void DeleteUser(String id)
    {
        Trace.TraceInformation("Deleting user {0} by {1}", id, loggedUserProvider.UserName);
        usersRepository.DeleteUser(id);
        usersRepository.Save();
        AddLog(ActionType.DELETE, string.Format("Deleted user: {0}", id));
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
            actionLogsRepository.InsertLogAction(ActionLog.Create(actionType, ObjectType, message, httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value));
            actionLogsRepository.Save();
        }
        catch (Exception e)
        {
            Trace.TraceError("Exception during log. Exception: {0}", e.Message);
        }
    }

    private IEnumerable<UserModel> GetTableDetails()
    {
        var res = usersRetriver.GetAllUsers();
        var yesterdayPlaces = new Dictionary<string, int>(res.Count);
        res = res.OrderByDescending(user => user.YesterdayPoints).ToList();
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

