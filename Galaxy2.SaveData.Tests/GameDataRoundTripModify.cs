using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Galaxy2.SaveData.Chunks.Game;
using Xunit;
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.Tests.Utils;

namespace Galaxy2.SaveData.Tests;

public class GameDataRoundTripModify(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Binary_ReadThenModifyThenWrite_ReadMatchesOriginal()
    {
        var inputBin = "TestData/GameData_Input.bin";
        var outputDir = "GameData_RoundTrip_Modify";
            
        var origBin = $"{outputDir}/GameData_orig.bin";
        var tmpBin = $"{outputDir}/GameData_roundtrip.bin";
        var origJson = $"{outputDir}/GameData_orig.json";
        var roundJson = $"{outputDir}/GameData_round.json";
            
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
            
        File.Copy(inputBin, origBin, true);
            
        // Deserialize original file into object
        var save = SaveDataFile.ReadFile(inputBin, FileType.WiiBin);

        var user1 = save.UserFileInfo.First(x => x.Name.ToString().StartsWith("user1"));
        var player = user1.UserFile!.GameData!.First(x => x is PlayerStatusChunk) as PlayerStatusChunk;
        player!.PlayerStatus.PlayerLeft = 32;
            
        // Serialize back out to a temporary file
        save.WriteFile(tmpBin, FileType.WiiBin);

        // Produce JSON from both files using the existing JSON generator
        Json.Program.Main(["wii2json", inputBin, "-o", origJson]);
        Json.Program.Main(["wii2json", tmpBin, "-o", roundJson]);

        var referenceJson = File.ReadAllText(origJson);
        var generatedJson = File.ReadAllText(roundJson);

        var referenceToken = JsonNode.Parse(referenceJson);
        var generatedToken = JsonNode.Parse(generatedJson);

        var diffs = referenceToken.CompareWith(generatedToken, ignoredKeys: ["Misc.last_modified"]);
        foreach (var d in diffs)
        {
            testOutputHelper.WriteLine(d);
        }

        Assert.True(diffs.Count == 1, "JSON doesn't reflect change");
            
        var referenceBin = File.ReadAllBytes(origBin);
        var generatedBin = File.ReadAllBytes(tmpBin);
          
        var excludedBinaryDiffs = Exclusions.Make(Exclusions.TimestampMode.Approximate);
        var diffsBlocks = referenceBin
            .CompareWith(generatedBin, excludedBinaryDiffs)
            .AlsoPrintDiffs(testOutputHelper);
            
        Assert.True(diffsBlocks.Count == 1, "Round-tripped binary doesn't reflect change (checksum and modified data)");
    }
}