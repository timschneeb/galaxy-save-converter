using System.Text.Json.Serialization;
using SMGSaveData.Galaxy2.Model.Chunks.Game.Attributes;
using SMGSaveData.Galaxy2.String;
using SMGSaveData.Galaxy2.Utils;

namespace SMGSaveData.Galaxy2.Model.Chunks.Sysconf;

// SMG1
public class SysConfigData1
{
    [JsonPropertyName("time_announced")]
    public DateTime TimeAnnounced { get; set; }
    [JsonPropertyName("time_sent")]
    public DateTime TimeSent { get; set; }
    [JsonPropertyName("sent_bytes")]
    public uint SentBytes { get; set; }

    public static SysConfigData1 ReadFrom(EndianAwareReader reader, int dataSize)
    {
        var sysConfig = new SysConfigData1();
        var dataStartPos = reader.BaseStream.Position;

        var attributes = reader.ReadAttributeTableHeader().AsOffsetDictionary();
        var fieldsDataStartPos = reader.BaseStream.Position;

        if (reader.TryReadU8(fieldsDataStartPos, attributes, "mTimeAnnounced", out var timeAnnounced))
            sysConfig.TimeAnnounced = reader.ConsoleType == ConsoleType.Wii
                ? OsTime.WiiTicksToUnix(timeAnnounced)
                : DateTimeOffset.FromUnixTimeSeconds(timeAnnounced).UtcDateTime;

        if (reader.TryReadI64(fieldsDataStartPos, attributes, "mTimeSent", out var timeSent))
            sysConfig.TimeSent = reader.ConsoleType == ConsoleType.Wii
                ? OsTime.WiiTicksToUnix(timeSent)
                : DateTimeOffset.FromUnixTimeSeconds(timeSent).UtcDateTime;

        if (reader.TryReadU32(fieldsDataStartPos, attributes, "mSentBytes", out var sentBytes))
            sysConfig.SentBytes = sentBytes;

        reader.BaseStream.Position = dataStartPos + dataSize;
        return sysConfig;
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        using var ms = new MemoryStream();
        using var fw = writer.NewWriter(ms);
        var attrs = new List<(ushort key, ushort offset)>();

        AddTime("mTimeAnnounced", TimeAnnounced);
        AddTime("mTimeSent", TimeSent);
        AddU32("mSentBytes", SentBytes);

        fw.Flush();
        var dataSize = (ushort)ms.Length;
        var header = new AttributeTableHeader { Offsets = attrs, DataSize = dataSize };
        writer.WriteAttributeTableHeader(header);
        writer.Write(ms.ToArray());
        return;

        void AddTime(string name, DateTime v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.WriteTime(v);
        }

        void AddU32(string name, uint v)
        {
            var key = HashKey.Compute(name);
            var offset = (ushort)ms.Position;
            attrs.Add((key, offset));
            fw.WriteUInt32(v);
        }
    }
}