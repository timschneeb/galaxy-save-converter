using System.Text;

namespace Galaxy2.SaveData.String;

public struct HashKey(uint value)
{
    public uint Value { get; set; } = value;
    public ushort ShortValue => (ushort)(Value & 0xFFFF);
    private const uint HashKeyMultiplier = 31;

    public override string ToString() => $"0x{Value:X}";

    public static HashKey FromString(string s) => new(
        Encoding.UTF8.GetBytes(s)
            .Select(b => (sbyte)b)
            .Aggregate<sbyte, uint>(0, (current, b) => (uint)b + (current * HashKeyMultiplier))
    );

    public static ushort Compute(string name) => FromString(name).ShortValue;
}