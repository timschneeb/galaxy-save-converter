using Galaxy2.SaveData.Face;

namespace Galaxy2.SaveData.Chunks.Config
{
    public class ConfigDataMii
    {
        public ConfigDataMiiFlag Flag { get; set; }
        public RFLCreateID MiiId { get; set; }
        public ConfigDataMiiIcon IconId { get; set; }
    }

    public struct ConfigDataMiiFlag(byte value)
    {
        private byte _value = value;

        public bool Unk2
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
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
