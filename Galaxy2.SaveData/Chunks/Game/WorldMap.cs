namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageWorldMap
    {
        private const int WorldCapacity = 8;
        public byte[] StarCheckPointFlag { get; set; } = new byte[WorldCapacity];
        public byte WorldNo { get; set; } = 1;
    }
}
