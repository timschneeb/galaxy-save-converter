using System.Text.Json.Serialization;
using Galaxy2.SaveData.Chunks.Game;
using Galaxy2.SaveData.Chunks.Config;
using Galaxy2.SaveData.Chunks.Sysconf;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Save
{
    public class SaveDataUserFileInfo
    {
        [JsonPropertyName("name")]
        public FixedString12? Name { get; set; }
        [JsonPropertyName("user_file")]
        public SaveDataUserFile? UserFile { get; set; }
    }

    public class SaveDataUserFile
    {
        [JsonPropertyName("GameData")]
        public List<GameDataChunk>? GameData { get; set; }

        [JsonPropertyName("ConfigData")]
        public List<ConfigDataChunk>? ConfigData { get; set; }

        [JsonPropertyName("SysConfigData")]
        public List<SysConfigData>? SysConfigData { get; set; }

        public static SaveDataUserFile ReadFrom(BinaryReader reader, string name)
        {
            var version = reader.ReadByte();
            if (version != 2)
                throw new InvalidDataException($"Unsupported SaveDataUserFile version: {version}");

            var chunkNum = reader.ReadByte();
            reader.ReadBytes(2); // reserved

            var userFile = new SaveDataUserFile();
            var startOfUserFile = reader.BaseStream.Position - 4;

            if (name.StartsWith("user"))
            {
                var chunks = new List<GameDataChunk>();
                for (var i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadGameChunk(reader);
                    if (chunk != null) chunks.Add(chunk);
                }
                userFile.GameData = chunks;
                reader.BaseStream.Position = startOfUserFile + 0xF80;
            }
            else if (name.StartsWith("config"))
            {
                var chunks = new List<ConfigDataChunk>();
                for (var i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadConfigChunk(reader);
                    if (chunk != null) chunks.Add(chunk);
                }
                userFile.ConfigData = chunks;
                reader.BaseStream.Position = startOfUserFile + 0x60;
            }
            else if (name == "sysconf")
            {
                var chunks = new List<SysConfigData>();
                for (var i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadSysConfigChunk(reader);
                    if (chunk != null) chunks.Add(chunk);
                }
                userFile.SysConfigData = chunks;
                reader.BaseStream.Position = startOfUserFile + 0x80;
            }

            return userFile;

            static GameDataChunk? ReadGameChunk(BinaryReader r)
            {
                var (magic, hash, size, inner, start) = r.ReadChunkHeader();
                GameDataChunk? result = null;
                switch (magic)
                {
                    case 0x504C4159: // PLAY
                        result = new PlayerStatusChunk { PlayerStatus = SaveDataStoragePlayerStatus.ReadFrom(r, inner) };
                        break;
                    case 0x464C4731: // FLG1
                        result = new EventFlagChunk { EventFlag = SaveDataStorageEventFlag.ReadFrom(r, inner) };
                        break;
                    case 0x53544631: // STF1
                        result = new TicoFatChunk { TicoFat = SaveDataStorageTicoFat.ReadFrom(r, inner) };
                        break;
                    case 0x564C4531: // VLE1
                        result = new EventValueChunk { EventValue = SaveDataStorageEventValue.ReadFrom(r, inner) };
                        break;
                    case 0x47414C41: // GALA
                        result = new GalaxyChunk { Galaxy = SaveDataStorageGalaxy.ReadFrom(r) };
                        break;
                    case 0x5353574D: // SSWM
                        result = new WorldMapChunk { WorldMap = SaveDataStorageWorldMap.ReadFrom(r, inner) };
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown GameData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                        r.BaseStream.Seek(inner, SeekOrigin.Current);
                        break;
                }

                r.BaseStream.Position = start + size;
                return result;
            }

            static ConfigDataChunk? ReadConfigChunk(BinaryReader r)
            {
                var (magic, hash, size, inner, start) = r.ReadChunkHeader();
                ConfigDataChunk? chunk = null;
                switch (magic)
                {
                    case 0x434F4E46: // CONF
                        chunk = new CreateChunk { Create = new ConfigDataCreate { IsCreated = r.ReadSByte() != 0 } };
                        break;
                    case 0x4D494920: // MII
                        chunk = new MiiChunk { Mii = ConfigDataMii.ReadFrom(r, inner) };
                        break;
                    case 0x4D495343: // MISC
                        var misc = new ConfigDataMisc { LastModified = r.ReadInt64Be() };
                        chunk = new MiscChunk { Misc = misc };
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown ConfigData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                        r.BaseStream.Seek(inner, SeekOrigin.Current);
                        break;
                }

                r.BaseStream.Position = start + size;
                return chunk;
            }

            static SysConfigData? ReadSysConfigChunk(BinaryReader r)
            {
                var (magic, hash, size, inner, start) = r.ReadChunkHeader();
                SysConfigData? chunk = null;
                if (magic == 0x53595343) // SYSC
                {
                    chunk = Galaxy2.SaveData.Chunks.Sysconf.SysConfigData.ReadFrom(r, inner);
                }
                else
                {
                    Console.Error.WriteLine($"Unknown SysConfigData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                    r.BaseStream.Seek(inner, SeekOrigin.Current);
                }

                r.BaseStream.Position = start + size;
                return chunk;
            }
        }

        public void WriteTo(BinaryWriter writer, string name)
        {
            var startPos = writer.BaseStream.Position;
            
            // write version
            writer.Write((byte)2);
            
            // determine chunks to write
            if (name.StartsWith("user"))
            {
                var chunks = GameData ?? [];
                writer.Write((byte)chunks.Count);
                writer.Write(new byte[2]); // reserved

                foreach (var c in chunks)
                {
                    using var ms = new MemoryStream();
                    using var bw = new BinaryWriter(ms);

                    // TODO: calculate hashes
                    if (c is PlayerStatusChunk p)
                    {
                        p.PlayerStatus.WriteTo(bw);
                        var body = ms.ToArray();
                        // Hash = data_size + header_size
                        // data_size = 1 + 2 + 2 + 2 + 1 = 8
                        // header_size = 4 + attribute_count*4; attribute_count = 5 => 4 + 20 = 24
                        uint playHash = (uint)(8 + 24);
                        writer.WriteChunkHeader(0x504C4159, playHash, body.Length);
                        writer.Write(body);
                    }
                    else if (c is EventFlagChunk ef)
                    {
                        ef.EventFlag.WriteTo(bw);
                        var body = ms.ToArray();
                        // Hash = HashCode::from("2bytes/flag")
                        var flgHash = HashKey.FromString("2bytes/flag").Value;
                        writer.WriteChunkHeader(0x464C4731, flgHash, body.Length);
                        writer.Write(body);
                    }
                    else if (c is TicoFatChunk tf)
                    {
                        tf.TicoFat.WriteTo(bw);
                        var body = ms.ToArray();
                        // Hash = HashCode::from("SaveDataStorageTicoFat").into_raw().wrapping_add(0x120)
                        var baseHash = HashKey.FromString("SaveDataStorageTicoFat").Value;
                        uint tfHash = unchecked(baseHash + 0x120u);
                        writer.WriteChunkHeader(0x53544631, tfHash, body.Length);
                        writer.Write(body);
                    }
                    else if (c is EventValueChunk ev)
                    {
                        ev.EventValue.WriteTo(bw);
                        var body = ms.ToArray();
                        // Hash = u32::from_be_bytes(*b"VLE1")
                        uint vleHash = ((uint)'V' << 24) | ((uint)'L' << 16) | ((uint)'E' << 8) | (uint)'1';
                        writer.WriteChunkHeader(0x564C4531, vleHash, body.Length);
                        writer.Write(body);
                    }
                    else if (c is GalaxyChunk g)
                    {
                        g.Galaxy.WriteTo(bw);
                        var body = ms.ToArray();
                        // Hash = scenario_data_size + stage_header_size + 2 
                        // scenario_data_size = 6, stage_header_size = 4 + 5*4 = 24
                        uint galaHash = (uint)(6 + 24 + 2);
                        writer.WriteChunkHeader(0x47414C41, galaHash, body.Length);
                        writer.Write(body);
                    }
                    else if (c is WorldMapChunk wm)
                    {
                        wm.WorldMap.WriteTo(bw);
                        var body = ms.ToArray();
                        // Hash = HashCode::from("SaveDataStorageWorldMap").into_raw().wrapping_mul(9)
                        var wmBase = HashKey.FromString("SaveDataStorageWorldMap").Value;
                        uint wmHash = unchecked(wmBase * 9u);
                        writer.WriteChunkHeader(0x5353574D, wmHash, body.Length);
                        writer.Write(body);
                    }
                 }

                 // pad to user file fixed size
                 var endTarget = startPos + 0xF80;
                 var cur = writer.BaseStream.Position;
                 if (cur < endTarget)
                 {
                     var pad = (int)(endTarget - cur);
                     writer.Write(new byte[pad]);
                 }
             }
            else if (name.StartsWith("config"))
            {
                var chunks = ConfigData ?? [];
                writer.Write((byte)chunks.Count);
                writer.Write(new byte[2]);

                foreach (var c in chunks)
                {
                    using var ms = new MemoryStream();
                    using var bw = new BinaryWriter(ms);
                    
                    if (c is CreateChunk cr)
                    {
                        // Writes -1 (0xFF) for true
                        bw.Write((sbyte)(cr.Create.IsCreated ? -1 : 0));
                        var body = ms.ToArray();
                        // HashCode::from_raw(0x2432DA)
                        writer.WriteChunkHeader(0x434F4E46, 0x2432DA, body.Length);
                        writer.Write(body);
                    }
                    else if (c is MiiChunk mc)
                    {
                        mc.Mii.WriteTo(bw);
                        var body = ms.ToArray();
                        // HashCode::from_raw(0x2836E9)
                        writer.WriteChunkHeader(0x4D494920, 0x2836E9, body.Length);
                        writer.Write(body);
                    }
                    else if (c is MiscChunk misc)
                    {
                        bw.WriteInt64Be(misc.Misc.LastModified);
                        var body = ms.ToArray();
                        // HashCode::from_raw(0x1)
                        writer.WriteChunkHeader(0x4D495343, 0x1, body.Length);
                        writer.Write(body);
                    }
                 }

                 var endTarget = startPos + 0x60;
                 var cur = writer.BaseStream.Position;
                 if (cur < endTarget)
                 {
                     var pad = (int)(endTarget - cur);
                     writer.Write(new byte[pad]);
                 }
             }
            else if (name == "sysconf")
            {
                var chunks = SysConfigData ?? [];
                writer.Write((byte)chunks.Count);
                writer.Write(new byte[2]);

                // TODO: calculate hashes
                foreach (var c in chunks)
                {
                    using var ms = new MemoryStream();
                    using var bw = new BinaryWriter(ms);
                    c.WriteTo(bw);
                    var body = ms.ToArray();
                    // SysConfig hash: HashCode::from_raw(0x3)
                    writer.WriteChunkHeader(0x53595343, 0x3, body.Length);
                    writer.Write(body);
                }

                var endTarget = startPos + 0x80;
                var cur = writer.BaseStream.Position;
                if (cur < endTarget)
                {
                    var pad = (int)(endTarget - cur);
                    writer.Write(new byte[pad]);
                }
            }
        }
    }
}
