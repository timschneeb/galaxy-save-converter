using System.Text.Json.Serialization;
using Galaxy2.SaveData.String;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStorageGalaxy
{
    [JsonPropertyName("galaxy")]
    public List<SaveDataStorageGalaxyStage> Galaxy { get; set; } = [];
    
    public static SaveDataStorageGalaxy ReadFrom(BinaryReader reader)
    {
        var galaxy = new SaveDataStorageGalaxy();
        var galaxyNum = reader.ReadUInt16();
        galaxy.Galaxy = new List<SaveDataStorageGalaxyStage>(galaxyNum);

        var stageSerializer = reader.ReadBinaryDataContentHeaderSerializer();
        var scenarioSerializer = reader.ReadBinaryDataContentHeaderSerializer();
        
        var stageHeaderSize = stageSerializer.dataSize;
        var scenarioHeaderSize = scenarioSerializer.dataSize;

        for (var i = 0; i < galaxyNum; i++)
        {
            var headerRaw = reader.ReadBytes(stageHeaderSize);
            var stage = new SaveDataStorageGalaxyStage();

            // Build stage attributes list from serializer metadata
            var stageAttrs = stageSerializer.attributes;
            stage.Attributes = ReadAttributes(headerRaw, stageAttrs, stageHeaderSize);

            // Determine scenario count from attribute list
            var keyScenarioNum = HashKey.Compute("mScenarioNum");
            var scenarioNum = stage.Attributes.First(a => a.Key == keyScenarioNum).Data[0];

            stage.Scenario = new List<SaveDataStorageGalaxyScenario>(scenarioNum);
            for (var j = 0; j < scenarioNum; j++)
            {
                var scRaw = reader.ReadBytes(scenarioHeaderSize);
                var scenario = new SaveDataStorageGalaxyScenario();
                var scAttrs = scenarioSerializer.attributes;
                scenario.Attributes = ReadAttributes(scRaw, scAttrs, scenarioHeaderSize);
                stage.Scenario.Add(scenario);
            }

            galaxy.Galaxy.Add(stage);
        }

        return galaxy;
    }

    private static List<SaveDataAttribute> ReadAttributes(byte[] headerRaw, IReadOnlyList<(ushort key, int offset)> attrs, int headerSize)
    {
        var list = new List<SaveDataAttribute>(attrs.Count);
        for (var i = 0; i < attrs.Count; i++)
        {
            var (key, offset) = attrs[i];
            var nextOffset = (i + 1 < attrs.Count) ? attrs[i + 1].offset : headerSize;
            var size = nextOffset - offset;
            if (offset < 0 || offset + size > headerRaw.Length || size <= 0)
            {
                throw new InvalidDataException(
                    $"Invalid attribute size or offset in header (key: {key}, offset: {offset}, size: {size}).");
            }

            var data = new byte[size];
            Buffer.BlockCopy(headerRaw, offset, data, 0, size);
            list.Add(new SaveDataAttribute(key, data));
        }

        return list;
    }

    // Helper: build a header block (byte[]) from an attributes list and a serializer layout
    private static byte[] BuildHeaderRaw(List<SaveDataAttribute>? attributes, IReadOnlyList<(ushort key, ushort offset)> layout, int dataSize)
    {
        var raw = new byte[dataSize];
        if (attributes == null || layout.Count == 0) return raw;

        for (var i = 0; i < layout.Count; i++)
        {
            var (key, offset) = layout[i];
            var nextOffset = (i + 1 < layout.Count) ? layout[i + 1].offset : dataSize;
            var size = nextOffset - offset;
            var attr = attributes.FirstOrDefault(a => a.Key == key);
            if (attr is { Data.Length: > 0 })
            {
                Buffer.BlockCopy(attr.Data, 0, raw, offset, Math.Min(size, attr.Data.Length));
            }
        }

        return raw;
    }

    // Helper: compute layout (key->offset list) and total data size from a sequence of attribute collections
    private static (List<(ushort key, ushort offset)> layout, ushort dataSize) BuildLayout(IEnumerable<IEnumerable<SaveDataAttribute>> groups)
    {
        var keySet = new HashSet<ushort>();
        var keyMaxSize = new Dictionary<ushort, int>();

        foreach (var attrs in groups)
        {
            foreach (var a in attrs)
            {
                keySet.Add(a.Key);
                var len = a.Data.Length;
                if (!keyMaxSize.TryGetValue(a.Key, out var cur) || len > cur) keyMaxSize[a.Key] = len;
            }
        }

        var layout = new List<(ushort key, ushort offset)>();
        using var ms = new MemoryStream();
        foreach (var key in keySet)
        {
            var offset = (ushort)ms.Position;
            layout.Add((key, offset));
            var size = keyMaxSize.GetValueOrDefault(key, 0);
            if (size > 0) ms.Write(new byte[size]);
        }

        var dataSize = (ushort)ms.Length;
        return (layout, dataSize);
    }
     
     public void WriteTo(EndianAwareWriter writer, out uint hash)
     {
         writer.WriteUInt16((ushort)Galaxy.Count);

         var (stageAttrs, stageDataSize) = BuildLayout(Galaxy.Select(s => s.Attributes));
         var stageHeaderSize = writer.WriteBinaryDataContentHeader(stageAttrs, stageDataSize);

         var (scenarioAttrs, scenarioDataSize) = BuildLayout(Galaxy.SelectMany(s => s.Scenario).Select(sc => sc.Attributes));
         writer.WriteBinaryDataContentHeader(scenarioAttrs, scenarioDataSize);
         
         foreach (var s in Galaxy)
         {
             // Build stage header block using the computed serializer layout
             writer.Write(BuildHeaderRaw(s.Attributes, stageAttrs, stageDataSize));

             // Write each scenario block for this stage
             foreach (var sc in s.Scenario)
             {
                 writer.Write(BuildHeaderRaw(sc.Attributes, scenarioAttrs, scenarioDataSize));
             }
         }
          
         if (writer.ConsoleType == ConsoleType.Switch)
         {
             writer.WriteAlignmentPadding(alignment: 4);
         }
          
         // Hash = scenario_data_size + stage_header_size + 2 
         hash = scenarioDataSize + stageHeaderSize + 2;
      }
  }

public class SaveDataStorageGalaxyStage
{
    [JsonPropertyName("attributes")]
    public List<SaveDataAttribute> Attributes { get; set; } = [];

    [JsonPropertyName("scenario")]
    public List<SaveDataStorageGalaxyScenario> Scenario { get; set; } = [];
    
    [JsonIgnore]
    public ushort GalaxyName
    {
        get => GetU16("mGalaxyName") ?? 0;
        set => SetU16("mGalaxyName", value);
    }
    [JsonIgnore]
    public ushort DataSize
    {
        get => GetU16("mDataSize") ?? 0;
        set => SetU16("mDataSize", value);
    }
    [JsonIgnore]
    public byte ScenarioNum
    {
        get => GetU8("mScenarioNum") ?? 0;
        set => SetU8("mScenarioNum", value);
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyState GalaxyState
    {
        get => (SaveDataStorageGalaxyState)(GetU8("mGalaxyState") ?? 0);
        set => SetU8("mGalaxyState", (byte)value);
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyFlag Flag
    {
        get => new(GetU8("mFlag") ?? 0);
        set => SetU8("mFlag", value.Value);
    }

    private SaveDataAttribute? FindAttr(string name)
    {
        var key = HashKey.Compute(name);
        return Attributes.FirstOrDefault(a => a.Key == key);
    }
    private byte? GetU8(string name)
    {
        var a = FindAttr(name);
        if (a == null || a.Data.Length < 1) return null;
        return a.Data[0];
    }
    private ushort? GetU16(string name)
    {
        // TODO: endianness
        var a = FindAttr(name);
        if (a == null || a.Data.Length < 2) return null;
        return (ushort)((a.Data[0] << 8) | a.Data[1]);
    }
    private void SetU8(string name, byte v)
    {
        var key = HashKey.Compute(name);
        var attr = Attributes.FirstOrDefault(a => a.Key == key);
        if (attr != null) attr.Data = [v];
        else Attributes.Add(new SaveDataAttribute(key, [v]));
    }
    private void SetU16(string name, ushort v)
    {
        // TODO: endianness
        var key = HashKey.Compute(name);
        var data = new[] { (byte)(v >> 8), (byte)(v & 0xFF) };
        var attr = Attributes.FirstOrDefault(a => a.Key == key);
        if (attr != null) attr.Data = data; else Attributes.Add(new SaveDataAttribute(key, data));
    }
}

public class SaveDataStorageGalaxyScenario
{
    [JsonPropertyName("attributes")]
    public List<SaveDataAttribute> Attributes { get; set; } = [];

    // Convenience accessors (not serialized)
    [JsonIgnore]
    public byte MissNum
    {
        get => GetU8("mMissNum") ?? 0;
        set => SetU8("mMissNum", value);
    }
    [JsonIgnore]
    public uint BestTime
    {
        get => GetU32("mBestTime") ?? 0u;
        set => SetU32("mBestTime", value);
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyScenarioFlag Flag
    {
        get => new(GetU8("mFlag") ?? 0);
        set => SetU8("mFlag", value.Value);
    }

    private SaveDataAttribute? FindAttr(string name)
    {
        var key = HashKey.Compute(name);
        return Attributes.FirstOrDefault(a => a.Key == key);
    }
    private byte? GetU8(string name)
    {
        var a = FindAttr(name);
        if ((a == null) || a.Data.Length < 1) return null;
        return a.Data[0];
    }
    private uint? GetU32(string name)
    {
        var a = FindAttr(name);
        if ((a == null) || a.Data.Length < 4) return null;
        // TODO: endianness
        return (uint)((a.Data[0] << 24) | (a.Data[1] << 16) | (a.Data[2] << 8) | a.Data[3]);
    }
    private void SetU8(string name, byte v)
    {
        var key = HashKey.Compute(name);
        var attr = Attributes.FirstOrDefault(a => a.Key == key);
        if (attr != null) attr.Data = [v]; else Attributes.Add(new SaveDataAttribute(key, [v]));
    }
    private void SetU32(string name, uint v)
    {
        // TODO: endianness
        var key = HashKey.Compute(name);
        var data = new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)(v & 0xFF) };
        var attr = Attributes.FirstOrDefault(a => a.Key == key);
        if (attr != null) attr.Data = data; else Attributes.Add(new SaveDataAttribute(key, data));
    }
}
    
public struct SaveDataStorageGalaxyScenarioFlag(byte value)
{
    [JsonIgnore]
    public byte Value { get; private set; } = value;

    [JsonPropertyName("power_star")]
    public bool PowerStar
    {
        get => (Value & 0b1) != 0;
        set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
    }

    [JsonPropertyName("bronze_star")]
    public bool BronzeStar
    {
        get => (Value & 0b10) != 0;
        set => Value = (byte)(value ? (Value | 0b10) : (Value & ~0b10));
    }

    [JsonPropertyName("already_visited")]
    public bool AlreadyVisited
    {
        get => (Value & 0b100) != 0;
        set => Value = (byte)(value ? (Value | 0b100) : (Value & ~0b100));
    }

    [JsonPropertyName("ghost_luigi")]
    public bool GhostLuigi
    {
        get => (Value & 0b1000) != 0;
        set => Value = (byte)(value ? (Value | 0b1000) : (Value & ~0b1000));
    }

    [JsonPropertyName("intrusively_luigi")]
    public bool IntrusivelyLuigi
    {
        get => (Value & 0b10000) != 0;
        set => Value = (byte)(value ? (Value | 0b10000) : (Value & ~0b10000));
    }
}

public enum SaveDataStorageGalaxyState : byte
{
    Closed = 0,
    New = 1,
    Opened = 2,
}

public struct SaveDataStorageGalaxyFlag(byte value)
{
    [JsonIgnore]
    public byte Value { get; private set; } = value;

    [JsonPropertyName("tico_coin")]
    public bool TicoCoin
    {
        get => (Value & 0b1) != 0;
        set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
    }
        
    [JsonPropertyName("comet")]
    public bool Comet
    {
        get => (Value & 0b10) != 0;
        set => Value = (byte)(value ? (Value | 0b10) : (Value & ~0b10));
    }
}