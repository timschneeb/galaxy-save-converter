using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageWorldMap
    {
        private const int WorldCapacity = 8;
        [JsonPropertyName("star_check_point_flag")]
        public byte[] StarCheckPointFlag { get; set; } = new byte[WorldCapacity];
        [JsonPropertyName("world_no")]
        public byte WorldNo { get; set; } = 1;
    }
}
