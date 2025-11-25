using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Chunks.Game
{
    public class SaveDataStorageGalaxy
    {
        [JsonPropertyName("galaxy")]
        public List<SaveDataStorageGalaxyStage> Galaxy { get; set; } = new List<SaveDataStorageGalaxyStage>();

        public static SaveDataStorageGalaxy ReadFrom(BinaryReader reader)
        {
            var galaxy = new SaveDataStorageGalaxy();
            var galaxyNum = reader.ReadUInt16Be();
            galaxy.Galaxy = new List<SaveDataStorageGalaxyStage>(galaxyNum);

            var stageSerializer = reader.ReadBinaryDataContentHeaderSerializer();
            // TODO: dont forget to write when serializing though
            _ = reader.ReadBinaryDataContentHeaderSerializer(); // discard scenario serializer

            var stageHeaderSize = stageSerializer.dataSize;

            for (var i = 0; i < galaxyNum; i++)
            {
                var headerRaw = reader.ReadBytes(stageHeaderSize);

                ushort ReadU16At(int off) => (off < 0 || off + 1 >= headerRaw.Length) ? (ushort)0 : (ushort)((headerRaw[off] << 8) | headerRaw[off + 1]);
                byte ReadU8At(int off) => (off < 0 || off >= headerRaw.Length) ? (byte)0 : headerRaw[off];

                var keyGalaxyName = HashKey.Compute("mGalaxyName");
                var keyDataSize = HashKey.Compute("mDataSize");
                var keyScenarioNum = HashKey.Compute("mScenarioNum");
                var keyGalaxyState = HashKey.Compute("mGalaxyState");
                var keyFlag = HashKey.Compute("mFlag");

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
                    stage.Scenario.Add(SaveDataStorageGalaxyScenario.ReadFrom(reader));

                galaxy.Galaxy.Add(stage);
            }

            return galaxy;
        }

        public void WriteTo(BinaryWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            // write number of galaxy stages
            writer.WriteUInt16Be((ushort)(Galaxy?.Count ?? 0));

            // Build stage header serializer attributes. Layout mirrors the reader expectations.
            var stageAttrs = new List<(ushort key, ushort offset)>
            {
                (HashKey.Compute("mGalaxyName"), 0),
                (HashKey.Compute("mDataSize"), 2),
                (HashKey.Compute("mScenarioNum"), 4),
                (HashKey.Compute("mGalaxyState"), 5),
                (HashKey.Compute("mFlag"), 6),
            };
            const ushort stageHeaderSize = 7;
            writer.WriteBinaryDataContentHeader(stageAttrs, stageHeaderSize);

            // Build scenario header serializer attributes
            var scenarioAttrs = new List<(ushort key, ushort offset)>
            {
                (HashKey.Compute("mMissNum"), 0),
                (HashKey.Compute("mBestTime"), 1),
                (HashKey.Compute("mFlag"), 5),
            };
            const ushort scenarioHeaderSize = 6;
            writer.WriteBinaryDataContentHeader(scenarioAttrs, scenarioHeaderSize);

            // write each stage: fixed header block then scenario entries
            if (Galaxy != null)
            {
                foreach (var s in Galaxy)
                {
                    var headerRaw = new byte[stageHeaderSize];

                    // mGalaxyName (u16 BE) at offset 0
                    headerRaw[0] = (byte)(s.GalaxyName >> 8);
                    headerRaw[1] = (byte)(s.GalaxyName & 0xFF);

                    // mDataSize (u16 BE) at offset 2. If unset, default to scenarioHeaderSize
                    var ds = s.FixedHeaderSize != 0 ? s.FixedHeaderSize : scenarioHeaderSize;
                    headerRaw[2] = (byte)(ds >> 8);
                    headerRaw[3] = (byte)(ds & 0xFF);

                    // mScenarioNum (u8) at offset 4
                    headerRaw[4] = s.ScenarioNum;

                    // mGalaxyState (u8) at offset 5
                    headerRaw[5] = (byte)s.GalaxyState;

                    // mFlag (u8) at offset 6 -- compute from boolean properties
                    byte flagByte = 0;
                    try
                    {
                        if (s.Flag.TicoCoin) flagByte |= 0b1;
                        if (s.Flag.Comet) flagByte |= 0b10;
                    }
                    catch { }
                    headerRaw[6] = flagByte;

                    writer.Write(headerRaw);

                    // write scenario entries
                    if (s.Scenario != null)
                    {
                        foreach (var sc in s.Scenario)
                            sc.WriteTo(writer);
                    }
                }
            }
        }
    }

    public class SaveDataStorageGalaxyStage
    {
        [JsonPropertyName("galaxy_name")]
        public ushort GalaxyName { get; set; }
        [JsonPropertyName("data_size")]
        public ushort FixedHeaderSize { get; set; }
        [JsonPropertyName("scenario_num")]
        public byte ScenarioNum { get; set; }
        [JsonPropertyName("galaxy_state")]
        public SaveDataStorageGalaxyState GalaxyState { get; set; }
        [JsonPropertyName("flag")]
        public SaveDataStorageGalaxyFlag Flag { get; set; }
        [JsonPropertyName("scenario")]
        public List<SaveDataStorageGalaxyScenario> Scenario { get; set; } = new List<SaveDataStorageGalaxyScenario>();
    }

    public class SaveDataStorageGalaxyScenario
    {
        [JsonPropertyName("miss_num")]
        public byte MissNum { get; set; }
        [JsonPropertyName("best_time")]
        public uint BestTime { get; set; }
        [JsonPropertyName("flag")]
        public SaveDataStorageGalaxyScenarioFlag Flag { get; set; }

        public static SaveDataStorageGalaxyScenario ReadFrom(BinaryReader reader)
        {
            return new SaveDataStorageGalaxyScenario
            {
                MissNum = reader.ReadByte(),
                BestTime = reader.ReadUInt32Be(),
                Flag = new SaveDataStorageGalaxyScenarioFlag(reader.ReadByte())
            };
        }

        public void WriteTo(BinaryWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.Write(MissNum);
            writer.WriteUInt32Be(BestTime);

            byte f = 0;
            try
            {
                if (Flag.PowerStar) f |= 0b1;
                if (Flag.BronzeStar) f |= 0b10;
                if (Flag.AlreadyVisited) f |= 0b100;
                if (Flag.GhostLuigi) f |= 0b1000;
                if (Flag.IntrusivelyLuigi) f |= 0b10000;
            }
            catch { }
            writer.Write(f);
        }
    }
    
    public struct SaveDataStorageGalaxyScenarioFlag(byte value)
    {
        private byte _value = value;

        [JsonPropertyName("power_star")]
        public bool PowerStar
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }

        [JsonPropertyName("bronze_star")]
        public bool BronzeStar
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
        }

        [JsonPropertyName("already_visited")]
        public bool AlreadyVisited
        {
            get => (_value & 0b100) != 0;
            set => _value = (byte)(value ? (_value | 0b100) : (_value & ~0b100));
        }

        [JsonPropertyName("ghost_luigi")]
        public bool GhostLuigi
        {
            get => (_value & 0b1000) != 0;
            set => _value = (byte)(value ? (_value | 0b1000) : (_value & ~0b1000));
        }

        [JsonPropertyName("intrusively_luigi")]
        public bool IntrusivelyLuigi
        {
            get => (_value & 0b10000) != 0;
            set => _value = (byte)(value ? (_value | 0b10000) : (_value & ~0b10000));
        }
    }

    public enum SaveDataStorageGalaxyState : byte
    {
        Closed = 0,
        New = 1,
        Opened = 2,
    }

    public struct SaveDataStorageGalaxyFlag(byte value)
    {
        private byte _value = value;

        [JsonPropertyName("tico_coin")]
        public bool TicoCoin
        {
            get => (_value & 0b1) != 0;
            set => _value = (byte)(value ? (_value | 0b1) : (_value & ~0b1));
        }
        
        [JsonPropertyName("comet")]
        public bool Comet
        {
            get => (_value & 0b10) != 0;
            set => _value = (byte)(value ? (_value | 0b10) : (_value & ~0b10));
        }
    }
}
