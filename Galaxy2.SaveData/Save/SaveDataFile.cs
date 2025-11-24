namespace Galaxy2.SaveData.Save
{
    public class SaveDataFile
    {
        public required SaveDataFileHeader Header { get; set; }
        public required List<SaveDataUserFileInfo> UserFileInfo { get; set; }
    }
}
