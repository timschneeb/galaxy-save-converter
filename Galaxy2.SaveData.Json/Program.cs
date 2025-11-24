using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Galaxy2.SaveData;
using Galaxy2.SaveData.Save;

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

            var saveData = SaveDataFile.ReadLeFile(inputFile);

            // Serialize the existing saveData structures directly. The storage classes are
            // annotated (or exposed via Json-friendly accessors) so they produce the same JSON shape.
            var rootObj = new { user_file_info = saveData.UserFileInfo };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new UShort2DArrayJsonConverter(),
                    new String.FixedString12JsonConverter(),
                    new ByteArrayAsNumberArrayJsonConverter()
                }
            };

            var json = JsonSerializer.Serialize(rootObj, options);
            File.WriteAllText(outputFile, json);
            Console.WriteLine($"Successfully converted {inputFile} to {outputFile}");
        }
    }
}