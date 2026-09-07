using Microsoft.AspNetCore.Authorization;

namespace Mundialito.Auth.Authorization;

/// <summary>
/// Succeeds when the caller's role, read from the database at request time, is one the
/// requirement allows.
///
/// This is the whole of the rule: no controller action performs a role check of its own.
/// </summary>
public class CurrentRoleHandler : AuthorizationHandler<CurrentRoleRequirement>
{
    private readonly ICurrentUserRoleProvider roleProvider;
    private readonly ILogger<CurrentRoleHandler> logger;

    public CurrentRoleHandler(ICurrentUserRoleProvider roleProvider, ILogger<CurrentRoleHandler> logger)
    {
        this.roleProvider = roleProvider;
        this.logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CurrentRoleRequirement requirement)
    {
        var role = await roleProvider.GetCurrentRoleAsync();
        // A null role means no authenticated caller, or a user that has since been deleted.
        // Leaving the requirement unmet produces a 403.
        if (role.HasValue && requirement.Allowed.Contains(role.Value))
        {
            context.Succeed(requirement);
            return;
        }
        logger.LogInformation("Denying {User}: current role is {Role}, allowed {Allowed}",
            context.User.Identity?.Name ?? "(anonymous)",
            role.HasValue ? role.Value.ToString() : "(none)",
            string.Join(",", requirement.Allowed));
    }
}
