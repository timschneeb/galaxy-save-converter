namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageGalaxy
    {
        public List<SaveDataStorageGalaxyStage> Galaxy { get; set; } = new List<SaveDataStorageGalaxyStage>();
    }

    public class SaveDataStorageGalaxyStage
    {
        public ushort GalaxyName { get; set; }
        public ushort FixedHeaderSize { get; set; }
        public byte ScenarioNum { get; set; }
        public SaveDataStorageGalaxyState GalaxyState { get; set; }
        public SaveDataStorageGalaxyFlag Flag { get; set; }
        public List<SaveDataStorageGalaxyScenario> Scenario { get; set; } = new List<SaveDataStorageGalaxyScenario>();
    }

    public enum SaveDataStorageGalaxyState : byte
    {
        Closed = 0,
        New = 1,
        Opened = 2,
    }

    public struct SaveDataStorageGalaxyFlag(byte value)
    {
        private byte _value = value;

        public bool TicoCoin
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }

        public bool Comet
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
        }
    }

    public class SaveDataStorageGalaxyScenario
    {
        public byte MissNum { get; set; }
        public uint BestTime { get; set; }
        public SaveDataStorageGalaxyScenarioFlag Flag { get; set; }
    }

    public struct SaveDataStorageGalaxyScenarioFlag(byte value)
    {
        private byte _value = value;

        public bool PowerStar
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }

        public bool BronzeStar
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
        }

        public bool AlreadyVisited
        {
            get => (_value & 0b100) != 0;
            set => _value = (byte)(value ? (_value | 0b100) : (_value & ~0b100));
        }

        public bool GhostLuigi
        {
            get => (_value & 0b1000) != 0;
            set => _value = (byte)(value ? (_value | 0b1000) : (_value & ~0b1000));
        }

        public bool IntrusivelyLuigi
        {
            get => (_value & 0b10000) != 0;
            set => _value = (byte)(value ? (_value | 0b10000) : (_value & ~0b10000));
        }
    }
}
