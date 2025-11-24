using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Sysconf
{
    public class SysConfigData
    {
        [JsonPropertyName("is_encourage_pal60")]
        public bool IsEncouragePal60 { get; set; }
        [JsonPropertyName("time_sent")]
        public long TimeSent { get; set; }
        [JsonPropertyName("sent_bytes")]
        public uint SentBytes { get; set; }
        [JsonPropertyName("bank_star_piece_num")]
        public ushort BankStarPieceNum { get; set; }
        [JsonPropertyName("bank_star_piece_max")]
        public ushort BankStarPieceMax { get; set; }
        [JsonPropertyName("gifted_player_left")]
        public byte GiftedPlayerLeft { get; set; }
        [JsonPropertyName("gifted_file_name_hash")]
        public ushort GiftedFileNameHash { get; set; }
    }
}
