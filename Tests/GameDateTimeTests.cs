using System.Text.Json;
using Mundialito.Logic;

namespace Tests;

public class GameDateTimeTests
{
    [Test]
    public void FromIsraelLocal_MexicoOpener_Is19Utc()
    {
        var utc = GameDateTime.FromIsraelLocal(2026, 6, 11, 22, 0);
        Assert.That(utc.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(utc, Is.EqualTo(new DateTime(2026, 6, 11, 19, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void ToUtc_Unspecified_TreatsAsUtcWallClock()
    {
        var unspecified = new DateTime(2026, 6, 11, 19, 0, 0, DateTimeKind.Unspecified);
        var utc = GameDateTime.ToUtc(unspecified);
        Assert.That(utc.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(utc.Hour, Is.EqualTo(19));
    }

    [Test]
    public void JsonConverter_WritesUtcWithZ()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());

        var json = JsonSerializer.Serialize(new DateTime(2026, 6, 11, 19, 0, 0, DateTimeKind.Utc), options);
        Assert.That(json, Does.Contain("2026-06-11T19:00:00"));
        Assert.That(json, Does.EndWith("Z\""));
    }
}
