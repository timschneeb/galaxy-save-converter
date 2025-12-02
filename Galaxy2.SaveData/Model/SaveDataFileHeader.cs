using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Model;

public class SaveDataFileHeader
{
    /*
     * This header is mostly used for reading binaries only.
     * 
     * When writing binary files, only the version is used from this class,
     * the other values are recalculated automatically.
     * Because of this, only the version is serialized to JSON.
     */
    [JsonIgnore]
    public uint Checksum { get; set; }
    [JsonPropertyName("version")]
    public uint Version { get; set; }
    [JsonIgnore]
    public uint UserFileInfoNum { get; set; }
    [JsonIgnore]
    public uint FileSize { get; set; }
}