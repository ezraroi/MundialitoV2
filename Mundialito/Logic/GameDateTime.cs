namespace Mundialito.Logic;

public static class GameDateTime
{
    private static readonly TimeZoneInfo IsraelTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    /// <summary>DB and API convention: Unspecified values are UTC wall-clock.</summary>
    public static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    public static DateTime EnsureUtcKind(DateTime value) => ToUtc(value);

    /// <summary>Converts Israel local wall-clock (from FIFA schedule) to UTC for storage.</summary>
    public static DateTime FromIsraelLocal(int year, int month, int day, int hour, int minute = 0)
    {
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, IsraelTimeZone);
    }
}
