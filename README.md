# Super Mario Galaxy 2 save data converter

This tool can parse, rebuild, and convert Super Mario Galaxy 2 save data files between different formats. 

## Supported Formats

* Nintendo Wii Save File (GameData.bin)
* Nintendo Switch Save File (GameData.bin)
* JSON File (*.json)

## Web app

A web application for this tool is available at: https://galaxy.0001002.xyz/

## CLI usage

To compile and run the tool you need to have .NET 10.0 SDK installed. 

```bash
dotnet run --project SMGSaveData.CLI --help
```
```
Description:
  Convert Galaxy2 save data between Nintendo Switch, Wii and JSON formats

Usage:
  SMGSaveData.CLI [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  switch2json <input>  Switch (.bin) -> .json
  wii2json <input>     Wii (.bin) -> .json
  json2switch <input>  .json -> Switch (.bin)
  json2wii <input>     .json -> Wii (.bin)
  switch2wii <input>   Switch (.bin) -> Wii (.bin)
  wii2switch <input>   Wii (.bin) -> Switch (.bin)
```

To convert saves from the Wii to Switch version:

```bash
dotnet run --project SMGSaveData.CLI wii2switch GameData.bin --output GameData_switch.bin
```
