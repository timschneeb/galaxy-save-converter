using System.Text.Json.Serialization;
using Galaxy2.SaveData.String;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Chunks.Game;

public class SaveDataStorageEventValue
{
    [JsonPropertyName("event_value")]
    public List<GameEventValue> EventValues { get; set; } = [];

    public static SaveDataStorageEventValue ReadFrom(EndianAwareReader reader, int dataSize)
    {
        var ev = new SaveDataStorageEventValue();
        var count = dataSize / 4;
        ev.EventValues = new List<GameEventValue>(count);
        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadUInt16();
            if (reader.ConsoleType == ConsoleType.Wii)
            {
                key = GameEventValue.WiiToSwitchKey(key);
            }
            
            ev.EventValues.Add(new GameEventValue { Key = key, Value = reader.ReadUInt16() });
        }

        return ev;
    }

    public void WriteTo(EndianAwareWriter writer)
    {
        foreach (var v in EventValues)
        {
            writer.WriteUInt16(writer.ConsoleType == ConsoleType.Wii ? GameEventValue.SwitchToWiiKey(v.Key) : v.Key);
            writer.WriteUInt16(v.Value);
        }
                
        if (writer.ConsoleType == ConsoleType.Switch)
        {
            writer.WriteAlignmentPadding(alignment: 4);
        }
    }
}


/// <remarks>
/// This structure uses the Nintendo Switch event value key hashing method.
/// </remarks>
public struct GameEventValue
{
    [JsonPropertyName("key")]
    public ushort Key { get; set; }
    [JsonPropertyName("value")]
    public ushort Value { get; set; }

    [JsonPropertyName("#comment")]
    public string? Comment
    {
        get
        {
            var key = Key; // copy required
            return WiiFlagHashMap.Values.FirstOrDefault(x => HashKey.Compute(x) == key);
        }
    }
    
    public static ushort WiiToSwitchKey(ushort wiiKey) =>
        HashKey.Compute(
            WiiFlagHashMap
                .First(x => x.Key == wiiKey)
                .Value
        );
    
    public static ushort SwitchToWiiKey(ushort switchKey) =>
        WiiFlagHashMap
            .First(x => HashKey.Compute(x.Value) == switchKey)
            .Key;

    /// <summary>
    /// Maps Wii event value key hashes to their string representations.
    /// Only the Switch event flags can be calculated from strings at the moment,
    /// for now use a lookup table for Wii flag values.
    /// </summary>
    public static Dictionary<ushort, string> WiiFlagHashMap => new()
    {
        {0xEF3E,"グライダー[ジャングル]/hi"},
        {0xEFC0,"グライダー[ジャングル]/lo"},
        {0x57D0,"グライダー[チャレンジ]/hi"},
        {0x5852,"グライダー[チャレンジ]/lo"},
        {0x3627,"ベストスコア[MokumokuValleyGalaxy]/lo"},
        {0x35A5,"ベストスコア[MokumokuValleyGalaxy]/hi"},
        {0xD37F,"ベストスコア[HoneyBeeVillageGalaxy]/lo"},
        {0xD2FD,"ベストスコア[HoneyBeeVillageGalaxy]/hi"},
        {0xC0DF,"ベストスコア[UnderGroundDangeonGalaxy]/lo"},
        {0xC05D,"ベストスコア[UnderGroundDangeonGalaxy]/hi"},
        {0xF825,"ベストスコア[TwisterTowerGalaxy]/lo"},
        {0xF7A3,"ベストスコア[TwisterTowerGalaxy]/hi"},
        {0x1FC2,"ベストスコア[KachikochiLavaGalaxy]/lo"},
        {0x1F40,"ベストスコア[KachikochiLavaGalaxy]/hi"},
        {0x3C66,"ベストスコア[WhiteSnowGalaxy]/lo"},
        {0x3BE4,"ベストスコア[WhiteSnowGalaxy]/hi"},
        {0xEDD7,"郵便屋[タスク手紙既読フラグ]/0"},
        {0xEDD8,"郵便屋[タスク手紙既読フラグ]/1"},
        {0x259E,"郵便屋[重要手紙既読フラグ]/0"},
        {0x259F,"郵便屋[重要手紙既読フラグ]/1"},
        {0x62D5,"郵便屋[最後に読んだタスク手紙インデックス]"},
        {0xFAA9,"郵便屋[ピーチ手紙を読んだ時の累積死亡回数]"},
        {0x8A49,"メッセージ既読フラグ/0"},
        {0x8A4A,"メッセージ既読フラグ/1"},
        {0xF1A0,"累積死亡回数"},
        {0x189C,"累積ゲームオーバー回数"},
        {0x2DF7,"累積プレイ時間/lo"},
        {0x2D75,"累積プレイ時間/hi"},
        {0x6AE0,"でしゃばりルイージ出現カウンタ"},
        {0x7B89,"銀行屋キノピオ[利子]"},
        {0xCEED,"顔惑星イベント番号/0"},
        {0xCEEE,"顔惑星イベント番号/1"},
        {0xCEEF,"顔惑星イベント番号/2"},
        {0xCEF0,"顔惑星イベント番号/3"},
        {0x567D,"顔惑星イベントグランドスター番号"},
        {0xA26D,"一定数死亡後のステージクリア回数"}
    };
}