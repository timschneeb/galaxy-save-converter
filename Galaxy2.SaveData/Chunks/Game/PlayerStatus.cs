using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStoragePlayerStatus
{
    [JsonPropertyName("attributes")]
    public List<SaveDataAttribute> Attributes { get; set; } = [];

    // Convenience accessors (not serialized separately)
    [JsonIgnore]
    public byte PlayerLeft
    {
        get => GetU8("mPlayerLeft") ?? 4;
        set => SetU8("mPlayerLeft", value);
    }

    [JsonIgnore]
    public ushort StockedStarPieceNum
    {
        get => GetU16("mStockedStarPieceNum") ?? 0;
        set => SetU16("mStockedStarPieceNum", value);
    }

    [JsonIgnore]
    public ushort StockedCoinNum
    {
        get => GetU16("mStockedCoinNum") ?? 0;
        set => SetU16("mStockedCoinNum", value);
    }

    [JsonIgnore]
    public ushort Last1upCoinNum
    {
        get => GetU16("mLast1upCoinNum") ?? 0;
        set => SetU16("mLast1upCoinNum", value);
    }

    [JsonIgnore]
    public SaveDataStoragePlayerStatusFlag Flag
    {
        get => new(GetU8("mFlag") ?? 0);
        set => SetU8("mFlag", value.Value);
    }

    public static SaveDataStoragePlayerStatus ReadFrom(BinaryReader reader, int dataSize)
    {
        var status = new SaveDataStoragePlayerStatus();
        var dataStartPos = reader.BaseStream.Position;

        var (attributes, headerDataSize) = reader.ReadAttributesAsDictionary();
        var fieldsDataStartPos = reader.BaseStream.Position;

        // convert to list sorted by offset so sizes can be determined
        var items = attributes
            .Select(kv => (key: kv.Key, offset: kv.Value))
            .OrderBy(x => x.offset)
            .ToList();

        for (int i = 0; i < items.Count; i++)
        {
            var key = items[i].key;
            var offset = items[i].offset;
            var nextOffset = (i + 1 < items.Count) ? items[i + 1].offset : headerDataSize;
            var size = nextOffset - offset;

            reader.BaseStream.Position = fieldsDataStartPos + offset;
            var data = reader.ReadBytes(size);
            status.Attributes.Add(new SaveDataAttribute(key, data));
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

    public void WriteTo(BinaryWriter writer)
    {
        // We'll build the fields area into a memory stream to compute offsets
        using var ms = new MemoryStream();
        using var fw = new EndianAwareWriter(ms);
        fw.BigEndian = writer is EndianAwareWriter { BigEndian: true };

        var attrs = new List<(ushort key, ushort offset)>();

        // write attributes sequentially into ms and record offsets
        foreach (var a in Attributes)
        {
            var key = a.Key;
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));

            // TODO swap endianness for multi-byte keys if needed
            if (a.Data is { Length: > 0 })
                fw.Write(a.Data);
        }

        fw.Flush();
        var dataSize = (ushort)ms.Length;

        // write header and fields to the target writer
        writer.WriteBinaryDataContentHeader(attrs, dataSize);
        writer.Write(ms.ToArray());
    }

    // helpers to get/set attributes by name (uses HashKey)
    private SaveDataAttribute? FindAttrByName(string name)
    {
        var key = HashKey.Compute(name);
        return Attributes.FirstOrDefault(a => a.Key == key);
    }

    private byte? GetU8(string name)
    {
        var a = FindAttrByName(name);
        if (a?.Data == null || a.Data.Length < 1) return null;
        return a.Data[0];
    }

    private ushort? GetU16(string name)
    {
        var a = FindAttrByName(name);
        if (a?.Data == null) return null;
        switch (a.Data.Length)
        {
            // TODO handle endianness properly?
            case >= 2:
                return (ushort)((a.Data[0] << 8) | a.Data[1]); // interpret as big-endian
            case 1:
                return a.Data[0];
            default:
                return null;
        }
    }

    private void SetU8(string name, byte v)
    {
        var key = HashKey.Compute(name);
        var attr = Attributes.FirstOrDefault(a => a.Key == key);
        if (attr != null)
        {
            attr.Data = [v];
        }
        else
        {
            Attributes.Add(new SaveDataAttribute(key, [v]));
        }
    }

    private void SetU16(string name, ushort v)
    {
        var key = HashKey.Compute(name);
        var attr = Attributes.FirstOrDefault(a => a.Key == key);
        // TODO: handle endianness properly?
        var data = new[] { (byte)(v >> 8), (byte)(v & 0xFF) };
        if (attr != null)
        {
            attr.Data = data;
        }
        else
        {
            Attributes.Add(new SaveDataAttribute(key, data));
        }
    }
}

public class SaveDataAttribute(ushort key, byte[] data)
{
    [JsonPropertyName("key")]
    public ushort Key { get; set; } = key;

    [JsonPropertyName("data")]
    public byte[] Data { get; set; } = data ?? [];
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