using System.Text.Json.Serialization;
using Galaxy2.SaveData.Model.Chunks.Game.Attributes;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Model.Chunks.Game;

public class SaveDataStoragePlayerStatus
{
    [JsonPropertyName("attributes")]
    public List<AbstractDataAttribute> Attributes { get; set; } = [];

    // Convenience accessors (not serialized separately)
    [JsonIgnore]
    public byte PlayerLeft
    {
        get => Attributes.FindByName<byte>("mPlayerLeft")?.Value ?? 4;
        set => Attributes.FindByName<byte>("mPlayerLeft")!.Value = value;
    }

    [JsonIgnore]
    public ushort StockedStarPieceNum
    {
        get => Attributes.FindByName<ushort>("mStockedStarPieceNum")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mStockedStarPieceNum")!.Value = value;
    }

    [JsonIgnore]
    public ushort StockedCoinNum
    {
        get => Attributes.FindByName<ushort>("mStockedCoinNum")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mStockedCoinNum")!.Value = value;
    }

    [JsonIgnore]
    public ushort Last1UpCoinNum
    {
        get => Attributes.FindByName<ushort>("mLast1upCoinNum")?.Value ?? 0;
        set => Attributes.FindByName<ushort>("mLast1upCoinNum")!.Value = value;
    }

    [JsonIgnore]
    public SaveDataStoragePlayerStatusFlag Flag
    {
        get => new(Attributes.FindByName<byte>("mFlag")?.Value ?? 0);
        set => Attributes.FindByName<byte>("mFlag")!.Value = value.Value;
    }
    
    public struct SaveDataStoragePlayerStatusFlag(byte value)
    {
        [JsonIgnore]
        public byte Value { get; private set; } = value;
        
        [JsonPropertyName("player_luigi")]
        public bool PlayerLuigi
        {
            get => (Value & 0b1) != 0;
            set => Value = (byte)(value ? (Value | 0b1) : (Value & ~0b1));
        }
    }

    public static SaveDataStoragePlayerStatus ReadFrom(BinaryReader reader, int dataSize)
    {
        var status = new SaveDataStoragePlayerStatus();
        var dataStartPos = reader.BaseStream.Position;

        var table = reader.ReadAttributeTableHeader();
        status.Attributes = reader.ReadAttributes(table);
        
        // advance stream to end of this data block
        reader.BaseStream.Position = dataStartPos + dataSize;
        return status;
    }

    public void WriteTo(EndianAwareWriter writer, out uint hash)
    {
        using var ms = new MemoryStream();
        using var fw = writer.NewWriter(ms);

        var attrs = new List<(ushort key, ushort offset)>();

        // Ensure all allowed attributes are present for the platform and at the correct locations
        var allowedAttrs = writer.ConsoleType == ConsoleType.Switch
            ? AllowedSwitchAttributes
            : AllowedWiiAttributes;
        
        var validatedAttrs = new List<AbstractDataAttribute>();
        foreach (var reqAttr in allowedAttrs)
        {
            // Insert existing attribute if present, otherwise the default one
            var existingAttr = Attributes.Find(a => a.Key == reqAttr.Key);
            validatedAttrs.Add(existingAttr ?? reqAttr);
        }
        
        // Write attributes sequentially into ms and record offsets
        foreach (var attr in validatedAttrs)
        {
            attrs.Add((attr.Key, (ushort)ms.Position));
            attr.WriteTo(fw);
        }
        fw.Flush();
        
        var dataSize = (ushort)ms.Length;
        var header = new AttributeTableHeader { Offsets = attrs, DataSize = dataSize };
        var headerSize = writer.WriteAttributeTableHeader(header);
        writer.Write(ms.ToArray());
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
        
        hash = dataSize + headerSize;
    }
    
    /*
        --- Switch Attributes (w/ size)
            4ED5: 1
            E352: 2
            450D: 2
            23EC: 2
            7579: 1
            AA83: 1
            7213: 1
            EA99: 1
            BF77: 2
            BFD3: 2
            1E85: 2
            EFDB: 2
            E6D1: 2
            3D5F: 4
            0AC6: 4
            71E3: 4
            E983: 1
       
        --- Wii Attributes
            4ED5: 1
            E352: 2
            450D: 2
            23EC: 2
            7579: 1
    */
    
    public static List<AbstractDataAttribute> AllowedSwitchAttributes { get; } =
    [
        new DataAttribute<byte>(0x4ED5, 0), // mPlayerLeft
        new DataAttribute<ushort>(0xE352, 0), // mStockedStarPieceNum
        new DataAttribute<ushort>(0x450D, 0), // mStockedCoinNum
        new DataAttribute<ushort>(0x23EC, 0), // mLast1upCoinNum
        new DataAttribute<byte>(0x7579, 0), // mFlag
        new DataAttribute<byte>(0xAA83, 0), // mAmiiboScanNum
        new DataAttribute<byte>(0x7213, 0), // mBankToadToolIndex
        new DataAttribute<byte>(0xEA99, 0), // mIsPictureBookOpened
        new DataAttribute<ushort>(0xBF77, 0), // mDemoSkipNum
        new DataAttribute<ushort>(0xBFD3, 0), // mMusicPlaySeconds
        new DataAttribute<ushort>(0x1E85, 0), // mPlayNum
        new DataAttribute<ushort>(0xEFDB, 0), // m2pNum
        new DataAttribute<ushort>(0xE6D1, 0), // mLuigiNum
        new DataAttribute<uint>(0x3D5F, 0), // mGameFinishTime
        new DataAttribute<uint>(0x0AC6, 0), // mBossesFinishedFlag
        new DataAttribute<uint>(0x71E3, 0), // mNpcConversationFlag
        new DataAttribute<byte>(0xE983, 0) // mIsAssistMode
    ];
    
    public static List<AbstractDataAttribute> AllowedWiiAttributes { get; } =
    [
        new DataAttribute<byte>(0x4ED5, 0), // mPlayerLeft
        new DataAttribute<ushort>(0xE352, 0), // mStockedStarPieceNum
        new DataAttribute<ushort>(0x450D, 0), // mStockedCoinNum
        new DataAttribute<ushort>(0x23EC, 0), // mLast1upCoinNum
        new DataAttribute<byte>(0x7579, 0), // mFlag
    ];
}