using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Galaxy2.SaveData.Tests
{
    public class GameDataJsonDeserialize
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GameDataJsonDeserialize(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void GeneratedJson_ShouldMatch_Reference()
        {
            // Run the JSON generator which should produce GameData.json
            Json.Program.Main(new[] { "TestData/GameData_Input.bin", "GameData.json" });

            var referenceJson = File.ReadAllText("TestData/GameData_Reference.json");
            var generatedJson = File.ReadAllText("GameData.json");

            var referenceToken = JToken.Parse(referenceJson);
            var generatedToken = JToken.Parse(generatedJson);

            var normRef = SortProperties(referenceToken);
            var normGen = SortProperties(generatedToken);

            // Use custom comparator that collects differences
            var diffs = new List<string>();
            FindDifferences(normRef, normGen, "$", diffs);

            if (diffs.Count > 0)
            {
                _testOutputHelper.WriteLine("Found JSON differences (first 200):");
                int i = 0;
                foreach (var d in diffs)
                {
                    _testOutputHelper.WriteLine(d);
                    if (++i >= 200) break;
                }

                _testOutputHelper.WriteLine($"Total differences: {diffs.Count}");
            }

            Assert.True(diffs.Count == 0, "Generated JSON does not match reference JSON. See test output for details.");

            try
            {
                if (File.Exists("GameData.json"))
                    File.Delete("GameData.json");
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine($"Warning: failed to delete generated GameData.json: {ex.Message}");
            }
        }

        // Recursively sort object properties so that ordering differences don't affect comparison
        private static JToken SortProperties(JToken token)
        {
            if (token is JObject obj)
            {
                var properties = obj.Properties()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => new JProperty(p.Name, SortProperties(p.Value)));
                return new JObject(properties);
            }

            if (token is JArray arr)
            {
                var newArr = new JArray();
                foreach (var item in arr)
                    newArr.Add(SortProperties(item));
                return newArr;
            }

            return token.DeepClone();
        }

        // Find differences between two JTokens and append human-readable messages to diffs
        private static void FindDifferences(JToken expected, JToken actual, string path, IList<string> diffs, int maxDiffs = 200)
        {
            if (diffs.Count >= maxDiffs)
                return;

            if (expected == null && actual == null)
                return;

            if (expected == null || actual == null)
            {
                diffs.Add($"{path}: expected {(expected == null ? "<missing>" : expected.ToString())} but got {(actual == null ? "<missing>" : actual.ToString())}");
                return;
            }

            // Treat integer and float both as numbers
            bool expectedIsNumber = expected.Type == JTokenType.Integer || expected.Type == JTokenType.Float;
            bool actualIsNumber = actual.Type == JTokenType.Integer || actual.Type == JTokenType.Float;

            if (expectedIsNumber && actualIsNumber)
            {
                try
                {
                    var e = Convert.ToDouble(((JValue)expected).Value);
                    var a = Convert.ToDouble(((JValue)actual).Value);
                    var diff = Math.Abs(e - a);
                    var rel = diff / Math.Max(1.0, Math.Abs(e));
                    if (!(diff <= 1e-9 || rel <= 1e-12))
                        diffs.Add($"{path}: numeric mismatch expected={e} actual={a} (absDiff={diff})");
                }
                catch
                {
                    // Fallback to string compare
                    if (expected.ToString() != actual.ToString())
                        diffs.Add($"{path}: value mismatch expected='{expected}' actual='{actual}'");
                }

                return;
            }

            if (expected.Type != actual.Type)
            {
                diffs.Add($"{path}: type mismatch expected={expected.Type} actual={actual.Type}");
                // continue to try deeper comparison where possible
            }

            if (expected is JObject expObj && actual is JObject actObj)
            {
                var expProps = new HashSet<string>(expObj.Properties().Select(p => p.Name));
                var actProps = new HashSet<string>(actObj.Properties().Select(p => p.Name));

                foreach (var prop in expProps.Except(actProps))
                {
                    diffs.Add($"{path}.{prop}: missing in actual");
                    if (diffs.Count >= maxDiffs) return;
                }

                foreach (var prop in actProps.Except(expProps))
                {
                    diffs.Add($"{path}.{prop}: unexpected property in actual");
                    if (diffs.Count >= maxDiffs) return;
                }

                foreach (var prop in expProps.Intersect(actProps).OrderBy(n => n))
                {
                    FindDifferences(expObj[prop], actObj[prop], path + "." + prop, diffs, maxDiffs);
                    if (diffs.Count >= maxDiffs) return;
                }

                return;
            }

            if (expected is JArray expArr && actual is JArray actArr)
            {
                if (expArr.Count != actArr.Count)
                {
                    diffs.Add($"{path}: array length mismatch expected={expArr.Count} actual={actArr.Count}");
                }

                var min = Math.Min(expArr.Count, actArr.Count);
                for (int i = 0; i < min; i++)
                {
                    FindDifferences(expArr[i], actArr[i], path + $"[{i}]", diffs, maxDiffs);
                    if (diffs.Count >= maxDiffs) return;
                }

                return;
            }

            // Fallback for JValue and any other token kinds
            if (expected is JValue expVal && actual is JValue actVal)
            {
                var ev = expVal.Value;
                var av = actVal.Value;
                if (ev == null && av == null) return;
                if (ev == null || av == null)
                {
                    diffs.Add($"{path}: expected={(ev == null ? "null" : ev.ToString())} actual={(av == null ? "null" : av.ToString())}");
                    return;
                }

                if (!ev.Equals(av))
                    diffs.Add($"{path}: value mismatch expected='{ev}' actual='{av}'");

                return;
            }

            // If types are same but not handled above, compare string representations
            if (expected.ToString() != actual.ToString())
                diffs.Add($"{path}: mismatch expected='{expected}' actual='{actual}'");
        }
    }
}
