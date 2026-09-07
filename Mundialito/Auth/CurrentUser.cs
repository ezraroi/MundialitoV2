using Microsoft.AspNetCore.Identity;
using Mundialito.DAL.Accounts;

namespace Mundialito.Auth;

/// <summary>
/// Registered scoped, so the memoized value below lives for exactly one request - a per
/// request lookup, not a cache, and it carries no staleness of its own. Same shape as
/// <see cref="Authorization.ICurrentUserRoleProvider"/>, which is deliberately left alone:
/// it is on the authorization path and its own query costs one round-trip.
///
/// Resolution goes through UserManager rather than querying the context directly, so the
/// instance handed back is tracked by the request's context exactly as before. Call sites
/// depend on that: GeneralBetsController assigns it to a new GeneralBet's navigation
/// property, and AccountController passes it to ChangePasswordAsync.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly UserManager<MundialitoUser> userManager;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<CurrentUser> logger;

    private bool resolved;
    private MundialitoUser? user;

    public CurrentUser(UserManager<MundialitoUser> userManager, IHttpContextAccessor httpContextAccessor, ILogger<CurrentUser> logger)
    {
        this.userManager = userManager;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public async Task<MundialitoUser?> GetAsync()
    {
        if (resolved)
            return user;
        resolved = true;
        user = await LookupAsync();
        return user;
    }

    private async Task<MundialitoUser?> LookupAsync()
    {
        // UserName, never ClaimTypes.NameIdentifier: the token carries a 'sub' claim holding a
        // constant from configuration, and the default inbound claim map turns 'sub' into
        // NameIdentifier - so the principal has two of them and FindFirstValue returns the
        // constant. See CurrentUserRoleProvider, which learned this the hard way.
        var userName = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            logger.LogWarning("Authenticated principal carries no name claim; treating the caller as unauthenticated");
            return null;
        }
        return await userManager.FindByNameAsync(userName);
    }
}
