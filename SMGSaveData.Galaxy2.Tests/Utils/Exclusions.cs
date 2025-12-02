using System.Collections.Generic;
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

namespace SMGSaveData.Galaxy2.Tests.Utils;

public static class Exclusions
{
    public struct AddressSpan(long start, long length)
    {
        public long Start { get; set; } = start;
        public long Length { get; set; } = length;
        
        public bool Contains(long address)
        {
            var c = address >= Start && address < Start + Length;
            return c;
        }
    }
    
    public enum TimestampMode
    {
        Skip,
        Approximate,
        Exact
    }
    
    public static IList<AddressSpan> Make(
        TimestampMode timestamp = TimestampMode.Exact,
        bool isSwitchFile = false,
        bool skipSwitchOnlyFields = false, 
        bool skipMiiData = false)
    {
        if (!skipSwitchOnlyFields && timestamp == TimestampMode.Exact && !skipMiiData)
            return [];
        
        const long user1Offset = 0x0080;
        const long user2Offset = 0x1060;
        const long user3Offset = 0x2040;
        const long sysConfigOffset = 0x3020;
        
        // Exclude checksum field always
        var spans = new List<AddressSpan> { new(0x0, 4) };
        if (timestamp != TimestampMode.Exact)
        {
            var timestampOffset = isSwitchFile ? 0xFB5 : 0xFB3;
            var sysConfigTimeOffset = 0x31;
            // Skip only the lower-precision part of the timestamp
            if (timestamp == TimestampMode.Approximate)
            {
                timestampOffset += 4;
                sysConfigTimeOffset += 4;
            }

            var timestampLength = timestamp == TimestampMode.Skip ? 8 : 4;
            
            // Relaxed comparison for timestamps due to minimal precision loss during Wii -> Switch/.NET conversion
            spans.AddRange([
                // User*/LastModified
                new(user1Offset + timestampOffset, timestampLength),
                new(user2Offset + timestampOffset, timestampLength),
                new(user3Offset + timestampOffset, timestampLength),
                new(sysConfigOffset + sysConfigTimeOffset, timestampLength)  // SysConfig/LastSent
            ]);
        }
        
        if (skipSwitchOnlyFields)
        {
            // Exclude fields that exist only on Switch and not on Wii and will cause differences
            // after a roundtrip conversion due to data being zeroed.
            // NOTE: only needed when comparing Switch -> Wii -> Switch conversions. Not valid on Wii save files.
            spans.AddRange([
                // Additional PlayerStatus attributes added on Switch
                new(user1Offset + 0x60, 18),
                new(user2Offset + 0x60, 18),
                new(user3Offset + 0x60, 18),
            ]);
        }

        if (skipMiiData)
        {
            spans.AddRange([
                // Mii data only exists on Wii; last byte is character icon
                new(user1Offset + 0xF9E, 9),
                new(user2Offset + 0xF9E, 9),
                new(user3Offset + 0xF9E, 9),
            ]);
        }
        return spans;
    }
    

    public static readonly string[] JsonKeys = 
    [
        // Ignore last modified timestamps in config chunks
        "last_modified",
        "time_sent",
        // Ignore Mii ID
        "mii_id",
        "icon_id"
    ];
}