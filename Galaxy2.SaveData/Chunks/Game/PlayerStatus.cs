using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game.Attributes;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStoragePlayerStatus
{
    [JsonPropertyName("attributes")]
    public List<AbstractDataAttribute> Attributes { get; set; } = [];

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

    public static SaveDataStoragePlayerStatus ReadFrom(BinaryReader reader, int dataSize)
    {
        var status = new SaveDataStoragePlayerStatus();
        var dataStartPos = reader.BaseStream.Position;

        var table = reader.ReadAttributeTableHeader();
        var fieldsDataStartPos = reader.BaseStream.Position;

        // convert to list sorted by offset so sizes can be determined
        var items = table.AsOffsetDictionary()
            .Select(kv => (key: kv.Key, offset: kv.Value))
            .OrderBy(x => x.offset)
            .ToList();

        for (var i = 0; i < items.Count; i++)
        {
            var key = items[i].key;
            var offset = items[i].offset;
            var nextOffset = (i + 1 < items.Count) ? items[i + 1].offset : table.DataSize;
            var size = nextOffset - offset;

            reader.BaseStream.Position = fieldsDataStartPos + offset;
            status.Attributes.Add(AbstractDataAttribute.ReadFrom(reader, key, size));
        }

        // advance stream to end of this data block
        reader.BaseStream.Position = dataStartPos + dataSize;
        return status;
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
        var headerSize = writer.WriteAttributeTableHeader(attrs, dataSize);
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