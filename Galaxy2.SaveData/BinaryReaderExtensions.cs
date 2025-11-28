using System.Runtime.CompilerServices;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData;

internal static class BinaryReaderExtensions
{
    extension(BinaryReader reader)
    {
        public (uint magic, uint hash, uint size, int innerSize, long startPos) ReadChunkHeader()
        {
            var start = reader.BaseStream.Position;
            var magic = reader.ReadUInt32();
            var hash = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var inner = (int)(size - 12);
            return (magic, hash, size, inner, start);
        }

        public AttributeTableHeader ReadBinaryDataContentHeaderSerializer()
        {
            var attributeNum = reader.ReadUInt16();
            var dataSize = reader.ReadUInt16();
            var attrs = new List<(ushort key, int offset)>();
            for (var i = 0; i < attributeNum; i++)
            {
                var key = reader.ReadUInt16();
                var offset = reader.ReadUInt16();
                attrs.Add((key, offset));
            }

            return new AttributeTableHeader()
            {
                AttributeNum = attributeNum,
                DataSize = dataSize,
                Offsets = attrs
            };
        }

        public bool TryReadU8(long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out byte value)
        {
            var key = HashKey.Compute(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadByte();
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadU16(long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out ushort value)
        {
            var key = HashKey.Compute(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadUInt16();
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadU32(long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out uint value)
        {
            var key = HashKey.Compute(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadUInt32();
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadI64(long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out long value)
        {
            var key = HashKey.Compute(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadInt64();
                return true;
            }
            value = 0;
            return false;
        }
    }
}