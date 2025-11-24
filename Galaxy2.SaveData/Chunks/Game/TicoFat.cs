using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageTicoFat
    {
        private const int PartsNum = 6;
        private const int CoinGalaxyNameNum = 16;
        private const int WorldCapacity = 8;

        [JsonPropertyName("star_piece_num")]
        public ushort[,] StarPieceNum { get; set; } = new ushort[WorldCapacity, PartsNum];
        [JsonPropertyName("coin_galaxy_name")]
        public ushort[] CoinGalaxyName { get; set; } = new ushort[CoinGalaxyNameNum];
    }
}
