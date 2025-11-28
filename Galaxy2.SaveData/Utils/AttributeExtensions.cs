using Galaxy2.SaveData.Chunks.Game.Attributes;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Utils;

public static class AttributeExtensions
{
    extension(List<AbstractDataAttribute> attrs)
    {
        public DataAttribute<T>? FindByName<T>(string name) where T : struct
        {
            var key = HashKey.Compute(name);
            return attrs.FirstOrDefault(a => a.Key == key) as DataAttribute<T>;
        }
    }
}