using System.Text;
using System.Buffers.Binary;

namespace Galaxy2.SaveData;

public class EndianAwareReader(Stream input, ConsoleType target) : BinaryReader(input)
{
    public bool BigEndian => ConsoleType == ConsoleType.Wii;
    public ConsoleType ConsoleType { get; set; } = target;
    
    public override short ReadInt16()
    {
        Span<byte> bytes = stackalloc byte[2];
        ReadExactly(bytes);
        return BigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(bytes)
            : BinaryPrimitives.ReadInt16LittleEndian(bytes);
    }

    public override int ReadInt32()
    {
        Span<byte> bytes = stackalloc byte[4];
        ReadExactly(bytes);
        return BigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(bytes)
            : BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    public override long ReadInt64()
    {
        Span<byte> bytes = stackalloc byte[8];
        ReadExactly(bytes);
        return BigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(bytes)
            : BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    public override ushort ReadUInt16()
    {
        Span<byte> bytes = stackalloc byte[2];
        ReadExactly(bytes);
        return BigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(bytes)
            : BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    public override uint ReadUInt32()
    {
        Span<byte> bytes = stackalloc byte[4];
        ReadExactly(bytes);
        return BigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(bytes)
            : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    public override ulong ReadUInt64()
    {
        Span<byte> bytes = stackalloc byte[8];
        ReadExactly(bytes);
        return BigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(bytes)
            : BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }
}