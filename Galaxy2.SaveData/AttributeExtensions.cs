using Galaxy2.SaveData.Chunks.Game;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData;

public static class AttributeExtensions
{
    extension(List<BaseSaveDataAttribute> attrs)
    {
        public SaveDataAttribute<T>? FindByName<T>(string name) where T : struct
        {
            var key = HashKey.Compute(name);
            return attrs.FirstOrDefault(a => a.Key == key) as SaveDataAttribute<T>;
        }
    }
}