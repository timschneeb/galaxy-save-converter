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
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);

            // TODO: placeholder checksum
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
            
            // TODO: double-check if the position changes are correct here

            // fill offsets
            var afterData = writer.BaseStream.Position;
            writer.BaseStream.Position = infoStart;
            foreach (var off in offsets)
            {
                // skip name
                writer.BaseStream.Position += 12;
                writer.WriteUInt32Be(off);
            }

            // restore position and write file size
            writer.BaseStream.Position = afterData;
            writer.WriteUInt32Be((uint)afterData);
        }
    }
}
