using SMGSaveData.Galaxy2.Model.Chunks.Game.Attributes;
using SMGSaveData.Galaxy2.String;

namespace SMGSaveData.Galaxy2.Utils;

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

        public AttributeTableHeader ReadAttributeTableHeader()
        {
            var attributeNum = reader.ReadUInt16();
            var dataSize = reader.ReadUInt16();
            var attrs = new List<(ushort key, ushort offset)>();
            for (var i = 0; i < attributeNum; i++)
            {
                var key = reader.ReadUInt16();
                var offset = reader.ReadUInt16();
                attrs.Add((key, offset));
            }

            return new AttributeTableHeader()
            {
                DataSize = dataSize,
                Offsets = attrs
            };
        }
        
        public List<AbstractDataAttribute> ReadAttributes(AttributeTableHeader table)
        {
            var list = new List<AbstractDataAttribute>(table.Offsets.Count);
            var headerStart = reader.BaseStream.Position;
        
            for (var i = 0; i < table.Offsets.Count; i++)
            {
                var (key, offset) = table.Offsets[i];
                var nextOffset = (i + 1 < table.Offsets.Count) ? table.Offsets[i + 1].offset : table.DataSize;
                var size = nextOffset - offset;
                if (offset + size > table.DataSize || size <= 0)
                {
                    throw new InvalidDataException(
                        $"Invalid attribute size or offset in header (key: {key}, offset: {offset}, size: {size}).");
                }

                reader.BaseStream.Position = headerStart + offset;
                list.Add(AbstractDataAttribute.ReadFrom(reader, key, size));
            }

            return list;
        }

        public bool TryReadU8(long fieldsStart, Dictionary<ushort,ushort> attrs, string keyName, out byte value)
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

        public bool TryReadU16(long fieldsStart, Dictionary<ushort,ushort> attrs, string keyName, out ushort value)
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

        public bool TryReadU32(long fieldsStart, Dictionary<ushort,ushort> attrs, string keyName, out uint value)
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

        public bool TryReadI64(long fieldsStart, Dictionary<ushort,ushort> attrs, string keyName, out long value)
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