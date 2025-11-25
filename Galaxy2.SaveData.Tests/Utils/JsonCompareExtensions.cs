using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Galaxy2.SaveData.Tests.Utils;

public static class JsonCompareExtensions
{
    // Recursively sort object properties so that ordering differences don't affect comparison
    private static JsonNode? SortProperties(this JsonNode? token)
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

    // Dump differences between two JsonNodes to list
    public static IList<string> CompareWith(this JsonNode? expected, JsonNode? actual, int maxDiffs = 200)
    {
        var diffs = new List<string>();
        expected.CompareWithRecursively(actual, diffs, "$", maxDiffs);
        if (diffs.Count >= maxDiffs)
        {
            diffs.Add("... (more differences omitted)");
        }
        
        return diffs;
    }
    
    private static void CompareWithRecursively(this JsonNode? expected, JsonNode? actual, IList<string> diffs, string path, int maxDiffs = 200)
    {
        if (diffs.Count >= maxDiffs)
            return;

        expected = SortProperties(expected);
        actual = SortProperties(actual);
        
        if (expected == null && actual == null)
            return;

        if (expected == null || actual == null)
        {
            diffs.Add($"{path}: expected {(expected == null ? "<missing>" : expected.ToJsonString())} but got {(actual == null ? "<missing>" : actual.ToJsonString())}");
            return;
        }

        // Treat integer and float both as numbers
        var expectedIsNumber = expected is JsonValue ev1 && (ev1.TryGetValue(out int _) || ev1.TryGetValue(out double _));
        var actualIsNumber = actual is JsonValue ev2 && (ev2.TryGetValue(out int _) || ev2.TryGetValue(out double _));

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
                expObj[prop].CompareWithRecursively(actObj[prop], diffs, path + "." + prop, maxDiffs);
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
            for (var i = 0; i < min; i++)
            {
                expArr[i].CompareWithRecursively(actArr[i], diffs, path + $"[{i}]", maxDiffs);
                if (diffs.Count >= maxDiffs) return;
            }

            return;
        }

        // Fallback for JsonValue and any other token kinds
        if (expected is JsonValue expVal && actual is JsonValue actVal)
        {
            var kind = expVal.GetValueKind();
            var akind = actVal.GetValueKind();

            if (kind != akind)
            {
                // Try to normalize boolean/string differences: compare textual JSON if kinds differ
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

        // If types are same but not handled above, compare string representations
        if (expected.ToJsonString() != actual.ToJsonString())
            diffs.Add($"{path}: mismatch expected='{expected}' actual='{actual}'");
    }
}