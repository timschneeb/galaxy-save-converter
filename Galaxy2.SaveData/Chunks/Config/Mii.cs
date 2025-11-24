using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Config
{
    public class ConfigDataMii
    {
        [JsonPropertyName("flag")]
        public byte Flag { get; set; }
        [JsonPropertyName("mii_id")]
        public byte[] MiiId { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0];
        [JsonPropertyName("icon_id")]
        public ConfigDataMiiIcon IconId { get; set; }
    }
    
    public enum ConfigDataMiiIcon : byte
    {
        Mii = 0,
        Mario = 1,
        Luigi = 2,
        Yoshi = 3,
        Kinopio = 4,
        Peach = 5,
        Rosetta = 6,
        Tico = 7,
    }
}
