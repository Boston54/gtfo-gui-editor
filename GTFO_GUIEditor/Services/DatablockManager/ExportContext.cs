using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Encapsulates contextual information, ID allocators, caches, and shared buffers for datablock export operations.
/// </summary>
public class ExportContext
{
    public MainWindowViewModel MainVm { get; }
    public string ExportRootPath { get; }
    public string LgTunerDirectory { get; }
    public string SchemasDirectory { get; }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    // ID Allocators
    public PersistentIdAllocator TextAlloc { get; } = new(100000);
    public PersistentIdAllocator EnemyGroupAlloc { get; } = new(100000);
    public PersistentIdAllocator SurvivalWaveSettingsAlloc { get; } = new(100000);
    public PersistentIdAllocator SurvivalWavePopulationAlloc { get; } = new(100000);
    public PersistentIdAllocator ChainedPuzzleAlloc { get; } = new(100000);
    public PersistentIdAllocator WardenObjectiveAlloc { get; } = new(100000);
    public PersistentIdAllocator DimensionAlloc { get; } = new(100000);
    public PersistentIdAllocator LevelLayoutAlloc { get; } = new(100000);
    public PersistentIdAllocator LightSettingsAlloc { get; } = new(100000);
    public PersistentIdAllocator FogSettingsAlloc { get; } = new(100000);
    public PersistentIdAllocator ComplexResourceAlloc { get; } = new(100000);
    public PersistentIdAllocator ExpeditionBalanceAlloc { get; } = new(100000);
    public PersistentIdAllocator RundownAlloc { get; } = new(100000);

    // Vanilla datablock blocks
    public List<JsonObject> VanillaTextBlocks { get; private set; } = new();
    public List<JsonObject> VanillaEnemyBlocks { get; private set; } = new();
    public List<JsonObject> VanillaChainedPuzzleTypeBlocks { get; private set; } = new();
    public List<JsonObject> VanillaExpeditionBalanceBlocks { get; private set; } = new();
    public List<JsonObject> VanillaChainedPuzzleBlocks { get; private set; } = new();
    public List<JsonObject> VanillaEnemyGroupBlocks { get; private set; } = new();
    public List<JsonObject> VanillaSurvivalWaveSettingsBlocks { get; private set; } = new();
    public List<JsonObject> VanillaSurvivalWavePopulationBlocks { get; private set; } = new();

    // Export output containers
    public List<JsonObject> TextBlocks { get; } = new();
    public List<JsonObject> EnemyGroupBlocks { get; } = new();
    public List<JsonObject> SurvivalWaveSettingsBlocks { get; } = new();
    public List<JsonObject> SurvivalWavePopulationBlocks { get; } = new();
    public List<JsonObject> ChainedPuzzleBlocks { get; } = new();
    public List<JsonObject> WardenObjectiveBlocks { get; } = new();
    public List<JsonObject> LevelLayoutBlocks { get; } = new();
    public List<JsonObject> DimensionBlocks { get; } = new();
    public List<JsonObject> ExpeditionBalanceBlocks { get; } = new();
    public List<JsonObject> ComplexResourceSetBlocks { get; } = new();
    public List<JsonObject> LightSettingsBlocks { get; } = new();
    public List<JsonObject> FogSettingsBlocks { get; } = new();
    public List<JsonObject> ConsumableDistributionBlocks { get; } = new();
    public List<JsonObject> GameSetupBlocks { get; } = new();
    public List<JsonObject> RundownBlocks { get; } = new();

    // Cross-referencing caches
    public Dictionary<LevelLayoutBlock, uint> LayoutIds { get; } = new();
    public Dictionary<LevelLayoutBlock, uint> SecondaryLayoutIds { get; } = new();
    public Dictionary<LevelLayoutBlock, uint> ThirdLayoutIds { get; } = new();
    public Dictionary<LevelLayoutBlock, uint> LevelCrsIds { get; } = new();
    public Dictionary<LevelLayoutBlock, uint> LevelExpeditionBalanceIds { get; } = new();
    public Dictionary<LevelLayoutBlock, (uint Main, uint Secondary, uint Overload, List<uint> Chained)> ObjectiveIds { get; } = new();
    public Dictionary<uint, double> EnemyCosts { get; private set; } = new();

    private readonly Dictionary<string, uint> _bloodGroupCache = new();

    public int DatablocksExportedCount { get; set; }
    public int LevelGenFilesExportedCount { get; set; }

    public ExportContext(MainWindowViewModel mainVm, string exportRootPath)
    {
        MainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
        ExportRootPath = exportRootPath ?? throw new ArgumentNullException(nameof(exportRootPath));
        LgTunerDirectory = Path.Combine(exportRootPath, "Custom", "LGTunerAdvanced");
        SchemasDirectory = Path.Combine(exportRootPath, ".schemas");

        InitializeVanillaData();
    }

    private void InitializeVanillaData()
    {
        VanillaTextBlocks = LoadVanillaBlocksArray("TextDataBlock.json");
        RegisterExistingIds(VanillaTextBlocks, TextAlloc);
        TextBlocks.AddRange(VanillaTextBlocks);

        VanillaEnemyBlocks = LoadVanillaBlocksArray("EnemyDataBlock.json");
        VanillaChainedPuzzleTypeBlocks = LoadVanillaBlocksArray("ChainedPuzzleTypeDataBlock.json");

        VanillaExpeditionBalanceBlocks = LoadVanillaBlocksArray("ExpeditionBalanceDataBlock.json");
        RegisterExistingIds(VanillaExpeditionBalanceBlocks, ExpeditionBalanceAlloc);

        VanillaChainedPuzzleBlocks = LoadVanillaBlocksArray("ChainedPuzzleDataBlock.json");
        RegisterExistingIds(VanillaChainedPuzzleBlocks, ChainedPuzzleAlloc);
        ChainedPuzzleBlocks.AddRange(VanillaChainedPuzzleBlocks);

        VanillaEnemyGroupBlocks = LoadVanillaBlocksArray("EnemyGroupDataBlock.json");
        RegisterExistingIds(VanillaEnemyGroupBlocks, EnemyGroupAlloc);

        VanillaSurvivalWaveSettingsBlocks = LoadVanillaBlocksArray("SurvivalWaveSettingsDataBlock.json");
        RegisterExistingIds(VanillaSurvivalWaveSettingsBlocks, SurvivalWaveSettingsAlloc);

        VanillaSurvivalWavePopulationBlocks = LoadVanillaBlocksArray("SurvivalWavePopulationDataBlock.json");
        RegisterExistingIds(VanillaSurvivalWavePopulationBlocks, SurvivalWavePopulationAlloc);

        EnemyCosts = LoadEnemyCosts(VanillaEnemyBlocks);
    }

    public uint SynthesizeText(string? text, string description = "")
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        if (uint.TryParse(text, out uint existingId) && existingId > 0)
        {
            return existingId;
        }

        uint newId = TextAlloc.Allocate();
        var textObj = new JsonObject
        {
            ["SkipLocalization"] = true,
            ["MachineTranslation"] = false,
            ["English"] = text,
            ["Description"] = description,
            ["CharacterMetaData"] = 1,
            ["ExportVersion"] = 1,
            ["ImportVersion"] = 1,
            ["name"] = $"CustomText_{newId}",
            ["internalEnabled"] = true,
            ["persistentID"] = newId
        };
        TextBlocks.Add(textObj);
        return newId;
    }

    public uint SynthesizeEnemyGroup(IEnumerable<EnemyGroupItem> items, string groupSuffix)
    {
        var itemList = items?.Where(i => i != null && i.Count > 0).ToList() ?? new List<EnemyGroupItem>();
        if (itemList.Count == 0) return 0;

        string sig = string.Join(";", itemList.OrderBy(i => i.EnemyId).Select(i => $"{i.EnemyId}:{i.Count}"));
        if (_bloodGroupCache.TryGetValue(sig, out uint cachedId))
        {
            return cachedId;
        }

        double totalScore = 0;
        foreach (var item in itemList)
        {
            double cost = EnemyCosts.GetValueOrDefault(item.EnemyId, 1.0);
            totalScore += cost * item.Count;
        }

        var rolesArray = new JsonArray();
        foreach (var item in itemList)
        {
            string role = GetEnemyRoleName(item.EnemyId);
            double cost = EnemyCosts.GetValueOrDefault(item.EnemyId, 1.0);
            double itemTotalScore = cost * item.Count;
            double proportion = totalScore > 0 ? (itemTotalScore / totalScore) : (1.0 / itemList.Count);
            string dist = MapToDistributionEnum(proportion);

            rolesArray.Add(new JsonObject
            {
                ["Enemy"] = item.EnemyId,
                ["Role"] = role,
                ["Distribution"] = dist,
                ["DistributionValue"] = proportion
            });
        }

        uint newGroupId = EnemyGroupAlloc.Allocate();
        var groupObj = new JsonObject
        {
            ["MaxScore"] = totalScore > 0 ? totalScore : 10.0,
            ["Roles"] = rolesArray,
            ["name"] = $"BloodDoor_Group_{groupSuffix}_{newGroupId}",
            ["internalEnabled"] = true,
            ["persistentID"] = newGroupId
        };

        EnemyGroupBlocks.Add(groupObj);
        _bloodGroupCache[sig] = newGroupId;
        return newGroupId;
    }

    public static List<JsonObject> LoadVanillaBlocksArray(string fileName)
    {
        return DatablockManager.Instance.GetRawBlocks(fileName);
    }

    public static JsonArray? LoadVanillaHeadersArray(string fileName)
    {
        return DatablockManager.Instance.GetRawHeaders(fileName);
    }

    public static void RegisterExistingIds(IEnumerable<JsonObject> blocks, PersistentIdAllocator allocator)
    {
        foreach (var b in blocks)
        {
            if (b.TryGetPropertyValue("persistentID", out var idNode) && idNode != null)
            {
                if (uint.TryParse(idNode.ToString(), out uint id))
                {
                    allocator.RegisterExisting(id);
                }
            }
        }
    }

    private static Dictionary<uint, double> LoadEnemyCosts(IEnumerable<JsonObject> enemyBlocks)
    {
        var costs = new Dictionary<uint, double>
        {
            [24] = 1.0, // Striker
            [26] = 1.0, // Shooter
            [35] = 4.0, // Big Striker
            [36] = 4.0, // Big Shooter
            [40] = 10.0, // Mother / Tank
            [41] = 1.0, // Shadow
            [42] = 4.0, // Big Shadow
            [38] = 5.0, // Scout
        };

        foreach (var eb in enemyBlocks)
        {
            if (eb.TryGetPropertyValue("persistentID", out var idNode) && idNode != null &&
                uint.TryParse(idNode.ToString(), out uint id))
            {
                if (!costs.ContainsKey(id))
                {
                    costs[id] = 1.0;
                }
            }
        }

        return costs;
    }

    private static string GetEnemyRoleName(uint enemyId)
    {
        return enemyId switch
        {
            24 => "Melee",
            26 => "Ranged",
            35 => "MeleeLarge",
            36 => "RangedLarge",
            40 => "Boss",
            41 => "Melee",
            42 => "MeleeLarge",
            38 => "MiniBoss",
            _ => "Melee"
        };
    }

    private static string MapToDistributionEnum(double proportion)
    {
        if (proportion >= 0.875) return "Rel_100";
        if (proportion >= 0.625) return "Rel_75";
        if (proportion >= 0.375) return "Rel_50";
        if (proportion >= 0.20) return "Rel_25";
        if (proportion >= 0.125) return "Rel_15";
        if (proportion >= 0.075) return "Rel_10";
        return "Rel_05";
    }

    public static void WriteDatablockFile(string rootDir, string fileName, IEnumerable<JsonObject> blocks, JsonArray? customHeaders = null)
    {
        string fullPath = Path.Combine(rootDir, fileName);
        var blocksArray = new JsonArray();
        foreach (var b in blocks)
        {
            blocksArray.Add(JsonNode.Parse(b.ToJsonString()));
        }

        var wrapper = new JsonObject
        {
            ["Headers"] = customHeaders ?? new JsonArray(),
            ["Blocks"] = blocksArray
        };

        string json = wrapper.ToJsonString(JsonOptions);
        File.WriteAllText(fullPath, json);
    }
}
