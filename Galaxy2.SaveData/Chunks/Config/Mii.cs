using System.Text.Json.Serialization;
using System.IO;

namespace Galaxy2.SaveData.Chunks.Config
{
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
            mii.MiiId = reader.ReadBytes(8);
            mii.IconId = (ConfigDataMiiIcon)reader.ReadByte();
            reader.BaseStream.Position = start + dataSize;
            return mii;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Flag);
            writer.Write(MiiId);
            writer.Write((byte)IconId);
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
}
