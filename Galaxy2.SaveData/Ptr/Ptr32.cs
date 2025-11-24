namespace Galaxy2.SaveData.Ptr
{
    public class Ptr32<T>(T? value)
    {
        public T? Value { get; set; } = value;
    }
}
