using System.Text.Json;
using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Json
{
    /// <summary>
    /// Serializes byte[] as a JSON number array instead of base64 strings.
    /// Also supports reading either a number array or a base64 string (for backward compatibility).
    /// </summary>
    public sealed class ByteArrayAsNumberArrayJsonConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null!;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<byte>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetByte(out byte b))
                        {
                            list.Add(b);
                            continue;
                        }

                        if (reader.TryGetInt32(out int i))
                        {
                            if (i < 0 || i > 255)
                                throw new JsonException($"Numeric array element out of range for byte: {i}");
                            list.Add((byte)i);
                            continue;
                        }

                        throw new JsonException("Unable to parse number as byte.");
                    }

                    throw new JsonException("Expected number token when reading byte[]");
                }

                return list.ToArray();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                // Accept legacy base64-encoded strings when reading.
                var s = reader.GetString();
                if (s is null)
                    throw new JsonException("Expected non-null string when reading byte[]");
                try
                {
                    return Convert.FromBase64String(s);
                }
                catch (FormatException ex)
                {
                    throw new JsonException("Invalid base64 for byte[]", ex);
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach (var b in value)
            {
                writer.WriteNumberValue(b);
            }
            writer.WriteEndArray();
        }
    }
}
