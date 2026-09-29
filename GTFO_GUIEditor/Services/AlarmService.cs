using System;
using System.Collections.Generic;
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
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vanilla", "datablocks");
        if (Directory.Exists(outputDir))
        {
            return outputDir;
        }

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "vanilla", "datablocks");
        if (Directory.Exists(cwdDir))
        {
            return cwdDir;
        }

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "vanilla", "datablocks");
        if (Directory.Exists(cwdProjectDir))
        {
            return cwdProjectDir;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "vanilla", "datablocks");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "vanilla", "datablocks");
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

    private class ChainedPuzzleBlock
    {
        [JsonPropertyName("SurvivalWaveSettings")]
        public uint SurvivalWaveSettings { get; set; }

        [JsonPropertyName("SurvivalWavePopulation")]
        public uint SurvivalWavePopulation { get; set; }
    }

    private class ChainedPuzzleDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<ChainedPuzzleBlock> Blocks { get; set; } = new();
    }

    private static PopulationBlock? FindBestPopulationMatch(
        AlarmBlock alarm,
        Dictionary<uint, PopulationBlock> populations,
        Dictionary<uint, uint> chainedPuzzleMap)
    {
        if (populations.Count == 0)
        {
            return null;
        }

        // Exact name match
        var exactName = populations.Values.FirstOrDefault(p => string.Equals(p.Name, alarm.Name, StringComparison.OrdinalIgnoreCase));
        if (exactName != null)
        {
            return exactName;
        }

        // Specialized substring match (excluding generic "Baseline")
        var nonBaselinePops = populations.Values
            .Where(p => !string.IsNullOrWhiteSpace(p.Name) && !p.Name.Equals("Baseline", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var substringMatch = nonBaselinePops
            .Where(p => p.Name.Length > 2 && alarm.Name.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Name.Length)
            .FirstOrDefault();
        if (substringMatch != null)
        {
            return substringMatch;
        }

        // Specialized token overlap match (e.g. Bullrush, Shadows, Flyers, Tank, Mother, Pouncers, Birthers, Snatchers, SquidBoss, Hybrids, Corrupted)
        var alarmTokens = alarm.Name.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var tokenMatch = nonBaselinePops
            .Select(p => new
            {
                Population = p,
                Score = alarmTokens.Count(t => t.Length > 2 && p.Name.Contains(t, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => Math.Abs(x.Population.Name.Length - alarm.Name.Length))
            .Select(x => x.Population)
            .FirstOrDefault();
        if (tokenMatch != null)
        {
            return tokenMatch;
        }

        // Direct PersistentId match (if IDs match directly)
        if (populations.TryGetValue(alarm.PersistentId, out var idMatch))
        {
            return idMatch;
        }

        // 5. Chained Puzzle explicit mapping
        if (chainedPuzzleMap.TryGetValue(alarm.PersistentId, out uint popId) && populations.TryGetValue(popId, out var popFromPuzzle))
        {
            return popFromPuzzle;
        }

        return null;
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

        var chainedPuzzleMap = new Dictionary<uint, uint>();
        if (!string.IsNullOrEmpty(chainedPuzzlePath) && File.Exists(chainedPuzzlePath))
        {
            try
            {
                string puzzleJson = File.ReadAllText(chainedPuzzlePath);
                var puzzleFile = JsonSerializer.Deserialize<ChainedPuzzleDataBlockFile>(puzzleJson, JsonOptions);
                if (puzzleFile?.Blocks != null)
                {
                    foreach (var block in puzzleFile.Blocks)
                    {
                        if (block.SurvivalWaveSettings > 0 && block.SurvivalWavePopulation > 0)
                        {
                            chainedPuzzleMap[block.SurvivalWaveSettings] = block.SurvivalWavePopulation;
                        }
                    }
                }
            }
            catch
            {
                // Ignore puzzle load failures
            }
        }

        var baselinePop = populations.Values.FirstOrDefault(p => string.Equals(p.Name, "Baseline", StringComparison.OrdinalIgnoreCase))
                          ?? populations.Values.FirstOrDefault()
                          ?? new PopulationBlock();

        var alarms = new List<AlarmBlock>();
        if (!string.IsNullOrEmpty(settingsPath) && File.Exists(settingsPath))
        {
            try
            {
                string settingsJson = File.ReadAllText(settingsPath);
                var settingsFile = JsonSerializer.Deserialize<SurvivalWaveSettingsDataBlockFile>(settingsJson, JsonOptions);
                if (settingsFile?.Blocks != null && settingsFile.Blocks.Count > 0)
                {
                    foreach (var alarm in settingsFile.Blocks)
                    {
                        var pop = FindBestPopulationMatch(alarm, populations, chainedPuzzleMap) ?? baselinePop;

                        alarm.WaveRoleWeakling = pop.WaveRoleWeakling;
                        alarm.WaveRoleStandard = pop.WaveRoleStandard;
                        alarm.WaveRoleSpecial = pop.WaveRoleSpecial;
                        alarm.WaveRoleMiniBoss = pop.WaveRoleMiniBoss;
                        alarm.WaveRoleBoss = pop.WaveRoleBoss;

                        alarms.Add(alarm);
                    }
                }
            }
            catch
            {
                // Fallback to fresh alarm
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
        return new AlarmBlock
        {
            PersistentId = id,
            Name = name,
            InternalEnabled = true
        };
    }

    public static AlarmBlock CreateAlarmFromTemplate(AlarmBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
