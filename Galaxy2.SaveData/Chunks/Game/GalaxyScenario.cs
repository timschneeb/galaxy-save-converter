using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game.Attributes;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Chunks.Game;

public class GalaxyScenario
{
    [JsonPropertyName("attributes")]
    public List<AbstractDataAttribute> Attributes { get; set; } = [];

    [JsonIgnore]
    public byte MissNum
    {
        get => Attributes.FindByName<byte>("mMissNum")?.Value ?? 0;
        set => Attributes.FindByName<byte>("mMissNum")!.Value = value;
    }
    [JsonIgnore]
    public uint BestTime
    {
        get => Attributes.FindByName<uint>("mBestTime")?.Value ?? 0;
        set => Attributes.FindByName<uint>("mBestTime")!.Value = value;
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyScenarioFlag Flag
    {
        get => new(Attributes.FindByName<byte>("mFlag")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mFlag")!.Value = value.Value;
    }
    
    public struct SaveDataStorageGalaxyScenarioFlag(byte value)
    {
        [JsonIgnore]
        public byte Value { get; private set; } = value;

        [JsonPropertyName("power_star")]
        public bool PowerStar
        {
            get => (Value & 0b1) != 0;
            set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
        }

        [JsonPropertyName("bronze_star")]
        public bool BronzeStar
        {
            get => (Value & 0b10) != 0;
            set => Value = (byte)(value ? (Value | 0b10) : (Value & ~0b10));
        }

        [JsonPropertyName("already_visited")]
        public bool AlreadyVisited
        {
            get => (Value & 0b100) != 0;
            set => Value = (byte)(value ? (Value | 0b100) : (Value & ~0b100));
        }

        [JsonPropertyName("ghost_luigi")]
        public bool GhostLuigi
        {
            get => (Value & 0b1000) != 0;
            set => Value = (byte)(value ? (Value | 0b1000) : (Value & ~0b1000));
        }

        [JsonPropertyName("intrusively_luigi")]
        public bool IntrusivelyLuigi
        {
            get => (Value & 0b10000) != 0;
            set => Value = (byte)(value ? (Value | 0b10000) : (Value & ~0b10000));
        }
    }
}