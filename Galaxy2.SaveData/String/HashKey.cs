using System.Text;

namespace Galaxy2.SaveData.String
{
    public struct HashKey(uint value)
    {
        public uint Value { get; set; } = value;
        public ushort ShortValue => (ushort)(Value & 0xFFFF);

        public override string ToString()
        {
            return $"0x{Value:X}";
        }

        public static HashKey FromString(string s)
        {
            uint hash = 0;
            const uint hashKey = 31;

            foreach (var b in Encoding.UTF8.GetBytes(s).Select(b => (sbyte)b))
            {
                hash = (uint)b + (hash * hashKey);
            }

            return new HashKey(hash);
        }
        
        public static ushort Compute(string name) => FromString(name).ShortValue;
    }
}