using System.IO;
using System.Text.Json.Nodes;
using Xunit;
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.Tests.Utils;

namespace Galaxy2.SaveData.Tests;

public class GameDataJsonRoundTrip(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Binary_ThenJson_ThenBinary_ReadMatchesOriginal()
    {
        var inputBin = "TestData/GameData_Input.bin";
        var outputDir = "GameData_Json_RoundTrip";
            
        var origBin = $"{outputDir}/GameData_orig.bin";
        var json = $"{outputDir}/GameData.json";
        var roundBin = $"{outputDir}/GameData_roundtrip.bin";
        var roundJson = $"{outputDir}/GameData_roundtrip.json";
            
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
            
        File.Copy(inputBin, origBin, true);
        
        var save = SaveDataFile.ReadFile(inputBin, FileType.WiiBin);
        save.WriteFile(json, FileType.Json);
        
        var saveBack = SaveDataFile.ReadFile(json, FileType.Json);
        saveBack.WriteFile(roundBin, FileType.WiiBin);

        var saveBackJson = SaveDataFile.ReadFile(roundBin, FileType.WiiBin);
        saveBackJson.WriteFile(roundJson, FileType.Json);
        
        var referenceBin = File.ReadAllBytes(origBin);
        var generatedBin = File.ReadAllBytes(roundBin);
            
        var excludedBinaryDiffs = Exclusions.Make(Exclusions.TimestampMode.Approximate);
        var diffsBlocks = referenceBin
            .CompareWith(generatedBin, excludedBinaryDiffs)
            .AlsoPrintDiffs(testOutputHelper);

        Assert.True(diffsBlocks.Count == 0, "Round-tripped binary file does not match original binary file. See test output for differing blocks.");        
        
        var referenceToken = JsonNode.Parse(File.ReadAllText(json));
        var generatedToken = JsonNode.Parse(File.ReadAllText(roundJson));

        var diffs = referenceToken.CompareWith(generatedToken, ignoredKeys: ["Misc.last_modified"]);
        foreach (var d in diffs)
        {
            testOutputHelper.WriteLine(d);
        }

        Assert.True(diffs.Count == 0, "Round-tripped JSON does not match original JSON. See test output for details.");
    }
}