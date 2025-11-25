using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData
{
    internal static class BinaryReaderExtensions
    {
        public static ushort ReadUInt16Be(this BinaryReader reader)
        {
            var b = reader.ReadBytes(2);
            return (ushort)((b[0] << 8) | b[1]);
        }

        public static uint ReadUInt32Be(this BinaryReader reader)
        {
            var b = reader.ReadBytes(4);
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        public static long ReadInt64Be(this BinaryReader reader)
        {
            var b = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToInt64(b, 0);
        }

        public static (uint magic, uint hash, uint size, int innerSize, long startPos) ReadChunkHeader(this BinaryReader reader)
        {
            var start = reader.BaseStream.Position;
            var magic = reader.ReadUInt32Be();
            var hash = reader.ReadUInt32Be();
            var size = reader.ReadUInt32Be();
            var inner = (int)(size - 12);
            return (magic, hash, size, inner, start);
        }

        public static (ushort attributeNum, int dataSize, List<(ushort key, int offset)> attributes) ReadBinaryDataContentHeaderSerializer(this BinaryReader reader)
        {
            var attributeNum = reader.ReadUInt16Be();
            var dataSize = reader.ReadUInt16Be();
            var attrs = new List<(ushort key, int offset)>();
            for (var i = 0; i < attributeNum; i++)
            {
                var key = reader.ReadUInt16Be();
                var offset = reader.ReadUInt16Be();
                attrs.Add((key, offset));
            }
            return (attributeNum, dataSize, attrs);
        }

        public static (Dictionary<ushort,int> attributes, int headerDataSize) ReadAttributesAsDictionary(this BinaryReader reader)
        {
            var (attributeNum, dataSize, attrs) = reader.ReadBinaryDataContentHeaderSerializer();
            var dict = new Dictionary<ushort,int>(attributeNum);
            foreach (var a in attrs) dict[a.key] = a.offset;
            return (dict, dataSize);
        }

        private static ushort ComputeKey(string name) => (ushort)(HashKey.FromString(name).Value & 0xFFFF);

        public static bool TryReadU8(this BinaryReader reader, long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out byte value)
        {
            var key = ComputeKey(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadByte();
                return true;
            }
            value = 0;
            return false;
        }

        public static bool TryReadU16(this BinaryReader reader, long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out ushort value)
        {
            var key = ComputeKey(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadUInt16Be();
                return true;
            }
            value = 0;
            return false;
        }

        public static bool TryReadU32(this BinaryReader reader, long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out uint value)
        {
            var key = ComputeKey(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadUInt32Be();
                return true;
            }
            value = 0;
            return false;
        }

        public static bool TryReadI64(this BinaryReader reader, long fieldsStart, Dictionary<ushort,int> attrs, string keyName, out long value)
        {
            var key = ComputeKey(keyName);
            if (attrs.TryGetValue(key, out var off))
            {
                reader.BaseStream.Position = fieldsStart + off;
                value = reader.ReadInt64Be();
                return true;
            }
            value = 0;
            return false;
        }
    }
}
