using Galaxy2.SaveData.String;
using Galaxy2.SaveData.Ptr;

namespace Galaxy2.SaveData.Save
{
    public class SaveDataUserFileInfo
    {
        public FixedString12? Name { get; set; }
        public Ptr32<SaveDataUserFile>? UserFile { get; set; }
    }

    public class SaveDataUserFile
    {
        // This will be a container for one of the three data chunk types.
        // In C#, we can use a property of type object and check its type at runtime.
        public object? Data { get; set; }
    }
}
