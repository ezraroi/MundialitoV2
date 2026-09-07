using System.Reflection;
using Mundialito.Controllers;
using Mundialito.DAL;
using Mundialito.DAL.ActionLogs;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

/// <summary>
/// Audit logging must not share the transaction it audits. Every repository in a request
/// shares one MundialitoDbContext, so an audit write through that context commits whatever
/// business change happens to be pending - which is how a rejected bet update came to be
/// saved anyway (#171). <see cref="ActionLogger"/> opens its own scope; these tests fail the
/// build if anything takes the shared repository in order to log.
///
/// Their reach is constructor dependencies, so they cannot see a class that writes an
/// ActionLog through an injected MundialitoDbContext. Nothing does today - UsersController
/// holds the context for UserFollows, which is business data, not the audit trail - but that
/// is the blind spot, and closing it needs the shared unit-of-work work in item 1 of #172
/// rather than a bigger reflection query.
/// </summary>
[TestFixture]
public class AuditLoggingGuardTests
{
    private static IEnumerable<(Type Type, ParameterInfo Parameter)> ConstructorParameters() =>
        typeof(BetsController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => (t, p)));

    [Test]
    public void OnlyTheActionLoggerTakesTheActionLogsRepository()
    {
        var offenders = ConstructorParameters()
            .Where(x => x.Type != typeof(ActionLogger))
            .Where(x => x.Parameter.ParameterType == typeof(IActionLogsRepository))
            .Select(x => x.Type.Name)
            .Distinct()
            .ToList();

        Assert.That(offenders, Is.Empty,
            "These write through the request's shared DbContext to log; take IActionLogger instead: "
            + string.Join(", ", offenders));
    }

    [Test]
    public void TheActionLoggerOwnsItsContextRatherThanBorrowingTheRequests()
    {
        var parameters = typeof(ActionLogger).GetConstructors().SelectMany(c => c.GetParameters()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(parameters.Select(p => p.ParameterType), Does.Contain(typeof(IServiceScopeFactory)),
                "the logger must open its own scope");
            Assert.That(parameters.Select(p => p.ParameterType), Does.Not.Contain(typeof(MundialitoDbContext)),
                "taking the request's context defeats the point");
            Assert.That(parameters.Select(p => p.ParameterType), Does.Not.Contain(typeof(IActionLogsRepository)),
                "the injected repository is bound to the request's context");
        });
    }
}
