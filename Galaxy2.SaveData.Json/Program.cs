using System.Text.Json;
using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game;

namespace Galaxy2.SaveData.Json
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Galaxy2.SaveData.Json <input.bin> <output.json>");
                return;
            }

            var inputFile = args[0];
            var outputFile = args[1];

            var saveData = BinaryData.ReadLeFile(inputFile);

            // Convert to a Rust-like JSON shape.
            var root = new Dictionary<string, object?>();

            var userFileInfoList = new List<Dictionary<string, object?>>();

            foreach (var ufi in saveData.UserFileInfo)
            {
                var ufiDict = new Dictionary<string, object?>();
                var name = ufi.Name != null ? ufi.Name.ToString() : string.Empty;
                ufiDict["name"] = name;

                var userFileDict = new Dictionary<string, object?>();

                if (ufi.UserFile != null && ufi.UserFile.Value != null)
                {
                    var data = ufi.UserFile.Value.Data;
                    // GameData case
                    if (data is List<GameDataChunk> gdChunks)
                    {
                        var gameDataArr = new List<object?>();
                        foreach (var chunk in gdChunks)
                        {
                            if (chunk is PlayerStatusChunk p)
                            {
                                var d = p.Data;
                                var obj = new Dictionary<string, object?>();
                                var inner = new Dictionary<string, object?>();
                                inner["player_left"] = d.PlayerLeft;
                                inner["stocked_star_piece_num"] = d.StockedStarPieceNum;
                                inner["stocked_coin_num"] = d.StockedCoinNum;
                                inner["last_1up_coin_num"] = d.Last1upCoinNum;
                                var flagDict = new Dictionary<string, object?>();
                                flagDict["player_luigi"] = d.Flag.PlayerLuigi;
                                inner["flag"] = flagDict;
                                obj["PlayerStatus"] = inner;
                                gameDataArr.Add(obj);
                            }
                            else if (chunk is EventFlagChunk ef)
                            {
                                var d = ef.Data;
                                var obj = new Dictionary<string, object?>();
                                var inner = new Dictionary<string, object?>();
                                var arr = new List<object?>();
                                foreach (var f in d.EventFlags)
                                {
                                    arr.Add(new Dictionary<string, object?> { { "key", f.Key }, { "value", f.Value } });
                                }
                                inner["event_flag"] = arr;
                                obj["EventFlag"] = inner;
                                gameDataArr.Add(obj);
                            }
                            else if (chunk is TicoFatChunk tf)
                            {
                                var d = tf.Data;
                                var obj = new Dictionary<string, object?>();
                                var inner = new Dictionary<string, object?>();
                                var sp = new List<List<ushort>>();
                                for (int i = 0; i < 8; i++)
                                {
                                    var row = new List<ushort>();
                                    for (int j = 0; j < 6; j++)
                                    {
                                        row.Add(d.StarPieceNum[i, j]);
                                    }
                                    sp.Add(row);
                                }
                                inner["star_piece_num"] = sp;
                                var coins = new List<ushort>();
                                for (int i = 0; i < 16; i++) coins.Add(d.CoinGalaxyName[i]);
                                inner["coin_galaxy_name"] = coins;
                                obj["TicoFat"] = inner;
                                gameDataArr.Add(obj);
                            }
                            else if (chunk is EventValueChunk ev)
                            {
                                var d = ev.Data;
                                var obj = new Dictionary<string, object?>();
                                var inner = new Dictionary<string, object?>();
                                var arr = new List<object?>();
                                foreach (var v in d.EventValues)
                                {
                                    arr.Add(new Dictionary<string, object?> { { "key", v.Key }, { "value", v.Value } });
                                }
                                inner["event_value"] = arr;
                                obj["EventValue"] = inner;
                                gameDataArr.Add(obj);
                            }
                            else if (chunk is GalaxyChunk g)
                            {
                                var d = g.Data;
                                var obj = new Dictionary<string, object?>();
                                var inner = new Dictionary<string, object?>();
                                var stages = new List<object?>();
                                inner["galaxy"] = stages; // rust uses galaxy field for list
                                foreach (var s in d.Galaxy)
                                {
                                    var stageObj = new Dictionary<string, object?>();
                                    stageObj["galaxy_name"] = s.GalaxyName;
                                    // Emit data_size using FixedHeaderSize if available
                                    stageObj["data_size"] = s.FixedHeaderSize;
                                    // Use actual scenario count to match Rust's scenario_num
                                    stageObj["scenario_num"] = s.Scenario?.Count ?? 0;
                                    // Emit galaxy_state as string (Rust uses names like "Closed")
                                    stageObj["galaxy_state"] = s.GalaxyState.ToString();
                                    // Expand galaxy flags
                                    stageObj["flag"] = new Dictionary<string, object?> {
                                        { "tico_coin", s.Flag.TicoCoin },
                                        { "comet", s.Flag.Comet }
                                    };
                                    var scenarios = new List<object?>();
                                    foreach (var sc in s.Scenario)
                                    {
                                        scenarios.Add(new Dictionary<string, object?> {
                                            { "miss_num", sc.MissNum },
                                            { "best_time", sc.BestTime },
                                            { "flag", new Dictionary<string, object?> {
                                                { "power_star", sc.Flag.PowerStar },
                                                { "bronze_star", sc.Flag.BronzeStar },
                                                { "already_visited", sc.Flag.AlreadyVisited },
                                                { "ghost_luigi", sc.Flag.GhostLuigi },
                                                { "intrusively_luigi", sc.Flag.IntrusivelyLuigi }
                                            } }
                                        });
                                    }
                                    stageObj["scenario"] = scenarios;
                                    stages.Add(stageObj);
                                }
                                obj["Galaxy"] = inner;
                                gameDataArr.Add(obj);
                            }
                            else if (chunk is WorldMapChunk wm)
                            {
                                var d = wm.Data;
                                var obj = new Dictionary<string, object?>();
                                var inner = new Dictionary<string, object?>();
                                // Emit star_check_point_flag as integer array to match Rust JSON
                                var flagsArr = new List<int>();
                                foreach (var b in d.StarCheckPointFlag) flagsArr.Add(b);
                                inner["star_check_point_flag"] = flagsArr;
                                inner["world_no"] = d.WorldNo;
                                obj["WorldMap"] = inner;
                                gameDataArr.Add(obj);
                            }
                        }

                        userFileDict["GameData"] = gameDataArr;
                    }
                    else if (data is List<Chunks.Config.ConfigDataChunk> configChunks)
                    {
                        // Emit ConfigData array in Rust-like shape
                        var cfgArr = new List<object?>();
                        foreach (var cc in configChunks)
                        {
                            if (cc is Chunks.Config.CreateChunk cr)
                            {
                                cfgArr.Add(new Dictionary<string, object?> { { "Create", new Dictionary<string, object?> { { "is_created", cr.Data.IsCreated } } } });
                            }
                            else if (cc is Chunks.Config.MiiChunk mi)
                            {
                                var m = new Dictionary<string, object?>();
                                var miInner = new Dictionary<string, object?>();
                                miInner["flag"] = mi.Data.Flag.Value;
                                // mii_id as list of ints
                                var idList = new List<int>();
                                foreach (var b in mi.Data.MiiId.Id) idList.Add(b);
                                miInner["mii_id"] = idList;
                                // icon_id as Rust's enum name
                                miInner["icon_id"] = mi.Data.IconId.ToString();
                                m["Mii"] = miInner;
                                cfgArr.Add(m);
                            }
                            else if (cc is Chunks.Config.MiscChunk ms)
                            {
                                cfgArr.Add(new Dictionary<string, object?> { { "Misc", new Dictionary<string, object?> { { "last_modified", ms.Data.LastModified.Value } } } });
                            }
                        }
                        userFileDict["ConfigData"] = cfgArr;
                    }
                    else if (data is List<Chunks.Sysconf.SysConfigDataChunk> sysChunks)
                    {
                        var sysArr = new List<object?>();
                        foreach (var sc in sysChunks)
                        {
                            if (sc is Chunks.Sysconf.SysConfigChunk sy)
                            {
                                var inner = new Dictionary<string, object?>();
                                inner["is_encourage_pal60"] = sy.Data.IsEncouragePal60;
                                inner["time_sent"] = sy.Data.TimeSent.Value;
                                inner["sent_bytes"] = sy.Data.SentBytes;
                                inner["bank_star_piece_num"] = sy.Data.BankStarPieceNum;
                                inner["bank_star_piece_max"] = sy.Data.BankStarPieceMax;
                                inner["gifted_player_left"] = sy.Data.GiftedPlayerLeft;
                                inner["gifted_file_name_hash"] = sy.Data.GiftedFileNameHash;
                                sysArr.Add(new Dictionary<string, object?> { { "SysConfig", inner } });
                            }
                        }
                        userFileDict["SysConfigData"] = sysArr;
                    }
                    else
                    {
                        // For other user_file types (unknown), keep raw serialization
                        userFileDict["UserFileRaw"] = data;
                    }
                }

                ufiDict["user_file"] = userFileDict;
                userFileInfoList.Add(ufiDict);
            }

            root["user_file_info"] = userFileInfoList;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // Keep default naming since keys are already snake_case where required
            };

            var json = JsonSerializer.Serialize(root, options);
            File.WriteAllText(outputFile, json);
            Console.WriteLine($"Successfully converted {inputFile} to {outputFile}");
        }
    }
}