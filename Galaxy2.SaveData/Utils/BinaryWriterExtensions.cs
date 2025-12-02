using Galaxy2.SaveData.Model.Chunks.Game.Attributes;

namespace Galaxy2.SaveData.Utils;

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
        /// Writes a chunk header (magic, hash, size)
        /// Returns the number of bytes written.
        /// </summary>
        public uint WriteChunkHeader(uint magic, uint hash, int innerSize)
        {
            const uint headerSize = 12;
            writer.WriteUInt32(magic);
            writer.WriteUInt32(hash);
            writer.WriteUInt32((uint)(innerSize + headerSize));
            return headerSize;
        }

        /// <summary>
        /// Writes an attribute table header serializer (attribute count, data size, then attribute entries key+offset)
        /// Assumes offsets are u16 and dataSize fits in u16.
        /// Returns the number of bytes written.
        /// </summary>
        public uint WriteAttributeTableHeader(AttributeTableHeader table)
        {
            writer.WriteUInt16((ushort)table.Offsets.Count);
            writer.WriteUInt16(table.DataSize);
            foreach (var a in table.Offsets)
            {
                writer.WriteUInt16(a.key);
                writer.WriteUInt16(a.offset);
            }
            return 4 + (uint)(table.Offsets.Count * 4);
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