namespace Mundialito.DAL.ActionLogs;

/// <summary>
/// Writes the audit trail. The one rule: it must never share a transaction with the work it
/// is describing. Every repository in a request holds the same scoped MundialitoDbContext,
/// so a log written through that context calls SaveChanges on whatever business change is
/// pending - which is how a bet update that was about to be rejected got committed anyway
/// (#171). Implementations open their own unit of work.
/// </summary>
public interface IActionLogger
{
    /// <param name="objectType">The kind of thing acted on - "Bet", "Game", "GeneralBet",
    /// "User". Stored as-is; existing rows and the forensic queries that read them depend
    /// on these strings.</param>
    void Log(ActionType actionType, string objectType, string message);
}
