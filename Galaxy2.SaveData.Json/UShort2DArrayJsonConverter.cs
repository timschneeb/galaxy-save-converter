using System.Text.Json;
using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Json;

public class UShort2DArrayJsonConverter : JsonConverter<ushort[,]>
{
    public override ushort[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new ushort[0,0];
        }

        var list = JsonSerializer.Deserialize<List<List<ushort>>>(ref reader, options);
        if (list == null) return new ushort[0,0];
        var rows = list.Count;
        var cols = rows > 0 ? list[0].Count : 0;
        var arr = new ushort[rows, cols];
        for (var i = 0; i < rows; i++)
        {
            var row = list[i] ?? [];
            for (var j = 0; j < row.Count && j < cols; j++) arr[i, j] = row[j];
        }
        return arr;
    }

    public override void Write(Utf8JsonWriter writer, ushort[,] value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var rows = value.GetLength(0);
        var cols = value.GetLength(1);

        writer.WriteStartArray();
        for (var i = 0; i < rows; i++)
        {
            writer.WriteStartArray();
            for (var j = 0; j < cols; j++)
            {
                writer.WriteNumberValue(value[i, j]);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}