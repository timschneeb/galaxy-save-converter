namespace Galaxy2.SaveData.Save
{
    public class SaveDataFileHeader
    {
        public uint Checksum { get; set; }
        public uint Version { get; set; }
        public uint UserFileInfoNum { get; set; }
        public uint FileSize { get; set; }
    }
}
