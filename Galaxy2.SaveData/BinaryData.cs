using System;
using System.IO;
using System.Collections.Generic;
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.String;
using Galaxy2.SaveData.Chunks.Game;
using Galaxy2.SaveData.Chunks.Config;
using Galaxy2.SaveData.Chunks.Sysconf;

namespace Galaxy2.SaveData
{
    public class BinaryData
    {
        public static SaveDataFile ReadLeFile(string path)
        {
            using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
            var header = new SaveDataFileHeader
            {
                Checksum = reader.ReadUInt32Be(),
                Version = reader.ReadUInt32Be(),
                UserFileInfoNum = reader.ReadUInt32Be(),
                FileSize = reader.ReadUInt32Be()
            };

            var userFileInfo = new List<SaveDataUserFileInfo>();
            var userFileOffsets = new List<uint>();
            for (var i = 0; i < header.UserFileInfoNum; i++)
            {
                var name = new FixedString12(reader);
                var offset = reader.ReadUInt32Be();
                userFileInfo.Add(new SaveDataUserFileInfo { Name = name });
                userFileOffsets.Add(offset);
            }

            for (var i = 0; i < header.UserFileInfoNum; i++)
            {
                if (userFileOffsets[i] == 0) continue;
                reader.BaseStream.Seek(userFileOffsets[i], SeekOrigin.Begin);
                userFileInfo[i].UserFile = ReadSaveDataUserFile(reader, userFileInfo[i].Name!.ToString()!);
            }

            return new SaveDataFile { Header = header, UserFileInfo = userFileInfo };
        }

        private static SaveDataUserFile ReadSaveDataUserFile(BinaryReader reader, string name)
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
                    var chunk = ReadGameDataChunk(reader);
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
                    var chunk = ReadConfigDataChunk(reader);
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
                    var chunk = ReadSysConfigDataChunk(reader);
                    if (chunk != null) chunks.Add(chunk);
                }
                userFile.SysConfigData = chunks;
                reader.BaseStream.Position = startOfUserFile + 0x80;
            }

            return userFile;
        }

        private static GameDataChunk? ReadGameDataChunk(BinaryReader reader)
        {
            var (magic, _, size, inner, start) = reader.ReadChunkHeader();
            GameDataChunk? result = null;

            switch (magic)
            {
                case 0x504C4159: // PLAY
                    result = new PlayerStatusChunk { PlayerStatus = ReadPlayerStatus(reader, inner) };
                    break;
                case 0x464C4731: // FLG1
                    result = new EventFlagChunk { EventFlag = ReadEventFlag(reader, inner) };
                    break;
                case 0x53544631: // STF1
                    result = new TicoFatChunk { TicoFat = ReadTicoFat(reader, inner) };
                    break;
                case 0x564C4531: // VLE1
                    result = new EventValueChunk { EventValue = ReadEventValue(reader, inner) };
                    break;
                case 0x47414C41: // GALA
                    result = new GalaxyChunk { Galaxy = ReadGalaxy(reader) };
                    break;
                case 0x5353574D: // SSWM
                    result = new WorldMapChunk { WorldMap = ReadWorldMap(reader) };
                    break;
                default:
                    Console.Error.WriteLine($"Unknown GameData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                    reader.BaseStream.Seek(inner, SeekOrigin.Current);
                    break;
            }

            // ensure end of chunk
            reader.BaseStream.Position = start + size;
            return result;
        }

        private static ConfigDataChunk? ReadConfigDataChunk(BinaryReader reader)
        {
            var (magic, _, size, inner, start) = reader.ReadChunkHeader();
            ConfigDataChunk? chunk = null;
            switch (magic)
            {
                case 0x434F4E46: // CONF
                    chunk = new CreateChunk { Create = new ConfigDataCreate { IsCreated = reader.ReadSByte() != 0 } };
                    break;
                case 0x4D494920: // MII
                    chunk = new MiiChunk { Mii = ReadMii(reader) };
                    break;
                case 0x4D495343: // MISC
                    var misc = new ConfigDataMisc { LastModified = reader.ReadInt64Be() };
                    chunk = new MiscChunk { Misc = misc };
                    break;
                default:
                    Console.Error.WriteLine($"Unknown ConfigData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                    reader.BaseStream.Seek(inner, SeekOrigin.Current);
                    break;
            }

            reader.BaseStream.Position = start + size;
            return chunk;
        }

        private static SysConfigData? ReadSysConfigDataChunk(BinaryReader reader)
        {
            var (magic, _, size, inner, start) = reader.ReadChunkHeader();
            SysConfigData? chunk = null;
            if (magic == 0x53595343) // SYSC
            {
                chunk = ReadSysConfig(reader, inner);
            }
            else
            {
                Console.Error.WriteLine($"Unknown SysConfigData chunk magic: 0x{magic:X8} at 0x{start:X} (size={size}), skipping");
                reader.BaseStream.Seek(inner, SeekOrigin.Current);
            }

            reader.BaseStream.Position = start + size;
            return chunk;
        }

        // --- specific chunk parsers ---
        private static SaveDataStoragePlayerStatus ReadPlayerStatus(BinaryReader reader, int dataSize)
        {
            var status = new SaveDataStoragePlayerStatus();
            var start = reader.BaseStream.Position;

            var (attributes, _) = reader.ReadAttributesAsDictionary();
            var fieldsStart = reader.BaseStream.Position;

            if (reader.TryReadU8(fieldsStart, attributes, "mPlayerLeft", out var playerLeft))
                status.PlayerLeft = playerLeft;
            if (reader.TryReadU16(fieldsStart, attributes, "mStockedStarPieceNum", out var sspn))
                status.StockedStarPieceNum = sspn;
            if (reader.TryReadU16(fieldsStart, attributes, "mStockedCoinNum", out var scn))
                status.StockedCoinNum = scn;
            if (reader.TryReadU16(fieldsStart, attributes, "mLast1upCoinNum", out var l1Up))
                status.Last1upCoinNum = l1Up;
            if (reader.TryReadU8(fieldsStart, attributes, "mFlag", out var flag))
                status.Flag = new SaveDataStoragePlayerStatusFlag(flag);

            reader.BaseStream.Position = start + dataSize;
            return status;
        }

        private static SaveDataStorageEventFlag ReadEventFlag(BinaryReader reader, int dataSize)
        {
            var eventFlag = new SaveDataStorageEventFlag();
            var count = dataSize / 2;
            eventFlag.EventFlags = new List<GameEventFlag>(count);
            for (var i = 0; i < count; i++)
                eventFlag.EventFlags.Add(new GameEventFlag(reader.ReadUInt16Be()));
            return eventFlag;
        }

        private static SaveDataStorageTicoFat ReadTicoFat(BinaryReader reader, int dataSize)
        {
            var tico = new SaveDataStorageTicoFat();
            for (var i = 0; i < 8; i++)
                for (var j = 0; j < 6; j++)
                    tico.StarPieceNum[i, j] = reader.ReadUInt16Be();
            for (var i = 0; i < 16; i++)
                tico.CoinGalaxyName[i] = reader.ReadUInt16Be();
            return tico;
        }

        private static SaveDataStorageEventValue ReadEventValue(BinaryReader reader, int dataSize)
        {
            var ev = new SaveDataStorageEventValue();
            var count = dataSize / 4;
            ev.EventValues = new List<GameEventValue>(count);
            for (var i = 0; i < count; i++)
                ev.EventValues.Add(new GameEventValue { Key = reader.ReadUInt16Be(), Value = reader.ReadUInt16Be() });
            return ev;
        }

        private static SaveDataStorageGalaxy ReadGalaxy(BinaryReader reader)
        {
            var galaxy = new SaveDataStorageGalaxy();
            var galaxyNum = reader.ReadUInt16Be();
            galaxy.Galaxy = new List<SaveDataStorageGalaxyStage>(galaxyNum);

            var stageSerializer = reader.ReadBinaryDataContentHeaderSerializer();
            _ = reader.ReadBinaryDataContentHeaderSerializer(); // scenario serializer (discard)

            var stageHeaderSize = stageSerializer.dataSize;

            for (var i = 0; i < galaxyNum; i++)
            {
                var headerRaw = reader.ReadBytes(stageHeaderSize);

                // small local readers for headerRaw
                ushort ReadU16At(int off) => (off < 0 || off + 1 >= headerRaw.Length) ? (ushort)0 : (ushort)((headerRaw[off] << 8) | headerRaw[off + 1]);
                byte ReadU8At(int off) => (off < 0 || off >= headerRaw.Length) ? (byte)0 : headerRaw[off];

                var keyGalaxyName = ComputeKey("mGalaxyName");
                var keyDataSize = ComputeKey("mDataSize");
                var keyScenarioNum = ComputeKey("mScenarioNum");
                var keyGalaxyState = ComputeKey("mGalaxyState");
                var keyFlag = ComputeKey("mFlag");

                int offGalaxyName = -1, offDataSize = -1, offScenarioNum = -1, offGalaxyState = -1, offFlag = -1;
                foreach (var a in stageSerializer.attributes)
                {
                    if (a.key == keyGalaxyName) offGalaxyName = a.offset;
                    else if (a.key == keyDataSize) offDataSize = a.offset;
                    else if (a.key == keyScenarioNum) offScenarioNum = a.offset;
                    else if (a.key == keyGalaxyState) offGalaxyState = a.offset;
                    else if (a.key == keyFlag) offFlag = a.offset;
                }

                var name = offGalaxyName >= 0 ? ReadU16At(offGalaxyName) : (ushort)0;
                var ds = offDataSize >= 0 ? ReadU16At(offDataSize) : (ushort)0;
                var scnum = offScenarioNum >= 0 ? ReadU8At(offScenarioNum) : (byte)0;
                var gstate = offGalaxyState >= 0 ? ReadU8At(offGalaxyState) : (byte)0;
                var gflag = offFlag >= 0 ? ReadU8At(offFlag) : (byte)0;

                var stage = new SaveDataStorageGalaxyStage
                {
                    GalaxyName = name,
                    FixedHeaderSize = ds,
                    ScenarioNum = scnum,
                    GalaxyState = (SaveDataStorageGalaxyState)gstate,
                    Flag = new SaveDataStorageGalaxyFlag(gflag)
                };

                if (gstate > 2) throw new InvalidDataException($"Invalid GalaxyState value {gstate} for stage {i}");

                stage.Scenario = new List<SaveDataStorageGalaxyScenario>(stage.ScenarioNum);
                for (var j = 0; j < stage.ScenarioNum; j++)
                    stage.Scenario.Add(ReadGalaxyScenario(reader));

                galaxy.Galaxy.Add(stage);
            }

            return galaxy;
        }

        private static SaveDataStorageGalaxyScenario ReadGalaxyScenario(BinaryReader reader)
        {
            return new SaveDataStorageGalaxyScenario
            {
                MissNum = reader.ReadByte(),
                BestTime = reader.ReadUInt32Be(),
                Flag = new SaveDataStorageGalaxyScenarioFlag(reader.ReadByte())
            };
        }

        private static ConfigDataMii ReadMii(BinaryReader reader)
        {
            return new ConfigDataMii
            {
                Flag = reader.ReadByte(),
                MiiId = reader.ReadBytes(8),
                IconId = (ConfigDataMiiIcon)reader.ReadByte()
            };
        }

        private static SysConfigData ReadSysConfig(BinaryReader reader, int dataSize)
        {
            var sys = new SysConfigData();
            var start = reader.BaseStream.Position;

            var (attributes, _) = reader.ReadAttributesAsDictionary();
            var fieldsStart = reader.BaseStream.Position;

            if (reader.TryReadU8(fieldsStart, attributes, "mIsEncouragePal60", out var pal)) sys.IsEncouragePal60 = pal != 0;
            if (reader.TryReadI64(fieldsStart, attributes, "mTimeSent", out var ts)) sys.TimeSent = ts;
            if (reader.TryReadU32(fieldsStart, attributes, "mSentBytes", out var sb)) sys.SentBytes = sb;
            if (reader.TryReadU16(fieldsStart, attributes, "mBankStarPieceNum", out var bn)) sys.BankStarPieceNum = bn;
            if (reader.TryReadU16(fieldsStart, attributes, "mBankStarPieceMax", out var bm)) sys.BankStarPieceMax = bm;
            if (reader.TryReadU8(fieldsStart, attributes, "mGiftedPlayerLeft", out var gl)) sys.GiftedPlayerLeft = gl;
            if (reader.TryReadU16(fieldsStart, attributes, "mGiftedFileNameHash", out var gfnh)) sys.GiftedFileNameHash = gfnh;

            reader.BaseStream.Position = start + dataSize;
            return sys;
        }

        private static SaveDataStorageWorldMap ReadWorldMap(BinaryReader reader)
        {
            return new SaveDataStorageWorldMap
            {
                StarCheckPointFlag = reader.ReadBytes(8),
                WorldNo = reader.ReadByte()
            };
        }

        private static ushort ComputeKey(string name) => (ushort)(Binary.HashCode.FromString(name).Value & 0xFFFF);
    }
}
