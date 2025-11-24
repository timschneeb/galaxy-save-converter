using System.Text.Json.Serialization;

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