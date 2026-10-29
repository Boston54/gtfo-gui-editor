using System.Text.Json.Nodes;

namespace GTFO_GUIEditor.Services;

public class ChainedPuzzleDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        foreach (var alarm in context.MainVm.Alarms.Alarms)
        {
            if (alarm == null) continue;

            uint popId = 0;
            uint waveSettingsId = 0;

            if (alarm.SpawnsEnemies)
            {
                popId = context.SurvivalWavePopulationAlloc.Allocate(alarm.PersistentId > 0 ? alarm.PersistentId : null);
                var popObj = new JsonObject
                {
                    ["WaveRoleWeakling"] = alarm.WaveRoleWeakling,
                    ["WaveRoleStandard"] = alarm.WaveRoleStandard,
                    ["WaveRoleSpecial"] = alarm.WaveRoleSpecial,
                    ["WaveRoleMiniBoss"] = alarm.WaveRoleMiniBoss,
                    ["WaveRoleBoss"] = alarm.WaveRoleBoss,
                    ["name"] = string.IsNullOrWhiteSpace(alarm.Name)
                        ? $"SurvivalWavePopulation_{popId}"
                        : $"{alarm.Name}_Pop",
                    ["internalEnabled"] = true,
                    ["persistentID"] = popId
                };
                context.SurvivalWavePopulationBlocks.Add(popObj);

                var popFilterArray = new JsonArray();
                if (alarm.FilterWeakling) popFilterArray.Add("Weakling");
                if (alarm.FilterStandard) popFilterArray.Add("Standard");
                if (alarm.FilterSpecial) popFilterArray.Add("Special");
                if (alarm.FilterMiniBoss) popFilterArray.Add("MiniBoss");
                if (alarm.FilterBoss) popFilterArray.Add("Boss");

                waveSettingsId = context.SurvivalWaveSettingsAlloc.Allocate(alarm.PersistentId > 0 ? alarm.PersistentId : null);
                var waveObj = new JsonObject
                {
                    ["m_pauseBeforeStart"] = alarm.PauseBeforeStart,
                    ["m_pauseBetweenGroups"] = alarm.PauseBetweenGroups,
                    ["m_wavePauseMin_atCost"] = alarm.WavePauseMin_AtCost,
                    ["m_wavePauseMax_atCost"] = alarm.WavePauseMax_AtCost,
                    ["m_wavePauseMin"] = alarm.WavePauseMin,
                    ["m_wavePauseMax"] = alarm.WavePauseMax,
                    ["m_populationFilter"] = popFilterArray,
                    ["m_filterType"] = alarm.FilterType.ToString(),
                    ["m_chanceToRandomizeSpawnDirectionPerWave"] = alarm.ChanceToRandomizeSpawnDirectionPerWave,
                    ["m_chanceToRandomizeSpawnDirectionPerGroup"] = alarm.ChanceToRandomizeSpawnDirectionPerGroup,
                    ["m_overrideWaveSpawnType"] = alarm.OverrideWaveSpawnType,
                    ["m_survivalWaveSpawnType"] = alarm.SurvivalWaveSpawnType.ToString(),
                    ["m_populationPointsTotal"] = alarm.PopulationPointsTotal,
                    ["m_populationPointsPerWaveStart"] = alarm.PopulationPointsPerWaveStart,
                    ["m_populationPointsPerWaveEnd"] = alarm.PopulationPointsPerWaveEnd,
                    ["m_populationPointsMinPerGroup"] = alarm.PopulationPointsMinPerGroup,
                    ["m_populationPointsPerGroupStart"] = alarm.PopulationPointsPerGroupStart,
                    ["m_populationPointsPerGroupEnd"] = alarm.PopulationPointsPerGroupEnd,
                    ["m_populationRampOverTime"] = alarm.PopulationRampOverTime,
                    ["name"] = string.IsNullOrWhiteSpace(alarm.Name)
                        ? $"SurvivalWaveSettings_{waveSettingsId}"
                        : $"{alarm.Name}_Settings",
                    ["internalEnabled"] = true,
                    ["persistentID"] = waveSettingsId
                };
                context.SurvivalWaveSettingsBlocks.Add(waveObj);
            }

            var puzzleSteps = new JsonArray();
            if (alarm.ChainedPuzzle != null && alarm.ChainedPuzzle.Count > 0)
            {
                foreach (var step in alarm.ChainedPuzzle)
                {
                    puzzleSteps.Add(new JsonObject { ["PuzzleType"] = step.PuzzleType });
                }
            }
            else
            {
                puzzleSteps.Add(new JsonObject { ["PuzzleType"] = 6 });
            }

            uint puzzleId = context.ChainedPuzzleAlloc.Allocate(alarm.PersistentId > 0 ? alarm.PersistentId : null);
            var puzzleObj = new JsonObject
            {
                ["PublicAlarmName"] = string.IsNullOrWhiteSpace(alarm.PublicAlarmName)
                    ? "Alarm"
                    : alarm.PublicAlarmName,
                ["TriggerAlarmOnActivate"] = alarm.SpawnsEnemies,
                ["SurvivalWaveSettings"] = waveSettingsId,
                ["SurvivalWavePopulation"] = popId,
                ["SurvivalWaveAreaDistance"] = 2,
                ["DisableSurvivalWaveOnComplete"] = alarm.DisableSurvivalWaveOnComplete,
                ["UseRandomPositions"] = alarm.UseRandomPositions,
                ["WantedDistanceFromStartPos"] = alarm.WantedDistanceFromStartPos,
                ["WantedDistanceBetweenPuzzleComponents"] = alarm.WantedDistanceBetweenPuzzleComponents,
                ["ChainedPuzzle"] = puzzleSteps,
                ["OnlyShowHUDWhenPlayerIsClose"] = false,
                ["AlarmSoundStart"] = 3339129407,
                ["AlarmSoundStop"] = 42633153,
                ["name"] = string.IsNullOrWhiteSpace(alarm.Name) ? $"ChainedPuzzle_{puzzleId}" : alarm.Name,
                ["internalEnabled"] = true,
                ["persistentID"] = puzzleId
            };
            context.ChainedPuzzleBlocks.Add(puzzleObj);
        }

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_ChainedPuzzleDataBlock_bin.json",
            context.ChainedPuzzleBlocks);

        context.DatablocksExportedCount++;

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_ChainedPuzzleTypeDataBlock_bin.json",
            context.VanillaChainedPuzzleTypeBlocks);

        context.DatablocksExportedCount++;
    }
}
