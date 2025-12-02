using System.IO;
using System.Text.Json.Nodes;
using SMGSaveData.CLI;
using SMGSaveData.Galaxy2.Model;
using Xunit;
using SMGSaveData.Galaxy2.Tests.Utils;

namespace SMGSaveData.Galaxy2.Tests;

public class GameDataRoundTrip(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Binary_ReadThenWrite_ReadMatchesOriginal()
    {
        var inputBin = "TestData/GameData_Input.bin";
        var outputDir = "GameData_RoundTrip";
            
        var origBin = $"{outputDir}/GameData_orig.bin";
        var tmpBin = $"{outputDir}/GameData_roundtrip.bin";
        var origJson = $"{outputDir}/GameData_orig.json";
        var roundJson = $"{outputDir}/GameData_round.json";
            
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
            
        File.Copy(inputBin, origBin, true);
            
        // Deserialize original file into object
        var save = SaveDataFile.ReadFile(inputBin, FileType.WiiBin);

        // Serialize back out to a temporary file
        save.WriteFile(tmpBin, FileType.WiiBin);
        
        var referenceBin = File.ReadAllBytes(origBin);
        var generatedBin = File.ReadAllBytes(tmpBin);
            
        var excludedBinaryDiffs = Exclusions.Make(Exclusions.TimestampMode.Approximate);
        var diffsBlocks = referenceBin
            .CompareWith(generatedBin, excludedBinaryDiffs)
            .AlsoPrintDiffs(testOutputHelper);

        Assert.True(diffsBlocks.Count == 0, "Round-tripped binary file does not match original binary file. See test output for differing blocks.");        
        
        // Produce JSON from both files using the existing JSON generator
        Program.Main(["wii2json", inputBin, "-o", origJson]);
        Program.Main(["wii2json", tmpBin, "-o", roundJson]);

        var referenceJson = File.ReadAllText(origJson);
        var generatedJson = File.ReadAllText(roundJson);

        var referenceToken = JsonNode.Parse(referenceJson);
        var generatedToken = JsonNode.Parse(generatedJson);

        var diffs = referenceToken.CompareWith(generatedToken, ignoredKeys: ["Misc.last_modified"]);
        foreach (var d in diffs)
        {
            testOutputHelper.WriteLine(d);
        }

        Assert.True(diffs.Count == 0, "Round-tripped JSON does not match original JSON. See test output for details.");
    }
}