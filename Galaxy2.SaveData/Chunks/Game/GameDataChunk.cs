namespace Galaxy2.SaveData.Chunks.Game
{
    public abstract class GameDataChunk
    {
    }

    public class PlayerStatusChunk : GameDataChunk 
    {
        public required SaveDataStoragePlayerStatus Data { get; set; }
    }
    public class EventFlagChunk : GameDataChunk 
    {
        public required SaveDataStorageEventFlag Data { get; set; }
    }
    public class TicoFatChunk : GameDataChunk 
    {
        public required SaveDataStorageTicoFat Data { get; set; }
    }
    public class EventValueChunk : GameDataChunk
    {
        public required SaveDataStorageEventValue Data { get; set; }
    }
    public class GalaxyChunk : GameDataChunk 
    {
        public required SaveDataStorageGalaxy Data { get; set; }
    }
    public class WorldMapChunk : GameDataChunk
    {
        public required SaveDataStorageWorldMap Data { get; set; }
    }
}