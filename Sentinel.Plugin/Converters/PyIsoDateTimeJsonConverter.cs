using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentinel.Plugin.Converters
{
    public class PyIsoDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"));
        }
    }

}
