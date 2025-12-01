using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Galaxy2.SaveData.Tests.Utils;

public static class JsonCompareExtensions
{
    extension(JsonNode? token)
    {
        /// Recursively sort object properties so that ordering differences don't affect comparison
        private JsonNode? SortProperties()
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

        /// Dump differences between two JsonNodes to list
        public IList<string> CompareWith(JsonNode? actual, IList<string>? ignoredKeys = null, int maxDiffs = 200)
        {
            var diffs = new List<string>();
            token.CompareWithRecursively(actual, diffs, "$", ignoredKeys ?? [], maxDiffs);
            if (diffs.Count >= maxDiffs)
            {
                diffs.Add("... (more differences omitted)");
            }
        
            return diffs;
        }

        private void CompareWithRecursively(JsonNode? actual, IList<string> diffs, string path, IList<string> ignoredKeys, int maxDiffs = 200)
        {
            if (diffs.Count >= maxDiffs || ignoredKeys.Any(path.Contains))
                return;

            token = SortProperties(token);
            actual = SortProperties(actual);
        
            if (token == null && actual == null)
                return;

            if (token == null || actual == null)
            {
                diffs.Add($"{path}: expected {(token == null ? "<missing>" : token.ToJsonString())} but got {(actual == null ? "<missing>" : actual.ToJsonString())}");
                return;
            }

            // Treat integer and float both as numbers
            var expectedIsNumber = token is JsonValue ev1 && (ev1.TryGetValue(out int _) || ev1.TryGetValue(out double _));
            var actualIsNumber = actual is JsonValue ev2 && (ev2.TryGetValue(out int _) || ev2.TryGetValue(out double _));

            if (expectedIsNumber && actualIsNumber)
            {
                try
                {
                    var e = Convert.ToDouble(token.GetValue<double>());
                    var a = Convert.ToDouble(actual.GetValue<double>());
                    var diff = Math.Abs(e - a);
                    var rel = diff / Math.Max(1.0, Math.Abs(e));
                    if (!(diff <= 1e-9 || rel <= 1e-12))
                        diffs.Add($"{path}: numeric mismatch expected={e} actual={a} (absDiff={diff})");
                }
                catch
                {
                    if (token.ToJsonString() != actual.ToJsonString())
                        diffs.Add($"{path}: value mismatch expected='{token}' actual='{actual}'");
                }

                return;
            }

            if (token.GetValueKind() != actual.GetValueKind())
            {
                diffs.Add($"{path}: type mismatch expected={token.GetValueKind()} actual={actual.GetValueKind()}");
            }

            if (token is JsonObject expObj && actual is JsonObject actObj)
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
                    expObj[prop].CompareWithRecursively(actObj[prop], diffs, path + "." + prop, ignoredKeys, maxDiffs);
                    if (diffs.Count >= maxDiffs) return;
                }

                return;
            }

            if (token is JsonArray expArr && actual is JsonArray actArr)
            {
                if (expArr.Count != actArr.Count)
                {
                    diffs.Add($"{path}: array length mismatch expected={expArr.Count} actual={actArr.Count}");
                }

                var min = Math.Min(expArr.Count, actArr.Count);
                for (var i = 0; i < min; i++)
                {
                    expArr[i].CompareWithRecursively(actArr[i], diffs, path + $"[{i}]", ignoredKeys, maxDiffs);
                    if (diffs.Count >= maxDiffs) return;
                }

                return;
            }

            // Fallback for JsonValue and any other token kinds
            if (token is JsonValue expVal && actual is JsonValue actVal)
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
            if (token.ToJsonString() != actual.ToJsonString())
                diffs.Add($"{path}: mismatch expected='{token}' actual='{actual}'");
        }
    }
}