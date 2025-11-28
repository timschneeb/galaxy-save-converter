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
        
        for (var i = 0; i < galaxyNum; i++)
        {
            var stage = new SaveDataStorageGalaxyStage();
            stage.Attributes = ReadAttributes(reader, stageSerializer);
            stage.Scenario = new List<SaveDataStorageGalaxyScenario>(stage.ScenarioNum);
            
            for (var j = 0; j < stage.ScenarioNum; j++)
            {
                var scenario = new SaveDataStorageGalaxyScenario();
                scenario.Attributes = ReadAttributes(reader, scenarioSerializer);
                stage.Scenario.Add(scenario);
            }

            galaxy.Galaxy.Add(stage);
        }

        return galaxy;
    }

    private static List<BaseSaveDataAttribute> ReadAttributes(BinaryReader reader, AttributeTableHeader table)
    {
        var list = new List<BaseSaveDataAttribute>(table.AttributeNum);
        var headerStart = reader.BaseStream.Position;
        
        for (var i = 0; i < table.AttributeNum; i++)
        {
            var (key, offset) = table.Offsets[i];
            var nextOffset = (i + 1 < table.AttributeNum) ? table.Offsets[i + 1].offset : table.DataSize;
            var size = nextOffset - offset;
            if (offset < 0 || offset + size > table.DataSize || size <= 0)
            {
                throw new InvalidDataException(
                    $"Invalid attribute size or offset in header (key: {key}, offset: {offset}, size: {size}).");
            }

            reader.BaseStream.Position = headerStart + offset;
            list.Add(BaseSaveDataAttribute.ReadFrom(reader, key, size));
        }

        return list;
    }

    private static void WriteAttributes(EndianAwareWriter writer, List<BaseSaveDataAttribute> attributes, IReadOnlyList<(ushort key, ushort offset)> layout, int dataSize)
    {
        using var ms = new MemoryStream(dataSize);
        using var bw = writer.NewWriter(ms);

        if (layout.Count == 0)
            return;

        for (var i = 0; i < layout.Count; i++)
        {
            var (key, offset) = layout[i];
            var nextOffset = (i + 1 < layout.Count) ? layout[i + 1].offset : dataSize;
            var size = nextOffset - offset;
            var attr = attributes.First(a => a.Key == key);
            if(attr.Size > size)
                throw new InvalidDataException($"Attribute data size {attr.Size} exceeds allocated size {size} for key {key}.");
            
            attr.WriteTo(bw);
        }
        writer.Write(ms.ToArray());
    }

    // Helper: compute layout (key->offset list) and total data size from a sequence of attribute collections
    private static (List<(ushort key, ushort offset)> layout, ushort dataSize) BuildLayout(IEnumerable<IEnumerable<BaseSaveDataAttribute>> groups)
    {
        var keySet = new HashSet<ushort>();
        var keyMaxSize = new Dictionary<ushort, int>();

        foreach (var attrs in groups)
        {
            foreach (var a in attrs)
            {
                keySet.Add(a.Key);
                var len = a.Size;
                if (!keyMaxSize.TryGetValue(a.Key, out var cur) || len > cur)
                    keyMaxSize[a.Key] = len;
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
             WriteAttributes(writer, s.Attributes, stageAttrs, stageDataSize);

             foreach (var sc in s.Scenario)
             {
                 WriteAttributes(writer, sc.Attributes, scenarioAttrs, scenarioDataSize);
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
    public List<BaseSaveDataAttribute> Attributes { get; set; } = [];

    [JsonPropertyName("scenario")]
    public List<SaveDataStorageGalaxyScenario> Scenario { get; set; } = [];
    
    [JsonIgnore]
    public ushort GalaxyName
    {
        get => Attributes.FindByName<ushort>("mGalaxyName")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mGalaxyName")!.Value = value;
    }
    [JsonIgnore]
    public ushort DataSize
    {
        get => Attributes.FindByName<ushort>("mDataSize")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mDataSize")!.Value = value;
    }
    [JsonIgnore]
    public byte ScenarioNum
    {
        get => Attributes.FindByName<byte>("mScenarioNum")?.Value ?? 0;
        set => Attributes.FindByName<byte>("mScenarioNum")!.Value = value;
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyState GalaxyState
    {
        get => (SaveDataStorageGalaxyState)(Attributes.FindByName<byte>("mGalaxyState")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mGalaxyState")!.Value = (byte)value;
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyFlag Flag
    {
        get => new(Attributes.FindByName<byte>("mFlag")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mFlag")!.Value = value.Value;
    }
}

public class SaveDataStorageGalaxyScenario
{
    [JsonPropertyName("attributes")]
    public List<BaseSaveDataAttribute> Attributes { get; set; } = [];

    [JsonIgnore]
    public byte MissNum
    {
        get => Attributes.FindByName<byte>("mMissNum")?.Value ?? 0;
        set => Attributes.FindByName<byte>("mMissNum")!.Value = value;
    }
    [JsonIgnore]
    public uint BestTime
    {
        get => Attributes.FindByName<uint>("mBestTime")?.Value ?? 0;
        set => Attributes.FindByName<uint>("mBestTime")!.Value = value;
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyScenarioFlag Flag
    {
        get => new(Attributes.FindByName<byte>("mFlag")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mFlag")!.Value = value.Value;
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