using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;
using Galaxy2.SaveData.Save;

namespace Galaxy2.SaveData.Tests
{
    public class GameDataRoundTrip
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GameDataRoundTrip(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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
            var save = SaveDataFile.ReadLeFile(inputBin);

            // Serialize back out to a temporary file
            save.WriteLeFile(tmpBin);

            // Produce JSON from both files using the existing JSON generator
            Json.Program.Main(new[] { inputBin, origJson });
            Json.Program.Main(new[] { tmpBin, roundJson });

            var referenceJson = File.ReadAllText(origJson);
            var generatedJson = File.ReadAllText(roundJson);

            var referenceToken = JsonNode.Parse(referenceJson);
            var generatedToken = JsonNode.Parse(generatedJson);

            var normRef = SortProperties(referenceToken);
            var normGen = SortProperties(generatedToken);

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

            Assert.True(diffs.Count == 0, "Round-tripped JSON does not match original JSON. See test output for details.");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        // Reuse helpers from the existing test to normalize and compare JSON
        private static JsonNode? SortProperties(JsonNode? token)
        {
            if (token is JsonObject obj)
            {
                var properties = obj
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .ToDictionary(kv => kv.Key, kv => SortProperties(kv.Value));
                return JsonNode.Parse(JsonSerializer.Serialize(properties));
            }

            if (token is JsonArray arr)
            {
                var newArr = new JsonArray();
                foreach (var item in arr)
                    newArr.Add(SortProperties(item));
                return newArr;
            }

            return token?.DeepClone();
        }

        private static void FindDifferences(JsonNode? expected, JsonNode? actual, string path, IList<string> diffs, int maxDiffs = 200)
        {
            if (diffs.Count >= maxDiffs)
                return;

            if (expected == null && actual == null)
                return;

            if (expected == null || actual == null)
            {
                diffs.Add($"{path}: expected {(expected == null ? "<missing>" : expected.ToJsonString())} but got {(actual == null ? "<missing>" : actual.ToJsonString())}");
                return;
            }

            bool expectedIsNumber = expected is JsonValue ev1 && (ev1.TryGetValue(out int _) || ev1.TryGetValue(out double _));
            bool actualIsNumber = actual is JsonValue ev2 && (ev2.TryGetValue(out int _) || ev2.TryGetValue(out double _));

            if (expectedIsNumber && actualIsNumber)
            {
                try
                {
                    var e = Convert.ToDouble(expected.GetValue<double>());
                    var a = Convert.ToDouble(actual.GetValue<double>());
                    var diff = Math.Abs(e - a);
                    var rel = diff / Math.Max(1.0, Math.Abs(e));
                    if (!(diff <= 1e-9 || rel <= 1e-12))
                        diffs.Add($"{path}: numeric mismatch expected={e} actual={a} (absDiff={diff})");
                }
                catch
                {
                    if (expected.ToJsonString() != actual.ToJsonString())
                        diffs.Add($"{path}: value mismatch expected='{expected}' actual='{actual}'");
                }

                return;
            }

            if (expected.GetValueKind() != actual.GetValueKind())
            {
                diffs.Add($"{path}: type mismatch expected={expected.GetValueKind()} actual={actual.GetValueKind()}");
            }

            if (expected is JsonObject expObj && actual is JsonObject actObj)
            {
                var expProps = new HashSet<string>(expObj.Select(p => p.Key));
                var actProps = new HashSet<string>(actObj.Select(p => p.Key));

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

            if (expected is JsonArray expArr && actual is JsonArray actArr)
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

            if (expected is JsonValue expVal && actual is JsonValue actVal)
            {
                var kind = expVal.GetValueKind();
                var akind = actVal.GetValueKind();

                if (kind != akind)
                {
                    if (expVal.ToJsonString() == actVal.ToJsonString()) return;
                    diffs.Add($"{path}: type mismatch value kinds expected={kind} actual={akind}");
                    return;
                }

                switch (kind)
                {
                    case JsonValueKind.Number:
                        try
                        {
                            var eNum = expVal.GetValue<double>();
                            var aNum = actVal.GetValue<double>();
                            var diff = Math.Abs(eNum - aNum);
                            var rel = diff / Math.Max(1.0, Math.Abs(eNum));
                            if (!(diff <= 1e-9 || rel <= 1e-12))
                                diffs.Add($"{path}: numeric mismatch expected={eNum} actual={aNum} (absDiff={diff})");
                        }
                        catch
                        {
                            if (expVal.ToJsonString() != actVal.ToJsonString())
                                diffs.Add($"{path}: value mismatch expected='{expVal}' actual='{actVal}'");
                        }
                        return;
                    case JsonValueKind.String:
                        var es = expVal.GetValue<string?>();
                        var asv = actVal.GetValue<string?>();
                        if (es != asv)
                            diffs.Add($"{path}: value mismatch expected='{es}' actual='{asv}'");
                        return;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        var eb = expVal.GetValue<bool>();
                        var ab = actVal.GetValue<bool>();
                        if (eb != ab)
                            diffs.Add($"{path}: value mismatch expected='{eb}' actual='{ab}'");
                        return;
                    case JsonValueKind.Null:
                        return;
                    default:
                        if (expVal.ToJsonString() != actVal.ToJsonString())
                            diffs.Add($"{path}: value mismatch expected='{expVal}' actual='{actVal}'");
                        return;
                }
            }

            if (expected.ToJsonString() != actual.ToJsonString())
                diffs.Add($"{path}: mismatch expected='{expected}' actual='{actual}'");
        }
    }
}