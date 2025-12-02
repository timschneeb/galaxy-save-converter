using System.Buffers.Binary;
using System.Text.Json.Serialization;
using SMGSaveData.Galaxy2.Model.Chunks.Config;
using SMGSaveData.Galaxy2.Model.Chunks.Game;
using SMGSaveData.Galaxy2.Model.Chunks.Sysconf;
using SMGSaveData.Galaxy2.String;
using SMGSaveData.Galaxy2.Utils;

namespace SMGSaveData.Galaxy2.Model;

public class SaveDataUserFileInfo
{
    [JsonPropertyName("name")]
    public required FixedString12 Name { get; set; }
    [JsonPropertyName("user_file")]
    public required SaveDataUserFile UserFile { get; set; }
}

public class SaveDataUserFile
{
    [JsonPropertyName("GameData")]
    public List<GameDataChunk>? GameData { get; set; }

    [JsonPropertyName("ConfigData")]
    public List<ConfigDataChunk>? ConfigData { get; set; }

    [JsonPropertyName("SysConfigData")]
    public List<SysConfigData>? SysConfigData { get; set; }

    public static SaveDataUserFile ReadFrom(EndianAwareReader reader, string name)
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

        static GameDataChunk? ReadGameChunk(EndianAwareReader r)
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
                    result = new TicoFatChunk { TicoFat = SaveDataStorageTicoFat.ReadFrom(r) };
                    break;
                case 0x564C4531: // VLE1
                    result = new EventValueChunk { EventValue = SaveDataStorageEventValue.ReadFrom(r, inner) };
                    break;
                case 0x47414C41: // GALA
                    result = new GalaxyChunk { Galaxy = SaveDataStorageGalaxy.ReadFrom(r) };
                    break;
                case 0x5353574D: // SSWM
                    result = new WorldMapChunk { WorldMap = SaveDataStorageWorldMap.ReadFrom(r) };
                    break;
                default:
                    Console.Error.WriteLine($"Unknown GameData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                    r.BaseStream.Seek(inner, SeekOrigin.Current);
                    break;
            }

            r.BaseStream.Position = start + size;
            return result;
        }

        static ConfigDataChunk? ReadConfigChunk(EndianAwareReader r)
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
                    var misc = new ConfigDataMisc
                    {
                        LastModified = r.ReadTime()
                    };
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

        static SysConfigData? ReadSysConfigChunk(EndianAwareReader r)
        {
            var (magic, hash, size, inner, start) = r.ReadChunkHeader();
            SysConfigData? chunk = null;
            if (magic == 0x53595343) // SYSC
            {
                chunk = Chunks.Sysconf.SysConfigData.ReadFrom(r, inner);
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

    public void WriteTo(EndianAwareWriter writer, string name)
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
                using var bw = writer.NewWriter(ms);

                if (c is PlayerStatusChunk p)
                {
                    p.PlayerStatus.WriteTo(bw, out var hash);
                    var body = ms.ToArray();
                    writer.WriteChunkHeader(0x504C4159, hash, body.Length);
                    writer.Write(body);
                }
                else if (c is EventFlagChunk ef)
                {
                    ef.EventFlag.WriteTo(bw);
                    var body = ms.ToArray();
                    var flgHash = HashKey.FromString("2bytes/flag").Value;
                    writer.WriteChunkHeader(0x464C4731, flgHash, body.Length);
                    writer.Write(body);
                }
                else if (c is TicoFatChunk tf)
                {
                    tf.TicoFat.WriteTo(bw);
                    var body = ms.ToArray();
                    var baseHash = HashKey.FromString("SaveDataStorageTicoFat").Value;
                    var tfHash =
                        // These constants are hard-coded at compile time in the game.
                        // Probably defined as a preprocessor macro, maybe in relation to the size of some data type?
                        writer.ConsoleType == ConsoleType.Switch
                        ? unchecked(baseHash + 0x1e0u)
                        : unchecked(baseHash + 0x120u);
                    writer.WriteChunkHeader(0x53544631, tfHash, body.Length);
                    writer.Write(body);
                }
                else if (c is EventValueChunk ev)
                {
                    ev.EventValue.WriteTo(bw);
                    var body = ms.ToArray();
                    var vleHash = BinaryPrimitives.ReadUInt32BigEndian("VLE1"u8);
                    writer.WriteChunkHeader(0x564C4531, vleHash, body.Length);
                    writer.Write(body);
                }
                else if (c is GalaxyChunk g)
                {
                    g.Galaxy.WriteTo(bw, out var hash);
                    var body = ms.ToArray();
                    writer.WriteChunkHeader(0x47414C41, hash, body.Length);
                    writer.Write(body);
                }
                else if (c is WorldMapChunk wm)
                {
                    wm.WorldMap.WriteTo(bw);
                    var body = ms.ToArray();
                    var wmBase = HashKey.FromString("SaveDataStorageWorldMap").Value;
                    var wmHash = unchecked(wmBase * 9u);
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
                using var bw = writer.NewWriter(ms);
                    
                switch (c)
                {
                    case CreateChunk cr:
                    {
                        // Writes -1 (0xFF) for true
                        bw.Write((sbyte)(cr.Create.IsCreated ? -1 : 0));
                        if (writer.ConsoleType == ConsoleType.Switch)
                        {
                            bw.WriteAlignmentPadding(alignment: 4);
                        }
                        
                        var body = ms.ToArray();
                        writer.WriteChunkHeader(0x434F4E46, 0x2432DA, body.Length);
                        writer.Write(body);
                        break;
                    }
                    case MiiChunk mc:
                    {
                        mc.Mii.WriteTo(bw);
                        
                        var body = ms.ToArray();
                        writer.WriteChunkHeader(0x4D494920, 0x2836E9, body.Length);
                        writer.Write(body);
                        break;
                    }
                    case MiscChunk misc:
                    {
                        bw.WriteTime(misc.Misc.LastModified);
                        if (writer.ConsoleType == ConsoleType.Switch)
                        {
                            bw.WriteAlignmentPadding(alignment: 4);
                        }
                        
                        var body = ms.ToArray();
                        writer.WriteChunkHeader(0x4D495343, 0x1, body.Length);
                        writer.Write(body);
                        break;
                    }
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

            foreach (var c in chunks)
            {
                using var ms = new MemoryStream();
                using var bw = writer.NewWriter(ms);
                
                c.WriteTo(bw);
                var body = ms.ToArray();
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