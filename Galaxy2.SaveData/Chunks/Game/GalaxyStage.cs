using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game.Attributes;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Chunks.Game;

public class GalaxyStage
{
    [JsonPropertyName("attributes")]
    public List<AbstractDataAttribute> Attributes { get; set; } = [];

    [JsonPropertyName("scenario")]
    public List<GalaxyScenario> Scenarios { get; set; } = [];
    
    [JsonIgnore]
    public ushort GalaxyName
    {
        get => Attributes.FindByName<ushort>("mGalaxyName")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mGalaxyName")!.Value = value;
    }
    [JsonIgnore]
    public ushort DataSize
    {
        get => Attributes.FindByName<ushort>("mDataSize")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mDataSize")!.Value = value;
    }
    [JsonIgnore]
    public byte ScenarioNum
    {
        get => Attributes.FindByName<byte>("mScenarioNum")?.Value ?? 0;
        set => Attributes.FindByName<byte>("mScenarioNum")!.Value = value;
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyState GalaxyState
    {
        get => (SaveDataStorageGalaxyState)(Attributes.FindByName<byte>("mGalaxyState")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mGalaxyState")!.Value = (byte)value;
    }
    [JsonIgnore]
    public SaveDataStorageGalaxyFlag Flag
    {
        get => new(Attributes.FindByName<byte>("mFlag")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mFlag")!.Value = value.Value;
    }
    
    public enum SaveDataStorageGalaxyState : byte
    {
        Closed = 0,
        New = 1,
        Opened = 2,
    }

    public struct SaveDataStorageGalaxyFlag(byte value)
    {
        [JsonIgnore]
        public byte Value { get; private set; } = value;

        [JsonPropertyName("tico_coin")]
        public bool TicoCoin
        {
            get => (Value & 0b1) != 0;
            set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
        }
        
        [JsonPropertyName("comet")]
        public bool Comet
        {
            get => (Value & 0b10) != 0;
            set => Value = (byte)(value ? (Value | 0b10) : (Value & ~0b10));
        }
    }
    
    /*
        --- Switch Attributes (w/ size)
        Galaxy Stage Attributes:
            8208: 2
            0658: 2
            6729: 1
            ACB4: 1
            7579: 1

        --- Wii Attributes
        Galaxy Stage Attributes:
            8208: 2
            0658: 2
            6729: 1
            ACB4: 1
            7579: 1
           
        Same attribute keys and sizes on both platforms.
     */
}