using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(PlayerStatusChunk), "PlayerStatusChunk")]
    [JsonDerivedType(typeof(EventFlagChunk), "EventFlagChunk")]
    [JsonDerivedType(typeof(TicoFatChunk), "TicoFatChunk")]
    [JsonDerivedType(typeof(EventValueChunk), "EventValueChunk")]
    [JsonDerivedType(typeof(GalaxyChunk), "GalaxyChunk")]
    [JsonDerivedType(typeof(WorldMapChunk), "WorldMapChunk")]
    public abstract class GameDataChunk
    {
    }

    public class PlayerStatusChunk : GameDataChunk 
    {
        public required SaveDataStoragePlayerStatus PlayerStatus { get; set; }
    }
    public class EventFlagChunk : GameDataChunk 
    {
        public required SaveDataStorageEventFlag EventFlag { get; set; }
    }
    public class TicoFatChunk : GameDataChunk 
    {
        public required SaveDataStorageTicoFat TicoFat { get; set; }
    }
    public class EventValueChunk : GameDataChunk
    {
        public required SaveDataStorageEventValue EventValue { get; set; }
    }
    public class GalaxyChunk : GameDataChunk 
    {
        public required SaveDataStorageGalaxy Galaxy { get; set; }
    }
    public class WorldMapChunk : GameDataChunk
    {
        public required SaveDataStorageWorldMap WorldMap { get; set; }
    }
}