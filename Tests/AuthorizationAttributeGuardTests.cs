using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mundialito.Auth.Authorization;
using Mundialito.Controllers;

namespace Tests;

/// <summary>
/// Role based [Authorize(Roles = "...")] reads the role claim baked into the caller's 60 day
/// JWT. That claim is no longer issued, so any attribute that reappears would deny every
/// user in production. These tests fail the build instead.
/// </summary>
[TestFixture]
public class AuthorizationAttributeGuardTests
{
    private static readonly string[] KnownPolicies = { Policies.ActiveOrAdmin, Policies.AdminOnly };

    /// <summary>Every [Authorize] in the API, on a controller or on an action, with a
    /// human readable location so a failure names the offender.</summary>
    private static IEnumerable<(string Where, AuthorizeAttribute Attribute)> AuthorizedActions()
    {
        var controllers = typeof(BetsController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);
        foreach (var controller in controllers)
        {
            foreach (var attribute in controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
                yield return ($"{controller.Name} (controller)", attribute);
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            foreach (var attribute in method.GetCustomAttributes<AuthorizeAttribute>(inherit: false))
                yield return ($"{controller.Name}.{method.Name}", attribute);
        }
    }

    [Test]
    public void NoActionUsesRoleBasedAuthorize()
    {
        var offenders = AuthorizedActions()
            .Where(x => !string.IsNullOrEmpty(x.Attribute.Roles))
            .Select(x => $"{x.Where} -> Roles=\"{x.Attribute.Roles}\"")
            .ToList();

        Assert.That(offenders, Is.Empty,
            "These actions still authorize on the stale role claim: " + string.Join(", ", offenders));
    }

    [Test]
    public void EveryPolicyNameIsRegistered()
    {
        var unknown = AuthorizedActions()
            .Where(x => !string.IsNullOrEmpty(x.Attribute.Policy) && !KnownPolicies.Contains(x.Attribute.Policy))
            .Select(x => $"{x.Where} -> Policy=\"{x.Attribute.Policy}\"")
            .ToList();

        Assert.That(unknown, Is.Empty,
            "These actions reference a policy that Program.cs does not register: " + string.Join(", ", unknown));
    }

    [Test]
    public void ExpectedNumberOfActionsAreRoleGated()
    {
        var byPolicy = AuthorizedActions()
            .Where(x => !string.IsNullOrEmpty(x.Attribute.Policy))
            .GroupBy(x => x.Attribute.Policy!)
            .ToDictionary(g => g.Key, g => g.Count());

        // 4 player-facing writes (2 bets - the mybet upsert and delete - plus 2 general
        // bets), 14 admin actions.
        // A silently dropped attribute would leave an endpoint open to any signed-in user.
        Assert.Multiple(() =>
        {
            Assert.That(byPolicy.GetValueOrDefault(Policies.ActiveOrAdmin), Is.EqualTo(4));
            Assert.That(byPolicy.GetValueOrDefault(Policies.AdminOnly), Is.EqualTo(14));
        });
    }
}
