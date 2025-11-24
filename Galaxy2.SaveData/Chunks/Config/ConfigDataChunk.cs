namespace Galaxy2.SaveData.Chunks.Config
{
    public abstract class ConfigDataChunk
    {
    }

    public class CreateChunk : ConfigDataChunk 
    {
        public required ConfigDataCreate Data { get; set; }
    }
    public class MiiChunk : ConfigDataChunk 
    {
        public required ConfigDataMii Data { get; set; }
    }
    public class MiscChunk : ConfigDataChunk 
    {
        public required ConfigDataMisc Data { get; set; }
    }
}
