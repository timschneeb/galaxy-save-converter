using System.Text.Json.Serialization;
using System.IO;

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

        public static SysConfigData ReadFrom(BinaryReader reader, int dataSize)
        {
            var sysConfig = new SysConfigData();
            var dataStartPos = reader.BaseStream.Position;

            var (attributes, _) = reader.ReadAttributesAsDictionary();
            var fieldsDataStartPos = reader.BaseStream.Position;

            if (reader.TryReadU8(fieldsDataStartPos, attributes, "mIsEncouragePal60", out var pal60))
                sysConfig.IsEncouragePal60 = pal60 != 0;

            if (reader.TryReadI64(fieldsDataStartPos, attributes, "mTimeSent", out var timeSent))
                sysConfig.TimeSent = timeSent;

            if (reader.TryReadU32(fieldsDataStartPos, attributes, "mSentBytes", out var sentBytes))
                sysConfig.SentBytes = sentBytes;

            if (reader.TryReadU16(fieldsDataStartPos, attributes, "mBankStarPieceNum", out var bankNum))
                sysConfig.BankStarPieceNum = bankNum;

            if (reader.TryReadU16(fieldsDataStartPos, attributes, "mBankStarPieceMax", out var bankMax))
                sysConfig.BankStarPieceMax = bankMax;

            if (reader.TryReadU8(fieldsDataStartPos, attributes, "mGiftedPlayerLeft", out var giftedLeft))
                sysConfig.GiftedPlayerLeft = giftedLeft;

            if (reader.TryReadU16(fieldsDataStartPos, attributes, "mGiftedFileNameHash", out var gfnh))
                sysConfig.GiftedFileNameHash = gfnh;

            reader.BaseStream.Position = dataStartPos + dataSize;
            return sysConfig;
        }
    }
}
