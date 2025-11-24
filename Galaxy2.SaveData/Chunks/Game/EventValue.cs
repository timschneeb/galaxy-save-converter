using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageEventValue
    {
        [JsonPropertyName("event_value")]
        public List<GameEventValue> EventValues { get; set; } = [];
    }

    public struct GameEventValue
    {
        [JsonPropertyName("key")]
        public ushort Key { get; set; }
        [JsonPropertyName("value")]
        public ushort Value { get; set; }
    }
}
