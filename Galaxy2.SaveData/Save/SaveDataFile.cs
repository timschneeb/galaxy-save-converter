namespace Galaxy2.SaveData.Save
{
    public class SaveDataFile
    {
        public required SaveDataFileHeader Header { get; set; }
        public required List<SaveDataUserFileInfo> UserFileInfo { get; set; }

        public static SaveDataFile ReadLeFile(string path)
        {
            using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
            var header = new SaveDataFileHeader
            {
                Checksum = reader.ReadUInt32Be(),
                Version = reader.ReadUInt32Be(),
                UserFileInfoNum = reader.ReadUInt32Be(),
                FileSize = reader.ReadUInt32Be()
            };

            var userFileInfo = new List<SaveDataUserFileInfo>();
            var userFileOffsets = new List<uint>();
            for (var i = 0; i < header.UserFileInfoNum; i++)
            {
                var name = new String.FixedString12(reader);
                var offset = reader.ReadUInt32Be();
                userFileInfo.Add(new SaveDataUserFileInfo { Name = name });
                userFileOffsets.Add(offset);
            }

            for (var i = 0; i < header.UserFileInfoNum; i++)
            {
                if (userFileOffsets[i] == 0) continue;
                reader.BaseStream.Seek(userFileOffsets[i], SeekOrigin.Begin);
                userFileInfo[i].UserFile = SaveDataUserFile.ReadFrom(reader, userFileInfo[i].Name!.ToString()!);
            }

            return new SaveDataFile { Header = header, UserFileInfo = userFileInfo };
        }

        public void WriteLeFile(string path)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            using var writer = new BinaryWriter(fs);

            // placeholder checksum (will be computed and written at the end)
            writer.WriteUInt32Be(0);
            // version
            writer.WriteUInt32Be(Header.Version);
            // user file info num
            writer.WriteUInt32Be((uint)UserFileInfo.Count);
            // preliminary file size
            writer.WriteUInt32Be(0);

            // reserve space for user file info entries
            var infoStart = writer.BaseStream.Position;
            foreach (var u in UserFileInfo)
            {
                u.Name!.Value.WriteTo(writer);
                writer.WriteUInt32Be(0); // TODO: placeholder offset, are these overwritten later?
            }

            // write each user file and remember offsets
            var offsets = new List<uint>();
            foreach (var u in UserFileInfo)
            {
                var offset = (uint)writer.BaseStream.Position;
                offsets.Add(offset);
                u.UserFile?.WriteTo(writer, u.Name!.ToString()!);
            }
            
            // fill offsets
            var afterData = writer.BaseStream.Position;
            writer.BaseStream.Position = infoStart;
            foreach (var off in offsets)
            {
                // skip name (FixedString12 occupies 12 bytes)
                writer.BaseStream.Position += 12;
                writer.WriteUInt32Be(off);
            }

            // write file size into header (located after checksum, version and userFileInfoNum)
            writer.BaseStream.Position = 12; // checksum(4) + version(4) + userFileInfoNum(4)
            writer.WriteUInt32Be((uint)afterData);

            // compute checksum over the file bytes after the checksum field (i.e., starting at offset 4),
            // using little-endian 16-bit words like Rust's Checksum::from_le_bytes
            writer.Flush();
            fs.Flush();

            var remainingLen = afterData - 4;
            var remaining = new byte[remainingLen];
            fs.Position = 4;
            var totalRead = 0;
            while (totalRead < remainingLen)
            {
                var read = fs.Read(remaining, totalRead, (int)(remainingLen - totalRead));
                if (read == 0) break;
                totalRead += read;
            }

            unchecked
            {
                ushort sum = 0;
                ushort invSum = 0;
                for (int i = 0; i + 1 < remainingLen; i += 2)
                {
                    // little-endian u16
                    ushort term = (ushort)(remaining[i] | (remaining[i + 1] << 8));
                    sum = (ushort)(sum + term);
                    invSum = (ushort)(invSum + (ushort)~term);
                }

                uint checksum = ((uint)sum << 16) | invSum;

                // write checksum at beginning (big-endian as other reads use ReadUInt32Be for header)
                writer.BaseStream.Position = 0;
                writer.WriteUInt32Be(checksum);
            }
        }
    }
}
