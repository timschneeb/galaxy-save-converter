using System.Text;

namespace Galaxy2.SaveData.Binary
{
    public struct HashCode(uint value)
    {
        public uint Value { get; set; } = value;

        public static HashCode FromString(string s)
        {
            uint hash = 0;
            const uint hashKey = 31;

            foreach (sbyte b in Encoding.UTF8.GetBytes(s).Select(b => (sbyte)b))
            {
                hash = (uint)b + (hash * hashKey);
            }

            return new HashCode(hash);
        }

        public override string ToString()
        {
            return $"0x{Value:X}";
        }
    }
}