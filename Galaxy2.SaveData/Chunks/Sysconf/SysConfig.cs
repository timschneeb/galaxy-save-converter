using Galaxy2.SaveData.Time;

namespace Galaxy2.SaveData.Chunks.Sysconf
{
    public class SysConfigData
    {
        public bool IsEncouragePal60 { get; set; }
        public OSTime TimeSent { get; set; }
        public uint SentBytes { get; set; }
        public ushort BankStarPieceNum { get; set; }
        public ushort BankStarPieceMax { get; set; }
        public byte GiftedPlayerLeft { get; set; }
        public ushort GiftedFileNameHash { get; set; }
    }
}
