using System.Text.Json.Serialization;

namespace SMGSaveData.Galaxy2.Model.Chunks.Config;

public class ConfigDataMisc
{
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; set; }
}