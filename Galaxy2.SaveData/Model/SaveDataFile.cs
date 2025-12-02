using System.Buffers;
using System.Text.Json;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Model;

public class SaveDataFile
{
    public required SaveDataFileHeader Header { get; set; }
    public required List<SaveDataUserFileInfo> UserFileInfo { get; set; }

    public static SaveDataFile ReadFile(string path, FileType from)
    {
        if (from == FileType.Json)
        {
            return ReadJsonString(File.ReadAllText(path));
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadBinary(fs, from);
    }
    
    public static SaveDataFile ReadBinary(Stream stream, FileType from)
    {
        using var reader = new EndianAwareReader(stream, from == FileType.WiiBin ? ConsoleType.Wii : ConsoleType.Switch);

        var header = new SaveDataFileHeader
        {
            Checksum = reader.ReadUInt32(),
            Version = reader.ReadUInt32(),
            UserFileInfoNum = reader.ReadUInt32(),
            FileSize = reader.ReadUInt32()
        };

        var userFileInfo = new List<SaveDataUserFileInfo>((int)header.UserFileInfoNum);
        for (var i = 0; i < header.UserFileInfoNum; i++)
        {
            var name = new String.FixedString12(reader);
            var offset = reader.ReadUInt32();

            if (offset == 0)
            {
                Console.Error.WriteLine($"Warning: Skipping user file '{name}' with zero offset.");
                continue;
            }

            var returnPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            userFileInfo.Add(new SaveDataUserFileInfo
            {
                Name = name,
                UserFile = SaveDataUserFile.ReadFrom(reader, name.ToString())
            });

            reader.BaseStream.Seek(returnPos, SeekOrigin.Begin);
        }

        return new SaveDataFile { Header = header, UserFileInfo = userFileInfo };
    }
    
    public static SaveDataFile ReadJsonString(string json) => JsonSerializer.Deserialize<SaveDataFile>(json, JsonOptions.SerializerOptions)!;
    
    public void WriteFile(string path, FileType from)
    {
        if (from == FileType.Json)
        {
            File.WriteAllText(path, ToJson());
            return;
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        WriteBinary(fs, from);
    }
    
    public void WriteBinary(Stream stream, FileType fileType)
    {
        using var writer = new EndianAwareWriter(stream, fileType == FileType.WiiBin ? ConsoleType.Wii : ConsoleType.Switch);

        // header placeholders (checksum=0, version, count, fileSize=0)
        writer.WriteUInt32(0);
        writer.WriteUInt32(Header.Version);
        writer.WriteUInt32((uint)UserFileInfo.Count);
        writer.WriteUInt32(0);

        // reserve table for name+offset
        var infoStart = writer.BaseStream.Position;
        foreach (var u in UserFileInfo)
        {
            u.Name.WriteTo(writer);
            writer.WriteUInt32(0);
        }

        // write user files and collect offsets locally
        var offsets = new List<uint>(UserFileInfo.Count);
        foreach (var u in UserFileInfo)
        {
            var off = (uint)writer.BaseStream.Position;
            offsets.Add(off);
            u.UserFile.WriteTo(writer, u.Name!.ToString()!);
        }

        // back-fill offsets into table
        var endPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = infoStart;
        foreach (var off in offsets)
        {
            writer.BaseStream.Position += 12; // skip FixedString12
            writer.WriteUInt32(off);
        }

        // write file size in header (after checksum, version, count)
        writer.BaseStream.Position = 12;
        writer.WriteUInt32((uint)endPosition);

        // compute checksum from byte 4 to end using streaming buffer
        writer.Flush();
        stream.Flush();
        var checksum = ComputeChecksum(stream, endPosition, writer.BigEndian);

        // write checksum at start
        writer.BaseStream.Position = 0;
        writer.WriteUInt32(checksum);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions.SerializerOptions);

    /// <summary>
    /// Compute checksum over the stream from offset 4 up to endPosition.
    /// A trailing odd byte is ignored.
    /// </summary>
    private static uint ComputeChecksum(Stream stream, long endPosition, bool bigEndian)
    {
        const int start = 4;
        var byteCount = endPosition - start;
        if (byteCount <= 1) return 0;

        ushort sum = 0;
        ushort invSum = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            stream.Position = start;
            var remainingEven = byteCount & ~1L;
            while (remainingEven > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remainingEven);
                if ((toRead & 1) != 0) toRead--;
                var read = stream.Read(buffer, 0, toRead);
                if (read <= 0) break;

                var process = read & ~1;
                if (bigEndian)
                {
                    for (var i = 0; i < process; i += 2)
                    {
                        var term = (ushort)((buffer[i] << 8) | buffer[i + 1]);
                        unchecked
                        {
                            sum = (ushort)(sum + term);
                            invSum = (ushort)(invSum + (ushort)~term);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < process; i += 2)
                    {
                        var term = (ushort)(buffer[i] | (buffer[i + 1] << 8));
                        unchecked
                        {
                            sum = (ushort)(sum + term);
                            invSum = (ushort)(invSum + (ushort)~term);
                        }
                    }
                }

                remainingEven -= process;
            }

            return ((uint)sum << 16) | invSum;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}