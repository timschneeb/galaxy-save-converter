using System.Collections.Generic;
using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game;
using Galaxy2.SaveData.Chunks.Config;
using Galaxy2.SaveData.Chunks.Sysconf;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Save
{
    public class SaveDataUserFileInfo
    {
        [JsonPropertyName("name")]
        public FixedString12? Name { get; set; }
        [JsonPropertyName("user_file")]
        public SaveDataUserFile? UserFile { get; set; }
    }

    public class SaveDataUserFile
    {
        // Explicit buckets matching the original DTO shape
        [JsonPropertyName("GameData")]
        public List<GameDataChunk>? GameData { get; set; }

        [JsonPropertyName("ConfigData")]
        public List<ConfigDataChunk>? ConfigData { get; set; }

        [JsonPropertyName("SysConfigData")]
        public List<SysConfigData>? SysConfigData { get; set; }

        // If we couldn't interpret the data, keep a raw placeholder
        [JsonPropertyName("UserFileRaw")]
        public object? UserFileRaw { get; set; }
    }
}
