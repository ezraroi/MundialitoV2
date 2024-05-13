namespace Mundialito.DAL.ActionLogs;

public class ActionLogsRepository : GenericRepository<ActionLog>, IActionLogsRepository
{
    public ActionLogsRepository(MundialitoDbContext context)
        : base(context)
    {
        
    }

    public IEnumerable<ActionLog> GetAllLogs()
    {
        return Get();
    }

    public ActionLog InsertLogAction(ActionLog logAction)
    {
        return Insert(logAction);
    }
}
