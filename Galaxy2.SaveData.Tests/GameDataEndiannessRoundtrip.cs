using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.Tests.Utils;

namespace Galaxy2.SaveData.Tests;

public class GameDataEndiannessRoundtrip(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Binary_ReadBeWriteLeReadLeWriteBe_ReadMatchesOriginal()
    {
        var inputBin = "TestData/GameData_Input.bin";
        var outputDir = "GameData_RoundTrip_Endianness";
            
        var origBin = $"{outputDir}/GameData_orig_BE.bin";
        var leBin = $"{outputDir}/GameData_LE.bin";
        var roundtripBin = $"{outputDir}/GameData_roundtrip_BE.bin";
        var origJson = $"{outputDir}/GameData_orig_BE.json";
        var leJson = $"{outputDir}/GameData_LE.json";
        var roundtripJson = $"{outputDir}/GameData_roundtrip_BE.json";
        
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
            
        File.Copy(inputBin, origBin, true);
            
        // Deserialize original file into object
        var save = SaveDataFile.ReadFile(inputBin, bigEndian: true);

        // Serialize back out to a temporary file
        save.WriteFile(leBin, bigEndian: false);
        
        var saveLe = SaveDataFile.ReadFile(leBin, bigEndian: false);
        saveLe.WriteFile(roundtripBin, bigEndian: true);
        
        var diffsBlocks = File.ReadAllBytes(origBin).CompareWith(File.ReadAllBytes(roundtripBin));
        foreach (var d in diffsBlocks)
        {
            testOutputHelper.WriteLine(d);
        }
            
        Assert.True(diffsBlocks.Count == 0, "Round-tripped binary file does not match original binary file. See test output for differing blocks.");        
        
        // Produce JSON from both files using the existing JSON generator
        Json.Program.Main(["be2json", inputBin, origJson]);
        Json.Program.Main(["le2json", leBin, leJson]);
        Json.Program.Main(["be2json", roundtripBin, roundtripJson]);

        AssertJsonFilesEqual(origJson, leJson);
        AssertJsonFilesEqual(origJson, roundtripJson);
        AssertJsonFilesEqual(leJson, roundtripJson);
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