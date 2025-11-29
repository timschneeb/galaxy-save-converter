namespace Galaxy2.SaveData.Tests.Utils;

public static class Exclusions
{
    public static readonly long[] Addresses = 
    [
        // Relaxed comparison for timestamps due to minimal precision loss during Wii -> Switch/.NET conversion 
        0x1029, 0x103A,
        0x2019, 0x201A,
        0x2FE9, 0x2FFA,
        0x3057, 0x3058,
        // Skip initial checksum
        0x0, 0x1, 0x2, 0x3
    ];
}