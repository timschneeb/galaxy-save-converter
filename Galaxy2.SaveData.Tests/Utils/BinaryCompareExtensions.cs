using System;
using System.Collections.Generic;

namespace Galaxy2.SaveData.Tests.Utils;

public static class BinaryCompareExtensions
{
    private const int MaxBytesToShow = 32;
    
    private static string DumpBytes(byte[] data, long offset, long length)
    {
        var want = (int)Math.Min(MaxBytesToShow, length);
        var expAvail = offset < data.Length ? Math.Min(want, (int)(data.Length - offset)) : 0;

        if (expAvail == 0)
            return "<EOF>";
        return BitConverter.ToString(data, (int)offset, expAvail).Replace('-', ' ') + (length > expAvail ? " ..." : "");
    }
    
    // Dump differences between two byte arrays to list
    public static IList<string> CompareWith(this byte[] expected, byte[] actual, int maxDiffs = 200)
    {
        var diffs = new List<string>();
        var diffsBlocks = new List<(long offset, long length)>();
        var minLen = Math.Min(expected.Length, actual.Length);
        long pos = 0;
        while (pos < minLen)
        {
            if (expected[pos] != actual[pos] && !Exclusions.Addresses.Contains(pos))
            {
                var start = pos;
                while (pos < minLen && expected[pos] != actual[pos]) pos++;
                var len = pos - start;
                diffsBlocks.Add((start, len));
            }
            else
            {
                pos++;
            }
        }
            
        if (expected.Length != actual.Length)
        {
            // Tail difference: extra bytes in one of the files
            long start = minLen;
            long len = Math.Abs(expected.Length - actual.Length);
            diffsBlocks.Add((start, len));
        }
            
        var c = 0;
        foreach (var (offset, length) in diffsBlocks)
        {
            
            var expectedDump = DumpBytes(expected, offset, length);
            var actualDump = DumpBytes(actual, offset, length);
            diffs.Add($"Block {++c}: offset=0x{offset:X} ({offset}), length={length}, expected=[{expectedDump}], actual=[{actualDump}]");
            if (diffs.Count >= maxDiffs)
            {
                diffs.Add("... (more differences omitted)");
                break;
            }
        }
        
        return diffs;
    }
}