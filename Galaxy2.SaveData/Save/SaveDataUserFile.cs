using System.Collections.Generic;
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
        // Explicit buckets matching the original DTO shape
        [JsonPropertyName("GameData")]
        public List<GameDataChunk>? GameData { get; set; }

        [JsonPropertyName("ConfigData")]
        public List<ConfigDataChunk>? ConfigData { get; set; }

        [JsonPropertyName("SysConfigData")]
        public List<SysConfigData>? SysConfigData { get; set; }

        // If we couldn't interpret the data, keep a raw placeholder
        [JsonPropertyName("UserFileRaw")]
        public object? UserFileRaw { get; set; }

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
                var chunks = new List<Galaxy2.SaveData.Chunks.Game.GameDataChunk>();
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
                var chunks = new List<Galaxy2.SaveData.Chunks.Config.ConfigDataChunk>();
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
                var chunks = new List<Galaxy2.SaveData.Chunks.Sysconf.SysConfigData>();
                for (var i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadSysConfigChunk(reader);
                    if (chunk != null) chunks.Add(chunk);
                }
                userFile.SysConfigData = chunks;
                reader.BaseStream.Position = startOfUserFile + 0x80;
            }

            return userFile;

            static Galaxy2.SaveData.Chunks.Game.GameDataChunk? ReadGameChunk(BinaryReader r)
            {
                var (magic, _, size, inner, start) = r.ReadChunkHeader();
                Galaxy2.SaveData.Chunks.Game.GameDataChunk? result = null;
                switch (magic)
                {
                    case 0x504C4159: // PLAY
                        result = new Galaxy2.SaveData.Chunks.Game.PlayerStatusChunk { PlayerStatus = Galaxy2.SaveData.Chunks.Game.SaveDataStoragePlayerStatus.ReadFrom(r, inner) };
                        break;
                    case 0x464C4731: // FLG1
                        result = new Galaxy2.SaveData.Chunks.Game.EventFlagChunk { EventFlag = Galaxy2.SaveData.Chunks.Game.SaveDataStorageEventFlag.ReadFrom(r, inner) };
                        break;
                    case 0x53544631: // STF1
                        result = new Galaxy2.SaveData.Chunks.Game.TicoFatChunk { TicoFat = Galaxy2.SaveData.Chunks.Game.SaveDataStorageTicoFat.ReadFrom(r, inner) };
                        break;
                    case 0x564C4531: // VLE1
                        result = new Galaxy2.SaveData.Chunks.Game.EventValueChunk { EventValue = Galaxy2.SaveData.Chunks.Game.SaveDataStorageEventValue.ReadFrom(r, inner) };
                        break;
                    case 0x47414C41: // GALA
                        result = new Galaxy2.SaveData.Chunks.Game.GalaxyChunk { Galaxy = Galaxy2.SaveData.Chunks.Game.SaveDataStorageGalaxy.ReadFrom(r) };
                        break;
                    case 0x5353574D: // SSWM
                        result = new Galaxy2.SaveData.Chunks.Game.WorldMapChunk { WorldMap = Galaxy2.SaveData.Chunks.Game.SaveDataStorageWorldMap.ReadFrom(r, inner) };
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown GameData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                        r.BaseStream.Seek(inner, SeekOrigin.Current);
                        break;
                }

                r.BaseStream.Position = start + size;
                return result;
            }

            static Galaxy2.SaveData.Chunks.Config.ConfigDataChunk? ReadConfigChunk(BinaryReader r)
            {
                var (magic, _, size, inner, start) = r.ReadChunkHeader();
                Galaxy2.SaveData.Chunks.Config.ConfigDataChunk? chunk = null;
                switch (magic)
                {
                    case 0x434F4E46: // CONF
                        chunk = new Galaxy2.SaveData.Chunks.Config.CreateChunk { Create = new Galaxy2.SaveData.Chunks.Config.ConfigDataCreate { IsCreated = r.ReadSByte() != 0 } };
                        break;
                    case 0x4D494920: // MII
                        chunk = new Galaxy2.SaveData.Chunks.Config.MiiChunk { Mii = Galaxy2.SaveData.Chunks.Config.ConfigDataMii.ReadFrom(r, inner) };
                        break;
                    case 0x4D495343: // MISC
                        var misc = new Galaxy2.SaveData.Chunks.Config.ConfigDataMisc { LastModified = r.ReadInt64Be() };
                        chunk = new Galaxy2.SaveData.Chunks.Config.MiscChunk { Misc = misc };
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown ConfigData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                        r.BaseStream.Seek(inner, SeekOrigin.Current);
                        break;
                }

                r.BaseStream.Position = start + size;
                return chunk;
            }

            static Galaxy2.SaveData.Chunks.Sysconf.SysConfigData? ReadSysConfigChunk(BinaryReader r)
            {
                var (magic, _, size, inner, start) = r.ReadChunkHeader();
                Galaxy2.SaveData.Chunks.Sysconf.SysConfigData? chunk = null;
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
    }
}
