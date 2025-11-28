namespace Galaxy2.SaveData;

public class AttributeTableHeader
{
    public ushort AttributeNum { get; set; }
    public int DataSize { get; set; }
    public List<(ushort key, int offset)> Offsets { get; set; } = [];
    
    public Dictionary<ushort, int> AsOffsetDictionary()
    {
        var dict = new Dictionary<ushort,int>(AttributeNum);
        foreach (var a in Offsets) dict[a.key] = a.offset;
        return dict;
    }
}