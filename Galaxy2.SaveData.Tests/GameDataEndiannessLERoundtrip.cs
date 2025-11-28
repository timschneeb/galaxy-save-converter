using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.Tests.Utils;

[assembly: CaptureConsole]

namespace Galaxy2.SaveData.Tests;

public class GameDataEndiannessLERoundtrip(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Binary_ReadLeWriteBeReadBeWriteLe_ReadMatchesOriginal()
    {
        var inputBin = "TestData/GameData_LE_SwitchPort.bin";
        var outputDir = "GameData_RoundTrip_EndiannessLE";
            
        var origBin = $"{outputDir}/GameData_orig_LE.bin";
        var beBin = $"{outputDir}/GameData_BE.bin";
        var roundtripBin = $"{outputDir}/GameData_roundtrip_LE.bin";
        var origJson = $"{outputDir}/GameData_orig_LE.json";
        var beJson = $"{outputDir}/GameData_BE.json";
        var roundtripJson = $"{outputDir}/GameData_roundtrip_LE.json";
        
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
            
        File.Copy(inputBin, origBin, true);
            
        // Deserialize original file into object
        var save = SaveDataFile.ReadFile(inputBin, ConsoleType.Switch);

        // Serialize back out to a temporary file
        save.WriteFile(beBin, ConsoleType.Wii);
        
        var saveLe = SaveDataFile.ReadFile(beBin, ConsoleType.Wii);
        saveLe.WriteFile(roundtripBin, ConsoleType.Switch);
        
        var diffsBlocks = File.ReadAllBytes(origBin).CompareWith(File.ReadAllBytes(roundtripBin));
        foreach (var d in diffsBlocks)
        {
            testOutputHelper.WriteLine(d);
        }
        
        // Produce JSON from both files using the existing JSON generator
        Json.Program.Main(["le2json", inputBin, origJson]);
        Json.Program.Main(["be2json", beBin, beJson]);
        Json.Program.Main(["le2json", roundtripBin, roundtripJson]);
        
        Assert.True(diffsBlocks.Count == 0, "Round-tripped binary file does not match original binary file. See test output for differing blocks.");        

        AssertJsonFilesEqual(origJson, beJson);
        AssertJsonFilesEqual(origJson, roundtripJson);
        AssertJsonFilesEqual(beJson, roundtripJson);
    }
    
    private void AssertJsonFilesEqual(string origJson, string genJson)
    {
        var referenceJson = File.ReadAllText(origJson);
        var generatedJson = File.ReadAllText(genJson);
        var referenceToken = JsonNode.Parse(referenceJson);
        var generatedToken = JsonNode.Parse(generatedJson);
        var diffs = referenceToken.CompareWith(generatedToken);
        
        if (diffs.Count > 0)
        {
            testOutputHelper.WriteLine($"=> Differences between {origJson} and {genJson}:");
        }
        
        foreach (var d in diffs)
        {
            testOutputHelper.WriteLine(d);
        }
        
        Assert.True(diffs.Count == 0, "Generated JSON does not match reference JSON. See test output for details.");
    }
}