using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;

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

        public void WriteTo(BinaryWriter writer)
        {
            using var ms = new MemoryStream();
            using var fw = new BinaryWriter(ms);
            var attrs = new List<(ushort key, ushort offset)>();

            void AddU8(string name, byte v)
            {
                var key = HashKey.Compute(name);
                var offset = (ushort)ms.Position;
                attrs.Add((key, offset));
                fw.Write(v);
            }

            void AddI64(string name, long v)
            {
                var key = HashKey.Compute(name);
                var offset = (ushort)ms.Position;
                attrs.Add((key, offset));
                fw.WriteInt64Be(v);
            }

            void AddU32(string name, uint v)
            {
                var key = HashKey.Compute(name);
                var offset = (ushort)ms.Position;
                attrs.Add((key, offset));
                fw.WriteUInt32Be(v);
            }

            void AddU16(string name, ushort v)
            {
                var key = HashKey.Compute(name);
                var offset = (ushort)ms.Position;
                attrs.Add((key, offset));
                fw.WriteUInt16Be(v);
            }

            AddU8("mIsEncouragePal60", IsEncouragePal60 ? (byte)1 : (byte)0);
            AddI64("mTimeSent", TimeSent);
            AddU32("mSentBytes", SentBytes);
            AddU16("mBankStarPieceNum", BankStarPieceNum);
            AddU16("mBankStarPieceMax", BankStarPieceMax);
            AddU8("mGiftedPlayerLeft", GiftedPlayerLeft);
            AddU16("mGiftedFileNameHash", GiftedFileNameHash);

            fw.Flush();
            var dataSize = (ushort)ms.Length;
            writer.WriteBinaryDataContentHeader(attrs, dataSize);
            writer.Write(ms.ToArray());
        }
    }
}
