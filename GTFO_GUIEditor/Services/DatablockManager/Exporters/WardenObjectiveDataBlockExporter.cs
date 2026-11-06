using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            var secZones = level.GridState?.GetZonesForSector(SectorType.Secondary) ?? new List<int>();
            bool secExists = secZones.Count > 0 || level.HasSecondaryDoor || (level.GridState?.HasSecondaryDoor == true) ||
                             (context.MainVm.Rundown.SelectedRundown?.Expeditions.Any(e => e.LevelLayoutData == level.PersistentId && e.SecondaryLayerEnabled) == true);

            uint secObjId = 0;
            if (secExists && level.SecondarySector?.PrimaryObjective != null)
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

                if (level.SecondarySector.Objectives.Count > 1)
                {
                    for (int i = 1; i < level.SecondarySector.Objectives.Count; i++)
                    {
                        uint chainedId = context.WardenObjectiveAlloc.Allocate();
                        var chainedObj = CompileWardenObjective(
                            level.SecondarySector.Objectives[i],
                            chainedId,
                            context.SynthesizeText,
                            context.SurvivalWaveSettingsAlloc,
                            context.SurvivalWavePopulationAlloc,
                            context.SurvivalWaveSettingsBlocks,
                            context.SurvivalWavePopulationBlocks);
                        wardenObjectiveBlocks.Add(chainedObj);
                    }
                }
            }

            var thirdZones = level.GridState?.GetZonesForSector(SectorType.Overload) ?? new List<int>();
            bool thirdExists = thirdZones.Count > 0 || level.HasOverloadDoor || (level.GridState?.HasOverloadDoor == true) ||
                               (context.MainVm.Rundown.SelectedRundown?.Expeditions.Any(e => e.LevelLayoutData == level.PersistentId && e.ThirdLayerEnabled) == true);

            uint thirdObjId = 0;
            if (thirdExists && level.OverloadSector?.PrimaryObjective != null)
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

                if (level.OverloadSector.Objectives.Count > 1)
                {
                    for (int i = 1; i < level.OverloadSector.Objectives.Count; i++)
                    {
                        uint chainedId = context.WardenObjectiveAlloc.Allocate();
                        var chainedObj = CompileWardenObjective(
                            level.OverloadSector.Objectives[i],
                            chainedId,
                            context.SynthesizeText,
                            context.SurvivalWaveSettingsAlloc,
                            context.SurvivalWavePopulationAlloc,
                            context.SurvivalWaveSettingsBlocks,
                            context.SurvivalWavePopulationBlocks);
                        wardenObjectiveBlocks.Add(chainedObj);
                    }
                }
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
        Func<string?, string, uint> textSynth,
        bool isTerminalCommand = false)
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

            var evObj = new JsonObject
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
                ["DialogueID"] = ev.DialogueID,
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
            };

            // Handling SpecialNumber vs ChainPuzzle when inside a terminal command
            if (isTerminalCommand && (ev.Type == EventType.AddChainPuzzleToSecurityDoor || ev.Type == EventType.ForceResetChainPuzzle || ev.Type == EventType.ForceCompleteChainPuzzle))
            {
                evObj["SpecialNumber"] = (int)ev.ChainPuzzle;
                evObj["ChainPuzzle"] = 0;
            }

            // AWO Custom Payloads
            switch (ev.Type)
            {
                case EventType.CloseSecurityDoor:
                    evObj["CleanUpEnemiesBehind"] = ev.CleanUpEnemiesBehind;
                    break;

                case EventType.LockSecurityDoor:
                case EventType.SetDoorInteraction:
                case EventType.SetSuccessScreen:
                    evObj["SpecialText"] = string.IsNullOrWhiteSpace(ev.SpecialText)
                        ? 0
                        : textSynth(ev.SpecialText, "Special Text");
                    if (ev.Type == EventType.SetSuccessScreen && ev.SuccessScreen.HasSuccessScreen)
                    {
                        evObj["SuccessScreen"] = new JsonObject
                        {
                            ["Type"] = ev.SuccessScreen.Type.ToString(),
                            ["CustomSuccessScreen"] = ev.SuccessScreen.CustomSuccessScreen,
                            ["OverrideMusic"] = ev.SuccessScreen.OverrideMusic
                        };
                    }
                    break;

                case EventType.ModifyReactorWaveState:
                    evObj["Reactor"] = new JsonObject
                    {
                        ["State"] = ev.Reactor.State.ToString(),
                        ["Wave"] = ev.Reactor.Wave,
                        ["Progress"] = ev.Reactor.Progress
                    };
                    break;

                case EventType.Countdown:
                    var progressArr = new JsonArray();
                    foreach (var p in ev.Countdown.EventsOnProgress)
                    {
                        if (p == null) continue;
                        progressArr.Add(new JsonObject
                        {
                            ["Progress"] = p.Progress,
                            ["Events"] = SerializeWardenEvents(p.Events, textSynth, isTerminalCommand)
                        });
                    }

                    evObj["Countdown"] = new JsonObject
                    {
                        ["CanShowHours"] = ev.Countdown.CanShowHours,
                        ["TitleText"] = string.IsNullOrWhiteSpace(ev.Countdown.TitleText)
                            ? 0
                            : textSynth(ev.Countdown.TitleText, "Countdown Title"),
                        ["TimerColor"] = ev.Countdown.TimerColor ?? "red",
                        ["EventsOnProgress"] = progressArr,
                        ["EventsOnDone"] = SerializeWardenEvents(ev.Countdown.EventsOnDone, textSynth, isTerminalCommand)
                    };
                    break;

                case EventType.SetLightDataInZone:
                    evObj["SetZoneLight"] = new JsonObject
                    {
                        ["Type"] = ev.SetZoneLight.Type.ToString(),
                        ["LightDataID"] = ev.SetZoneLight.LightDataID,
                        ["TransitionDuration"] = ev.SetZoneLight.TransitionDuration,
                        ["Seed"] = ev.SetZoneLight.Seed
                    };
                    break;

                case EventType.AlertEnemiesInZone:
                    evObj["SpecialNumber"] = ev.SpecialNumber;
                    break;

                case EventType.CleanUpEnemiesInZone:
                    var cleanBlacklist = new JsonArray();
                    foreach (var bl in ev.CleanupEnemies.AreaBlacklist) cleanBlacklist.Add(bl.AreaIndex);

                    var excludeEnemies = new JsonArray();
                    foreach (var ex in ev.CleanupEnemies.ExcludeEnemyIDs) excludeEnemies.Add(ex.EnemyID);

                    var includeOnlyEnemies = new JsonArray();
                    foreach (var inc in ev.CleanupEnemies.IncludeOnlyIDs) includeOnlyEnemies.Add(inc.EnemyID);

                    evObj["CleanupEnemies"] = new JsonObject
                    {
                        ["Type"] = ev.CleanupEnemies.Type.ToString(),
                        ["AreaIndex"] = ev.CleanupEnemies.AreaIndex,
                        ["AreaBlacklist"] = cleanBlacklist,
                        ["IncludeHibernate"] = ev.CleanupEnemies.IncludeHibernate,
                        ["IncludeAggressive"] = ev.CleanupEnemies.IncludeAggressive,
                        ["IncludeScout"] = ev.CleanupEnemies.IncludeScout,
                        ["ExcludeEnemyID"] = excludeEnemies,
                        ["IncludeOnlyID"] = includeOnlyEnemies
                    };
                    break;

                case EventType.SpawnHibernateInZone:
                    var hibBlacklist = new JsonArray();
                    foreach (var bl in ev.SpawnHibernates.AreaBlacklist) hibBlacklist.Add(bl.AreaIndex);

                    evObj["SpawnHibernates"] = new JsonObject
                    {
                        ["AreaIndex"] = ev.SpawnHibernates.AreaIndex,
                        ["AreaBlacklist"] = hibBlacklist,
                        ["EnemyID"] = ev.SpawnHibernates.EnemyID,
                        ["PlacementRandomWeight"] = ev.SpawnHibernates.PlacementRandomWeight,
                        ["PlacementScore"] = ev.SpawnHibernates.PlacementScore,
                        ["ResetPlacementInfo"] = ev.SpawnHibernates.ResetPlacementInfo,
                        ["Position"] = new JsonObject
                        {
                            ["x"] = ev.SpawnHibernates.Position.X,
                            ["y"] = ev.SpawnHibernates.Position.Y,
                            ["z"] = ev.SpawnHibernates.Position.Z
                        },
                        ["Rotation"] = new JsonObject
                        {
                            ["x"] = ev.SpawnHibernates.Rotation.X,
                            ["y"] = ev.SpawnHibernates.Rotation.Y,
                            ["z"] = ev.SpawnHibernates.Rotation.Z
                        },
                        ["RandomizeRotation"] = ev.SpawnHibernates.RandomizeRotation
                    };
                    break;

                case EventType.SpawnScoutInZone:
                    var scoutBlacklist = new JsonArray();
                    foreach (var bl in ev.SpawnScouts.AreaBlacklist) scoutBlacklist.Add(bl.AreaIndex);

                    evObj["SpawnScouts"] = new JsonObject
                    {
                        ["AreaIndex"] = ev.SpawnScouts.AreaIndex,
                        ["AreaBlacklist"] = scoutBlacklist,
                        ["GroupType"] = (int)ev.SpawnScouts.GroupType,
                        ["Difficulty"] = ev.SpawnScouts.Difficulty
                    };
                    break;

                case EventType.AddTerminalCommand:
                    var cmdOutputs = new JsonArray();
                    foreach (var po in ev.AddTerminalCommand.PostCommandOutputs)
                    {
                        if (po == null) continue;
                        cmdOutputs.Add(new JsonObject
                        {
                            ["LineType"] = po.LineType.ToString(),
                            ["Time"] = po.Time,
                            ["Output"] = textSynth(po.Output, "Post Command Output")
                        });
                    }

                    evObj["AddTerminalCommand"] = new JsonObject
                    {
                        ["TerminalIndex"] = ev.AddTerminalCommand.TerminalIndex,
                        ["CommandNumber"] = ev.AddTerminalCommand.CommandNumber,
                        ["Command"] = ev.AddTerminalCommand.Command ?? string.Empty,
                        ["CommandDesc"] = string.IsNullOrWhiteSpace(ev.AddTerminalCommand.CommandDesc)
                            ? 0
                            : textSynth(ev.AddTerminalCommand.CommandDesc, "Terminal Command Desc"),
                        ["AutoIndentCommandDesc"] = ev.AddTerminalCommand.AutoIndentCommandDesc,
                        ["PostCommandOutputs"] = cmdOutputs,
                        ["CommandEvents"] = SerializeWardenEvents(ev.AddTerminalCommand.CommandEvents, textSynth, isTerminalCommand: true),
                        ["ProgressWaitBeforeEvents"] = ev.AddTerminalCommand.ProgressWaitBeforeEvents,
                        ["SpecialCommandRule"] = (int)ev.AddTerminalCommand.SpecialCommandRule
                    };
                    break;

                case EventType.HideTerminalCommand:
                    evObj["HideTerminalCommand"] = new JsonObject
                    {
                        ["TerminalIndex"] = ev.HideTerminalCommand.TerminalIndex,
                        ["CommandEnum"] = (uint)ev.HideTerminalCommand.CommandEnum,
                        ["CommandNumber"] = ev.HideTerminalCommand.CommandNumber,
                        ["DeleteCommand"] = ev.HideTerminalCommand.DeleteCommand
                    };
                    break;

                case EventType.UnhideTerminalCommand:
                    evObj["UnhideTerminalCommand"] = new JsonObject
                    {
                        ["TerminalIndex"] = ev.UnhideTerminalCommand.TerminalIndex,
                        ["CommandNumber"] = ev.UnhideTerminalCommand.CommandNumber,
                        ["CommandEnum"] = (uint)ev.UnhideTerminalCommand.CommandEnum
                    };
                    break;

                case EventType.SetActiveEnemyWave:
                    evObj["ActiveEnemyWave"] = new JsonObject
                    {
                        ["HasActiveEnemyWave"] = ev.ActiveEnemyWave.HasActiveEnemyWave,
                        ["EnemyGroupInfrontOfDoor"] = ev.ActiveEnemyWave.EnemyGroupInfrontOfDoor,
                        ["EnemyGroupInArea"] = ev.ActiveEnemyWave.EnemyGroupInArea,
                        ["EnemyGroupsInArea"] = ev.ActiveEnemyWave.EnemyGroupsInArea
                    };
                    break;

                case EventType.GiveResource:
                    var playerFilter = new JsonArray();
                    foreach (int pIdx in ev.GiveResource.GetPlayerFilter()) playerFilter.Add(pIdx);

                    evObj["GiveResource"] = new JsonObject
                    {
                        ["Health"] = ev.GiveResource.Health,
                        ["MainAmmo"] = ev.GiveResource.MainAmmo,
                        ["SpecialAmmo"] = ev.GiveResource.SpecialAmmo,
                        ["ToolAmmo"] = ev.GiveResource.ToolAmmo,
                        ["PlayerFilter"] = playerFilter,
                        ["FullTeamOverflow"] = ev.GiveResource.FullTeamOverflow,
                        ["UseLocation"] = ev.GiveResource.UseLocation,
                        ["IncludeSupplyEfficiency"] = ev.GiveResource.IncludeSupplyEfficiency
                    };
                    break;

                case EventType.NestedEvent:
                    var nestedObj = new JsonObject
                    {
                        ["Type"] = ev.NestedEvent.Type.ToString()
                    };
                    if (ev.NestedEvent.Type == AwoNestedEventType.RandomAny || ev.NestedEvent.Type == AwoNestedEventType.RandomWeighted)
                    {
                        nestedObj["MaxRandomEvents"] = ev.NestedEvent.MaxRandomEvents;
                        nestedObj["AllowRepeatsInRandom"] = ev.NestedEvent.AllowRepeatsInRandom;
                    }
                    var nFilter = new JsonArray();
                    foreach (int p in ev.NestedEvent.GetPlayerFilter()) nFilter.Add(p);
                    nestedObj["PlayerFilter"] = nFilter;
                    nestedObj["EventsToActivate"] = SerializeWardenEvents(ev.NestedEvent.EventsToActivate, textSynth, isTerminalCommand);

                    if (ev.NestedEvent.Type == AwoNestedEventType.RandomWeighted)
                    {
                        var wheelArr = new JsonArray();
                        foreach (var wheel in ev.NestedEvent.WheelOfEvents)
                        {
                            if (wheel == null) continue;
                            wheelArr.Add(new JsonObject
                            {
                                ["DebugName"] = wheel.DebugName ?? string.Empty,
                                ["Weight"] = wheel.Weight,
                                ["RepeatCount"] = wheel.RepeatCount,
                                ["IsInfinite"] = wheel.IsInfinite,
                                ["Events"] = SerializeWardenEvents(wheel.Events, textSynth, isTerminalCommand)
                            });
                        }
                        nestedObj["WheelOfEvents"] = wheelArr;
                    }
                    evObj["NestedEvent"] = nestedObj;
                    break;

                case EventType.StartEventLoop:
                    evObj["EventLoop"] = new JsonObject
                    {
                        ["LoopIndex"] = ev.EventLoop.LoopIndex,
                        ["LoopDelay"] = ev.EventLoop.LoopDelay,
                        ["LoopCount"] = ev.EventLoop.LoopCount,
                        ["EventsToActivate"] = SerializeWardenEvents(ev.EventLoop.EventsToActivate, textSynth, isTerminalCommand)
                    };
                    break;

                case EventType.TeleportPlayer:
                    var tpDataArr = new JsonArray();
                    foreach (var tp in ev.TeleportPlayer.TPData)
                    {
                        if (tp == null) continue;
                        var tpItem = new JsonObject
                        {
                            ["PlayerIndex"] = tp.PlayerIndex,
                            ["DimensionIndex"] = tp.DimensionIndex,
                            ["Position"] = new JsonObject
                            {
                                ["x"] = tp.Position.X,
                                ["y"] = tp.Position.Y,
                                ["z"] = tp.Position.Z
                            },
                            ["WorldEventObjectFilter"] = tp.WorldEventObjectFilter ?? string.Empty,
                            ["PlayWarpAnimation"] = tp.PlayWarpAnimation,
                            ["FlashDuration"] = tp.FlashDuration
                        };
                        if (tp.UseLookDirectionVector)
                        {
                            tpItem["LookDirectionV3"] = new JsonObject
                            {
                                ["x"] = tp.LookDirectionV3.X,
                                ["y"] = tp.LookDirectionV3.Y,
                                ["z"] = tp.LookDirectionV3.Z
                            };
                        }
                        else
                        {
                            tpItem["LookDirection"] = (int)tp.LookDirection;
                        }
                        tpDataArr.Add(tpItem);
                    }

                    evObj["TeleportPlayer"] = new JsonObject
                    {
                        ["FlashTeleport"] = ev.TeleportPlayer.FlashTeleport,
                        ["WarpSentries"] = ev.TeleportPlayer.WarpSentries,
                        ["WarpBigPickups"] = ev.TeleportPlayer.WarpBigPickups,
                        ["SendBigPickupsToHost"] = ev.TeleportPlayer.SendBigPickupsToHost,
                        ["FullTeamOverflow"] = ev.TeleportPlayer.FullTeamOverflow,
                        ["FromLocation"] = new JsonObject
                        {
                            ["Enabled"] = ev.TeleportPlayer.FromLocation.Enabled,
                            ["DimensionIndex"] = ev.TeleportPlayer.FromLocation.DimensionIndex,
                            ["Layer"] = (int)ev.TeleportPlayer.FromLocation.Layer,
                            ["LocalIndex"] = ev.TeleportPlayer.FromLocation.LocalIndex
                        },
                        ["TPData"] = tpDataArr
                    };
                    break;

                case EventType.InfectPlayer:
                    var infFilter = new JsonArray();
                    foreach (int p in ev.InfectPlayer.GetPlayerFilter()) infFilter.Add(p);
                    evObj["InfectPlayer"] = new JsonObject
                    {
                        ["PlayerFilter"] = infFilter,
                        ["FullTeamOverflow"] = ev.InfectPlayer.FullTeamOverflow,
                        ["InfectionAmount"] = ev.InfectPlayer.InfectionAmount,
                        ["InfectOverTime"] = ev.InfectPlayer.InfectOverTime,
                        ["Interval"] = ev.InfectPlayer.Interval,
                        ["UseZone"] = ev.InfectPlayer.UseZone
                    };
                    break;

                case EventType.DamagePlayer:
                    var dmgFilter = new JsonArray();
                    foreach (int p in ev.DamagePlayer.GetPlayerFilter()) dmgFilter.Add(p);
                    evObj["DamagePlayer"] = new JsonObject
                    {
                        ["PlayerFilter"] = dmgFilter,
                        ["FullTeamOverflow"] = ev.DamagePlayer.FullTeamOverflow,
                        ["DamageAmount"] = ev.DamagePlayer.DamageAmount,
                        ["DealPercentageDamage"] = ev.DamagePlayer.DealPercentageDamage,
                        ["DamageOverTime"] = ev.DamagePlayer.DamageOverTime,
                        ["Interval"] = ev.DamagePlayer.Interval,
                        ["UseZone"] = ev.DamagePlayer.UseZone
                    };
                    break;

                case EventType.RevivePlayer:
                    var revFilter = new JsonArray();
                    foreach (int p in ev.RevivePlayer.GetPlayerFilter()) revFilter.Add(p);
                    evObj["RevivePlayer"] = new JsonObject
                    {
                        ["PlayerFilter"] = revFilter,
                        ["FullTeamOverflow"] = ev.RevivePlayer.FullTeamOverflow
                    };
                    break;

                case EventType.AdjustAWOTimer:
                    evObj["AdjustTimer"] = new JsonObject
                    {
                        ["Duration"] = ev.AdjustTimer.Duration,
                        ["Speed"] = ev.AdjustTimer.Speed,
                        ["UpdateTitleText"] = ev.AdjustTimer.UpdateTitleText,
                        ["TitleText"] = string.IsNullOrWhiteSpace(ev.AdjustTimer.TitleText)
                            ? 0
                            : textSynth(ev.AdjustTimer.TitleText, "Adjust Timer Title"),
                        ["UpdateBodyText"] = ev.AdjustTimer.UpdateBodyText,
                        ["BodyText"] = string.IsNullOrWhiteSpace(ev.AdjustTimer.BodyText)
                            ? 0
                            : textSynth(ev.AdjustTimer.BodyText, "Adjust Timer Body"),
                        ["UpdateColor"] = ev.AdjustTimer.UpdateColor,
                        ["TimerColor"] = ev.AdjustTimer.TimerColor ?? "red",
                        ["UpdateSpecialHud"] = ev.AdjustTimer.UpdateSpecialHud,
                        ["SpecialHudIndex"] = ev.AdjustTimer.SpecialHudIndex,
                        ["SpecialHudTimeModifier"] = ev.AdjustTimer.SpecialHudTimeModifier
                    };
                    break;

                case EventType.Countup:
                    var countupProgressArr = new JsonArray();
                    foreach (var p in ev.Countup.EventsOnProgress)
                    {
                        if (p == null) continue;
                        countupProgressArr.Add(new JsonObject
                        {
                            ["Progress"] = p.Progress,
                            ["Events"] = SerializeWardenEvents(p.Events, textSynth, isTerminalCommand)
                        });
                    }

                    evObj["Countup"] = new JsonObject
                    {
                        ["StartValue"] = ev.Countup.StartValue,
                        ["Speed"] = ev.Countup.Speed,
                        ["TitleText"] = string.IsNullOrWhiteSpace(ev.Countup.TitleText)
                            ? 0
                            : textSynth(ev.Countup.TitleText, "Countup Title"),
                        ["BodyText"] = ev.Countup.BodyText ?? "[COUNTUP]",
                        ["TimerColor"] = ev.Countup.TimerColor ?? "red",
                        ["DecimalPoints"] = ev.Countup.DecimalPoints,
                        ["EventsOnProgress"] = countupProgressArr,
                        ["EventsOnDone"] = SerializeWardenEvents(ev.Countup.EventsOnDone, textSynth, isTerminalCommand)
                    };
                    break;

                case EventType.SpawnNavMarker:
                    evObj["NavMarker"] = new JsonObject
                    {
                        ["Index"] = ev.NavMarker.Index,
                        ["Style"] = ev.NavMarker.Style,
                        ["Title"] = string.IsNullOrWhiteSpace(ev.NavMarker.Title)
                            ? 0
                            : textSynth(ev.NavMarker.Title, "Nav Marker Title"),
                        ["Color"] = ev.NavMarker.Color ?? "#B36FF6",
                        ["UsePin"] = ev.NavMarker.UsePin
                    };
                    break;

                case EventType.ShakeScreen:
                    evObj["CameraShake"] = new JsonObject
                    {
                        ["Radius"] = ev.CameraShake.Radius,
                        ["Amplitude"] = ev.CameraShake.Amplitude,
                        ["Frequency"] = ev.CameraShake.Frequency,
                        ["Directional"] = ev.CameraShake.Directional
                    };
                    break;

                case EventType.StartPortalMachine:
                    evObj["Portal"] = new JsonObject
                    {
                        ["TargetDimension"] = ev.Portal.TargetDimension,
                        ["TeleportDelay"] = ev.Portal.TeleportDelay,
                        ["PreventPortalWarpTeamEvent"] = ev.Portal.PreventPortalWarpTeamEvent
                    };
                    break;

                case EventType.MultiProgression:
                    var mpArr = new JsonArray();
                    foreach (var item in ev.MultiProgression.Items)
                    {
                        if (item == null) continue;
                        mpArr.Add(new JsonObject
                        {
                            ["CustomSubObjectiveHeader"] = string.IsNullOrWhiteSpace(item.CustomSubObjectiveHeader)
                                ? 0
                                : textSynth(item.CustomSubObjectiveHeader, "MP Header"),
                            ["CustomSubObjective"] = string.IsNullOrWhiteSpace(item.CustomSubObjective)
                                ? 0
                                : textSynth(item.CustomSubObjective, "MP SubObjective"),
                            ["Index"] = item.Index,
                            ["Priority"] = item.Priority,
                            ["Layer"] = (int)item.Layer,
                            ["IsLayerIndependent"] = item.IsLayerIndependent,
                            ["OverrideTag"] = item.OverrideTag
                        });
                    }
                    evObj["MultiProgression"] = mpArr;
                    break;

                case EventType.PlayWaveRoarSound:
                    evObj["WaveRoarSound"] = new JsonObject
                    {
                        ["RoarSound"] = ev.WaveRoarSound.RoarSound.ToString(),
                        ["RoarSize"] = ev.WaveRoarSound.RoarSize.ToString(),
                        ["IsOutside"] = ev.WaveRoarSound.IsOutside
                    };
                    break;

                case EventType.CustomHudText:
                    evObj["CustomHud"] = new JsonObject
                    {
                        ["TitleText"] = string.IsNullOrWhiteSpace(ev.CustomHud.TitleText)
                            ? 0
                            : textSynth(ev.CustomHud.TitleText, "Custom Hud Title"),
                        ["BodyText"] = string.IsNullOrWhiteSpace(ev.CustomHud.BodyText)
                            ? 0
                            : textSynth(ev.CustomHud.BodyText, "Custom Hud Body")
                    };
                    break;

                case EventType.SpecialHudTimer:
                    var hudProgressArr = new JsonArray();
                    foreach (var p in ev.SpecialHud.EventsOnProgress)
                    {
                        if (p == null) continue;
                        hudProgressArr.Add(new JsonObject
                        {
                            ["Progress"] = p.Progress,
                            ["Events"] = SerializeWardenEvents(p.Events, textSynth, isTerminalCommand)
                        });
                    }

                    evObj["SpecialHud"] = new JsonObject
                    {
                        ["Type"] = ev.SpecialHud.Type.ToString(),
                        ["Index"] = ev.SpecialHud.Index,
                        ["Message"] = string.IsNullOrWhiteSpace(ev.SpecialHud.Message)
                            ? 0
                            : textSynth(ev.SpecialHud.Message, "Special Hud Message"),
                        ["Style"] = ev.SpecialHud.Style.ToString(),
                        ["Priority"] = ev.SpecialHud.Priority,
                        ["ShowTimeInProgressBar"] = ev.SpecialHud.ShowTimeInProgressBar,
                        ["InvertProgress"] = ev.SpecialHud.InvertProgress,
                        ["EventsOnProgress"] = hudProgressArr,
                        ["EventsOnDone"] = SerializeWardenEvents(ev.SpecialHud.EventsOnDone, textSynth, isTerminalCommand)
                    };
                    break;

                case EventType.ForcePlayPlayerDialogue:
                    evObj["PlayerDialogue"] = new JsonObject
                    {
                        ["Type"] = ev.PlayerDialogue.Type.ToString(),
                        ["CharacterID"] = ev.PlayerDialogue.CharacterID,
                        ["IntensityState"] = ev.PlayerDialogue.IntensityState.ToString()
                    };
                    break;

                case EventType.SetTerminalLog:
                    var stlObj = new JsonObject
                    {
                        ["TerminalIndex"] = ev.SetTerminalLog.TerminalIndex,
                        ["Type"] = ev.SetTerminalLog.Type.ToString(),
                        ["FileName"] = ev.SetTerminalLog.FileName ?? string.Empty
                    };
                    if (ev.SetTerminalLog.Type == AwoTerminalLogType.Add)
                    {
                        stlObj["FileContent"] = string.IsNullOrWhiteSpace(ev.SetTerminalLog.FileContent)
                            ? 0
                            : textSynth(ev.SetTerminalLog.FileContent, "Terminal Log Content");
                        stlObj["FileContentOriginalLanguage"] = ev.SetTerminalLog.FileContentOriginalLanguage;
                        stlObj["AttachedAudioFile"] = ev.SetTerminalLog.AttachedAudioFile;
                        stlObj["AttachedAudioByteSize"] = ev.SetTerminalLog.AttachedAudioByteSize;
                        stlObj["PlayerDialogToTriggerAfterAudio"] = ev.SetTerminalLog.PlayerDialogToTriggerAfterAudio;
                        stlObj["EventsOnFileRead"] = SerializeWardenEvents(ev.SetTerminalLog.EventsOnFileRead, textSynth, isTerminalCommand);
                    }
                    else if (ev.SetTerminalLog.Type == AwoTerminalLogType.Move)
                    {
                        stlObj["TargetDimensionIndex"] = ev.SetTerminalLog.TargetDimensionIndex;
                        stlObj["TargetLayer"] = (int)ev.SetTerminalLog.TargetLayer;
                        stlObj["TargetLocalIndex"] = ev.SetTerminalLog.TargetLocalIndex;
                        stlObj["TargetTerminalIndex"] = ev.SetTerminalLog.TargetTerminalIndex;
                    }
                    evObj["SetTerminalLog"] = stlObj;
                    break;

                case EventType.SetPocketItem:
                    var itemsArr = new JsonArray();
                    foreach (var item in ev.ObjectiveItems.Items)
                    {
                        if (item == null) continue;
                        itemsArr.Add(new JsonObject
                        {
                            ["Index"] = item.Index,
                            ["Count"] = item.Count,
                            ["IsOnTop"] = item.IsOnTop,
                            ["ItemName"] = string.IsNullOrWhiteSpace(item.ItemName)
                                ? 0
                                : textSynth(item.ItemName, "Pocket Item Name"),
                            ["TagType"] = item.TagType.ToString(),
                            ["PlayerIndex"] = item.PlayerIndex,
                            ["CustomTag"] = item.CustomTag ?? string.Empty
                        });
                    }
                    evObj["ObjectiveItems"] = itemsArr;
                    break;

                case EventType.SetExpeditionEnvironment:
                    evObj["EnvironmentData"] = new JsonObject
                    {
                        ["EnvironmentWetness"] = SerializeEnvironmentValue(ev.EnvironmentData.EnvironmentWetness),
                        ["UpdateColor"] = ev.EnvironmentData.UpdateColor,
                        ["DustColor"] = ev.EnvironmentData.DustColor ?? "white",
                        ["DustAlphaBoost"] = SerializeEnvironmentValue(ev.EnvironmentData.DustAlphaBoost),
                        ["DustTurbulence"] = SerializeEnvironmentValue(ev.EnvironmentData.DustTurbulence)
                    };
                    break;

                case EventType.ClearWardenIntelQueue:
                    evObj["SpecialBool"] = ev.SpecialBool;
                    break;

                case EventType.FadeScreenInOut:
                    evObj["FadeScreen"] = new JsonObject
                    {
                        ["FadeColor"] = ev.FadeScreen.FadeColor ?? "black",
                        ["CurveTimeStart"] = ev.FadeScreen.CurveTimeStart,
                        ["CurveValueStart"] = ev.FadeScreen.CurveValueStart,
                        ["CurveTimeEnd"] = ev.FadeScreen.CurveTimeEnd,
                        ["CurveValueEnd"] = ev.FadeScreen.CurveValueEnd,
                        ["FadeSpeed"] = ev.FadeScreen.FadeSpeed
                    };
                    break;
            }

            array.Add(evObj);
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

    private static JsonNode SerializeEnvironmentValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return JsonValue.Create("Unchanged")!;
        }

        string trimmed = value.Trim();
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double dVal))
        {
            return JsonValue.Create(dVal)!;
        }

        return JsonValue.Create(trimmed)!;
    }
}
