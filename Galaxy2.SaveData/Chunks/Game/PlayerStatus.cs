using System.Text.Json.Serialization;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStoragePlayerStatus
{
    [JsonPropertyName("player_left")]
    public byte PlayerLeft { get; set; } = 4;
    [JsonPropertyName("stocked_star_piece_num")]
    public ushort StockedStarPieceNum { get; set; }
    [JsonPropertyName("stocked_coin_num")]
    public ushort StockedCoinNum { get; set; }
    [JsonPropertyName("last_1up_coin_num")]
    public ushort Last1upCoinNum { get; set; }
    [JsonPropertyName("flag")]
    public SaveDataStoragePlayerStatusFlag Flag { get; set; }

    public static SaveDataStoragePlayerStatus ReadFrom(BinaryReader reader, int dataSize)
    {
        var status = new SaveDataStoragePlayerStatus();
        var dataStartPos = reader.BaseStream.Position;

        var (attributes, headerDataSize) = reader.ReadAttributesAsDictionary();
        var fieldsDataStartPos = reader.BaseStream.Position;
        
        foreach (var (k,o) in attributes)
        {

            reader.BaseStream.Position = fieldsDataStartPos + o;
            var u8 = reader.ReadByte();
            reader.BaseStream.Position = fieldsDataStartPos + o;
            var u16 = reader.ReadUInt16();
                
            Console.WriteLine($"Attribute key: 0x{k:X4}, offset: 0x{o:X4} => u8: {u8}, u16: {u16}");
        }
        
        // TODO attributes 
        /* WII
         
         Attribute key: 0x4ED5, offset: 0x0000 => u8: 9, u16: 2304
           Attribute key: 0xE352, offset: 0x0001 => u8: 0, u16: 178
           Attribute key: 0x450D, offset: 0x0003 => u8: 1, u16: 328
           Attribute key: 0x23EC, offset: 0x0005 => u8: 1, u16: 300
           Attribute key: 0x7579, offset: 0x0007 => u8: 0, u16: 70
           
         */
        
        /* SWITCH
         
         Attribute key: 0x4ED5, offset: 0x0000 => u8: 22, u16: 9750
           Attribute key: 0xE352, offset: 0x0001 => u8: 38, u16: 38
           Attribute key: 0x450D, offset: 0x0003 => u8: 88, u16: 88
           Attribute key: 0x23EC, offset: 0x0005 => u8: 0, u16: 0
           Attribute key: 0x7579, offset: 0x0007 => u8: 0, u16: 512
           
           Attribute key: 0xAA83, offset: 0x0008 => u8: 2, u16: 2
           Attribute key: 0x7213, offset: 0x0009 => u8: 0, u16: 0
           Attribute key: 0xEA99, offset: 0x000A => u8: 0, u16: 0
           Attribute key: 0xBF77, offset: 0x000B => u8: 0, u16: 0
           Attribute key: 0xBFD3, offset: 0x000D => u8: 32, u16: 32
           Attribute key: 0x1E85, offset: 0x000F => u8: 3, u16: 3
           Attribute key: 0xEFDB, offset: 0x0011 => u8: 0, u16: 0
           Attribute key: 0xE6D1, offset: 0x0013 => u8: 0, u16: 0
           Attribute key: 0x3D5F, offset: 0x0015 => u8: 73, u16: 1609
           Attribute key: 0x0AC6, offset: 0x0019 => u8: 64, u16: 64
           Attribute key: 0x71E3, offset: 0x001D => u8: 0, u16: 0
           Attribute key: 0xE983, offset: 0x0021 => u8: 0, u16: 0
                    
         */
        
        if (reader.TryReadU8(fieldsDataStartPos, attributes, "mPlayerLeft", out var playerLeft))
            status.PlayerLeft = playerLeft;
        if (reader.TryReadU16(fieldsDataStartPos, attributes, "mStockedStarPieceNum", out var sspn))
            status.StockedStarPieceNum = sspn;
        if (reader.TryReadU16(fieldsDataStartPos, attributes, "mStockedCoinNum", out var scn))
            status.StockedCoinNum = scn;
        if (reader.TryReadU16(fieldsDataStartPos, attributes, "mLast1upCoinNum", out var l1up))
            status.Last1upCoinNum = l1up;
        if (reader.TryReadU8(fieldsDataStartPos, attributes, "mFlag", out var flag))
            status.Flag = new SaveDataStoragePlayerStatusFlag(flag);

        reader.BaseStream.Position = dataStartPos + dataSize;
        return status;
    }

    public void WriteTo(BinaryWriter writer)
    {
        // We'll build the fields area into a memory stream to compute offsets
        using var ms = new MemoryStream();
        using var fw = new EndianAwareWriter(ms);
        fw.BigEndian = writer is EndianAwareWriter { BigEndian: true };

        var attrs = new List<(ushort key, ushort offset)>();

        AddU8("mPlayerLeft", PlayerLeft);
        AddU16("mStockedStarPieceNum", StockedStarPieceNum);
        AddU16("mStockedCoinNum", StockedCoinNum);
        AddU16("mLast1upCoinNum", Last1upCoinNum);
        AddU8("mFlag", Flag.Value);

        fw.Flush();

        var dataSize = (ushort)ms.Length;

        // write header and fields to the target writer
        writer.WriteBinaryDataContentHeader(attrs, dataSize);
        writer.Write(ms.ToArray());
        return;

        // helper to add a byte field
        void AddU8(string name, byte v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.Write(v);
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

public struct SaveDataStoragePlayerStatusFlag(byte value)
{
    [JsonIgnore]
    public byte Value { get; private set; } = value;

    [JsonPropertyName("player_luigi")]
    public bool PlayerLuigi
    {
        get => (Value & 0b1) != 0;
        set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
    }
}