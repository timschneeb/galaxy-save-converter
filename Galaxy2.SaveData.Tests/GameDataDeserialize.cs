using System;
using System.IO;
using System.Text.Json.Nodes;
using Galaxy2.SaveData.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Galaxy2.SaveData.Tests
{
    public class GameDataJsonDeserialize(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public void GeneratedJson_ShouldMatch_Reference()
        {
            // Run the JSON generator which should produce GameData.json
            Json.Program.Main(["TestData/GameData_Input.bin", "GameData.json"]);

            var referenceJson = File.ReadAllText("TestData/GameData_Reference.json");
            var generatedJson = File.ReadAllText("GameData.json");

            var referenceToken = JsonNode.Parse(referenceJson);
            var generatedToken = JsonNode.Parse(generatedJson);
            
            var diffs = referenceToken.CompareWith(generatedToken);
            foreach (var d in diffs)
            {
                testOutputHelper.WriteLine(d);
            }

            Assert.True(diffs.Count == 0, "Generated JSON does not match reference JSON. See test output for details.");

            try
            {
                if (File.Exists("GameData.json"))
                    File.Delete("GameData.json");
            }
            catch (Exception ex)
            {
                testOutputHelper.WriteLine($"Warning: failed to delete generated GameData.json: {ex.Message}");
            }
        }
    }
}
