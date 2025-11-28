using System;
using System.IO;
using System.Text.Json.Nodes;
using Galaxy2.SaveData.Tests.Utils;
using Xunit;

namespace Galaxy2.SaveData.Tests;

public class GameDataJsonDeserialize(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void GeneratedJson_BE_ShouldMatch_Reference() => 
        Deserialize("be2json", "TestData/GameData_Input.bin", "TestData/GameData_Reference.json");

    [Fact]
    public void GeneratedJson_LE_ShouldMatch_Reference() => 
        Deserialize("le2json", "TestData/GameData_LE_SwitchPort.bin", "TestData/GameData_LE_Reference.json");

    private void Deserialize(string mode, string inputFile, string referenceJsonFile)
    {
        const string outputDir = "GameData_JsonDeserialize";
        var outputJson = $"{outputDir}/{mode}_GameData.json";
        
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
        
        // Run the JSON generator which should produce GameData.json
        Json.Program.Main([mode, inputFile, outputJson]);

        var referenceJson = File.ReadAllText(referenceJsonFile);
        var generatedJson = File.ReadAllText(outputJson);

        var referenceToken = JsonNode.Parse(referenceJson);
        var generatedToken = JsonNode.Parse(generatedJson);
            
        var diffs = referenceToken.CompareWith(generatedToken);
        foreach (var d in diffs)
        {
            testOutputHelper.WriteLine(d);
        }

        Assert.True(diffs.Count == 0, "Generated JSON does not match reference JSON. See test output for details.");
    }
}