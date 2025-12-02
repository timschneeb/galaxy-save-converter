using System.Text.Json;
using System.Text.Json.Serialization;
using SMGSaveData.Galaxy2.Utils.Converters;

namespace SMGSaveData.Galaxy2.Utils;

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