using System.Text;
using System.Buffers.Binary;

namespace Galaxy2.SaveData;

public class EndianAwareWriter(Stream input) : BinaryWriter(input)
{
    public bool BigEndian { get; set; }
    
    public override void Write(short value)
    {
        Span<byte> bytes = stackalloc byte[2];
        if (BigEndian)
        {
            BinaryPrimitives.WriteInt16BigEndian(bytes, value);
        }
        else
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
        }
        var arr = bytes.ToArray();
        Console.Error.WriteLine($"DEBUG Write(short) BigEndian={BigEndian} bytes={BitConverter.ToString(arr)}");
        BaseStream.Write(arr, 0, arr.Length);
    }

    public override void Write(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (BigEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        }
        Write(bytes);
    }

    public override void Write(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (BigEndian)
        {
            BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        }
        else
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        }
        Write(bytes);
    }

    public override void Write(ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        if (BigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        }
        Write(bytes);
    }

    public override void Write(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (BigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        }
        Write(bytes);
    }

    public override void Write(ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (BigEndian)
        {
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        }
        Write(bytes);
    }
}