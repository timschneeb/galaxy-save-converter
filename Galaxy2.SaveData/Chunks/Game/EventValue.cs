namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageEventValue
    {
        public List<GameEventValue> EventValues { get; set; } = [];
    }

    public struct GameEventValue
    {
        public ushort Key { get; set; }
        public ushort Value { get; set; }
    }
}
