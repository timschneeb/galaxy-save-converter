namespace Galaxy2.SaveData.Model.Chunks.Game.Attributes;

public class AttributeTableHeader
{
    public ushort DataSize { get; set; }
    public List<(ushort key, ushort offset)> Offsets { get; set; } = [];
    
    public Dictionary<ushort, ushort> AsOffsetDictionary()
    {
        var dict = new Dictionary<ushort,ushort>(Offsets.Count);
        foreach (var a in Offsets) dict[a.key] = a.offset;
        return dict;
    }
}