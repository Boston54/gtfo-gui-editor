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
    public string AraLevelLayoutDirectory { get; }
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
    public List<JsonObject> EnemyPopulationBlocks { get; } = new();
    public Dictionary<uint, JsonObject> WardenObjectiveBlocks { get; } = new();
    public Dictionary<uint, JsonObject> ExpeditionBalanceBlocks { get; } = new();
    public Dictionary<uint, JsonObject> ComplexResourceSetBlocks { get; } = new();
    public Dictionary<uint, JsonObject> LevelLayoutBlocks { get; } = new();
    public Dictionary<uint, JsonObject> DimensionBlocks { get; } = new();
    public Dictionary<uint, JsonObject> AtmosphereBlocks { get; } = new();
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
        AraLevelLayoutDirectory = Path.Combine(importRootPath, "Custom", "AdditionalRundownAdvancements", "LevelLayout");
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
        LoadPopulationBlocks();
        LoadBlocks("GameData_WardenObjectiveDataBlock_bin.json", "WardenObjectiveDataBlock.json", WardenObjectiveBlocks);
        LoadBlocks("GameData_ExpeditionBalanceDataBlock_bin.json", "ExpeditionBalanceDataBlock.json", ExpeditionBalanceBlocks);
        LoadBlocks("GameData_ComplexResourceSetDataBlock_bin.json", "ComplexResourceSetDataBlock.json", ComplexResourceSetBlocks);
        LoadBlocks("GameData_LevelLayoutDataBlock_bin.json", "LevelLayoutDataBlock.json", LevelLayoutBlocks);
        LoadBlocks("GameData_DimensionDataBlock_bin.json", "DimensionDataBlock.json", DimensionBlocks);
        LoadBlocks("GameData_AtmosphereDataBlock_bin.json", "AtmosphereDataBlock.json", AtmosphereBlocks);
        LoadBlocks("GameData_RundownDataBlock_bin.json", "RundownDataBlock.json", RundownBlocks);
        LoadBlocks("GameData_GameSetupDataBlock_bin.json", "GameSetupDataBlock.json", GameSetupBlocks);
    }

    private void LoadPopulationBlocks()
    {
        var candidatePaths = new[]
        {
            Path.Combine(ImportRootPath, "GameData_EnemyPopulationDataBlock_bin.json"),
            Path.Combine(ImportRootPath, "EnemyPopulationDataBlock.json"),
            Path.Combine(ImportRootPath, "Datablocks", "GameData_EnemyPopulationDataBlock_bin.json"),
            Path.Combine(ImportRootPath, "Datablocks", "EnemyPopulationDataBlock.json")
        };
        string? path = candidatePaths.FirstOrDefault(File.Exists);
        if (path == null) return;

        try
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is JsonObject obj &&
                obj["Blocks"] is JsonArray blocks)
            {
                EnemyPopulationBlocks.AddRange(blocks.OfType<JsonObject>());
                DatablocksImportedCount++;
            }
        }
        catch (Exception ex)
        {
            Warnings.Add($"Failed to parse {path}: {ex.Message}");
        }
    }

    private void LoadBlocks(string fileName, string vanillaFileName, Dictionary<uint, JsonObject> targetDict)
    {
        var blocks = LoadBlocksArray(fileName, vanillaFileName, out bool isCustom);
        if (isCustom) DatablocksImportedCount++;

        Dictionary<uint, JsonObject>? vanillaBlocks = null;
        if (isCustom)
        {
            vanillaBlocks = new Dictionary<uint, JsonObject>();
            foreach (var vanillaBlock in ExportContext.LoadVanillaBlocksArray(vanillaFileName))
            {
                if (vanillaBlock.TryGetPropertyValue("persistentID", out var vanillaIdNode) &&
                    vanillaIdNode != null &&
                    uint.TryParse(vanillaIdNode.ToString(), out uint vanillaId))
                {
                    vanillaBlocks[vanillaId] = vanillaBlock;
                }
            }
        }

        foreach (var block in blocks)
        {
            if (block.TryGetPropertyValue("persistentID", out var pidNode) && pidNode != null &&
                uint.TryParse(pidNode.ToString(), out uint pid))
            {
                if (isCustom &&
                    vanillaBlocks != null &&
                    vanillaBlocks.TryGetValue(pid, out var vanillaBlock) &&
                    string.Equals(
                        block["name"]?.ToString(),
                        vanillaBlock["name"]?.ToString(),
                        StringComparison.Ordinal))
                {
                    continue;
                }
                targetDict[pid] = block;
            }
        }
    }

    public List<JsonObject> LoadBlocksArray(string fileName, string vanillaFallbackFileName, out bool isCustom)
    {
        string shortName = fileName.StartsWith("GameData_") && fileName.EndsWith("_bin.json")
            ? fileName.Substring("GameData_".Length, fileName.Length - "GameData_".Length - "_bin.json".Length) + ".json"
            : fileName;

        var candidatePaths = new[]
        {
            Path.Combine(ImportRootPath, fileName),
            Path.Combine(ImportRootPath, shortName),
            Path.Combine(ImportRootPath, "Datablocks", fileName),
            Path.Combine(ImportRootPath, "datablocks", fileName),
            Path.Combine(ImportRootPath, "Datablocks", shortName),
            Path.Combine(ImportRootPath, "datablocks", shortName),
            Path.Combine(ImportRootPath, "GameData", fileName),
            Path.Combine(ImportRootPath, "GameData", shortName)
        };

        string? resolvedPath = candidatePaths.FirstOrDefault(File.Exists);

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
        return new List<JsonObject>();
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
        if (string.IsNullOrWhiteSpace(levelName)) return null;

        var possibleDirNames = new[]
        {
            LgTunerDirectory,
            Path.Combine(ImportRootPath, "Custom", "LGTunerAdvanced"),
            Path.Combine(ImportRootPath, "custom", "lgtuneradvanced"),
            Path.Combine(ImportRootPath, "Custom", "LGTuner"),
            Path.Combine(ImportRootPath, "custom", "lgtuner"),
            Path.Combine(ImportRootPath, "LGTunerAdvanced"),
            Path.Combine(ImportRootPath, "lgtuneradvanced"),
            Path.Combine(ImportRootPath, "LGTuner"),
            Path.Combine(ImportRootPath, "lgtuner"),
            ImportRootPath
        };

        var possibleFileNames = new List<string>
        {
            $"LevelGen_{levelName}.json",
            $"LevelGen_{levelName}.lgtuner.json"
        };

        foreach (var dir in possibleDirNames)
        {
            if (!Directory.Exists(dir)) continue;
            bool isLgTunerDir = dir.Contains("LGTuner", StringComparison.OrdinalIgnoreCase);

            var dirFileNames = new List<string>(possibleFileNames);
            if (isLgTunerDir)
            {
                dirFileNames.Add($"{levelName}.json");
            }

            foreach (var fn in dirFileNames)
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
                        (isLgTunerDir && nameOnly.Equals(levelName, StringComparison.OrdinalIgnoreCase)))
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

        // Recursive fallback across ImportRootPath for LGTuner files
        try
        {
            if (Directory.Exists(ImportRootPath))
            {
                var found = Directory.EnumerateFiles(ImportRootPath, "*.json", SearchOption.AllDirectories)
                    .FirstOrDefault(f =>
                    {
                        string dirName = Path.GetFileName(Path.GetDirectoryName(f) ?? string.Empty);
                        if (dirName.Equals("LevelLayout", StringComparison.OrdinalIgnoreCase) ||
                            f.Contains("AdditionalRundownAdvancements", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        string nameOnly = Path.GetFileNameWithoutExtension(f);
                        bool isLgDir = f.Contains("LGTuner", StringComparison.OrdinalIgnoreCase);
                        return nameOnly.Equals($"LevelGen_{levelName}", StringComparison.OrdinalIgnoreCase) ||
                               (isLgDir && nameOnly.Equals(levelName, StringComparison.OrdinalIgnoreCase));
                    });
                if (found != null) return found;
            }
        }
        catch
        {
            // Ignore directory enumeration failures
        }

        return null;
    }

    public string? FindAraLevelLayoutConfigFile(uint mainLayoutId, string? expeditionPrefix = null, string? publicName = null)
    {
        var possibleDirNames = new[]
        {
            AraLevelLayoutDirectory,
            Path.Combine(ImportRootPath, "Custom", "AdditionalRundownAdvancements", "LevelLayout"),
            Path.Combine(ImportRootPath, "custom", "additionalrundownadvancements", "levellayout"),
            Path.Combine(ImportRootPath, "Custom", "AdditionalRundownAdvancements", "levellayout"),
            Path.Combine(ImportRootPath, "AdditionalRundownAdvancements", "LevelLayout"),
            Path.Combine(ImportRootPath, "additionalrundownadvancements", "levellayout"),
            Path.Combine(ImportRootPath, "Custom", "ARA", "LevelLayout"),
            Path.Combine(ImportRootPath, "ARA", "LevelLayout"),
            ImportRootPath
        };

        var nameCandidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(expeditionPrefix))
        {
            nameCandidates.Add(expeditionPrefix);
        }
        if (!string.IsNullOrWhiteSpace(publicName))
        {
            nameCandidates.Add(publicName);
        }
        if (mainLayoutId > 0)
        {
            nameCandidates.Add(mainLayoutId.ToString());
            nameCandidates.Add($"LevelLayout_{mainLayoutId}");
            nameCandidates.Add($"Level_{mainLayoutId}");
        }

        // 1. Direct check and MainLevelLayout inspection in known directories
        foreach (var dir in possibleDirNames)
        {
            if (!Directory.Exists(dir)) continue;

            // Direct candidate filename check
            foreach (var cand in nameCandidates)
            {
                string p = Path.Combine(dir, $"{cand}.json");
                if (File.Exists(p)) return p;
            }

            // Inspect all json files in dir for MainLevelLayout match
            try
            {
                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    if (mainLayoutId > 0 && TryMatchesMainLevelLayout(file, mainLayoutId))
                    {
                        return file;
                    }

                    string nameOnly = Path.GetFileNameWithoutExtension(file);
                    if (nameCandidates.Any(c => nameOnly.Equals(c, StringComparison.OrdinalIgnoreCase)))
                    {
                        return file;
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }
        }

        // 2. Recursive fallback in ImportRootPath
        try
        {
            if (Directory.Exists(ImportRootPath))
            {
                foreach (var file in Directory.EnumerateFiles(ImportRootPath, "*.json", SearchOption.AllDirectories))
                {
                    string dirName = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty);
                    if (dirName.Equals("LevelLayout", StringComparison.OrdinalIgnoreCase) ||
                        file.Contains("AdditionalRundownAdvancements", StringComparison.OrdinalIgnoreCase) ||
                        file.Contains("ARA", StringComparison.OrdinalIgnoreCase))
                    {
                        if (mainLayoutId > 0 && TryMatchesMainLevelLayout(file, mainLayoutId))
                        {
                            return file;
                        }

                        string nameOnly = Path.GetFileNameWithoutExtension(file);
                        if (nameCandidates.Any(c => nameOnly.Equals(c, StringComparison.OrdinalIgnoreCase)))
                        {
                            return file;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore enumeration errors
        }

        return null;
    }

    public List<string> FindAllAraLevelLayoutConfigFiles(IEnumerable<uint> layoutIds, string? expeditionPrefix = null, string? publicName = null)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var idList = layoutIds.Where(id => id > 0).Distinct().ToList();

        foreach (var id in idList)
        {
            var single = FindAraLevelLayoutConfigFile(id, expeditionPrefix, publicName);
            if (single != null && File.Exists(single))
            {
                result.Add(single);
            }
        }

        // Check suffix variants
        var suffixes = new[] { "", "_Secondary", "_Third", "_Overload", "_Main" };
        var prefixes = new List<string>();
        if (!string.IsNullOrWhiteSpace(expeditionPrefix)) prefixes.Add(expeditionPrefix);
        if (!string.IsNullOrWhiteSpace(publicName)) prefixes.Add(publicName);

        var possibleDirNames = new[]
        {
            AraLevelLayoutDirectory,
            Path.Combine(ImportRootPath, "Custom", "AdditionalRundownAdvancements", "LevelLayout"),
            Path.Combine(ImportRootPath, "custom", "additionalrundownadvancements", "levellayout"),
            Path.Combine(ImportRootPath, "AdditionalRundownAdvancements", "LevelLayout"),
            Path.Combine(ImportRootPath, "Custom", "ARA", "LevelLayout"),
            Path.Combine(ImportRootPath, "ARA", "LevelLayout"),
            ImportRootPath
        };

        foreach (var dir in possibleDirNames)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var pre in prefixes)
            {
                foreach (var suf in suffixes)
                {
                    string candidatePath = Path.Combine(dir, $"{pre}{suf}.json");
                    if (File.Exists(candidatePath))
                    {
                        result.Add(candidatePath);
                    }
                }
            }

            try
            {
                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    foreach (var id in idList)
                    {
                        if (TryMatchesMainLevelLayout(file, id))
                        {
                            result.Add(file);
                        }
                    }
                }
            }
            catch { }
        }

        return result.ToList();
    }

    private static bool TryMatchesMainLevelLayout(string filePath, uint expectedLayoutId)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            var node = JsonNode.Parse(content);
            if (node is JsonObject obj && obj.TryGetPropertyValue("MainLevelLayout", out var layoutNode) && layoutNode != null)
            {
                if (uint.TryParse(layoutNode.ToString(), out uint id) && id == expectedLayoutId)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return false;
    }
}
