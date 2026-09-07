using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Mundialito.Auth.Authorization;
using Mundialito.DAL.Accounts;

namespace Tests;

[TestFixture]
public class CurrentRoleHandlerTests
{
    private sealed class FakeCurrentUserRoleProvider : ICurrentUserRoleProvider
    {
        private readonly Role? role;
        public int Calls { get; private set; }

        public FakeCurrentUserRoleProvider(Role? role) => this.role = role;

        public Task<Role?> GetCurrentRoleAsync()
        {
            Calls++;
            return Task.FromResult(role);
        }
    }

    private static ClaimsPrincipal Authenticated(string userName = "someone", string? tokenRole = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, userName) };
        // A role claim may still be present in an old token issued before the fix.
        // Nothing should ever consult it.
        if (tokenRole != null)
            claims.Add(new Claim(ClaimTypes.Role, tokenRole));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static async Task<bool> Evaluate(Role? roleInDb, CurrentRoleRequirement requirement, ClaimsPrincipal user)
    {
        var handler = new CurrentRoleHandler(new FakeCurrentUserRoleProvider(roleInDb), NullLogger<CurrentRoleHandler>.Instance);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static CurrentRoleRequirement ActiveOrAdmin => new(Role.Active, Role.Admin);
    private static CurrentRoleRequirement AdminOnly => new(Role.Admin);

    [Test]
    public async Task ActiveOrAdmin_UserIsActiveInDb_Succeeds()
    {
        Assert.That(await Evaluate(Role.Active, ActiveOrAdmin, Authenticated()), Is.True);
    }

    [Test]
    public async Task ActiveOrAdmin_UserIsAdminInDb_Succeeds()
    {
        Assert.That(await Evaluate(Role.Admin, ActiveOrAdmin, Authenticated()), Is.True);
    }

    [Test]
    public async Task ActiveOrAdmin_UserIsDisabledInDb_Fails()
    {
        Assert.That(await Evaluate(Role.Disabled, ActiveOrAdmin, Authenticated()), Is.False);
    }

    [Test]
    public async Task ActiveOrAdmin_TokenSaysDisabledButDbSaysActive_Succeeds()
    {
        // Sentry 64032504: the player was activated after their 60 day token was minted.
        Assert.That(await Evaluate(Role.Active, ActiveOrAdmin, Authenticated(tokenRole: "Disabled")), Is.True);
    }

    [Test]
    public async Task ActiveOrAdmin_TokenSaysActiveButDbSaysDisabled_Fails()
    {
        // The other direction: deactivation must take effect before the token expires.
        Assert.That(await Evaluate(Role.Disabled, ActiveOrAdmin, Authenticated(tokenRole: "Active")), Is.False);
    }

    [Test]
    public async Task AdminOnly_TokenSaysAdminButDbSaysActive_Fails()
    {
        Assert.That(await Evaluate(Role.Active, AdminOnly, Authenticated(tokenRole: "Admin")), Is.False);
    }

    [Test]
    public async Task AdminOnly_UserIsAdminInDb_Succeeds()
    {
        Assert.That(await Evaluate(Role.Admin, AdminOnly, Authenticated()), Is.True);
    }

    [Test]
    public async Task UserNoLongerExists_Fails()
    {
        Assert.That(await Evaluate(null, ActiveOrAdmin, Authenticated()), Is.False);
    }

    [Test]
    public async Task UnauthenticatedPrincipal_Fails()
    {
        Assert.That(await Evaluate(null, ActiveOrAdmin, new ClaimsPrincipal(new ClaimsIdentity())), Is.False);
    }

    [Test]
    public void Requirement_RejectsAnEmptyRoleList()
    {
        Assert.Throws<ArgumentException>(() => new CurrentRoleRequirement());
    }
}
