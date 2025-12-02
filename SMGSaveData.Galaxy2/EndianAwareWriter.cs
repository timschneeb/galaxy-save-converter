using System.Buffers.Binary;
using SMGSaveData.Galaxy2.Model;
using SMGSaveData.Galaxy2.Utils;

namespace SMGSaveData.Galaxy2;

public class EndianAwareWriter(Stream input, ConsoleType consoleType) : BinaryWriter(input)
{
    public bool BigEndian => ConsoleType == ConsoleType.Wii;
    public ConsoleType ConsoleType { get; set; } = consoleType;
    
    /// <summary>
    /// Creates a new EndianAwareWriter with the same <c name="ConsoleType"/> and endianness.
    /// </summary>
    public EndianAwareWriter NewWriter(Stream stream)
    {
        return new EndianAwareWriter(stream, ConsoleType);
    }
    
    /// <summary>
    /// Writes a DateTime as either Wii ticks or Unix time seconds, depending on the target console.
    /// </summary>
    /// <param name="time">Date time to write. If not in range 2000-2199, the current time will be written as fallback</param>
    public void WriteTime(DateTime time)
    {
        if (time.Year is < 2000 or > 2199)
        {
            time = DateTimeOffset.UtcNow.DateTime;
        }
        
        var ticks = ConsoleType == ConsoleType.Wii
            ? OsTime.UnixToWiiTicks(time)
            : ((DateTimeOffset)time).ToUnixTimeSeconds();
        this.WriteInt64(ticks);
    }
    
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
        Write(bytes);
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