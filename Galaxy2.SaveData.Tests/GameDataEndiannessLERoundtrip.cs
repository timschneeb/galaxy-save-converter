using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Galaxy2.SaveData.Model;
using Xunit;
using Galaxy2.SaveData.Tests.Utils;

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
        var save = SaveDataFile.ReadFile(inputBin, FileType.SwitchBin);

        // Serialize back out to a temporary file
        save.WriteFile(beBin, FileType.WiiBin);
        
        var saveLe = SaveDataFile.ReadFile(beBin, FileType.WiiBin);
        saveLe.WriteFile(roundtripBin, FileType.SwitchBin);

        var excludedBinaryDiffs = 
            Exclusions.Make(Exclusions.TimestampMode.Skip, isSwitchFile: true, skipSwitchOnlyFields: true)
                // Exclude some additional Switch-only fields in GalaxyScenario structs
                .Concat(new List<Exclusions.AddressSpan>()
                {
                    new(0x6F0, 16),
                    new(0x700, 2),
                    new(0xF57, 6)
                })
                .ToList();
        
        var diffsBlocks = File.ReadAllBytes(origBin)
            .CompareWith(File.ReadAllBytes(roundtripBin), excludedBinaryDiffs)
            .AlsoPrintDiffs(testOutputHelper);
        
        // Produce JSON from both files using the existing JSON generator
        Json.Program.Main(["switch2json", inputBin, "-o", origJson]);
        Json.Program.Main(["wii2json", beBin, "-o", beJson]);
        Json.Program.Main(["switch2json", roundtripBin, "-o", roundtripJson]);
        
        Assert.True(diffsBlocks.Count == 0, "Round-tripped binary file does not match original binary file. See test output for differing blocks.");        

        AssertJsonFilesEqual(origJson, roundtripJson);
    }
    
    private void AssertJsonFilesEqual(string origJson, string genJson)
    {
        var referenceJson = File.ReadAllText(origJson);
        var generatedJson = File.ReadAllText(genJson);
        var referenceToken = JsonNode.Parse(referenceJson);
        var generatedToken = JsonNode.Parse(generatedJson);
        var diffs = referenceToken.CompareWith(generatedToken, ignoredKeys: ["Misc.last_modified", "PlayerStatus.attributes", ".scenario", "time_sent"]);
        
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