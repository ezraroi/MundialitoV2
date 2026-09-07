using Microsoft.AspNetCore.Authorization;
using Mundialito.DAL.Accounts;

namespace Mundialito.Auth.Authorization;

/// <summary>
/// Requires that the caller's current role, as stored in the database, is one of
/// <see cref="Allowed"/>.
/// </summary>
public class CurrentRoleRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<Role> Allowed { get; }

    public CurrentRoleRequirement(params Role[] allowed)
    {
        if (allowed == null || allowed.Length == 0)
            throw new ArgumentException("A role requirement must allow at least one role", nameof(allowed));
        Allowed = allowed;
    }
}
