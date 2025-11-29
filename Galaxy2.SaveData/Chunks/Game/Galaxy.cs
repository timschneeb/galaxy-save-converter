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
            stage.Attributes = reader.ReadAttributes(stageSerializer);
            stage.Scenarios = new List<GalaxyScenario>(stage.ScenarioNum);

            var dataSize = stage.Attributes.Sum(x => x.Size);
            
            for (var j = 0; j < stage.ScenarioNum; j++)
            {
                var scenario = new GalaxyScenario();
                scenario.Attributes = reader.ReadAttributes(scenarioSerializer);
                stage.Scenarios.Add(scenario);
                
                dataSize += (ushort)scenario.Attributes.Sum(x => x.Size);
            }

            Console.WriteLine($"[Galaxy] Stage {i}; data size: {dataSize} bytes");
            stage.DataSize = (ushort)dataSize;
            
            galaxy.Galaxy.Add(stage);
        }
        
        return galaxy;
    }
    
    public void WriteTo(EndianAwareWriter writer, out uint hash)
    {
        writer.WriteUInt16((ushort)Galaxy.Count);

        // Validate attributes
        foreach (var s in Galaxy)
        {
            foreach (var sc in s.Scenarios)
            {
                // Ensure all allowed attributes are present for the platform and at the correct locations
                var allowedAttributes = writer.ConsoleType == ConsoleType.Switch
                    ? GalaxyScenario.AllowedSwitchAttributes
                    : GalaxyScenario.AllowedWiiAttributes;
        
                var validatedAttrs = new List<AbstractDataAttribute>();
                foreach (var reqAttr in allowedAttributes)
                {
                    // Insert existing attribute if present, otherwise the default one
                    var existingAttr = sc.Attributes.Find(a => a.Key == reqAttr.Key);
                    validatedAttrs.Add(existingAttr ?? reqAttr);
                }
                
                sc.Attributes = validatedAttrs;
            }
        }
        
        var stageHeader = BuildHeaderLayout(Galaxy.Select(s => s.Attributes));
        var stageHeaderSize = writer.WriteAttributeTableHeader(stageHeader);

        var scenarioHeader = BuildHeaderLayout(Galaxy.SelectMany(s => s.Scenarios).Select(sc => sc.Attributes));
        writer.WriteAttributeTableHeader(scenarioHeader);
         
        foreach (var s in Galaxy)
        {
            s.Attributes.ForEach(x => x.WriteTo(writer));

            foreach (var sc in s.Scenarios)
            {
                sc.Attributes.ForEach(x => x.WriteTo(writer));
            }
        }
          
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
          
        hash = scenarioHeader.DataSize + stageHeaderSize + 2;
    }

    // Helper: compute layout (key->offset list) and total data size from a sequence of attribute collections
    private static AttributeTableHeader BuildHeaderLayout(IEnumerable<IEnumerable<AbstractDataAttribute>> groups)
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

        return new AttributeTableHeader { Offsets = layout, DataSize = (ushort)ms.Length };
    }
}