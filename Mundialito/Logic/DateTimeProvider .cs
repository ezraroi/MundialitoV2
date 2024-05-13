
namespace Mundialito.Logic;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UTCNow { get { return DateTime.Now.ToUniversalTime(); } }
}
