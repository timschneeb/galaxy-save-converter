using System.Text.Json;
using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.String;

public class FixedString12JsonConverter : JsonConverter<FixedString12>
{
    public override FixedString12 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return new FixedString12(string.Empty);
            case JsonTokenType.String:
                return new FixedString12(reader.GetString() ?? string.Empty);
            default:
            {
                // For numbers, booleans, objects, arrays: capture raw JSON text for the value
                using var doc = JsonDocument.ParseValue(ref reader);
                var raw = doc.RootElement.GetRawText();
                return new FixedString12(raw);
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, FixedString12 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}