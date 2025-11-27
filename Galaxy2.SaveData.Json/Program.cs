using System.Text.Json;
using System.Text.Json.Serialization;
using Galaxy2.SaveData.Save;
using Galaxy2.SaveData.String;

namespace Galaxy2.SaveData.Json;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new UShort2DArrayJsonConverter(),
            new FixedString12JsonConverter(),
            new ByteArrayAsNumberArrayJsonConverter()
        }
    };
    
    public static void Main(string[] args)
    {
        if (args.Length != 3 || (args[0] != "le2json" && args[0] != "be2json"))
        {
            Console.WriteLine("Usage: Galaxy2.SaveData.Json <mode> <input.bin> <output.json>");
            Console.WriteLine("Modes:");
            Console.WriteLine("\tle2json - Convert little-endian binary save data (Switch) to JSON");
            Console.WriteLine("\tbe2json - Convert big-endian binary save data (Wii) to JSON");
            return;
        }
        
        var mode = args[0];
        var inputFile = args[1];
        var outputFile = args[2];

        var saveData = SaveDataFile.ReadFile(inputFile, bigEndian: mode == "be2json");
        var rootObj = new { user_file_info = saveData.UserFileInfo };
        var json = JsonSerializer.Serialize(rootObj, JsonOptions);
        File.WriteAllText(outputFile, json);
        Console.WriteLine($"Successfully converted {inputFile} to {outputFile}");
        
        saveData.WriteFile("reconstructed_save_data.bin", bigEndian: mode == "be2json");
    }
}