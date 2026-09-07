using Microsoft.EntityFrameworkCore;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;

namespace Mundialito.Auth.Authorization;

/// <summary>
/// Database backed implementation of <see cref="ICurrentUserRoleProvider"/>.
/// Registered scoped, so the memoized value below lives for exactly one request - it is a
/// per request lookup, not a cache, and carries no staleness of its own.
/// </summary>
public class CurrentUserRoleProvider : ICurrentUserRoleProvider
{
    private readonly MundialitoDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<CurrentUserRoleProvider> logger;

    private bool resolved;
    private Role? role;

    public CurrentUserRoleProvider(MundialitoDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserRoleProvider> logger)
    {
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public async Task<Role?> GetCurrentRoleAsync()
    {
        // Several actions ask for the role more than once per request (the policy handler
        // and then the action itself), so only hit the database the first time.
        if (resolved)
            return role;
        resolved = true;
        role = await LookupAsync();
        return role;
    }

    private async Task<Role?> LookupAsync()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        // Match on the user name rather than ClaimTypes.NameIdentifier. The token carries a
        // 'sub' claim holding a fixed string from configuration (JwtRegisteredClaimNamesSub),
        // and the default inbound claim map turns 'sub' into ClaimTypes.NameIdentifier - so
        // the principal ends up with two NameIdentifier claims and FindFirstValue returns the
        // constant one, never the user id. UserName is unambiguous, uniquely indexed, and is
        // what every other lookup in this codebase already uses.
        var userName = user.Identity.Name;
        if (string.IsNullOrEmpty(userName))
        {
            logger.LogWarning("Authenticated principal carries no name claim; denying");
            return null;
        }
        return await context.Users.Where(u => u.UserName == userName).Select(u => (Role?)u.Role).FirstOrDefaultAsync();
    }
}
