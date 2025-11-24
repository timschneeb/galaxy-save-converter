using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;

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
}