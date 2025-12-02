using System.Text;

namespace SMGSaveData.Galaxy2.String;

public struct HashKey(uint value)
{
    public uint Value { get; set; } = value;
    public ushort ShortValue => (ushort)(Value & 0xFFFF);
    private const uint HashKeyMultiplier = 31;

    public override string ToString() => $"0x{Value:X}";

    // TODO: On Switch, Japanese strings use UTF-8; on Wii something else is used, adapt if needed
    public static HashKey FromString(string s) => new(
        Encoding.UTF8.GetBytes(s)
            .Select(b => (sbyte)b)
            .Aggregate<sbyte, uint>(0, (current, b) => (uint)b + (current * HashKeyMultiplier))
    );

    public static ushort Compute(string name) => FromString(name).ShortValue;
}