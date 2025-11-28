using System.Runtime.CompilerServices;

namespace Galaxy2.SaveData;

internal static class BinaryWriterExtensions
{
    extension(BinaryWriter writer)
    {
        // --- Explicit helper methods ---
        public void WriteInt16(short value) => writer.Write(value);
        public void WriteInt32(int value) => writer.Write(value);
        public void WriteInt64(long value) => writer.Write(value);
        public void WriteUInt16(ushort value) => writer.Write(value);
        public void WriteUInt32(uint value) => writer.Write(value);
        public void WriteUInt64(ulong value) => writer.Write(value);
        
        /// <summary>
        /// Writes a chunk header (magic, hash, size). The size written is innerSize + 12.
        /// </summary>
        public void WriteChunkHeader(uint magic, uint hash, int innerSize)
        {
            writer.WriteUInt32(magic);
            writer.WriteUInt32(hash);
            writer.WriteUInt32((uint)(innerSize + 12));
        }

        /// <summary>
        /// Writes a binary-data-content header serializer (attribute count, data size, then attribute entries key+offset)
        /// Assumes offsets are u16 and dataSize fits in u16.
        /// Returns the number of bytes written.
        /// </summary>
        public uint WriteBinaryDataContentHeader(List<(ushort key, ushort offset)> attrs, ushort dataSize)
        {
            writer.WriteUInt16((ushort)attrs.Count);
            writer.WriteUInt16(dataSize);
            foreach (var a in attrs)
            {
                writer.WriteUInt16(a.key);
                writer.WriteUInt16(a.offset);
            }
            return 4 + (uint)(attrs.Count * 4);
        }
        
        /// <summary>
        /// Writes padding bytes to align the current stream position to the specified alignment.
        /// </summary>
        public void WriteAlignmentPadding(int alignment)
        {
            var pos = writer.BaseStream.Position;
            var pad = (int)((alignment - (pos % alignment)) % alignment);
            if (pad > 0)
                writer.Write(new byte[pad]);
        }
    }
}