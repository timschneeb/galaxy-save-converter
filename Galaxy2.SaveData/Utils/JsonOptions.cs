using System.Text.Json;
using System.Text.Json.Serialization;
using Galaxy2.SaveData.Utils.Converters;

namespace Galaxy2.SaveData.Utils;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new UShort2DArrayJsonConverter(),
            new FixedString12JsonConverter(),
            new ByteArrayAsNumberArrayJsonConverter()
        }
    };
}