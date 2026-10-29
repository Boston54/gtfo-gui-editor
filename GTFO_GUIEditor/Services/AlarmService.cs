using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class AlarmService
{
    public class PopulationBlock
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

    public class SurvivalWavePopulationDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<PopulationBlock> Blocks { get; set; } = new();
    }

    public class SurvivalWaveSettingsDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<AlarmBlock> Blocks { get; set; } = new();
    }

    private class ChainedPuzzleTypeDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<ChainedPuzzleTypeEntry> Blocks { get; set; } = new();
    }

    public class ChainedPuzzleBlock
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

        [JsonPropertyName("DisableSurvivalWaveOnComplete")]
        public bool DisableSurvivalWaveOnComplete { get; set; } = true;

        [JsonPropertyName("UseRandomPositions")]
        public bool UseRandomPositions { get; set; }

        [JsonPropertyName("internalEnabled")]
        public bool InternalEnabled { get; set; } = true;

        [JsonPropertyName("WantedDistanceFromStartPos")]
        public double WantedDistanceFromStartPos { get; set; }

        [JsonPropertyName("WantedDistanceBetweenPuzzleComponents")]
        public double WantedDistanceBetweenPuzzleComponents { get; set; }

        [JsonPropertyName("ChainedPuzzle")]
        public List<ChainedPuzzleItem>? ChainedPuzzle { get; set; }
    }

    public class ChainedPuzzleDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<ChainedPuzzleBlock> Blocks { get; set; } = new();
    }

    public static List<AlarmBlock> BuildAlarms(
        SurvivalWavePopulationDataBlockFile? populationFile,
        SurvivalWaveSettingsDataBlockFile? settingsFile,
        ChainedPuzzleDataBlockFile? puzzleFile)
    {
        var populations = populationFile?.Blocks?
            .GroupBy(block => block.PersistentId)
            .ToDictionary(group => group.Key, group => group.Last()) ?? new Dictionary<uint, PopulationBlock>();

        var waveSettingsMap = settingsFile?.Blocks?
            .GroupBy(block => block.PersistentId)
            .ToDictionary(group => group.Key, group => group.Last()) ?? new Dictionary<uint, AlarmBlock>();

        var baselinePop = populations.Values.FirstOrDefault(population =>
                              string.Equals(population.Name, "Baseline", StringComparison.OrdinalIgnoreCase))
                          ?? populations.Values.FirstOrDefault()
                          ?? new PopulationBlock();

        var alarms = new List<AlarmBlock>();
        foreach (var puzzleBlock in puzzleFile?.Blocks ?? new List<ChainedPuzzleBlock>())
        {
            var alarm = new AlarmBlock();
            EditorDefaultsService.ApplyAlarmDefaults(alarm);

            alarm.PersistentId = puzzleBlock.PersistentId;
            alarm.Name = puzzleBlock.Name ?? $"ChainedPuzzle_{puzzleBlock.PersistentId}";
            alarm.PublicAlarmName = puzzleBlock.PublicAlarmName ?? string.Empty;
            alarm.InternalEnabled = puzzleBlock.InternalEnabled;
            alarm.SpawnsEnemies = puzzleBlock.TriggerAlarmOnActivate;
            alarm.DisableSurvivalWaveOnComplete = puzzleBlock.DisableSurvivalWaveOnComplete;
            alarm.UseRandomPositions = puzzleBlock.UseRandomPositions;
            alarm.WantedDistanceFromStartPos = puzzleBlock.WantedDistanceFromStartPos;
            alarm.WantedDistanceBetweenPuzzleComponents = puzzleBlock.WantedDistanceBetweenPuzzleComponents;
            alarm.ChainedPuzzle = new ObservableCollection<ChainedPuzzleItem>(
                puzzleBlock.ChainedPuzzle?.Select(item => item.Clone()) ?? Enumerable.Empty<ChainedPuzzleItem>());

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
