using System;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class ChainedPuzzleDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var alarms = new ObservableCollection<AlarmBlock>();

        foreach (var kvp in context.ChainedPuzzleBlocks)
        {
            var puzzleObj = kvp.Value;
            if (puzzleObj == null) continue;

            uint puzzleId = kvp.Key;
            string name = puzzleObj["name"]?.ToString() ?? $"ChainedPuzzle_{puzzleId}";
            string publicName = puzzleObj["PublicAlarmName"]?.ToString() ?? "Alarm";
            bool internalEnabled = puzzleObj["internalEnabled"]?.GetValue<bool>() ?? true;
            bool spawnsEnemies = puzzleObj["TriggerAlarmOnActivate"]?.GetValue<bool>() ?? false;
            bool disableWaveOnComplete = puzzleObj["DisableSurvivalWaveOnComplete"]?.GetValue<bool>() ?? false;
            bool useRandomPositions = puzzleObj["UseRandomPositions"]?.GetValue<bool>() ?? false;
            double wantedDistFromStart = puzzleObj["WantedDistanceFromStartPos"]?.GetValue<double>() ?? 0;
            double wantedDistBetween = puzzleObj["WantedDistanceBetweenPuzzleComponents"]?.GetValue<double>() ?? 0;

            uint waveSettingsId = puzzleObj["SurvivalWaveSettings"]?.GetValue<uint>() ?? 0;
            uint popId = puzzleObj["SurvivalWavePopulation"]?.GetValue<uint>() ?? 0;

            var alarm = new AlarmBlock
            {
                PersistentId = puzzleId,
                Name = name,
                PublicAlarmName = publicName,
                InternalEnabled = internalEnabled,
                SpawnsEnemies = spawnsEnemies,
                DisableSurvivalWaveOnComplete = disableWaveOnComplete,
                UseRandomPositions = useRandomPositions,
                WantedDistanceFromStartPos = wantedDistFromStart,
                WantedDistanceBetweenPuzzleComponents = wantedDistBetween,
                ChainedPuzzle = new ObservableCollection<ChainedPuzzleItem>()
            };

            if (puzzleObj["ChainedPuzzle"] is JsonArray stepsArray)
            {
                foreach (var stepNode in stepsArray)
                {
                    if (stepNode is JsonObject stepObj)
                    {
                        uint pType = stepObj["PuzzleType"]?.GetValue<uint>() ?? 6;
                        alarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = pType });
                    }
                }
            }

            if (popId > 0 && context.SurvivalWavePopulationBlocks.TryGetValue(popId, out var popObj) && popObj != null)
            {
                alarm.WaveRoleWeakling = popObj["WaveRoleWeakling"]?.GetValue<uint>() ?? 0;
                alarm.WaveRoleStandard = popObj["WaveRoleStandard"]?.GetValue<uint>() ?? 0;
                alarm.WaveRoleSpecial = popObj["WaveRoleSpecial"]?.GetValue<uint>() ?? 0;
                alarm.WaveRoleMiniBoss = popObj["WaveRoleMiniBoss"]?.GetValue<uint>() ?? 0;
                alarm.WaveRoleBoss = popObj["WaveRoleBoss"]?.GetValue<uint>() ?? 0;
            }

            if (waveSettingsId > 0 && context.SurvivalWaveSettingsBlocks.TryGetValue(waveSettingsId, out var waveObj) && waveObj != null)
            {
                alarm.PauseBeforeStart = waveObj["m_pauseBeforeStart"]?.GetValue<double>() ?? 0;
                alarm.PauseBetweenGroups = waveObj["m_pauseBetweenGroups"]?.GetValue<double>() ?? 0;
                alarm.WavePauseMin_AtCost = waveObj["m_wavePauseMin_atCost"]?.GetValue<double>() ?? 0;
                alarm.WavePauseMax_AtCost = waveObj["m_wavePauseMax_atCost"]?.GetValue<double>() ?? 0;
                alarm.WavePauseMin = waveObj["m_wavePauseMin"]?.GetValue<double>() ?? 0;
                alarm.WavePauseMax = waveObj["m_wavePauseMax"]?.GetValue<double>() ?? 0;

                if (waveObj["m_populationFilter"] is JsonArray filterArr)
                {
                    foreach (var filterNode in filterArr)
                    {
                        string fStr = filterNode?.ToString() ?? "";
                        if (fStr.Equals("Weakling", StringComparison.OrdinalIgnoreCase)) alarm.FilterWeakling = true;
                        if (fStr.Equals("Standard", StringComparison.OrdinalIgnoreCase)) alarm.FilterStandard = true;
                        if (fStr.Equals("Special", StringComparison.OrdinalIgnoreCase)) alarm.FilterSpecial = true;
                        if (fStr.Equals("MiniBoss", StringComparison.OrdinalIgnoreCase)) alarm.FilterMiniBoss = true;
                        if (fStr.Equals("Boss", StringComparison.OrdinalIgnoreCase)) alarm.FilterBoss = true;
                    }
                }

                string filterTypeStr = waveObj["m_filterType"]?.ToString() ?? "Exclude";
                if (Enum.TryParse<WaveFilterType>(filterTypeStr, true, out var fType))
                {
                    alarm.FilterType = fType;
                }

                alarm.ChanceToRandomizeSpawnDirectionPerWave = waveObj["m_chanceToRandomizeSpawnDirectionPerWave"]?.GetValue<double>() ?? 0;
                alarm.ChanceToRandomizeSpawnDirectionPerGroup = waveObj["m_chanceToRandomizeSpawnDirectionPerGroup"]?.GetValue<double>() ?? 0;
                alarm.OverrideWaveSpawnType = waveObj["m_overrideWaveSpawnType"]?.GetValue<bool>() ?? false;

                string spawnTypeStr = waveObj["m_survivalWaveSpawnType"]?.ToString() ?? "InRelationToClosestAlivePlayer";
                if (Enum.TryParse<SurvivalWaveSpawnType>(spawnTypeStr, true, out var sType))
                {
                    alarm.SurvivalWaveSpawnType = sType;
                }

                alarm.PopulationPointsTotal = waveObj["m_populationPointsTotal"]?.GetValue<double>() ?? 0;
                alarm.PopulationPointsPerWaveStart = waveObj["m_populationPointsPerWaveStart"]?.GetValue<double>() ?? 0;
                alarm.PopulationPointsPerWaveEnd = waveObj["m_populationPointsPerWaveEnd"]?.GetValue<double>() ?? 0;
                alarm.PopulationPointsMinPerGroup = waveObj["m_populationPointsMinPerGroup"]?.GetValue<double>() ?? 0;
                alarm.PopulationPointsPerGroupStart = waveObj["m_populationPointsPerGroupStart"]?.GetValue<double>() ?? 0;
                alarm.PopulationPointsPerGroupEnd = waveObj["m_populationPointsPerGroupEnd"]?.GetValue<double>() ?? 0;
                alarm.PopulationRampOverTime = waveObj["m_populationRampOverTime"]?.GetValue<double>() ?? 0;
            }

            alarms.Add(alarm);
        }

        context.MainVm.Alarms.Alarms.Clear();
        foreach (var a in alarms)
        {
            context.MainVm.Alarms.Alarms.Add(a);
        }
        if (context.MainVm.Alarms.Alarms.Count > 0)
        {
            context.MainVm.Alarms.SelectedAlarm = context.MainVm.Alarms.Alarms[0];
        }
    }
}
