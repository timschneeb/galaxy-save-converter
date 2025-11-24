// Add this using directive
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.String;
using Galaxy2.SaveData.Ptr;
using Galaxy2.SaveData.Chunks.Game;
using Galaxy2.SaveData.Chunks.Config;
using Galaxy2.SaveData.Chunks.Sysconf;
using Galaxy2.SaveData.Time;

namespace Galaxy2.SaveData
{
    public class BinaryData
    {
        public static SaveDataFile ReadLeFile(string path)
        {
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                Console.WriteLine($"Reading header at position: {reader.BaseStream.Position}");
                var header = new SaveDataFileHeader
                {
                    Checksum = ReadUInt32BigEndian(reader),
                    Version = ReadUInt32BigEndian(reader),
                    UserFileInfoNum = ReadUInt32BigEndian(reader),
                    FileSize = ReadUInt32BigEndian(reader)
                };
                Console.WriteLine($"Header: Checksum={header.Checksum}, Version={header.Version}, UserFileInfoNum={header.UserFileInfoNum}, FileSize={header.FileSize}");

                var userFileInfo = new List<SaveDataUserFileInfo>();
                var userFileOffsets = new List<uint>();
                for (int i = 0; i < header.UserFileInfoNum; i++)
                {
                    Console.WriteLine($"Reading UserFileInfo {i} at position: {reader.BaseStream.Position}");
                    var name = new FixedString12(reader);
                    var offset = ReadUInt32BigEndian(reader);
                    userFileInfo.Add(new SaveDataUserFileInfo { Name = name, UserFile = new Ptr32<SaveDataUserFile>(null) });
                    userFileOffsets.Add(offset);
                    Console.WriteLine($"  Name: {name}, Offset: {offset}");
                }

                for (int i = 0; i < header.UserFileInfoNum; i++)
                {
                    if (userFileOffsets[i] == 0) continue;
                    Console.WriteLine($"Seeking to UserFile {userFileInfo[i].Name} at offset: {userFileOffsets[i]}");
                    reader.BaseStream.Seek(userFileOffsets[i], SeekOrigin.Begin);
                    userFileInfo[i].UserFile!.Value = ReadSaveDataUserFile(reader, userFileInfo[i].Name!.ToString()!);
                }

                return new SaveDataFile { Header = header, UserFileInfo = userFileInfo };
            }
        }

        private static SaveDataUserFile ReadSaveDataUserFile(BinaryReader reader, string name)
        {
            Console.WriteLine($"Reading SaveDataUserFile for {name} at position: {reader.BaseStream.Position}");
            var version = reader.ReadByte();
            var chunkNum = reader.ReadByte();
            reader.ReadBytes(2); // Skip reserved bytes
            Console.WriteLine($"  Version: {version}, ChunkNum: {chunkNum}");

            var userFile = new SaveDataUserFile();
            long startOfUserFile = reader.BaseStream.Position - 4; // Position before reading version, chunkNum, reserved bytes

            int bufferSize = 0;
            if (name.StartsWith("user"))
            {
                bufferSize = 0xF80; // For GameDataChunk, 4096 bytes
                var chunks = new List<GameDataChunk>();
                for (int i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadGameDataChunk(reader);
                    if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }
                userFile.Data = chunks;
            }
            else if (name.StartsWith("config"))
            {
                bufferSize = 0x60; // For ConfigDataChunk, 96 bytes
                var chunks = new List<ConfigDataChunk>();
                for (int i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadConfigDataChunk(reader);
                     if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }
                userFile.Data = chunks;
            }
            else if (name == "sysconf")
            {
                bufferSize = 0x80; // For SysConfigDataChunk, 128 bytes
                var chunks = new List<SysConfigDataChunk>();
                for (int i = 0; i < chunkNum; i++)
                {
                    var chunk = ReadSysConfigDataChunk(reader);
                     if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }
                userFile.Data = chunks;
            }

            // Skip padding to align with T::BUFFER_SIZE - size_of::<u32>() in Rust
            // The 4 bytes are for version, chunkNum, and reserved
            reader.BaseStream.Position = startOfUserFile + bufferSize; // Seek to the end of the padded block

            return userFile;
        }

        private static GameDataChunk? ReadGameDataChunk(BinaryReader reader)
        {
            var chunkStartPos = reader.BaseStream.Position; // Capture starting position

            var magicBytes = reader.ReadBytes(4);
            Console.WriteLine($"  Raw Magic Bytes: {BitConverter.ToString(magicBytes)}");
            var magic = ToUInt32BigEndian(magicBytes);

            var hashBytes = reader.ReadBytes(4);
            Console.WriteLine($"  Raw Hash Bytes: {BitConverter.ToString(hashBytes)}");
            var hash = ToUInt32BigEndian(hashBytes);

            var sizeBytes = reader.ReadBytes(4);
            Console.WriteLine($"  Raw Size Bytes: {BitConverter.ToString(sizeBytes)}");
            var size = ToUInt32BigEndian(sizeBytes);
            var innerDataSize = (int)(size - 12); // Size of the data *after* the header

            Console.WriteLine($"  Magic: 0x{magic:X}, Hash: 0x{hash:X}, Size: {size}, InnerDataSize: {innerDataSize}");
            
            GameDataChunk? chunk = null; // Make nullable
            
            switch (magic)
            {
                case 0x504C4159: // PLAY
                    Console.WriteLine("  Chunk Type: PLAY");
                    chunk = new PlayerStatusChunk { Data = ReadPlayerStatus(reader, innerDataSize)! };
                    break;
                case 0x464C4731: // FLG1
                    Console.WriteLine("  Chunk Type: FLG1");
                    chunk = new EventFlagChunk { Data = ReadEventFlag(reader, innerDataSize)! };
                    break;
                case 0x53544631: // STF1
                    Console.WriteLine("  Chunk Type: STF1");
                    chunk = new TicoFatChunk { Data = ReadTicoFat(reader, innerDataSize)! };
                    break;
                case 0x564C4531: // VLE1
                    Console.WriteLine("  Chunk Type: VLE1");
                    chunk = new EventValueChunk { Data = ReadEventValue(reader, innerDataSize)! };
                    break;
                case 0x47414C41: // GALA
                    Console.WriteLine("  Chunk Type: GALA");
                    chunk = new GalaxyChunk { Data = ReadGalaxy(reader, innerDataSize)! };
                    break;
                case 0x5353574D: // SSWM
                    Console.WriteLine("  Chunk Type: SSWM");
                    chunk = new WorldMapChunk { Data = ReadWorldMap(reader, innerDataSize)! };
                    break;
                default:
                    Console.WriteLine($"  Unknown Chunk Type: 0x{magic:X}. Skipping {innerDataSize} bytes.");
                    // If unknown, just skip the inner data.
                    reader.BaseStream.Seek(innerDataSize, SeekOrigin.Current);
                    break;
            }
            
            // After reading the chunk's content, ensure the stream is at chunkStartPos + size (total size)
            reader.BaseStream.Position = chunkStartPos + size;

            return chunk;
        }


        private static SaveDataStoragePlayerStatus ReadPlayerStatus(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading PlayerStatus at position: {reader.BaseStream.Position}");
            var status = new SaveDataStoragePlayerStatus();
            var dataStartPos = reader.BaseStream.Position;

            var attributeNum = ReadUInt16BigEndian(reader);
            var headerDataSize = ReadUInt16BigEndian(reader);
            Console.WriteLine($"    AttributeNum: {attributeNum}, HeaderDataSize: {headerDataSize}");

            var attributes = new Dictionary<ushort, ushort>();
            for (int i = 0; i < attributeNum; i++)
            {
                var key = ReadUInt16BigEndian(reader);
                var offset = ReadUInt16BigEndian(reader);
                attributes.Add(key, offset);
                Console.WriteLine($"    Attribute {i}: Key=0x{key:X}, Offset={offset}");
            }

            var fieldsDataStartPos = reader.BaseStream.Position;
            Console.WriteLine($"    Fields data starts at: {fieldsDataStartPos}");

            var playerLeftOffset = attributes[(ushort)(Binary.HashCode.FromString("mPlayerLeft").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + playerLeftOffset;
            status.PlayerLeft = reader.ReadByte();
            Console.WriteLine($"      mPlayerLeft: {status.PlayerLeft} (Offset: {playerLeftOffset})");

            var stockedStarPieceNumOffset = attributes[(ushort)(Binary.HashCode.FromString("mStockedStarPieceNum").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + stockedStarPieceNumOffset;
            status.StockedStarPieceNum = ReadUInt16BigEndian(reader);
            Console.WriteLine($"      mStockedStarPieceNum: {status.StockedStarPieceNum} (Offset: {stockedStarPieceNumOffset})");

            var stockedCoinNumOffset = attributes[(ushort)(Binary.HashCode.FromString("mStockedCoinNum").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + stockedCoinNumOffset;
            status.StockedCoinNum = ReadUInt16BigEndian(reader);
            Console.WriteLine($"      mStockedCoinNum: {status.StockedCoinNum} (Offset: {stockedCoinNumOffset})");

            var last1upCoinNumOffset = attributes[(ushort)(Binary.HashCode.FromString("mLast1upCoinNum").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + last1upCoinNumOffset;
            status.Last1upCoinNum = ReadUInt16BigEndian(reader);
            Console.WriteLine($"      mLast1upCoinNum: {status.Last1upCoinNum} (Offset: {last1upCoinNumOffset})");

            var flagOffset = attributes[(ushort)(Binary.HashCode.FromString("mFlag").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + flagOffset;
            status.Flag = new SaveDataStoragePlayerStatusFlag(reader.ReadByte());
            Console.WriteLine($"      mFlag: {status.Flag} (Offset: {flagOffset})"); 
            
            // Ensure the reader's position is exactly dataSize bytes from dataStartPos
            reader.BaseStream.Position = dataStartPos + dataSize;

            return status;
        }

        private static SaveDataStorageEventFlag ReadEventFlag(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading EventFlag at position: {reader.BaseStream.Position}");
            var eventFlag = new SaveDataStorageEventFlag();
            var count = dataSize / 2;
            eventFlag.EventFlags = new List<GameEventFlag>();
            for (int i = 0; i < count; i++)
            {
                eventFlag.EventFlags.Add(new GameEventFlag(ReadUInt16BigEndian(reader)));
            }
            Console.WriteLine($"    Read {count} EventFlags.");
            return eventFlag;
        }

        private static SaveDataStorageTicoFat ReadTicoFat(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading TicoFat at position: {reader.BaseStream.Position}");
            var ticoFat = new SaveDataStorageTicoFat();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    ticoFat.StarPieceNum[i, j] = ReadUInt16BigEndian(reader);
                }
            }
            for (int i = 0; i < 16; i++)
            {
                ticoFat.CoinGalaxyName[i] = ReadUInt16BigEndian(reader);
            }
            Console.WriteLine("    Read TicoFat data.");
            return ticoFat;
        }

        private static SaveDataStorageEventValue ReadEventValue(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading EventValue at position: {reader.BaseStream.Position}");
            var eventValue = new SaveDataStorageEventValue();
            var count = dataSize / 4;
            eventValue.EventValues = new List<GameEventValue>();
            for (int i = 0; i < count; i++)
            {
                eventValue.EventValues.Add(new GameEventValue
                {
                    Key = ReadUInt16BigEndian(reader),
                    Value = ReadUInt16BigEndian(reader)
                });
            }
            Console.WriteLine($"    Read {count} EventValues.");
            return eventValue;
        }

        private static SaveDataStorageGalaxy ReadGalaxy(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading Galaxy at position: {reader.BaseStream.Position}");
            var galaxy = new SaveDataStorageGalaxy();
            var galaxyNum = ReadUInt16BigEndian(reader);
            galaxy.Galaxy = new List<SaveDataStorageGalaxyStage>();
            Console.WriteLine($"    GalaxyNum: {galaxyNum}");

            // Read the two BinaryDataContentHeaderSerializer blocks for stage and scenario
            var stageSerializer = ReadBinaryDataContentHeaderSerializer(reader, "Stage header serializer");
            var scenarioSerializer = ReadBinaryDataContentHeaderSerializer(reader, "Scenario header serializer");

            // stageSerializer.dataSize tells us how many bytes each stage header (the field data block) occupies
            int stageHeaderSize = stageSerializer.dataSize; // use per-stage data_size (T::data_size)

            // Read one stage header then its scenarios, repeating galaxyNum times.
            for (int i = 0; i < galaxyNum; i++)
            {
                var headerRaw = reader.ReadBytes(stageHeaderSize);
                // Helper to read bytes from headerRaw safely
                ushort ReadU16FromHeader(int off)
                {
                    if (off + 1 >= headerRaw.Length) return 0;
                    var b = new byte[] { headerRaw[off], headerRaw[off + 1] };
                    return ToUInt16BigEndian(b);
                }
                byte ReadU8FromHeader(int off)
                {
                    if (off >= headerRaw.Length) return 0;
                    return headerRaw[off];
                }

                // Map keys to offsets from the serializer attributes
                ushort keyGalaxyName = (ushort)(Binary.HashCode.FromString("mGalaxyName").Value & 0xFFFF);
                ushort keyDataSize = (ushort)(Binary.HashCode.FromString("mDataSize").Value & 0xFFFF);
                ushort keyScenarioNum = (ushort)(Binary.HashCode.FromString("mScenarioNum").Value & 0xFFFF);
                ushort keyGalaxyState = (ushort)(Binary.HashCode.FromString("mGalaxyState").Value & 0xFFFF);
                ushort keyFlag = (ushort)(Binary.HashCode.FromString("mFlag").Value & 0xFFFF);

                int offGalaxyName = -1, offDataSize = -1, offScenarioNum = -1, offGalaxyState = -1, offFlag = -1;
                foreach (var a in stageSerializer.attributes)
                {
                    if (a.key == keyGalaxyName) offGalaxyName = a.offset;
                    else if (a.key == keyDataSize) offDataSize = a.offset;
                    else if (a.key == keyScenarioNum) offScenarioNum = a.offset;
                    else if (a.key == keyGalaxyState) offGalaxyState = a.offset;
                    else if (a.key == keyFlag) offFlag = a.offset;
                }

                var name = offGalaxyName >= 0 ? ReadU16FromHeader(offGalaxyName) : (ushort)0;
                var ds = offDataSize >= 0 ? ReadU16FromHeader(offDataSize) : (ushort)0;
                var scnum = offScenarioNum >= 0 ? ReadU8FromHeader(offScenarioNum) : (byte)0;
                var gstate = offGalaxyState >= 0 ? ReadU8FromHeader(offGalaxyState) : (byte)0;
                var gflag = offFlag >= 0 ? ReadU8FromHeader(offFlag) : (byte)0;

                Console.WriteLine($"    Parsed Header[{i}] from serializer: name=0x{name:X}, data_size={ds}, scenario_num={scnum}, galaxy_state={gstate}, flag={gflag}");

                var stage = new SaveDataStorageGalaxyStage();
                stage.GalaxyName = name;
                stage.FixedHeaderSize = ds;
                stage.ScenarioNum = scnum;
                if (gstate > 2)
                {
                    throw new InvalidDataException($"Invalid GalaxyState value {gstate} for stage {i}");
                }
                stage.GalaxyState = (SaveDataStorageGalaxyState)gstate;
                stage.Flag = new SaveDataStorageGalaxyFlag(gflag);

                Console.WriteLine($"  Reading GalaxyStage body {i} at position: {reader.BaseStream.Position}");
                stage.Scenario = new List<SaveDataStorageGalaxyScenario>();
                for (int j = 0; j < stage.ScenarioNum; j++)
                {
                    stage.Scenario.Add(ReadGalaxyScenario(reader));
                }

                galaxy.Galaxy.Add(stage);
            }

            Console.WriteLine("    Read Galaxy stages.");
            return galaxy;
        }

        // Read and return a BinaryDataContentHeaderSerializer<T> structure from the stream.
        private static (ushort attributeNum, int dataSize, List<(ushort key, int offset)> attributes) ReadBinaryDataContentHeaderSerializer(BinaryReader reader, string label)
        {
            var attributeNum = ReadUInt16BigEndian(reader);
            var dataSize = ReadUInt16BigEndian(reader);
            Console.WriteLine($"    Read {label}: attribute_num={attributeNum}, data_size={dataSize}");
            var attrs = new List<(ushort key, int offset)>();
            for (int i = 0; i < attributeNum; i++)
            {
                var key = ReadUInt16BigEndian(reader);
                var offset = ReadUInt16BigEndian(reader);
                attrs.Add((key, offset));
                Console.WriteLine($"      Attribute {i}: key=0x{key:X}, offset={offset}");
            }
            return (attributeNum, dataSize, attrs);
        }

        private static SaveDataStorageWorldMap ReadWorldMap(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading WorldMap at position: {reader.BaseStream.Position}");
            var worldMap = new SaveDataStorageWorldMap();
            worldMap.StarCheckPointFlag = reader.ReadBytes(8);
            worldMap.WorldNo = reader.ReadByte();
            Console.WriteLine($"    StarCheckPointFlag: {BitConverter.ToString(worldMap.StarCheckPointFlag)}, WorldNo: {worldMap.WorldNo}");
            return worldMap;
        }


        private static ConfigDataChunk? ReadConfigDataChunk(BinaryReader reader)
        {
            var chunkStartPos = reader.BaseStream.Position;
            var magicBytes = reader.ReadBytes(4);
            Console.WriteLine($"  Raw Magic Bytes: {BitConverter.ToString(magicBytes)}");
            var magic = ToUInt32BigEndian(magicBytes);

            var hash = ReadUInt32BigEndian(reader);
            var size = ReadUInt32BigEndian(reader);
            var innerDataSize = (int)(size - 12);
            Console.WriteLine($"  Magic: 0x{magic:X}, Hash: 0x{hash:X}, Size: {size}, InnerDataSize: {innerDataSize}");

            ConfigDataChunk? chunk = null;
            
            switch (magic)
            {
                case 0x434F4E46: // CONF
                    Console.WriteLine("  Chunk Type: CONF");
                    chunk = new CreateChunk { Data = new ConfigDataCreate { IsCreated = reader.ReadSByte() != 0 }! };
                    break;
                case 0x4D494920: // MII
                    Console.WriteLine("  Chunk Type: MII");
                    chunk = new MiiChunk { Data = ReadMii(reader)! };
                    break;
                case 0x4D495343: // MISC
                    Console.WriteLine("  Chunk Type: MISC");
                    var miscData = new ConfigDataMisc();
                    miscData.LastModified = new OSTime(ReadInt64BigEndian(reader));
                    chunk = new MiscChunk { Data = miscData! };
                    break;
                default:
                    Console.WriteLine($"  Unknown Chunk Type: 0x{magic:X}. Skipping {innerDataSize} bytes.");
                    reader.BaseStream.Seek(innerDataSize, SeekOrigin.Current);
                    break;
            }

            reader.BaseStream.Position = chunkStartPos + size;

            return chunk;
        }

        private static ConfigDataMii ReadMii(BinaryReader reader)
        {
            Console.WriteLine($"  Reading Mii at position: {reader.BaseStream.Position}");
            var mii = new ConfigDataMii();
            mii.Flag = new ConfigDataMiiFlag(reader.ReadByte());
            mii.MiiId = new Face.RFLCreateID { Id = reader.ReadBytes(8) };
            mii.IconId = (ConfigDataMiiIcon)reader.ReadByte();
            Console.WriteLine($"    Flag: {mii.Flag}, MiiId: {BitConverter.ToString(mii.MiiId.Id)}, IconId: {mii.IconId}"); // Access _value directly
            return mii;
        }

        private static SysConfigDataChunk? ReadSysConfigDataChunk(BinaryReader reader)
        {
            var chunkStartPos = reader.BaseStream.Position;
            var magicBytes = reader.ReadBytes(4);
            Console.WriteLine($"  Raw Magic Bytes: {BitConverter.ToString(magicBytes)}");
            var magic = ToUInt32BigEndian(magicBytes);

            var hash = ReadUInt32BigEndian(reader);
            var size = ReadUInt32BigEndian(reader);
            var innerDataSize = (int)(size - 12);
            Console.WriteLine($"  Magic: 0x{magic:X}, Hash: 0x{hash:X}, Size: {size}, InnerDataSize: {innerDataSize}");

            SysConfigDataChunk? chunk = null;
            switch (magic)
            {
                case 0x53595343: // SYSC
                    Console.WriteLine("  Chunk Type: SYSC");
                    chunk = new SysConfigChunk { Data = ReadSysConfig(reader, innerDataSize)! };
                    break;
                default:
                    Console.WriteLine($"  Unknown Chunk Type: 0x{magic:X}. Skipping {innerDataSize} bytes.");
                    reader.BaseStream.Seek(innerDataSize, SeekOrigin.Current);
                    break;
            }

            reader.BaseStream.Position = chunkStartPos + size;

            return chunk;
        }

        private static SysConfigData ReadSysConfig(BinaryReader reader, int dataSize)
        {
            Console.WriteLine($"  Reading SysConfig at position: {reader.BaseStream.Position}");
            var sysConfig = new SysConfigData();
            var dataStartPos = reader.BaseStream.Position;

            var attributeNum = ReadUInt16BigEndian(reader);
            var headerDataSize = ReadUInt16BigEndian(reader);
            Console.WriteLine($"    AttributeNum: {attributeNum}, HeaderDataSize: {headerDataSize}");

            var attributes = new Dictionary<ushort, ushort>();
            for (int i = 0; i < attributeNum; i++)
            {
                var key = ReadUInt16BigEndian(reader);
                var offset = ReadUInt16BigEndian(reader);
                attributes.Add(key, offset);
                Console.WriteLine($"    Attribute {i}: Key=0x{key:X}, Offset={offset}");
            }

            var fieldsDataStartPos = reader.BaseStream.Position;
            Console.WriteLine($"    Fields data starts at: {fieldsDataStartPos}");

            var isEncouragePal60Offset = attributes[(ushort)(Binary.HashCode.FromString("mIsEncouragePal60").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + isEncouragePal60Offset;
            sysConfig.IsEncouragePal60 = reader.ReadByte() != 0;
            Console.WriteLine($"      mIsEncouragePal60: {sysConfig.IsEncouragePal60} (Offset: {isEncouragePal60Offset})");

            var timeSentOffset = attributes[(ushort)(Binary.HashCode.FromString("mTimeSent").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + timeSentOffset;
            sysConfig.TimeSent = new OSTime(ReadInt64BigEndian(reader));
            Console.WriteLine($"      mTimeSent: {sysConfig.TimeSent.Value} (Offset: {timeSentOffset})");

            var sentBytesOffset = attributes[(ushort)(Binary.HashCode.FromString("mSentBytes").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + sentBytesOffset;
            sysConfig.SentBytes = ReadUInt32BigEndian(reader);
            Console.WriteLine($"      mSentBytes: {sysConfig.SentBytes} (Offset: {sentBytesOffset})");

            var bankStarPieceNumOffset = attributes[(ushort)(Binary.HashCode.FromString("mBankStarPieceNum").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + bankStarPieceNumOffset;
            sysConfig.BankStarPieceNum = ReadUInt16BigEndian(reader);
            Console.WriteLine($"      mBankStarPieceNum: {sysConfig.BankStarPieceNum} (Offset: {bankStarPieceNumOffset})");

            var bankStarPieceMaxOffset = attributes[(ushort)(Binary.HashCode.FromString("mBankStarPieceMax").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + bankStarPieceMaxOffset;
            sysConfig.BankStarPieceMax = ReadUInt16BigEndian(reader);
            Console.WriteLine($"      mBankStarPieceMax: {sysConfig.BankStarPieceMax} (Offset: {bankStarPieceMaxOffset})");

            var giftedPlayerLeftOffset = attributes[(ushort)(Binary.HashCode.FromString("mGiftedPlayerLeft").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + giftedPlayerLeftOffset;
            sysConfig.GiftedPlayerLeft = reader.ReadByte();
            Console.WriteLine($"      mGiftedPlayerLeft: {sysConfig.GiftedPlayerLeft} (Offset: {giftedPlayerLeftOffset})");

            var giftedFileNameHashOffset = attributes[(ushort)(Binary.HashCode.FromString("mGiftedFileNameHash").Value & 0xFFFF)];
            reader.BaseStream.Position = fieldsDataStartPos + giftedFileNameHashOffset;
            sysConfig.GiftedFileNameHash = ReadUInt16BigEndian(reader);
            Console.WriteLine($"      mGiftedFileNameHash: {sysConfig.GiftedFileNameHash} (Offset: {giftedFileNameHashOffset})");

            // Ensure the reader's position is exactly dataSize bytes from dataStartPos
            reader.BaseStream.Position = dataStartPos + dataSize;

            return sysConfig;
        }

        private static SaveDataStorageGalaxyScenario ReadGalaxyScenario(BinaryReader reader)
        {
            Console.WriteLine($"  Reading GalaxyScenario at position: {reader.BaseStream.Position}");
            var scenario = new SaveDataStorageGalaxyScenario();
            scenario.MissNum = reader.ReadByte();
            scenario.BestTime = ReadUInt32BigEndian(reader);
            scenario.Flag = new SaveDataStorageGalaxyScenarioFlag(reader.ReadByte());
            Console.WriteLine($"    MissNum: {scenario.MissNum}, BestTime: {scenario.BestTime}, Flag: {scenario.Flag}");
            return scenario;
        }

        private static ushort ReadUInt16BigEndian(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            return ToUInt16BigEndian(bytes);
        }

        private static uint ReadUInt32BigEndian(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return ToUInt32BigEndian(bytes);
        }
        
        private static long ReadInt64BigEndian(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            return ToInt64BigEndian(bytes);
        }

        private static ushort ToUInt16BigEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt16(bytes, 0);
        }

        private static uint ToUInt32BigEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static long ToInt64BigEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt64(bytes, 0);
        }
    }
}
