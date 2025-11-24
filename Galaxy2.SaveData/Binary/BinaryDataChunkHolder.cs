namespace Galaxy2.SaveData.Binary
{
    public interface IChunkHolder
    {
        // Removed static abstract properties.
        // Implementations will provide instance properties for BufferSize and Version.
    }

    public class BinaryDataChunkHolder<T> //Removed constraint "where T : IChunkHolder"
    {
        public required List<T> Chunks { get; set; }

        // We will need to set BufferSize and Version from the concrete type T
        // or pass them in the constructor of BinaryDataChunkHolder if needed for serialization.
    }
}
