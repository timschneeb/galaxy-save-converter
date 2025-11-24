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
                var name = new Galaxy2.SaveData.String.FixedString12(reader);
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
    }
}
