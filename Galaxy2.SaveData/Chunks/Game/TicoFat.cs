using System.Text.Json.Serialization;
using System.IO;

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

        public static SaveDataStorageTicoFat ReadFrom(BinaryReader reader, int dataSize)
        {
            var ticoFat = new SaveDataStorageTicoFat();
            for (var i = 0; i < 8; i++)
                for (var j = 0; j < 6; j++)
                    ticoFat.StarPieceNum[i, j] = reader.ReadUInt16Be();
            for (var i = 0; i < 16; i++)
                ticoFat.CoinGalaxyName[i] = reader.ReadUInt16Be();
            return ticoFat;
        }
    }
}
