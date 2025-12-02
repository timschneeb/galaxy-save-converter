using System.Text.Json.Serialization;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Model.Chunks.Config;

public class ConfigDataMii
{
    [JsonPropertyName("flag")]
    public byte Flag { get; set; }
    [JsonPropertyName("mii_id")]
    public byte[] MiiId { get; set; } = new byte[8];
    [JsonPropertyName("icon_id")]
    public ConfigDataMiiIcon IconId { get; set; }

    public static ConfigDataMii ReadFrom(BinaryReader reader, int dataSize)
    {
        var mii = new ConfigDataMii();
        var start = reader.BaseStream.Position;
        mii.Flag = reader.ReadByte();
        mii.MiiId = reader.ReadBytes(8); // endianness not handled; however Switch version doesn't support Miis anyways
        mii.IconId = (ConfigDataMiiIcon)reader.ReadByte();
        reader.BaseStream.Position = start + dataSize;
        return mii;
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        writer.Write(Flag);
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            // Miis are not supported on Switch; write zeroed MiiId
            writer.Write(new byte[8]);
            if (IconId == ConfigDataMiiIcon.Mii)
            {
                IconId = ConfigDataMiiIcon.Tico;
            }
            writer.Write((byte)IconId);
            writer.WriteAlignmentPadding(alignment: 4);
        }
        else
        {
            writer.Write(MiiId);
            writer.Write((byte)IconId);
        }
    }
}
    
public enum ConfigDataMiiIcon : byte
{
    Mii = 0,
    Mario = 1,
    Luigi = 2,
    Yoshi = 3,
    Kinopio = 4,
    Peach = 5,
    Rosetta = 6,
    Tico = 7,
}