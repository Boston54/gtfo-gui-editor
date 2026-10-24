using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class AlarmService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetDatablocksDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks");
        if (Directory.Exists(outputDir))
        {
            return outputDir;
        }

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks");
        if (Directory.Exists(cwdDir))
        {
            return cwdDir;
        }

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks");
        if (Directory.Exists(cwdProjectDir))
        {
            return cwdProjectDir;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks");
            if (Directory.Exists(candidateVanilla))
            {
                return candidateVanilla;
            }

            dir = dir.Parent;
        }

        return outputDir;
    }

    public static string? FindEnemyDataBlockFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "EnemyDataBlock.json");
        return File.Exists(directPath) ? directPath : null;
    }

    public static string? FindSurvivalWaveSettingsFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "SurvivalWaveSettingsDataBlock.json");
        return File.Exists(directPath) ? directPath : null;
    }

    public static string? FindSurvivalWavePopulationFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "SurvivalWavePopulationDataBlock.json");
        return File.Exists(directPath) ? directPath : null;
    }

    public static string? FindChainedPuzzleFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "ChainedPuzzleDataBlock.json");
        return File.Exists(directPath) ? directPath : null;
    }

    public static string? FindChainedPuzzleTypeFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "ChainedPuzzleTypeDataBlock.json");
        return File.Exists(directPath) ? directPath : null;
    }

    public static List<ChainedPuzzleTypeEntry> LoadChainedPuzzleTypes(string? filePath = null)
    {
        filePath ??= FindChainedPuzzleTypeFile();

        var list = new List<ChainedPuzzleTypeEntry>();

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<ChainedPuzzleTypeDataBlockFile>(json, JsonOptions);
                if (data?.Blocks != null && data.Blocks.Count > 0)
                {
                    list.AddRange(data.Blocks);
                }
            }
            catch
            {
                // Return fallback types if deserialization fails
            }
        }

        if (list.Count == 0)
        {
            list.Add(new ChainedPuzzleTypeEntry { PersistentId = 2, Name = "SecurityScan_Big" });
            list.Add(new ChainedPuzzleTypeEntry { PersistentId = 3, Name = "SecurityScan_Small" });
            list.Add(new ChainedPuzzleTypeEntry { PersistentId = 4, Name = "SecurityScan_Cluster_Small" });
            list.Add(new ChainedPuzzleTypeEntry { PersistentId = 5, Name = "SecurityScan_Cluster_Big" });
            list.Add(new ChainedPuzzleTypeEntry { PersistentId = 6, Name = "SecurityScan_Big_RequireAll" });
        }

        return list;
    }

    public static List<EnemyEntry> LoadEnemies(string? filePath = null)
    {
        filePath ??= FindEnemyDataBlockFile();

        var enemies = new List<EnemyEntry>();

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<EnemyDataBlockFile>(json, JsonOptions);
                if (data?.Blocks != null && data.Blocks.Count > 0)
                {
                    enemies.AddRange(data.Blocks);
                }
            }
            catch
            {
                // Return fallback enemies if deserialization fails
            }
        }

        if (enemies.Count == 0)
        {
            enemies.Add(new EnemyEntry { PersistentId = 13, Name = "Striker_Wave", EnemyType = "Standard" });
            enemies.Add(new EnemyEntry { PersistentId = 21, Name = "Shooter_Wave", EnemyType = "Weakling" });
            enemies.Add(new EnemyEntry { PersistentId = 11, Name = "Shooter_Wave_Spread", EnemyType = "Special" });
            enemies.Add(new EnemyEntry { PersistentId = 16, Name = "Striker_Big_Wave", EnemyType = "MiniBoss" });
            enemies.Add(new EnemyEntry { PersistentId = 18, Name = "Tank", EnemyType = "Boss" });
        }

        return enemies;
    }

    private class PopulationBlock
    {
        [JsonPropertyName("persistentID")]
        public uint PersistentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("WaveRoleWeakling")]
        public uint WaveRoleWeakling { get; set; } = 21;

        [JsonPropertyName("WaveRoleStandard")]
        public uint WaveRoleStandard { get; set; } = 13;

        [JsonPropertyName("WaveRoleSpecial")]
        public uint WaveRoleSpecial { get; set; } = 11;

        [JsonPropertyName("WaveRoleMiniBoss")]
        public uint WaveRoleMiniBoss { get; set; } = 16;

        [JsonPropertyName("WaveRoleBoss")]
        public uint WaveRoleBoss { get; set; } = 18;
    }

    private class SurvivalWavePopulationDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<PopulationBlock> Blocks { get; set; } = new();
    }

    private class SurvivalWaveSettingsDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<AlarmBlock> Blocks { get; set; } = new();
    }

    private class ChainedPuzzleTypeDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<ChainedPuzzleTypeEntry> Blocks { get; set; } = new();
    }

    private class ChainedPuzzleBlock
    {
        [JsonPropertyName("persistentID")]
        public uint PersistentId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("PublicAlarmName")]
        public string? PublicAlarmName { get; set; }

        [JsonPropertyName("TriggerAlarmOnActivate")]
        public bool TriggerAlarmOnActivate { get; set; } = true;

        [JsonPropertyName("SurvivalWaveSettings")]
        public uint SurvivalWaveSettings { get; set; }

        [JsonPropertyName("SurvivalWavePopulation")]
        public uint SurvivalWavePopulation { get; set; }

        [JsonPropertyName("internalEnabled")]
        public bool InternalEnabled { get; set; } = true;

        [JsonPropertyName("WantedDistanceFromStartPos")]
        public double WantedDistanceFromStartPos { get; set; }

        [JsonPropertyName("WantedDistanceBetweenPuzzleComponents")]
        public double WantedDistanceBetweenPuzzleComponents { get; set; }

        [JsonPropertyName("ChainedPuzzle")]
        public List<ChainedPuzzleItem>? ChainedPuzzle { get; set; }
    }

    private class ChainedPuzzleDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<ChainedPuzzleBlock> Blocks { get; set; } = new();
    }

    public static List<AlarmBlock> LoadVanillaAlarms(string? settingsPath = null, string? populationPath = null, string? chainedPuzzlePath = null)
    {
        settingsPath ??= FindSurvivalWaveSettingsFile();
        populationPath ??= FindSurvivalWavePopulationFile();
        chainedPuzzlePath ??= FindChainedPuzzleFile();

        var populations = new Dictionary<uint, PopulationBlock>();
        if (!string.IsNullOrEmpty(populationPath) && File.Exists(populationPath))
        {
            try
            {
                string popJson = File.ReadAllText(populationPath);
                var popFile = JsonSerializer.Deserialize<SurvivalWavePopulationDataBlockFile>(popJson, JsonOptions);
                if (popFile?.Blocks != null)
                {
                    foreach (var block in popFile.Blocks)
                    {
                        populations[block.PersistentId] = block;
                    }
                }
            }
            catch
            {
                // Ignore pop file load failures
            }
        }

        var waveSettingsMap = new Dictionary<uint, AlarmBlock>();
        if (!string.IsNullOrEmpty(settingsPath) && File.Exists(settingsPath))
        {
            try
            {
                string settingsJson = File.ReadAllText(settingsPath);
                var settingsFile = JsonSerializer.Deserialize<SurvivalWaveSettingsDataBlockFile>(settingsJson, JsonOptions);
                if (settingsFile?.Blocks != null)
                {
                    foreach (var block in settingsFile.Blocks)
                    {
                        waveSettingsMap[block.PersistentId] = block;
                    }
                }
            }
            catch
            {
                // Ignore settings file load failures
            }
        }

        var baselinePop = populations.Values.FirstOrDefault(p => string.Equals(p.Name, "Baseline", StringComparison.OrdinalIgnoreCase))
                          ?? populations.Values.FirstOrDefault()
                          ?? new PopulationBlock();

        var alarms = new List<AlarmBlock>();
        if (!string.IsNullOrEmpty(chainedPuzzlePath) && File.Exists(chainedPuzzlePath))
        {
            try
            {
                string puzzleJson = File.ReadAllText(chainedPuzzlePath);
                var puzzleFile = JsonSerializer.Deserialize<ChainedPuzzleDataBlockFile>(puzzleJson, JsonOptions);
                if (puzzleFile?.Blocks != null && puzzleFile.Blocks.Count > 0)
                {
                    foreach (var puzzleBlock in puzzleFile.Blocks)
                    {
                        var alarm = new AlarmBlock();
                        EditorDefaultsService.ApplyAlarmDefaults(alarm);

                        alarm.PersistentId = puzzleBlock.PersistentId;
                        alarm.Name = puzzleBlock.Name ?? $"ChainedPuzzle_{puzzleBlock.PersistentId}";
                        alarm.PublicAlarmName = puzzleBlock.PublicAlarmName ?? string.Empty;
                        alarm.InternalEnabled = puzzleBlock.InternalEnabled;
                        alarm.SpawnsEnemies = puzzleBlock.TriggerAlarmOnActivate;
                        alarm.WantedDistanceFromStartPos = puzzleBlock.WantedDistanceFromStartPos;
                        alarm.WantedDistanceBetweenPuzzleComponents = puzzleBlock.WantedDistanceBetweenPuzzleComponents;

                        if (puzzleBlock.ChainedPuzzle != null && puzzleBlock.ChainedPuzzle.Count > 0)
                        {
                            alarm.ChainedPuzzle = new ObservableCollection<ChainedPuzzleItem>(
                                puzzleBlock.ChainedPuzzle.Select(c => c.Clone())
                            );
                        }
                        else
                        {
                            alarm.ChainedPuzzle = new ObservableCollection<ChainedPuzzleItem>();
                        }

                        // Populate Wave Roles from SurvivalWavePopulation
                        if (puzzleBlock.SurvivalWavePopulation > 0 &&
                            populations.TryGetValue(puzzleBlock.SurvivalWavePopulation, out var pop))
                        {
                            alarm.WaveRoleWeakling = pop.WaveRoleWeakling;
                            alarm.WaveRoleStandard = pop.WaveRoleStandard;
                            alarm.WaveRoleSpecial = pop.WaveRoleSpecial;
                            alarm.WaveRoleMiniBoss = pop.WaveRoleMiniBoss;
                            alarm.WaveRoleBoss = pop.WaveRoleBoss;
                        }
                        else
                        {
                            alarm.WaveRoleWeakling = baselinePop.WaveRoleWeakling;
                            alarm.WaveRoleStandard = baselinePop.WaveRoleStandard;
                            alarm.WaveRoleSpecial = baselinePop.WaveRoleSpecial;
                            alarm.WaveRoleMiniBoss = baselinePop.WaveRoleMiniBoss;
                            alarm.WaveRoleBoss = baselinePop.WaveRoleBoss;
                        }

                        // Populate Wave Settings from SurvivalWaveSettings
                        if (puzzleBlock.SurvivalWaveSettings > 0 &&
                            waveSettingsMap.TryGetValue(puzzleBlock.SurvivalWaveSettings, out var waveSettings))
                        {
                            alarm.PauseBeforeStart = waveSettings.PauseBeforeStart;
                            alarm.PauseBetweenGroups = waveSettings.PauseBetweenGroups;
                            alarm.WavePauseMin_AtCost = waveSettings.WavePauseMin_AtCost;
                            alarm.WavePauseMax_AtCost = waveSettings.WavePauseMax_AtCost;
                            alarm.WavePauseMin = waveSettings.WavePauseMin;
                            alarm.WavePauseMax = waveSettings.WavePauseMax;

                            alarm.FilterType = waveSettings.FilterType;
                            alarm.FilterWeakling = waveSettings.FilterWeakling;
                            alarm.FilterStandard = waveSettings.FilterStandard;
                            alarm.FilterSpecial = waveSettings.FilterSpecial;
                            alarm.FilterMiniBoss = waveSettings.FilterMiniBoss;
                            alarm.FilterBoss = waveSettings.FilterBoss;

                            alarm.ChanceToRandomizeSpawnDirectionPerWave = waveSettings.ChanceToRandomizeSpawnDirectionPerWave;
                            alarm.ChanceToRandomizeSpawnDirectionPerGroup = waveSettings.ChanceToRandomizeSpawnDirectionPerGroup;
                            alarm.OverrideWaveSpawnType = waveSettings.OverrideWaveSpawnType;
                            alarm.SurvivalWaveSpawnType = waveSettings.SurvivalWaveSpawnType;

                            alarm.PopulationPointsTotal = waveSettings.PopulationPointsTotal;
                            alarm.PopulationPointsPerWaveStart = waveSettings.PopulationPointsPerWaveStart;
                            alarm.PopulationPointsPerWaveEnd = waveSettings.PopulationPointsPerWaveEnd;
                            alarm.PopulationPointsMinPerGroup = waveSettings.PopulationPointsMinPerGroup;
                            alarm.PopulationPointsPerGroupStart = waveSettings.PopulationPointsPerGroupStart;
                            alarm.PopulationPointsPerGroupEnd = waveSettings.PopulationPointsPerGroupEnd;
                            alarm.PopulationRampOverTime = waveSettings.PopulationRampOverTime;
                        }

                        alarms.Add(alarm);
                    }
                }
            }
            catch
            {
                // Ignore puzzle load failures
            }
        }

        if (alarms.Count == 0)
        {
            alarms.Add(CreateFreshAlarm(1, "Apex"));
        }

        return alarms;
    }

    public static AlarmBlock CreateFreshAlarm(uint id, string name)
    {
        var alarm = new AlarmBlock
        {
            PersistentId = id,
            Name = name,
            InternalEnabled = true
        };
        EditorDefaultsService.ApplyAlarmDefaults(alarm);
        return alarm;
    }

    public static AlarmBlock CreateAlarmFromTemplate(AlarmBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
