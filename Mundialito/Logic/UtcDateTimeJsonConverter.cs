using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mundialito.Logic;

public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        var parsed = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return GameDateTime.ToUtc(parsed);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(GameDateTime.ToUtc(value).ToString("o"));
    }
}

public class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var parsed = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return GameDateTime.ToUtc(parsed);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(GameDateTime.ToUtc(value.Value).ToString("o"));
    }
}
