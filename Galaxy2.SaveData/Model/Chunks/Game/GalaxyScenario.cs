using System.Text.Json.Serialization;
using Galaxy2.SaveData.Model.Chunks.Game.Attributes;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Model.Chunks.Game;

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
    
    /*
        --- Switch Attributes (w/ size)
        Galaxy Scenario Attributes:
            CFBD: 1
            F25E: 4
            7579: 1
            2DA8: 2
            8DD1: 2
            26FF: 2

        --- Wii Attributes
        Galaxy Scenario Attributes:
            CFBD: 1
            F25E: 4
            7579: 1
     */
    
    public static List<AbstractDataAttribute> AllowedSwitchAttributes { get; } =
    [
        new DataAttribute<byte>(0xCFBD, 0), // mMissNum
        new DataAttribute<uint>(0xF25E, 0), // mBestTime
        new DataAttribute<byte>(0x7579, 0), // mFlag
        new DataAttribute<ushort>(0x2DA8, 0), // mClearStageNum
        new DataAttribute<ushort>(0x8DD1, 0), // mMissStageNum
        new DataAttribute<ushort>(0x26FF, 0) // mTotalPlaySecond
    ];
    
    public static List<AbstractDataAttribute> AllowedWiiAttributes { get; } =
    [
        new DataAttribute<byte>(0xCFBD, 0), // mMissNum
        new DataAttribute<uint>(0xF25E, 0), // mBestTime
        new DataAttribute<byte>(0x7579, 0) // mFlag
    ];
}