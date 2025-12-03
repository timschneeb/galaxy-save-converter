using System.Text.Json.Serialization;

namespace SMGSaveData.Galaxy2.Model.Chunks.Config;

public class ConfigDataMisc
{
    // SMG1: byte Flag
    
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; set; }
}