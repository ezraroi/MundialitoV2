using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Mundialito.Controllers;

namespace Tests;

/// <summary>
/// No action may bind a persistence entity as its request body. Doing so is mass assignment
/// over the row - every column the client cares to send, primary key included - and it is the
/// same defect as the client-supplied GameId that let a bet be moved between games (#171),
/// one level worse because there is no explicit copy step to audit. Request bodies are DTOs
/// in Mundialito.Models, which carry only the fields a caller is allowed to set.
/// </summary>
[TestFixture]
public class RequestBodyGuardTests
{
    private const string EntityNamespacePrefix = "Mundialito.DAL";

    /// <summary>Complex parameters of public actions - the ones model binding fills from the
    /// body. Route and query parameters are primitives and never match the rule below.</summary>
    private static IEnumerable<(string Where, Type Type)> ActionParameters()
    {
        var controllers = typeof(BetsController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);
        foreach (var controller in controllers)
        foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        foreach (var parameter in action.GetParameters())
            yield return ($"{controller.Name}.{action.Name}({parameter.Name})", parameter.ParameterType);
    }

    [Test]
    public void NoActionBindsAPersistenceEntityAsItsRequestBody()
    {
        var offenders = ActionParameters()
            .Where(x => x.Type.Namespace?.StartsWith(EntityNamespacePrefix, StringComparison.Ordinal) == true)
            .Select(x => $"{x.Where} -> {x.Type.Name}")
            .ToList();

        Assert.That(offenders, Is.Empty,
            "These bind an entity straight off the wire; give them a DTO in Mundialito.Models: "
            + string.Join(", ", offenders));
    }
}
