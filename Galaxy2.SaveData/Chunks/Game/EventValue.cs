using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageEventValue
    {
        [JsonPropertyName("event_value")]
        public List<GameEventValue> EventValues { get; set; } = new List<GameEventValue>();

        public static SaveDataStorageEventValue ReadFrom(BinaryReader reader, int dataSize)
        {
            var ev = new SaveDataStorageEventValue();
            var count = dataSize / 4;
            ev.EventValues = new List<GameEventValue>(count);
            for (var i = 0; i < count; i++)
                ev.EventValues.Add(new GameEventValue { Key = reader.ReadUInt16Be(), Value = reader.ReadUInt16Be() });
            return ev;
        }
    }

    public struct GameEventValue
    {
        [JsonPropertyName("key")]
        public ushort Key { get; set; }
        [JsonPropertyName("value")]
        public ushort Value { get; set; }
    }
}
