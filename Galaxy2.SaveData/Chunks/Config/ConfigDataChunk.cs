using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Config
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(CreateChunk), "CreateChunk")]
    [JsonDerivedType(typeof(MiiChunk), "MiiChunk")]
    [JsonDerivedType(typeof(MiscChunk), "MiscChunk")]
    public abstract class ConfigDataChunk
    {
    }

    public class CreateChunk : ConfigDataChunk 
    {
        public required ConfigDataCreate Create { get; set; }
    }
    public class MiiChunk : ConfigDataChunk 
    {
        public required ConfigDataMii Mii { get; set; }
    }
    public class MiscChunk : ConfigDataChunk 
    {
        public required ConfigDataMisc Misc { get; set; }
    }
}
