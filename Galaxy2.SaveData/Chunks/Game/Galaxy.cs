using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageGalaxy
    {
        [JsonPropertyName("galaxy")]
        public List<SaveDataStorageGalaxyStage> Galaxy { get; set; } = [];
    }

    public class SaveDataStorageGalaxyStage
    {
        [JsonPropertyName("galaxy_name")]
        public ushort GalaxyName { get; set; }
        [JsonPropertyName("data_size")]
        public ushort FixedHeaderSize { get; set; }
        [JsonPropertyName("scenario_num")]
        public byte ScenarioNum { get; set; }
        [JsonPropertyName("galaxy_state")]
        public SaveDataStorageGalaxyState GalaxyState { get; set; }
        [JsonPropertyName("flag")]
        public SaveDataStorageGalaxyFlag Flag { get; set; }
        [JsonPropertyName("scenario")]
        public List<SaveDataStorageGalaxyScenario> Scenario { get; set; } = [];
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

        [JsonPropertyName("tico_coin")]
        public bool TicoCoin
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }
        
        [JsonPropertyName("comet")]
        public bool Comet
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
        }
    }

    public class SaveDataStorageGalaxyScenario
    {
        [JsonPropertyName("miss_num")]
        public byte MissNum { get; set; }
        [JsonPropertyName("best_time")]
        public uint BestTime { get; set; }
        [JsonPropertyName("flag")]
        public SaveDataStorageGalaxyScenarioFlag Flag { get; set; }
    }

    public struct SaveDataStorageGalaxyScenarioFlag(byte value)
    {
        private byte _value = value;

        [JsonPropertyName("power_star")]
        public bool PowerStar
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }

        [JsonPropertyName("bronze_star")]
        public bool BronzeStar
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
        }

        [JsonPropertyName("already_visited")]
        public bool AlreadyVisited
        {
            get => (_value & 0b100) != 0;
            set => _value = (byte)(value ? (_value | 0b100) : (_value & ~0b100));
        }

        [JsonPropertyName("ghost_luigi")]
        public bool GhostLuigi
        {
            get => (_value & 0b1000) != 0;
            set => _value = (byte)(value ? (_value | 0b1000) : (_value & ~0b1000));
        }

        [JsonPropertyName("intrusively_luigi")]
        public bool IntrusivelyLuigi
        {
            get => (_value & 0b10000) != 0;
            set => _value = (byte)(value ? (_value | 0b10000) : (_value & ~0b10000));
        }
    }
}
