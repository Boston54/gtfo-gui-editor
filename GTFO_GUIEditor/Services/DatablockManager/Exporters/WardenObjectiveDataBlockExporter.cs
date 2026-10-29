using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class WardenObjectiveDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var wardenObjectiveBlocks = new List<JsonObject>();

        foreach (var level in context.MainVm.LevelLayout.Levels)
        {
            if (level == null) continue;

            uint mainObjId = context.WardenObjectiveAlloc.Allocate();
            var mainChainedIds = new List<uint> { mainObjId };

            var mainObj = CompileWardenObjective(
                level.MainSector.PrimaryObjective,
                mainObjId,
                context.SynthesizeText,
                context.SurvivalWaveSettingsAlloc,
                context.SurvivalWavePopulationAlloc,
                context.SurvivalWaveSettingsBlocks,
                context.SurvivalWavePopulationBlocks);
            wardenObjectiveBlocks.Add(mainObj);

            if (level.MainSector.Objectives.Count > 1)
            {
                for (int i = 1; i < level.MainSector.Objectives.Count; i++)
                {
                    uint chainedId = context.WardenObjectiveAlloc.Allocate();
                    mainChainedIds.Add(chainedId);
                    var chainedObj = CompileWardenObjective(
                        level.MainSector.Objectives[i],
                        chainedId,
                        context.SynthesizeText,
                        context.SurvivalWaveSettingsAlloc,
                        context.SurvivalWavePopulationAlloc,
                        context.SurvivalWaveSettingsBlocks,
                        context.SurvivalWavePopulationBlocks);
                    wardenObjectiveBlocks.Add(chainedObj);
                }
            }

            uint secObjId = 0;
            if (level.SecondarySector?.PrimaryObjective != null)
            {
                secObjId = context.WardenObjectiveAlloc.Allocate();
                var secObj = CompileWardenObjective(
                    level.SecondarySector.PrimaryObjective,
                    secObjId,
                    context.SynthesizeText,
                    context.SurvivalWaveSettingsAlloc,
                    context.SurvivalWavePopulationAlloc,
                    context.SurvivalWaveSettingsBlocks,
                    context.SurvivalWavePopulationBlocks);
                wardenObjectiveBlocks.Add(secObj);
            }

            uint thirdObjId = 0;
            if (level.OverloadSector?.PrimaryObjective != null)
            {
                thirdObjId = context.WardenObjectiveAlloc.Allocate();
                var thirdObj = CompileWardenObjective(
                    level.OverloadSector.PrimaryObjective,
                    thirdObjId,
                    context.SynthesizeText,
                    context.SurvivalWaveSettingsAlloc,
                    context.SurvivalWavePopulationAlloc,
                    context.SurvivalWaveSettingsBlocks,
                    context.SurvivalWavePopulationBlocks);
                wardenObjectiveBlocks.Add(thirdObj);
            }

            context.ObjectiveIds[level] = (mainObjId, secObjId, thirdObjId, mainChainedIds);
        }

        context.WardenObjectiveBlocks.Clear();
        context.WardenObjectiveBlocks.AddRange(wardenObjectiveBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_WardenObjectiveDataBlock_bin.json",
            context.WardenObjectiveBlocks);

        context.DatablocksExportedCount++;
    }

    public static JsonObject CompileWardenObjective(
        WardenObjective sector,
        uint persistentId,
        Func<string?, string, uint> textSynth,
        PersistentIdAllocator? waveSettingsAlloc = null,
        PersistentIdAllocator? wavePopAlloc = null,
        List<JsonObject>? waveSettingsBlocks = null,
        List<JsonObject>? wavePopBlocks = null)
    {
        var retrieveItemsArray = new JsonArray();
        if (sector.ObjectiveType == ObjectiveType.RetrieveBigItems)
        {
            int itemId = (int)sector.RetrieveItem;
            int count = Math.Max(1, sector.RetrieveItemPlacements.Count);
            for (int i = 0; i < count; i++)
            {
                retrieveItemsArray.Add(itemId);
            }
        }

        var postCommandOutputArray = new JsonArray();
        if (!string.IsNullOrWhiteSpace(sector.PostCommandOutput))
        {
            var lines = sector.PostCommandOutput.Split(new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                postCommandOutputArray.Add(line);
            }
        }

        var centralFogSteps = new JsonArray();
        if (sector.ObjectiveType == ObjectiveType.CentralGeneratorCluster &&
            sector.CentralGeneratorClusterFogTransitions != null)
        {
            foreach (var step in sector.CentralGeneratorClusterFogTransitions)
            {
                if (step == null) continue;
                centralFogSteps.Add(new JsonObject
                {
                    ["m_fogDataId"] = step.FogSetting,
                    ["m_transitionToTime"] = step.TransitionDuration
                });
            }
        }

        var reactorWaves = new JsonArray();
        if ((sector.ObjectiveType == ObjectiveType.Reactor_Startup ||
             sector.ObjectiveType == ObjectiveType.Reactor_Shutdown) && sector.ReactorStartupStages != null)
        {
            foreach (var stage in sector.ReactorStartupStages)
            {
                if (stage == null) continue;
                reactorWaves.Add(new JsonObject
                {
                    ["Warmup"] = stage.Warmup,
                    ["WarmupFail"] = stage.WarmupFail,
                    ["Wave"] = stage.Wave,
                    ["Verify"] = stage.Verify,
                    ["VerifyFail"] = stage.VerifyFail,
                    ["VerifyInOtherZone"] = stage.VerifyInOtherZone,
                    ["ZoneForVerification"] = $"Zone_{stage.ZoneForVerification}",
                    ["EnemyWaves"] = new JsonArray(),
                    ["Events"] = new JsonArray()
                });
            }
        }

        return new JsonObject
        {
            ["Type"] = sector.ObjectiveType.ToString(),
            ["Header"] = textSynth(sector.Header, "Objective Header"),
            ["MainObjective"] = textSynth(sector.MainObjective, "Main Objective"),
            ["WardenObjectiveSpecialUpdateType"] = "None",
            ["GenericItemFromStart"] = 0,
            ["DoNotMarkPickupItemsAsWardenObjectives"] = false,
            ["OverrideNoRequiredItemsForExit"] = false,
            ["FindLocationInfo"] = textSynth(sector.FindLocationInfo, "Find Location Info"),
            ["FindLocationInfoHelp"] = textSynth(sector.FindLocationInfoHelp, "Find Location Info Help"),
            ["GoToZone"] = textSynth(sector.GoToZone, "Go To Zone"),
            ["GoToZoneHelp"] = 0,
            ["InZoneFindItem"] = textSynth(sector.InZoneFindItem, "In Zone Find Item"),
            ["InZoneFindItemHelp"] = 0,
            ["SolveItem"] = textSynth(sector.SolveItem, "Solve Item"),
            ["SolveItemHelp"] = 0,
            ["GoToWinCondition_Elevator"] = textSynth("Return to the elevator", "Win Condition Elevator"),
            ["GoToWinConditionHelp_Elevator"] = 0,
            ["GoToWinCondition_CustomGeo"] = textSynth("Reach the extraction zone", "Win Condition Geo"),
            ["GoToWinConditionHelp_CustomGeo"] = 0,
            ["GoToWinCondition_ToMainLayer"] = 0,
            ["GoToWinConditionHelp_ToMainLayer"] = 0,
            ["ShowHelpDelay"] = 180.0,
            ["WavesOnElevatorLand"] = SerializeGenericWaveList(sector.WavesOnElevatorLand, $"Obj_{persistentId}_ElevatorLand", waveSettingsAlloc, wavePopAlloc, waveSettingsBlocks, wavePopBlocks),
            ["EventsOnElevatorLand"] = SerializeWardenEvents(sector.EventsOnElevatorLand, textSynth),
            ["WaveOnElevatorWardenIntel"] = 0,
            ["FogTransitionDataOnElevatorLand"] = 0,
            ["FogTransitionDurationOnElevatorLand"] = 0.0,
            ["OnActivateOnSolveItem"] = false,
            ["WavesOnActivate"] = SerializeGenericWaveList(sector.WavesOnActivate, $"Obj_{persistentId}_Activate", waveSettingsAlloc, wavePopAlloc, waveSettingsBlocks, wavePopBlocks),
            ["EventsOnActivate"] =
                SerializeWardenEvents(
                    sector.EventsOnActivate.Count > 0 ? sector.EventsOnActivate : sector.EventsOnActivation, textSynth),
            ["StopAllWavesBeforeGotoWin"] = sector.StopAllWavesBeforeGotoWin,
            ["WavesOnGotoWin"] = SerializeGenericWaveList(sector.WavesOnGotoWin, $"Obj_{persistentId}_GotoWin", waveSettingsAlloc, wavePopAlloc, waveSettingsBlocks, wavePopBlocks),
            ["WaveOnGotoWinTrigger"] = sector.WaveOnGotoWinTrigger.ToString(),
            ["EventsOnGotoWin"] = SerializeWardenEvents(sector.EventsOnGotoWin, textSynth),
            ["EventsOnGotoWinTrigger"] = "OnObjectiveCompleted",
            ["FogTransitionDataOnGotoWin"] = sector.FogTransitionDataOnGotoWin,
            ["FogTransitionDurationOnGotoWin"] = sector.FogTransitionDataOnGotoWinDuration,
            ["ChainedPuzzleToActive"] = sector.ChainedPuzzleToActive,
            ["ChainedPuzzleMidObjective"] = sector.ChainedPuzzleMidObjective,
            ["ChainedPuzzleAtExit"] = 11,
            ["ChainedPuzzleAtExitScanSpeedMultiplier"] = 1.0,
            ["Gather_RequiredCount"] = sector.ObjectiveType == ObjectiveType.GatherSmallItems
                ? Math.Max(1, sector.GatherItemPlacements.Count)
                : -1,
            ["Gather_ItemId"] = sector.ObjectiveType == ObjectiveType.GatherSmallItems ? (int)sector.GatherItem : 0,
            ["Gather_SpawnCount"] = sector.ObjectiveType == ObjectiveType.GatherSmallItems
                ? Math.Max(1, sector.GatherItemPlacements.Count)
                : 0,
            ["Gather_MaxPerZone"] =
                sector.ObjectiveType == ObjectiveType.GatherSmallItems ? sector.GatherMaxPerZone : 0,
            ["Retrieve_Items"] = retrieveItemsArray,
            ["ReactorWaves"] = reactorWaves,
            ["LightsOnFromBeginning"] = false,
            ["LightsOnDuringIntro"] = false,
            ["LightsOnWhenStartupComplete"] = false,
            ["DoNotSolveObjectiveOnReactorComplete"] = sector.DoNotSolveObjectiveOnReactorComplete,
            ["SpecialTerminalCommand"] = sector.SpecialTerminalCommand ?? string.Empty,
            ["SpecialTerminalCommandDesc"] = sector.SpecialTerminalCommandDesc ?? string.Empty,
            ["PostCommandOutput"] = postCommandOutputArray,
            ["SpecialCommandRule"] = sector.SpecialCommandRule.ToString(),
            ["PowerCellsToDistribute"] = sector.ObjectiveType == ObjectiveType.DistributePowerCells
                ? sector.DistributeGeneratorPlacements.Count
                : 0,
            ["Uplink_NumberOfVerificationRounds"] =
                (sector.ObjectiveType == ObjectiveType.TerminalUplink ||
                 sector.ObjectiveType == ObjectiveType.CorruptedTerminalUplink)
                    ? sector.UplinkNumberOfVerificationRounds
                    : 0,
            ["Uplink_NumberOfTerminals"] =
                (sector.ObjectiveType == ObjectiveType.TerminalUplink ||
                 sector.ObjectiveType == ObjectiveType.CorruptedTerminalUplink)
                    ? sector.TerminalUplinkPlacements.Count
                    : 1,
            ["Uplink_WaveSpawnType"] = "InSuppliedCourseNodeZone",
            ["CentralPowerGenClustser_NumberOfGenerators"] =
                sector.ObjectiveType == ObjectiveType.CentralGeneratorCluster
                    ? sector.DistributeGeneratorPlacements.Count
                    : 0,
            ["CentralPowerGenClustser_NumberOfPowerCells"] =
                sector.ObjectiveType == ObjectiveType.CentralGeneratorCluster
                    ? sector.CentralGeneratorClusterPowerCellPlacements.Count
                    : 4,
            ["CentralPowerGenClustser_FogDataSteps"] = centralFogSteps,
            ["ActivateHSU_ItemFromStart"] =
                sector.ObjectiveType == ObjectiveType.ActivateSmallHSU ? (int)sector.ItemFromStart : 0,
            ["ActivateHSU_ItemAfterActivation"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? (int)sector.ItemAfterActivation
                : 0,
            ["ActivateHSU_BringItemInElevator"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? sector.BringItemInElevator
                : true,
            ["ActivateHSU_MarkItemInElevatorAsWardenObjective"] = false,
            ["ActivateHSU_StopEnemyWavesOnActivation"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? sector.StopEnemyWavesOnActivation
                : false,
            ["ActivateHSU_ObjectiveCompleteAfterInsertion"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? sector.ObjectiveCompleteAfterInsertion
                : false,
            ["ActivateHSU_RequireItemAfterActivationInExitScan"] =
                sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                    ? sector.RequireItemAfterActivationInExitScan
                    : false,
            ["ActivateHSU_Events"] = new JsonArray(),
            ["Survival_TimeToActivate"] =
                sector.ObjectiveType == ObjectiveType.Survival ? (double)sector.TimeToActivate : 0.0,
            ["Survival_TimeToSurvive"] =
                sector.ObjectiveType == ObjectiveType.Survival ? (double)sector.TimeToSurvive : 0.0,
            ["Survival_TimerTitle"] =
                (sector.ObjectiveType == ObjectiveType.Survival && !string.IsNullOrWhiteSpace(sector.TimerTitle))
                    ? textSynth(sector.TimerTitle, "Survival Timer Title")
                    : 0,
            ["Survival_TimerToActivateTitle"] =
                (sector.ObjectiveType == ObjectiveType.Survival &&
                 !string.IsNullOrWhiteSpace(sector.TimerToActivateTitle))
                    ? textSynth(sector.TimerToActivateTitle, "Survival Timer To Activate Title")
                    : 0,
            ["GatherTerminal_SpawnCount"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? sector.GatherTerminalPlacements.Count
                : 0,
            ["GatherTerminal_RequiredCount"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? sector.GatherTerminalRequiredCount
                : 0,
            ["GatherTerminal_Command"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalCommand ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_CommandHelp"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalCommandHelp ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_DownloadingText"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalDownloadingText ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_DownloadCompleteText"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalDownloadCompleteText ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_DownloadTime"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? sector.GatherTerminalDownloadTime
                : -1.0,
            ["TimedTerminalSequence_NumberOfRounds"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequenceNumberOfRounds
                : 3,
            ["TimedTerminalSequence_NumberOfTerminals"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequencePlacements.Count
                : 1,
            ["TimedTerminalSequence_TimePerRound"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequenceTimePerRound
                : 90.0,
            ["TimedTerminalSequence_TimeForConfirmation"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequenceTimeForConfirmation
                : 10.0,
            ["TimedTerminalSequence_UseFilterForSourceTerminalPicking"] = false,
            ["TimedTerminalSequence_SourceTerminalWorldEventObjectFilter"] =
                sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                    ? (sector.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter ?? string.Empty)
                    : string.Empty,
            ["TimedTerminalSequence_EventsOnSequenceStart"] =
                SerializeWardenEvents(sector.EventsOnSequenceStart, textSynth),
            ["TimedTerminalSequence_EventsOnSequenceDone"] =
                SerializeWardenEvents(sector.EventsOnSequenceDone, textSynth),
            ["TimedTerminalSequence_EventsOnSequenceFail"] =
                SerializeWardenEvents(sector.EventsOnSequenceFail, textSynth),
            ["name"] = $"Objective_{persistentId}",
            ["internalEnabled"] = true,
            ["persistentID"] = persistentId
        };
    }

    public static JsonArray SerializeWardenEvents(IEnumerable<WardenEvent>? events,
        Func<string?, string, uint> textSynth)
    {
        var array = new JsonArray();
        if (events == null) return array;

        foreach (var ev in events)
        {
            if (ev == null) continue;
            string layer = ev.Layer switch
            {
                SectorType.Secondary => "SecondaryLayer",
                SectorType.Overload => "ThirdLayer",
                _ => "MainLayer"
            };

            array.Add(new JsonObject
            {
                ["Type"] = ev.Type.ToString(),
                ["Condition"] = new JsonObject
                {
                    ["ConditionIndex"] = -1,
                    ["IsTrue"] = false
                },
                ["Trigger"] = ev.Trigger.ToString(),
                ["ChainPuzzle"] = ev.ChainPuzzle,
                ["UseStaticBioscanPoints"] = ev.UseStaticBioscanPoints,
                ["Layer"] = layer,
                ["DimensionIndex"] = string.IsNullOrWhiteSpace(ev.Dimension) ? "Reality" : ev.Dimension,
                ["LocalIndex"] = $"Zone_{ev.Zone}",
                ["Delay"] = ev.Delay,
                ["Duration"] = ev.Duration,
                ["ClearDimension"] = ev.ClearDimension,
                ["WardenIntel"] = string.IsNullOrWhiteSpace(ev.WardenIntel)
                    ? 0
                    : textSynth(ev.WardenIntel, "Warden Intel"),
                ["CustomSubObjectiveHeader"] = string.IsNullOrWhiteSpace(ev.CustomSubObjectiveHeader)
                    ? 0
                    : textSynth(ev.CustomSubObjectiveHeader, "Sub Objective Header"),
                ["CustomSubObjective"] = string.IsNullOrWhiteSpace(ev.CustomSubObjective)
                    ? 0
                    : textSynth(ev.CustomSubObjective, "Sub Objective"),
                ["SoundID"] = ev.SoundID,
                ["SoundSubtitle"] = string.IsNullOrWhiteSpace(ev.SoundSubtitle)
                    ? 0
                    : textSynth(ev.SoundSubtitle, "Sound Subtitle"),
                ["DialogueID"] = 0,
                ["FogSetting"] = ev.FogSetting,
                ["FogTransitionDuration"] = (double)ev.FogTransitionDuration,
                ["EnemyWaveData"] = new JsonObject
                {
                    ["WaveSettings"] = 0,
                    ["WavePopulation"] = 0,
                    ["AreaDistance"] = 2,
                    ["SpawnDelay"] = 0.0,
                    ["TriggerAlarm"] = false,
                    ["IntelMessage"] = 0
                },
                ["EnemyID"] = ev.EnemyID,
                ["Position"] = new JsonObject
                {
                    ["x"] = ev.Position.X,
                    ["y"] = ev.Position.Y,
                    ["z"] = ev.Position.Z
                },
                ["Count"] = ev.Count,
                ["Enabled"] = ev.Enabled,
                ["TerminalCommand"] = "None",
                ["TerminalCommandRule"] = "Normal",
                ["SustainedEventSlotIndex"] = 0,
                ["SustainedEventStateCount"] = 0,
                ["SustainedEventStateDuration"] = 0.0,
                ["SustainedEventDelay"] = 0.0,
                ["WorldEventObjectFilter"] = ev.WorldEventObjectFilter ?? string.Empty
            });
        }

        return array;
    }

    public static JsonArray SerializeGenericWaveList(
        GenericWaveSettings? wave,
        string prefix,
        PersistentIdAllocator? waveSettingsAlloc,
        PersistentIdAllocator? wavePopAlloc,
        List<JsonObject>? waveSettingsBlocks,
        List<JsonObject>? wavePopBlocks)
    {
        var array = new JsonArray();
        if (wave == null || !wave.HasWaveData() || waveSettingsAlloc == null || wavePopAlloc == null ||
            waveSettingsBlocks == null || wavePopBlocks == null)
            return array;

        uint popId = wavePopAlloc.Allocate();
        var popObj = new JsonObject
        {
            ["WaveRoleWeakling"] = wave.WaveRoleWeakling,
            ["WaveRoleStandard"] = wave.WaveRoleStandard,
            ["WaveRoleSpecial"] = wave.WaveRoleSpecial,
            ["WaveRoleMiniBoss"] = wave.WaveRoleMiniBoss,
            ["WaveRoleBoss"] = wave.WaveRoleBoss,
            ["name"] = $"{prefix}_Pop_{popId}",
            ["internalEnabled"] = true,
            ["persistentID"] = popId
        };
        wavePopBlocks.Add(popObj);

        var popFilterArray = new JsonArray();
        if (wave.FilterWeakling) popFilterArray.Add("Weakling");
        if (wave.FilterStandard) popFilterArray.Add("Standard");
        if (wave.FilterSpecial) popFilterArray.Add("Special");
        if (wave.FilterMiniBoss) popFilterArray.Add("MiniBoss");
        if (wave.FilterBoss) popFilterArray.Add("Boss");

        uint waveSettingsId = waveSettingsAlloc.Allocate();
        var waveObj = new JsonObject
        {
            ["m_pauseBeforeStart"] = wave.PauseBeforeStart,
            ["m_pauseBetweenGroups"] = wave.PauseBetweenGroups,
            ["m_wavePauseMin_atCost"] = wave.WavePauseMin_AtCost,
            ["m_wavePauseMax_atCost"] = wave.WavePauseMax_AtCost,
            ["m_wavePauseMin"] = wave.WavePauseMin,
            ["m_wavePauseMax"] = wave.WavePauseMax,
            ["m_populationFilter"] = popFilterArray,
            ["m_filterType"] = wave.FilterType.ToString(),
            ["m_chanceToRandomizeSpawnDirectionPerWave"] = wave.ChanceToRandomizeSpawnDirectionPerWave,
            ["m_chanceToRandomizeSpawnDirectionPerGroup"] = wave.ChanceToRandomizeSpawnDirectionPerGroup,
            ["m_overrideWaveSpawnType"] = wave.OverrideWaveSpawnType,
            ["m_survivalWaveSpawnType"] = wave.SurvivalWaveSpawnType.ToString(),
            ["m_populationPointsTotal"] = wave.PopulationPointsTotal,
            ["m_populationPointsPerWaveStart"] = wave.PopulationPointsPerWaveStart,
            ["m_populationPointsPerWaveEnd"] = wave.PopulationPointsPerWaveEnd,
            ["m_populationPointsMinPerGroup"] = wave.PopulationPointsMinPerGroup,
            ["m_populationPointsPerGroupStart"] = wave.PopulationPointsPerGroupStart,
            ["m_populationPointsPerGroupEnd"] = wave.PopulationPointsPerGroupEnd,
            ["m_populationRampOverTime"] = wave.PopulationRampOverTime,
            ["name"] = $"{prefix}_Settings_{waveSettingsId}",
            ["internalEnabled"] = true,
            ["persistentID"] = waveSettingsId
        };
        waveSettingsBlocks.Add(waveObj);

        var waveEntry = new JsonObject
        {
            ["WaveSettings"] = waveSettingsId,
            ["WavePopulation"] = popId,
            ["AreaDistance"] = 2,
            ["SpawnDelay"] = wave.PauseBeforeStart,
            ["TriggerAlarm"] = true,
            ["IntelMessage"] = 0
        };
        array.Add(waveEntry);
        return array;
    }
}
