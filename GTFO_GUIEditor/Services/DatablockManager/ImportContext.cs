using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Encapsulates contextual information, loaded datablocks, caches, and buffers for datablock import operations.
/// </summary>
public class ImportContext
{
    public MainWindowViewModel MainVm { get; }
    public string ImportRootPath { get; }
    public string LgTunerDirectory { get; }
    public string SchemasDirectory { get; }

    // Loaded datablock dictionaries indexed by persistentID
    public Dictionary<uint, JsonObject> TextBlocks { get; } = new();
    public Dictionary<uint, JsonObject> LightSettingsBlocks { get; } = new();
    public Dictionary<uint, JsonObject> FogSettingsBlocks { get; } = new();
    public Dictionary<uint, JsonObject> ConsumableDistributionBlocks { get; } = new();
    public Dictionary<uint, JsonObject> ChainedPuzzleBlocks { get; } = new();
    public Dictionary<uint, JsonObject> SurvivalWaveSettingsBlocks { get; } = new();
    public Dictionary<uint, JsonObject> SurvivalWavePopulationBlocks { get; } = new();
    public Dictionary<uint, JsonObject> EnemyGroupBlocks { get; } = new();
    public Dictionary<uint, JsonObject> EnemyBlocks { get; } = new();
    public Dictionary<uint, JsonObject> WardenObjectiveBlocks { get; } = new();
    public Dictionary<uint, JsonObject> ExpeditionBalanceBlocks { get; } = new();
    public Dictionary<uint, JsonObject> ComplexResourceSetBlocks { get; } = new();
    public Dictionary<uint, JsonObject> LevelLayoutBlocks { get; } = new();
    public Dictionary<uint, JsonObject> DimensionBlocks { get; } = new();
    public Dictionary<uint, JsonObject> RundownBlocks { get; } = new();
    public Dictionary<uint, JsonObject> GameSetupBlocks { get; } = new();

    public int DatablocksImportedCount { get; set; }
    public int LevelsImportedCount { get; set; }
    public List<string> SkippedLevels { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<LevelImportFailure> FailedLevels { get; set; } = new();

    public ImportContext(MainWindowViewModel mainVm, string importRootPath)
    {
        MainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
        ImportRootPath = importRootPath ?? throw new ArgumentNullException(nameof(importRootPath));
        LgTunerDirectory = Path.Combine(importRootPath, "Custom", "LGTunerAdvanced");
        SchemasDirectory = Path.Combine(importRootPath, ".schemas");

        LoadAllDatablocks();
    }

    private void LoadAllDatablocks()
    {
        LoadBlocks("GameData_TextDataBlock_bin.json", "TextDataBlock.json", TextBlocks);
        LoadBlocks("GameData_LightSettingsDataBlock_bin.json", "LightSettingsDataBlock.json", LightSettingsBlocks);
        LoadBlocks("GameData_FogSettingsDataBlock_bin.json", "FogSettingsDataBlock.json", FogSettingsBlocks);
        LoadBlocks("GameData_ConsumableDistributionDataBlock_bin.json", "ConsumableDistributionDataBlock.json", ConsumableDistributionBlocks);
        LoadBlocks("GameData_ChainedPuzzleDataBlock_bin.json", "ChainedPuzzleDataBlock.json", ChainedPuzzleBlocks);
        LoadBlocks("GameData_SurvivalWaveSettingsDataBlock_bin.json", "SurvivalWaveSettingsDataBlock.json", SurvivalWaveSettingsBlocks);
        LoadBlocks("GameData_SurvivalWavePopulationDataBlock_bin.json", "SurvivalWavePopulationDataBlock.json", SurvivalWavePopulationBlocks);
        LoadBlocks("GameData_EnemyGroupDataBlock_bin.json", "EnemyGroupDataBlock.json", EnemyGroupBlocks);
        LoadBlocks("GameData_EnemyDataBlock_bin.json", "EnemyDataBlock.json", EnemyBlocks);
        LoadBlocks("GameData_WardenObjectiveDataBlock_bin.json", "WardenObjectiveDataBlock.json", WardenObjectiveBlocks);
        LoadBlocks("GameData_ExpeditionBalanceDataBlock_bin.json", "ExpeditionBalanceDataBlock.json", ExpeditionBalanceBlocks);
        LoadBlocks("GameData_ComplexResourceSetDataBlock_bin.json", "ComplexResourceSetDataBlock.json", ComplexResourceSetBlocks);
        LoadBlocks("GameData_LevelLayoutDataBlock_bin.json", "LevelLayoutDataBlock.json", LevelLayoutBlocks);
        LoadBlocks("GameData_DimensionDataBlock_bin.json", "DimensionDataBlock.json", DimensionBlocks);
        LoadBlocks("GameData_RundownDataBlock_bin.json", "RundownDataBlock.json", RundownBlocks);
        LoadBlocks("GameData_GameSetupDataBlock_bin.json", "GameSetupDataBlock.json", GameSetupBlocks);
    }

    private void LoadBlocks(string fileName, string vanillaFileName, Dictionary<uint, JsonObject> targetDict)
    {
        var blocks = LoadBlocksArray(fileName, vanillaFileName, out bool isCustom);
        if (isCustom)
        {
            DatablocksImportedCount++;
        }

        foreach (var block in blocks)
        {
            if (block.TryGetPropertyValue("persistentID", out var pidNode) && pidNode != null &&
                uint.TryParse(pidNode.ToString(), out uint pid))
            {
                targetDict[pid] = block;
            }
        }
    }

    public List<JsonObject> LoadBlocksArray(string fileName, string vanillaFallbackFileName, out bool isCustom)
    {
        string directPath = Path.Combine(ImportRootPath, fileName);
        string shortName = fileName.StartsWith("GameData_") && fileName.EndsWith("_bin.json")
            ? fileName.Substring("GameData_".Length, fileName.Length - "GameData_".Length - "_bin.json".Length) + ".json"
            : fileName;
        string altPath = Path.Combine(ImportRootPath, shortName);

        string? resolvedPath = null;
        if (File.Exists(directPath))
        {
            resolvedPath = directPath;
        }
        else if (File.Exists(altPath))
        {
            resolvedPath = altPath;
        }

        if (resolvedPath != null)
        {
            try
            {
                string json = File.ReadAllText(resolvedPath);
                var doc = JsonNode.Parse(json);
                if (doc is JsonObject obj && obj.TryGetPropertyValue("Blocks", out var blocksNode) && blocksNode is JsonArray arr)
                {
                    isCustom = true;
                    return arr.OfType<JsonObject>().ToList();
                }
            }
            catch (Exception ex)
            {
                Warnings.Add($"Failed to parse {resolvedPath}: {ex.Message}");
            }
        }

        isCustom = false;
        return ExportContext.LoadVanillaBlocksArray(vanillaFallbackFileName);
    }

    public string ResolveText(uint textId)
    {
        if (textId == 0) return string.Empty;

        if (TextBlocks.TryGetValue(textId, out var textBlock))
        {
            if (textBlock.TryGetPropertyValue("English", out var engNode) && engNode != null)
            {
                return engNode.ToString();
            }
            if (textBlock.TryGetPropertyValue("Translation", out var transNode) && transNode != null)
            {
                return transNode.ToString();
            }
        }

        return string.Empty;
    }

    public string? FindLgTunerConfigFile(string levelName)
    {
        var possibleDirNames = new[]
        {
            LgTunerDirectory,
            Path.Combine(ImportRootPath, "LGTunerAdvanced"),
            Path.Combine(ImportRootPath, "LGTuner"),
            ImportRootPath
        };

        var possibleFileNames = new[]
        {
            $"LevelGen_{levelName}.json",
            $"{levelName}.json",
            $"LevelGen_{levelName}.lgtuner.json"
        };

        foreach (var dir in possibleDirNames)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var fn in possibleFileNames)
            {
                string p = Path.Combine(dir, fn);
                if (File.Exists(p)) return p;
            }

            // Case-insensitive check in directory
            try
            {
                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    string nameOnly = Path.GetFileNameWithoutExtension(file);
                    if (nameOnly.Equals($"LevelGen_{levelName}", StringComparison.OrdinalIgnoreCase) ||
                        nameOnly.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }
            }
            catch
            {
                // Ignore directory enumeration failures
            }
        }

        return null;
    }
}
