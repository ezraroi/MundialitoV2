using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Mundialito.DAL.Accounts;

namespace Mundialito.Logic;

public class LoggedUserProvider : ILoggedUserProvider
{
    private IHttpContextAccessor httpContextAccessor;
    private UserManager<MundialitoUser> userManager;

    public LoggedUserProvider(IHttpContextAccessor httpContextAccessor, UserManager<MundialitoUser> userManager)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;

    }

    public String UserId
    {
        get { return httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;}
    }

    public String UserName
    {
        get { return httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Name).Value;}
    }
}
