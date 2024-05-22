using Mundialito.DAL;
using Mundialito.DAL.ActionLogs;

namespace DBBackup.EntityBackuper;

class ActionLogsEntityBackuper : AbstractEntityBackuper<ActionLog>
{
    public ActionLogsEntityBackuper(string directoryName, MundialitoDbContext mundialitoDbContext)
        : base(directoryName, "Action Logs", mundialitoDbContext)
    {

    }

    protected override List<string> GetFieldsToBackup()
    {
        return new List<string>() { "ActionLogId", "Username", "Type", "ObjectType", "Timestamp", "Message" };
    }

    protected override List<ActionLog> GetAllEntites()
    {
        var repository = new ActionLogsRepository(mundialitoDbContext);
        return repository.GetAllLogs().ToList();
    }

    protected override object ProcessValue(object obj, string propName)
    {
        if (propName == "Type")
            return Enum.GetName(typeof(ActionType), obj);
        return obj;
    }
}

