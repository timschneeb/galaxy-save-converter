using SMGSaveData.Galaxy2.Model.Chunks.Game.Attributes;
using SMGSaveData.Galaxy2.String;

namespace SMGSaveData.Galaxy2.Utils;

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