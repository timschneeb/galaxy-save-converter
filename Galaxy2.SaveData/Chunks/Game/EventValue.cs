using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStorageEventValue
{
    [JsonPropertyName("event_value")]
    public List<GameEventValue> EventValues { get; set; } = [];

    public static SaveDataStorageEventValue ReadFrom(BinaryReader reader, int dataSize)
    {
        var ev = new SaveDataStorageEventValue();
        var count = dataSize / 4;
        ev.EventValues = new List<GameEventValue>(count);
        for (var i = 0; i < count; i++)
            ev.EventValues.Add(new GameEventValue { Key = reader.ReadUInt16(), Value = reader.ReadUInt16() });
        return ev;
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        foreach (var v in EventValues)
        {
            writer.WriteUInt16(v.Key);
            writer.WriteUInt16(v.Value);
        }
    }
}

public struct GameEventValue
{
    [JsonPropertyName("key")]
    public ushort Key { get; set; }
    [JsonPropertyName("value")]
    public ushort Value { get; set; }
}