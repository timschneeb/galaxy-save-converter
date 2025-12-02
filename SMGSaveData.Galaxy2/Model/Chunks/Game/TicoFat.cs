using System.Text.Json.Serialization;
using SMGSaveData.Galaxy2.Utils;

namespace SMGSaveData.Galaxy2.Model.Chunks.Game;

public class SaveDataStorageTicoFat
{
    private const int PartsNum = 6;
    private const int CoinGalaxyNameNum = 16;
    private const int WorldCapacity = 8;

    [JsonPropertyName("star_piece_num")]
    public ushort[,] StarPieceNum { get; set; } = new ushort[WorldCapacity, PartsNum];
    [JsonPropertyName("coin_galaxy_name")]
    public ushort[] CoinGalaxyName { get; set; } = new ushort[CoinGalaxyNameNum];

    public static SaveDataStorageTicoFat ReadFrom(BinaryReader reader)
    {
        var ticoFat = new SaveDataStorageTicoFat();
        for (var i = 0; i < 8; i++)
        for (var j = 0; j < 6; j++)
            ticoFat.StarPieceNum[i, j] = reader.ReadUInt16();
        for (var i = 0; i < 16; i++)
            ticoFat.CoinGalaxyName[i] = reader.ReadUInt16();
        return ticoFat;
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        for (var i = 0; i < 8; i++)
        for (var j = 0; j < 6; j++)
            writer.WriteUInt16(StarPieceNum[i,j]);
        for (var i = 0; i < 16; i++)
            writer.WriteUInt16(CoinGalaxyName[i]);
        
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
    }
}