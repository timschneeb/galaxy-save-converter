namespace Galaxy2.SaveData.Face
{
    public struct RFLCreateID()
    {
        private const int Size = 8;
        public byte[] Id { get; set; } = new byte[Size];
    }
}
