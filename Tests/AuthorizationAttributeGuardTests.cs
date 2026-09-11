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

    /// <summary>The three game writes. Named rather than left to the count below, so a
    /// regression says which action lost its policy. SimulateGame is deliberately not one of
    /// them - see SimulateGameIsOpenToEveryActivePlayer.</summary>
    [Test]
    public void AdminOnlyGameActionsAreAllGated()
    {
        var adminActions = new[] { "PostGame", "PutGame", "DeleteGame" };

        var gated = typeof(GamesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => adminActions.Contains(m.Name))
            .Where(m => m.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
                .Any(a => a.Policy == Policies.AdminOnly))
            .Select(m => m.Name)
            .ToList();

        Assert.That(gated, Is.EquivalentTo(adminActions));
    }

    /// <summary>SimulateGame is a what-if, not a write: it reads everything detached and
    /// cannot persist, and the client has shown its panel to every signed-in user since the
    /// frontend-only admin guard was dropped. #172 item 3 filed it with the PostGame/PutGame/
    /// DeleteGame writes and made it AdminOnly, which 403'd that button for every player.
    /// Pinned so it is not swept back in with its siblings - and so it does not fall the
    /// other way either, to the bare class level [Authorize] it carried before, which would
    /// let accounts still waiting for approval run a full table recompute.</summary>
    [Test]
    public void SimulateGameIsOpenToEveryActivePlayer()
    {
        var policies = typeof(GamesController)
            .GetMethod(nameof(GamesController.SimulateGame))!
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Select(a => a.Policy)
            .ToList();

        Assert.That(policies, Is.EqualTo(new[] { Policies.ActiveOrAdmin }));
    }

    /// <summary>DeletePlayer is the action that stands between an admin misclick and other
    /// users' general bets - the FK to Players cascades, so an ungated delete would take
    /// those bets with it. Named rather than left to the count below, which is order blind:
    /// dropping the policy here and adding one elsewhere would keep the count green.</summary>
    [Test]
    public void AdminOnlyPlayerActionsAreAllGated()
    {
        var adminActions = new[] { "PostPlayer", "DeletePlayer" };

        var gated = typeof(PlayersController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => adminActions.Contains(m.Name))
            .Where(m => m.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
                .Any(a => a.Policy == Policies.AdminOnly))
            .Select(m => m.Name)
            .ToList();

        Assert.That(gated, Is.EquivalentTo(adminActions));
    }

    [Test]
    public void ExpectedNumberOfActionsAreRoleGated()
    {
        var byPolicy = AuthorizedActions()
            .Where(x => !string.IsNullOrEmpty(x.Attribute.Policy))
            .GroupBy(x => x.Attribute.Policy!)
            .ToDictionary(g => g.Key, g => g.Count());

        // 5 player-facing actions (2 bets - the mybet upsert and delete - plus 2 general
        // bets, plus SimulateGame, which is a read-only what-if and was briefly AdminOnly),
        // 16 admin actions (PostPlayer and DeletePlayer let an admin edit the golden boot
        // player list).
        // A silently dropped attribute would leave an endpoint open to any signed-in user.
        Assert.Multiple(() =>
        {
            Assert.That(byPolicy.GetValueOrDefault(Policies.ActiveOrAdmin), Is.EqualTo(5));
            Assert.That(byPolicy.GetValueOrDefault(Policies.AdminOnly), Is.EqualTo(16));
        });
    }
}
