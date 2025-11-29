using System.Text.Json.Serialization;
using Galaxy2.SaveData.String;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStorageEventFlag
{
    [JsonPropertyName("event_flag")]
    public List<GameEventFlag> EventFlags { get; set; } = new List<GameEventFlag>();

    public static SaveDataStorageEventFlag ReadFrom(EndianAwareReader reader, int dataSize)
    {
        var eventFlag = new SaveDataStorageEventFlag();
        var count = dataSize / 2;
        eventFlag.EventFlags = new List<GameEventFlag>(count);
        for (var i = 0; i < count; i++)
        {
            var raw = reader.ReadUInt16();
            if (reader.ConsoleType == ConsoleType.Wii)
            {
                raw = GameEventFlag.WiiToSwitchFlag(raw);
            }
            
            var flag = new GameEventFlag(raw);
            
            // Avoid misinterpreting padding as a valid flag on Switch.
            if (reader.ConsoleType == ConsoleType.Switch && flag.Key == 0)
                continue;

            eventFlag.EventFlags.Add(flag);
        }
        
        return eventFlag;
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        foreach (var f in EventFlags)
        {
            var raw = f.InnerValue;
            if (writer.ConsoleType == ConsoleType.Wii)
            {
                raw = GameEventFlag.SwitchToWiiFlag(raw);
            }
            
            writer.WriteUInt16(raw);
        }

        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
    }
}

/// <summary>
/// Event flag consisting of a key hash (bit 0-14) and a boolean value (bit 15).
/// </summary>
/// <remarks>
/// This structures uses the Nintendo Switch event flag key hashing method.
/// </remarks>
public struct GameEventFlag
{
    private const ushort KeyMask = 0x7FFF;
    private const ushort ValueMask = 0x8000; // bit 15

    private ushort _inner;

    public GameEventFlag(HashKey key, bool value)
    {
        // Place key in lower 15 bits and value in bit 15.
        _inner = (ushort)((key.ShortValue & KeyMask) | (value ? ValueMask : (ushort)0));
    }

    public GameEventFlag(ushort inner)
    {
        _inner = inner;
    }

    [JsonIgnore]
    public ushort InnerValue => _inner;

    /// <summary>
    /// Nintendo Switch event flag key as a hash of a UTF-8 string
    /// The Wii event flag key must be converted using the <c>WiiToSwitchKey</c> method first.
    /// </summary>
    [JsonPropertyName("key")]
    public ushort Key
    {
        get => (ushort)(_inner & KeyMask);
        set => _inner = (ushort)((_inner & ValueMask) | (value & KeyMask));
    }

    [JsonPropertyName("value")]
    public bool Value
    {
        get => (_inner & ValueMask) != 0;
        set => _inner = (ushort)((_inner & KeyMask) | (value ? ValueMask : (ushort)0));
    }
    
    [JsonPropertyName("#comment")]
    public string? Comment
    {
        get
        {
            var key = Key; // copy required
            return WiiFlagHashMap.Values.FirstOrDefault(x => (HashKey.Compute(x) & KeyMask) == (key & KeyMask));
        }
    }
    
    public static ushort WiiToSwitchFlag(ushort wiiKey) =>
        (ushort)((HashKey.Compute(
            WiiFlagHashMap
                .First(x => (x.Key & KeyMask) == (wiiKey & KeyMask))
                .Value
        ) & KeyMask) | (ushort)(wiiKey & ValueMask));
    
    public static ushort SwitchToWiiFlag(ushort switchKey) =>
        (ushort)((WiiFlagHashMap
            .First(x => (HashKey.Compute(x.Value) & KeyMask) == (switchKey & KeyMask))
            .Key & KeyMask) | (ushort)(switchKey & ValueMask));

    /// <summary>
    /// Maps Wii event flag key hashs to their string representations.
    /// Only the Switch event flags can be calculated from strings at the moment,
    /// for now use a lookup table for Wii flag keys.
    /// </summary>
    public static Dictionary<ushort, string> WiiFlagHashMap => new()
    {
        {0x7ABA,"ハチマリオ初変身"},
        {0x0C8E,"テレサマリオ初変身"},
        {0x184F,"ホッパーマリオ初変身"},
        {0x0E5E,"ファイアマリオ初変身"},
        {0x62E5,"アイスマリオ初変身"},
        {0x4C23,"無敵マリオ初変身"},
        {0x78A4,"ゴロ岩マリオ初変身"},
        {0x5BE8,"雲マリオ初変身"},
        {0x751F,"ドリル初ゲット"},
        {0x1880,"ライフアップキノコ解説"},
        {0x3D66,"１ＵＰキノコ解説"},
        {0x3637,"ヨッシー出会い"},
        {0x3F29,"コメットメダル解説"},
        {0x0B9B,"２Ｐサポート解説"},
        {0x4E78,"タマコロチュートリアル"},
        {0x2DC7,"グライバードチュートリアル"},
        {0x6AF4,"ワールド1初プレイ"},
        {0x59F5,"ワールド2初プレイ"},
        {0x48F6,"ワールド3初プレイ"},
        {0x37F7,"ワールド4初プレイ"},
        {0x26F8,"ワールド5初プレイ"},
        {0x15F9,"ワールド6初プレイ"},
        {0x04FA,"ワールド7初プレイ"},
        {0x7717,"ワールド2他のワールドへ誘導"},
        {0x58D8,"ワールド3他のワールドへ誘導"},
        {0x3A99,"ワールド4他のワールドへ誘導"},
        {0x1C5A,"ワールド5他のワールドへ誘導"},
        {0x7E1B,"ワールド6他のワールドへ誘導"},
        {0x4B98,"ワールド2ゲームオーバーによる誘導"},
        {0x1899,"ワールド3ゲームオーバーによる誘導"},
        {0x659A,"ワールド4ゲームオーバーによる誘導"},
        {0x329B,"ワールド5ゲームオーバーによる誘導"},
        {0x7F9C,"ワールド6ゲームオーバーによる誘導"},
        {0x4931,"グランドギャラクシーマップ初プレイ"},
        {0x1738,"グリーンスター出現デモ"},
        {0x6F30,"クッパ最終戦直前デモ"},
        {0x6A95,"IsOpenScenarioGoroRockGalaxy3"},
        {0x7E34,"IsOpenScenarioJungleGliderGalaxy2"},
        {0x13F5,"IsOpenScenarioThunderFleetGalaxy3"},
        {0x08D6,"IsOpenScenarioChallengeGliderGalaxy2"},
        {0x0381,"IsOpenScenarioHoneyBeeVillageGalaxy2"},
        {0x634F,"IsOpenScenarioUnderGroundDangeonGalaxy2"},
        {0x6E87,"IsOpenScenarioMokumokuValleyGalaxy2"},
        {0x280E,"オープニング実行"},
        {0x4827,"コメット解説"},
        {0x370D,"銀行屋キノピオ初回"},
        {0x77CC,"でしゃばりルイージ出現開始"},
        {0x2AD0,"ノーマルエンディング実行"},
        {0x7011,"ノーマルエンディング後デモ"},
        {0x5405,"スター120個エンディング実行"},
        {0x2006,"スター120個エンディング後デモ"},
        {0x00E9,"最終ギャラクシー出現"},
        {0x687B,"ノーマルエンディングメール送信"},
        {0x397A,"コンプリートメール送信"},
        {0x6AA2,"スターピースカウンターストップ"},
        {0x70B8,"コインカウンターストップ"},
        {0x6F34,"ルイージプレイ済"},
        {0x6060,"でしゃばりルイージ出現中"},
        {0x41CA,"ゲームオーバーで終了"}
    };
}