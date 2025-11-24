using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStoragePlayerStatus
    {
        [JsonPropertyName("player_left")]
        public byte PlayerLeft { get; set; } = 4;
        [JsonPropertyName("stocked_star_piece_num")]
        public ushort StockedStarPieceNum { get; set; }
        [JsonPropertyName("stocked_coin_num")]
        public ushort StockedCoinNum { get; set; }
        [JsonPropertyName("last_1up_coin_num")]
        public ushort Last1upCoinNum { get; set; }
        [JsonPropertyName("flag")]
        public SaveDataStoragePlayerStatusFlag Flag { get; set; }

        public static SaveDataStoragePlayerStatus ReadFrom(BinaryReader reader, int dataSize)
        {
            var status = new SaveDataStoragePlayerStatus();
            var dataStartPos = reader.BaseStream.Position;

            var (attributes, headerDataSize) = reader.ReadAttributesAsDictionary();
            var fieldsDataStartPos = reader.BaseStream.Position;

            if (reader.TryReadU8(fieldsDataStartPos, attributes, "mPlayerLeft", out var playerLeft))
                status.PlayerLeft = playerLeft;
            if (reader.TryReadU16(fieldsDataStartPos, attributes, "mStockedStarPieceNum", out var sspn))
                status.StockedStarPieceNum = sspn;
            if (reader.TryReadU16(fieldsDataStartPos, attributes, "mStockedCoinNum", out var scn))
                status.StockedCoinNum = scn;
            if (reader.TryReadU16(fieldsDataStartPos, attributes, "mLast1upCoinNum", out var l1up))
                status.Last1upCoinNum = l1up;
            if (reader.TryReadU8(fieldsDataStartPos, attributes, "mFlag", out var flag))
                status.Flag = new SaveDataStoragePlayerStatusFlag(flag);

            reader.BaseStream.Position = dataStartPos + dataSize;
            return status;
        }
    }

    public struct SaveDataStoragePlayerStatusFlag(byte value)
    {
        private byte _value = value;

        [JsonPropertyName("player_luigi")]
        public bool PlayerLuigi
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }
    }
}