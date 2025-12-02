using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Model.Chunks.Game.Attributes;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DataAttribute<byte>), "u8")]
[JsonDerivedType(typeof(DataAttribute<ushort>), "u16")]
[JsonDerivedType(typeof(DataAttribute<uint>), "u32")]
public abstract class AbstractDataAttribute(ushort key)
{
    [JsonPropertyName("key")]
    public ushort Key { get; set; } = key;
    [JsonIgnore]
    public abstract int Size { get; }
    
    public abstract void WriteTo(BinaryWriter writer);
    
    public static AbstractDataAttribute ReadFrom(BinaryReader reader, ushort key, int size)
    {
        return size switch
        {
            1 => new DataAttribute<byte>(key, reader.ReadByte()),
            2 => new DataAttribute<ushort>(key, reader.ReadUInt16()),
            4 => new DataAttribute<uint>(key, reader.ReadUInt32()),
            _ => throw new InvalidDataException($"Unsupported attribute data size: {size}"),
        };
    }
}

public class DataAttribute<T>(ushort key, T value) : AbstractDataAttribute(key) where T : struct
{
    [JsonPropertyName("value")]
    public T Value { get; set; } = value;
    
    [JsonIgnore]
    public override int Size => Marshal.SizeOf(default(T));
    
    public override void WriteTo(BinaryWriter writer)
    {
        switch (Value)
        {
            case byte b:
                writer.Write(b);
                break;
            case ushort us:
                writer.WriteUInt16(us);
                break;
            case uint ui:
                writer.WriteUInt32(ui);
                break;
            default:
                throw new InvalidDataException($"Unsupported attribute data type: {typeof(T)}");
        }
    }

    public override string ToString()
    {
        return Value.ToString() ?? "<null>";
    }
}