namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStoragePlayerStatus
    {
        public byte PlayerLeft { get; set; } = 4;
        public ushort StockedStarPieceNum { get; set; }
        public ushort StockedCoinNum { get; set; }
        public ushort Last1upCoinNum { get; set; }
        public SaveDataStoragePlayerStatusFlag Flag { get; set; }
    }

    public struct SaveDataStoragePlayerStatusFlag(byte value)
    {
        private byte _value = value;

        public bool PlayerLuigi
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }
    }
}