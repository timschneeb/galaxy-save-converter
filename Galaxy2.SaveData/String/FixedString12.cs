using System.Text;
using System.Text.Json.Serialization;

namespace Galaxy2.SaveData.String
{
    [JsonConverter(typeof(FixedString12JsonConverter))]
    public readonly struct FixedString12
    {
        private const int Size = 12;
        private readonly byte[] _buffer;

        public FixedString12(string s)
        {
            _buffer = new byte[Size];
            var bytes = Encoding.UTF8.GetBytes(s);
            Array.Copy(bytes, _buffer, Math.Min(bytes.Length, Size - 1));
        }

        public FixedString12(BinaryReader reader)
        {
            _buffer = reader.ReadBytes(Size);
        }

        public override string ToString()
        {
            int length = Array.IndexOf(_buffer, (byte)0);
            if (length == -1)
            {
                length = Size;
            }
            return Encoding.UTF8.GetString(_buffer, 0, length);
        }
    }
}