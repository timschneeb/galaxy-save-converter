using System.Text.Json;
using Galaxy2.SaveData.Save;
using System.CommandLine;
using Galaxy2.SaveData.Utils;

namespace Galaxy2.SaveData.Json;

public static class Program
{
    private static string DefaultFileName(FileType type)
    {
        return type switch
        {
            FileType.Json => "GameData.json",
            FileType.SwitchBin => "GameData_switch.bin",
            FileType.WiiBin => "GameData_wii.bin",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static int Main(string[] args)
    {
        var inputArg = new Argument<FileInfo>("input") { Description = "Input file path" };
        var outputArg = new Option<FileInfo>("--output", "-o") { Description = "Output file path" };

        var root = new RootCommand("Convert Galaxy2 save data between Nintendo Switch, Wii and JSON formats");

        // Subcommands
        var switch2Json = new Command("switch2json", "Switch (.bin) -> .json") { inputArg, outputArg };
        var wii2Json = new Command("wii2json", "Wii (.bin) -> .json") { inputArg, outputArg };
        var json2Switch = new Command("json2switch", ".json -> Switch (.bin)") { inputArg, outputArg };
        var json2Wii = new Command("json2wii", ".json -> Wii (.bin)") { inputArg, outputArg };
        var switch2Wii = new Command("switch2wii", "Switch (.bin) -> Wii (.bin)") { inputArg, outputArg };
        var wii2Switch = new Command("wii2switch", "Wii (.bin) -> Switch (.bin)") { inputArg, outputArg };

        switch2Json.SetAction(pr => Convert(pr, FileType.SwitchBin, FileType.Json));
        wii2Json.SetAction(pr => Convert(pr, FileType.WiiBin, FileType.Json));
        json2Switch.SetAction(pr => Convert(pr, FileType.Json, FileType.SwitchBin));
        json2Wii.SetAction(pr => Convert(pr, FileType.Json, FileType.WiiBin));
        switch2Wii.SetAction(pr => Convert(pr, FileType.SwitchBin, FileType.WiiBin));
        wii2Switch.SetAction(pr => Convert(pr, FileType.WiiBin, FileType.SwitchBin));

        root.Subcommands.Add(switch2Json);
        root.Subcommands.Add(wii2Json);
        root.Subcommands.Add(json2Switch);
        root.Subcommands.Add(json2Wii);
        root.Subcommands.Add(switch2Wii);
        root.Subcommands.Add(wii2Switch);

        var parse = root.Parse(args);
        return parse.Invoke();

        int Convert(ParseResult pr, FileType from, FileType to)
        {
            if (pr.Errors.Count > 0 || pr.GetValue(inputArg) is not { } inputFile)
            {
                foreach (var parseError in pr.Errors)
                {
                    Console.Error.WriteLine(parseError.Message);
                }
                return 1;
            }
            
            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Input file not found: {inputFile.FullName}");
                return 2;
            }

            var outputFile = pr.GetValue(outputArg) ?? new FileInfo(DefaultFileName(to));

            SaveDataFile
                .ReadFile(inputFile.FullName, from)
                .WriteFile(outputFile.FullName, to);
            return 0;
        }
    }
}