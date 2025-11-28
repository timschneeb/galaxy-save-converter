using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStoragePlayerStatus
{
    [JsonPropertyName("attributes")]
    public List<BaseSaveDataAttribute> Attributes { get; set; } = [];

    // Convenience accessors (not serialized separately)
    [JsonIgnore]
    public byte PlayerLeft
    {
        get => Attributes.FindByName<byte>("mPlayerLeft")?.Value ?? 4;
        set => Attributes.FindByName<byte>("mPlayerLeft")!.Value = value;
    }

    [JsonIgnore]
    public ushort StockedStarPieceNum
    {
        get => Attributes.FindByName<ushort>("mStockedStarPieceNum")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mStockedStarPieceNum")!.Value = value;
    }

    [JsonIgnore]
    public ushort StockedCoinNum
    {
        get => Attributes.FindByName<ushort>("mStockedCoinNum")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mStockedCoinNum")!.Value = value;
    }

    [JsonIgnore]
    public ushort Last1UpCoinNum
    {
        get => Attributes.FindByName<ushort>("mLast1upCoinNum")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mLast1upCoinNum")!.Value = value;
    }

    [JsonIgnore]
    public SaveDataStoragePlayerStatusFlag Flag
    {
        get => new(Attributes.FindByName<byte>("mFlag")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mFlag")!.Value = value.Value;
    }

    public static SaveDataStoragePlayerStatus ReadFrom(BinaryReader reader, int dataSize)
    {
        var status = new SaveDataStoragePlayerStatus();
        var dataStartPos = reader.BaseStream.Position;

        var table = reader.ReadBinaryDataContentHeaderSerializer();
        var fieldsDataStartPos = reader.BaseStream.Position;

        // convert to list sorted by offset so sizes can be determined
        var items = table.AsOffsetDictionary()
            .Select(kv => (key: kv.Key, offset: kv.Value))
            .OrderBy(x => x.offset)
            .ToList();

        for (int i = 0; i < items.Count; i++)
        {
            var key = items[i].key;
            var offset = items[i].offset;
            var nextOffset = (i + 1 < items.Count) ? items[i + 1].offset : table.DataSize;
            var size = nextOffset - offset;

            reader.BaseStream.Position = fieldsDataStartPos + offset;
            status.Attributes.Add(BaseSaveDataAttribute.ReadFrom(reader, key, size));
        }

        // advance stream to end of this data block
        reader.BaseStream.Position = dataStartPos + dataSize;
        return status;

        // 
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
    }

    public void WriteTo(EndianAwareWriter writer, out uint hash)
    {
        using var ms = new MemoryStream();
        using var fw = writer.NewWriter(ms);

        var attrs = new List<(ushort key, ushort offset)>();

        // write attributes sequentially into ms and record offsets
        foreach (var attr in Attributes)
        {
            attrs.Add((attr.Key, (ushort)ms.Position));
            attr.WriteTo(fw);
        }
        fw.Flush();
        
        var dataSize = (ushort)ms.Length;
        var headerSize = writer.WriteBinaryDataContentHeader(attrs, dataSize);
        writer.Write(ms.ToArray());
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
        
        // Hash = data_size + header_size
        // header_size = 4 + attribute_count*4
        // data_size = length of all attribute data
        hash = dataSize + headerSize;
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SaveDataAttribute<byte>), "u8")]
[JsonDerivedType(typeof(SaveDataAttribute<ushort>), "u16")]
[JsonDerivedType(typeof(SaveDataAttribute<uint>), "u32")]
public abstract class BaseSaveDataAttribute(ushort key)
{
    [JsonPropertyName("key")]
    public ushort Key { get; set; } = key;
    [JsonIgnore]
    public abstract int Size { get; }
    
    public abstract void WriteTo(BinaryWriter writer);
    
    public static BaseSaveDataAttribute ReadFrom(BinaryReader reader, ushort key, int size)
    {
        return size switch
        {
            1 => new SaveDataAttribute<byte>(key, reader.ReadByte()),
            2 => new SaveDataAttribute<ushort>(key, reader.ReadUInt16()),
            4 => new SaveDataAttribute<uint>(key, reader.ReadUInt32()),
            _ => throw new InvalidDataException($"Unsupported attribute data size: {size}"),
        };
    }
}

public class SaveDataAttribute<T>(ushort key, T value) : BaseSaveDataAttribute(key) where T : struct
{
    [JsonPropertyName("value")]
    public T Value { get; set; } = value;
    
    [JsonIgnore]
    public override int Size => Marshal.SizeOf(default(T));
    
    public override void WriteTo(BinaryWriter writer)
    {
        switch (Value)
        {
            case byte b:
                writer.Write(b);
                break;
            case ushort us:
                writer.WriteUInt16(us);
                break;
            case uint ui:
                writer.WriteUInt32(ui);
                break;
            default:
                throw new InvalidDataException($"Unsupported attribute data type: {typeof(T)}");
        }
    }

    public override string ToString()
    {
        return Value.ToString() ?? "<null>";
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