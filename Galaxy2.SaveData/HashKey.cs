namespace Galaxy2.SaveData
{
    internal static class HashKey
    {
        public static ushort Compute(string name) => (ushort)(Binary.HashCode.FromString(name).Value & 0xFFFF);
    }
}

