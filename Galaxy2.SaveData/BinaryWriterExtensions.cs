// ...existing code...

namespace Galaxy2.SaveData
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteUInt16Be(this BinaryWriter writer, ushort value)
        {
            var b = new byte[2];
            b[0] = (byte)(value >> 8);
            b[1] = (byte)(value & 0xFF);
            writer.Write(b);
        }

        public static void WriteUInt32Be(this BinaryWriter writer, uint value)
        {
            var b = new byte[4];
            b[0] = (byte)(value >> 24);
            b[1] = (byte)((value >> 16) & 0xFF);
            b[2] = (byte)((value >> 8) & 0xFF);
            b[3] = (byte)(value & 0xFF);
            writer.Write(b);
        }

        public static void WriteInt64Be(this BinaryWriter writer, long value)
        {
            var b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            writer.Write(b);
        }

        /// <summary>
        /// Writes a chunk header (magic, hash, size). The size written is innerSize + 12.
        /// </summary>
        public static void WriteChunkHeader(this BinaryWriter writer, uint magic, uint hash, int innerSize)
        {
            writer.WriteUInt32Be(magic);
            writer.WriteUInt32Be(hash);
            writer.WriteUInt32Be((uint)(innerSize + 12));
        }

        /// <summary>
        /// Writes a binary-data-content header serializer (attribute count, data size, then attribute entries key+offset) .
        /// Assumes offsets are u16 and dataSize fits in u16.
        /// </summary>
        public static void WriteBinaryDataContentHeader(this BinaryWriter writer, List<(ushort key, ushort offset)> attrs, ushort dataSize)
        {
            writer.WriteUInt16Be((ushort)attrs.Count);
            writer.WriteUInt16Be(dataSize);
            foreach (var a in attrs)
            {
                writer.WriteUInt16Be(a.key);
                writer.WriteUInt16Be(a.offset);
            }
        }
    }
}
