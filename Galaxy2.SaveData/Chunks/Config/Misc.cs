using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Config;

public class ConfigDataMisc
{
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; set; }
}