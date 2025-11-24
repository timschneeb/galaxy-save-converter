namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageEventFlag
    {
        public List<GameEventFlag> EventFlags { get; set; } = [];
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

        public ushort Key => (ushort)(_inner & KeyMask);
        public bool Value => (_inner & ValueMask) != 0;
    }
}
