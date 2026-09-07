namespace Mundialito.DAL.ActionLogs;

/// <summary>
/// Writes each audit row through a context of its own, resolved from a fresh scope.
///
/// It has to be a scope rather than an <c>IDbContextFactory</c>: MundialitoDbContext has no
/// DbContextOptions constructor and configures Npgsql in OnConfiguring, so AddDbContextFactory
/// cannot build one.
///
/// Registered as a singleton - IServiceScopeFactory, IHttpContextAccessor and ILogger are all
/// singletons, and it holds no per-request state of its own.
/// </summary>
public class ActionLogger : IActionLogger
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<ActionLogger> logger;

    public ActionLogger(IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor, ILogger<ActionLogger> logger)
    {
        this.scopeFactory = scopeFactory;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public void Log(ActionType actionType, string objectType, string message)
    {
        // A failure to record the audit trail must not fail the action being audited - that
        // was true of all five copies of this method and is kept deliberately.
        try
        {
            using var scope = scopeFactory.CreateScope();
            var logs = scope.ServiceProvider.GetRequiredService<IActionLogsRepository>();
            logs.InsertLogAction(ActionLog.Create(actionType, objectType, message,
                httpContextAccessor.HttpContext?.User.Identity?.Name));
            logs.Save();
        }
        catch (Exception e)
        {
            logger.LogError("Exception during log. Exception: {0}", e.Message);
        }
    }
}
