using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game.Attributes;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStorageGalaxy
{
    [JsonPropertyName("galaxy")]
    public List<GalaxyStage> Galaxy { get; set; } = [];
    
    public static SaveDataStorageGalaxy ReadFrom(BinaryReader reader)
    {
        var galaxy = new SaveDataStorageGalaxy();
        var galaxyNum = reader.ReadUInt16();
        galaxy.Galaxy = new List<GalaxyStage>(galaxyNum);

        var stageSerializer = reader.ReadAttributeTableHeader();
        var scenarioSerializer = reader.ReadAttributeTableHeader();
        
        for (var i = 0; i < galaxyNum; i++)
        {
            var stage = new GalaxyStage();
            stage.Attributes = ReadAttributes(reader, stageSerializer);
            stage.Scenarios = new List<GalaxyScenario>(stage.ScenarioNum);
            
            for (var j = 0; j < stage.ScenarioNum; j++)
            {
                var scenario = new GalaxyScenario();
                scenario.Attributes = ReadAttributes(reader, scenarioSerializer);
                stage.Scenarios.Add(scenario);
            }

            galaxy.Galaxy.Add(stage);
        }

        return galaxy;
    }

    private static List<AbstractDataAttribute> ReadAttributes(BinaryReader reader, AttributeTableHeader table)
    {
        var list = new List<AbstractDataAttribute>(table.AttributeNum);
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
            list.Add(AbstractDataAttribute.ReadFrom(reader, key, size));
        }

        return list;
    }

    private static void WriteAttributes(EndianAwareWriter writer, List<AbstractDataAttribute> attributes, IReadOnlyList<(ushort key, ushort offset)> layout, int dataSize)
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
    private static (List<(ushort key, ushort offset)> layout, ushort dataSize) BuildLayout(IEnumerable<IEnumerable<AbstractDataAttribute>> groups)
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
        var stageHeaderSize = writer.WriteAttributeTableHeader(stageAttrs, stageDataSize);

        var (scenarioAttrs, scenarioDataSize) = BuildLayout(Galaxy.SelectMany(s => s.Scenarios).Select(sc => sc.Attributes));
        writer.WriteAttributeTableHeader(scenarioAttrs, scenarioDataSize);
         
        foreach (var s in Galaxy)
        {
            WriteAttributes(writer, s.Attributes, stageAttrs, stageDataSize);

            foreach (var sc in s.Scenarios)
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