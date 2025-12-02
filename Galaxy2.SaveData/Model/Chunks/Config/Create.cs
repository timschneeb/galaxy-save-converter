using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Model.Chunks.Config;

public class ConfigDataCreate
{
    [JsonPropertyName("is_created")]
    public bool IsCreated { get; set; }
}