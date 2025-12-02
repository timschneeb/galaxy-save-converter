using System.Text.Json.Serialization;
using SMGSaveData.Galaxy2.Utils;

namespace SMGSaveData.Galaxy2.Model.Chunks.Game;

public class SaveDataStorageWorldMap
{
    private const int WorldCapacity = 8;
    [JsonPropertyName("star_check_point_flag")]
    public byte[] StarCheckPointFlag { get; set; } = new byte[WorldCapacity];
    [JsonPropertyName("world_no")]
    public byte WorldNo { get; set; } = 1;

    public static SaveDataStorageWorldMap ReadFrom(BinaryReader reader)
    {
        return new SaveDataStorageWorldMap
        {
            StarCheckPointFlag = reader.ReadBytes(WorldCapacity),
            WorldNo = reader.ReadByte()
        };
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        writer.Write(StarCheckPointFlag);
        writer.Write(WorldNo);
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
    }
}