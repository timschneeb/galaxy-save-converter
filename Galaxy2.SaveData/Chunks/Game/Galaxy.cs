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
            for (var si = 0; si < stageAttrs.Count; si++)
            {
                var (key, offset) = stageAttrs[si];
                var nextOffset = (si + 1 < stageAttrs.Count)
                    ? stageAttrs[si + 1].offset
                    : stageHeaderSize;
                var size = nextOffset - offset;
                if (offset < 0 || offset + size > headerRaw.Length || size <= 0)
                {
                    throw new InvalidDataException(
                        $"Invalid attribute size or offset in stage header (key: {key}, offset: {offset}, size: {size}).");
                }

                var data = new byte[size];
                Buffer.BlockCopy(headerRaw, offset, data, 0, size);
                stage.Attributes.Add(new SaveDataAttribute(key, data));
            }

            // Determine scenario count from attribute list
            var keyScenarioNum = HashKey.Compute("mScenarioNum");
            var scenarioNum = stage.Attributes.First(a => a.Key == keyScenarioNum).Data[0];

            stage.Scenario = new List<SaveDataStorageGalaxyScenario>(scenarioNum);
            for (var j = 0; j < scenarioNum; j++)
            {
                var scRaw = reader.ReadBytes(scenarioHeaderSize);
                var scenario = new SaveDataStorageGalaxyScenario();
                var scAttrs = scenarioSerializer.attributes.ToList();
                for (var k = 0; k < scAttrs.Count; k++)
                {
                    var (key, off) = scAttrs[k];
                    var nextOff = (k + 1 < scAttrs.Count) ? scAttrs[k + 1].offset : scenarioHeaderSize;
                    var size = nextOff - off;
                    if (off < 0 || off + size > scRaw.Length || size <= 0)
                    {
                        throw new InvalidDataException(
                            $"Invalid attribute size or offset in scenario header (key: {key}, offset: {off}, size: {size}).");
                    }
                    var data = new byte[size];
                    Buffer.BlockCopy(scRaw, off, data, 0, size);
                    scenario.Attributes.Add(new SaveDataAttribute(key, data));
                }
                stage.Scenario.Add(scenario);
            }

            galaxy.Galaxy.Add(stage);
        }

        return galaxy;
    }

    public void WriteTo(EndianAwareWriter writer, out uint hash)
    {
        writer.WriteUInt16((ushort)Galaxy.Count);

        List<(ushort key, ushort offset)> stageAttrs;
        ushort stageDataSize;
        uint stageHeaderSize;

        {
            // Build stage serializer dynamically from union of keys across all stages
            var stageKeySet = new HashSet<ushort>();
            var stageKeyMaxSize = new Dictionary<ushort, int>();
            foreach (var s in Galaxy)
            {
                foreach (var a in s.Attributes)
                {
                    stageKeySet.Add(a.Key);
                    var len = a.Data.Length;
                    if (!stageKeyMaxSize.TryGetValue(a.Key, out var cur) || len > cur) stageKeyMaxSize[a.Key] = len;
                }
            }

            // ensure there is at least one key so header sizes behave
            stageAttrs = new List<(ushort key, ushort offset)>();
            using var stageMs = new MemoryStream();
            foreach (var key in stageKeySet.ToList())
            {
                var offset = (ushort)stageMs.Position;
                stageAttrs.Add((key, offset));
                var size = stageKeyMaxSize.GetValueOrDefault(key, 0);
                if (size > 0) stageMs.Write(new byte[size]);
            }
            stageDataSize = (ushort)stageMs.Length;

            stageHeaderSize = writer.WriteBinaryDataContentHeader(stageAttrs, stageDataSize);
        }

        List<(ushort key, ushort offset)> scenarioAttrs;
        ushort scenarioDataSize;

        {
            var scenarioKeySet = new HashSet<ushort>();
            var scenarioKeyMaxSize = new Dictionary<ushort, int>();
            foreach (var s in Galaxy)
            {
                foreach (var sc in s.Scenario)
                {
                    foreach (var a in sc.Attributes)
                    {
                        scenarioKeySet.Add(a.Key);
                        var len = a.Data.Length;
                        if (!scenarioKeyMaxSize.TryGetValue(a.Key, out var cur) || len > cur) 
                            scenarioKeyMaxSize[a.Key] = len;
                    }
                }
            }

            scenarioAttrs = new List<(ushort key, ushort offset)>();
            using var scenarioMs = new MemoryStream();
            foreach (var key in scenarioKeySet)
            {
                var offset = (ushort)scenarioMs.Position;
                scenarioAttrs.Add((key, offset));
                var size = scenarioKeyMaxSize.GetValueOrDefault(key, 0);
                if (size > 0) scenarioMs.Write(new byte[size]);
            }
            scenarioDataSize = (ushort)scenarioMs.Length;

            writer.WriteBinaryDataContentHeader(scenarioAttrs, scenarioDataSize);
        }
        
        foreach (var s in Galaxy)
        {
            // Build stage header block using the computed serializer layout
            var headerRaw = new byte[stageDataSize];
            for (var i = 0; i < stageAttrs.Count; i++)
            {
                var (key, offset) = stageAttrs[i];
                var size = ((i + 1) < stageAttrs.Count) ? stageAttrs[i + 1].offset - offset : stageDataSize - offset;
                var attr = s.Attributes?.FirstOrDefault(a => a.Key == key);
                if (attr is { Data.Length: > 0 })
                {
                    Buffer.BlockCopy(attr.Data, 0, headerRaw, offset, Math.Min(size, attr.Data.Length));
                }
            }

            writer.Write(headerRaw);

            // Write each scenario block for this stage
            foreach (var sc in s.Scenario)
            {
                var scRaw = new byte[scenarioDataSize];
                for (var i = 0; i < scenarioAttrs.Count; i++)
                {
                    var (key, offset) = scenarioAttrs[i];
                    var size = ((i + 1) < scenarioAttrs.Count) ? scenarioAttrs[i + 1].offset - offset : scenarioDataSize - offset;
                    var attr = sc.Attributes?.FirstOrDefault(a => a.Key == key);
                    if (attr is { Data.Length: > 0 })
                    {
                        Buffer.BlockCopy(attr.Data, 0, scRaw, offset, Math.Min(size, attr.Data.Length));
                    }
                }
                writer.Write(scRaw);
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