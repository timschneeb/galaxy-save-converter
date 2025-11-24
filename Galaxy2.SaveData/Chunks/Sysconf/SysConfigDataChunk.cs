namespace Galaxy2.SaveData.Chunks.Sysconf
{
    public abstract class SysConfigDataChunk
    {
    }

    public class SysConfigChunk : SysConfigDataChunk
    {
        public required SysConfigData Data { get; set; }
    }
}
