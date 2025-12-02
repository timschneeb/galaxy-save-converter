using System.Text.Json.Serialization;

namespace SMGSaveData.Galaxy2.Model.Chunks.Config;

public class ConfigDataCreate
{
    [JsonPropertyName("is_created")]
    public bool IsCreated { get; set; }
}