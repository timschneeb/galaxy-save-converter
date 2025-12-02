using System.Text.Json.Serialization;
using Galaxy2.SaveData.Model.Chunks.Game.Attributes;
using Galaxy2.SaveData.String;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Model.Chunks.Sysconf;

public class SysConfigData
{
    [JsonPropertyName("is_encourage_pal60")]
    public bool IsEncouragePal60 { get; set; }
    [JsonPropertyName("time_sent")]
    public DateTime TimeSent { get; set; }
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

    public static SysConfigData ReadFrom(EndianAwareReader reader, int dataSize)
    {
        var sysConfig = new SysConfigData();
        var dataStartPos = reader.BaseStream.Position;

        var attributes = reader.ReadAttributeTableHeader().AsOffsetDictionary();
        var fieldsDataStartPos = reader.BaseStream.Position;

        if (reader.TryReadU8(fieldsDataStartPos, attributes, "mIsEncouragePal60", out var pal60))
            sysConfig.IsEncouragePal60 = pal60 != 0;

        if (reader.TryReadI64(fieldsDataStartPos, attributes, "mTimeSent", out var timeSent))
            sysConfig.TimeSent = reader.ConsoleType == ConsoleType.Wii
                ? OsTime.WiiTicksToUnix(timeSent)
                : DateTimeOffset.FromUnixTimeSeconds(timeSent).UtcDateTime;

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

    public void WriteTo(EndianAwareWriter writer)
    {
        using var ms = new MemoryStream();
        using var fw = writer.NewWriter(ms);
        var attrs = new List<(ushort key, ushort offset)>();

        AddU8("mIsEncouragePal60", IsEncouragePal60 ? (byte)1 : (byte)0);
        AddTime("mTimeSent", TimeSent);
        AddU32("mSentBytes", SentBytes);
        AddU16("mBankStarPieceNum", BankStarPieceNum);
        AddU16("mBankStarPieceMax", BankStarPieceMax);
        AddU8("mGiftedPlayerLeft", GiftedPlayerLeft);
        AddU16("mGiftedFileNameHash", GiftedFileNameHash);

        fw.Flush();
        var dataSize = (ushort)ms.Length;
        var header = new AttributeTableHeader { Offsets = attrs, DataSize = dataSize };
        writer.WriteAttributeTableHeader(header);
        writer.Write(ms.ToArray());
        return;

        void AddU8(string name, byte v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.Write(v);
        }

        void AddTime(string name, DateTime v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.WriteTime(v);
        }

        void AddU32(string name, uint v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.WriteUInt32(v);
        }

        void AddU16(string name, ushort v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.WriteUInt16(v);
        }
    }
}