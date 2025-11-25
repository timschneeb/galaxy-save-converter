using System.Text.Json.Serialization;
using Galaxy2.SaveData.String;

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

        public void WriteTo(BinaryWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            // We'll build the fields area into a memory stream to compute offsets
            using var ms = new MemoryStream();
            using var fw = new BinaryWriter(ms);

            var attrs = new List<(ushort key, ushort offset)>();

            AddU8("mPlayerLeft", PlayerLeft);
            AddU16("mStockedStarPieceNum", StockedStarPieceNum);
            AddU16("mStockedCoinNum", StockedCoinNum);
            AddU16("mLast1upCoinNum", Last1upCoinNum);
            AddU8("mFlag", Flag.Value);

            fw.Flush();

            var dataSize = (ushort)ms.Length;

            // write header and fields to the target writer
            writer.WriteBinaryDataContentHeader(attrs, dataSize);
            writer.Write(ms.ToArray());
            return;

            // helper to add a byte field
            void AddU8(string name, byte v)
            {
                var key = HashKey.Compute(name);
                var offset = (ushort)ms.Position;
                attrs.Add((key, offset));
                fw.Write(v);
            }

            void AddU16(string name, ushort v)
            {
                var key = HashKey.Compute(name);
                var offset = (ushort)ms.Position;
                attrs.Add((key, offset));
                fw.WriteUInt16Be(v);
            }
        }
    }

    public struct SaveDataStoragePlayerStatusFlag(byte value)
    {
        [JsonIgnore]
        public byte Value { get; private set; } = value;

        [JsonPropertyName("player_luigi")]
        public bool PlayerLuigi
        {
            get => (Value & 0b1) != 0;
            set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
        }
    }
}