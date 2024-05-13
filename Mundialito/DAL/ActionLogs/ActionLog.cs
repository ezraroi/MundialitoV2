using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mundialito.DAL.ActionLogs;

public class ActionLog
{
    public ActionLog()
    {

    }

    public int ActionLogId { get; set; }

    [Required]
    public required String Username { get; set; }

    [Required]
    public required ActionType Type { get; set; }

    [Required]
    public required String ObjectType { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public required DateTime Timestamp { get; set; }

    [Required]
    public required String Message { get; set; }

    public static ActionLog Create(ActionType logType, String objectType, String message, String username)
    {
        // TODO: Fix this tp the logged user
        // https://stackoverflow.com/questions/30701006/how-to-get-the-current-logged-in-user-id-in-asp-net-core
        return new ActionLog() 
        {
            Message = message,
            ObjectType = objectType,
            Username = username,
            Type = logType,
            Timestamp = DateTime.UtcNow
        };
    }
}
