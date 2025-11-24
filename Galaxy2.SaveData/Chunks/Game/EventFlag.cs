using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageEventFlag
    {
        [JsonPropertyName("event_flag")]
        public List<GameEventFlag> EventFlags { get; set; } = new List<GameEventFlag>();

        public static SaveDataStorageEventFlag ReadFrom(BinaryReader reader, int dataSize)
        {
            var eventFlag = new SaveDataStorageEventFlag();
            var count = dataSize / 2;
            eventFlag.EventFlags = new List<GameEventFlag>(count);
            for (var i = 0; i < count; i++)
            {
                var raw = reader.ReadUInt16Be();
                eventFlag.EventFlags.Add(new GameEventFlag(raw));
            }
            return eventFlag;
        }
    }

    public struct GameEventFlag
    {
        private const ushort KeyMask = 0x7FFF;
        private const ushort ValueMask = 0x8000;
        private const int ValueShift = 15;

        private ushort _inner;

        public GameEventFlag(Binary.HashCode key, bool value)
        {
            _inner = (ushort)(((ushort)key.Value & KeyMask) | (value ? ValueMask : (ushort)0));
        }

        public GameEventFlag(ushort inner)
        {
            _inner = inner;
        }

        [JsonPropertyName("key")]
        public ushort Key
        {
            get => (ushort)(_inner & KeyMask);
            set => _inner = (ushort)((_inner & ValueMask) | (value & KeyMask));
        }

        [JsonPropertyName("value")]
        public bool Value
        {
            get => (_inner & ValueMask) != 0;
            set
            {
                if (value)
                    _inner = (ushort)(_inner | ValueMask);
                else
                    _inner = (ushort)(_inner & KeyMask);
            }
        }
    }
}
