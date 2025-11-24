namespace Galaxy2.SaveData.Binary
{
    public interface IHeaderSerializer
    {
        public static abstract BinaryDataContentHeaderSerializer<T> CreateHeaderSerializer<T>() where T : IHeaderSerializer;
        public static abstract int HeaderSize { get; }
        public static abstract int DataSize { get; }
    }

    public struct BinaryDataContentAttribute
    {
        public ushort Key { get; set; }
        public ushort Offset { get; set; }
    }

    public class BinaryDataContentHeaderSerializer<T> where T : IHeaderSerializer
    {
        public required List<BinaryDataContentAttribute> Attributes { get; set; }
    }
}
