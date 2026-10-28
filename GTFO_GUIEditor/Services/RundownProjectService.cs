using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

public static class RundownProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string? CustomRecentFilesConfigPath { get; set; }

    private static string RecentFilesConfigPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CustomRecentFilesConfigPath))
            {
                return CustomRecentFilesConfigPath;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            string dir = Path.Combine(appData, "GTFO_GUIEditor");
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    // Ignore directory creation failure
                }
            }

            return Path.Combine(dir, "recent_files.json");
        }
    }

    public static RundownProjectFile ExportProject(MainWindowViewModel vm)
    {
        var project = new RundownProjectFile
        {
            FileVersion = 1,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            LastExportDirectory = vm.LastExportDirectory,
            Rundown = vm.Rundown.SelectedRundown?.Clone() ?? new RundownBlock(),
            Alarms = vm.Alarms.Alarms.Select(a => a.Clone()).ToList(),
            Lighting = vm.Lighting.Blocks.Select(b => b.Clone()).ToList(),
            ComplexResourceSets = new List<ComplexResourceSetBlock>(),
            FogSettings = vm.FogSettings.Blocks.Select(b => b.Clone()).ToList(),
            ConsumableDistributions = vm.Consumables.Blocks.Select(b => b.Clone()).ToList()
        };

        foreach (var level in vm.LevelLayout.Levels)
        {
            var levelData = new LevelLayoutBlockData
            {
                Name = level.Name,
                PersistentId = level.PersistentId,
                ComplexType = level.ComplexType,
                ComplexResourceSet = level.ComplexResourceSet,
                ElevatorTile = level.ElevatorTile,
                FogSettings = level.FogSettings,
                InternalEnabled = level.InternalEnabled,
                Dimensions = level.Dimensions.Select(d => d.Clone()).ToList(),
                MainSector = ExportSectorSettings(level.MainSector),
                SecondarySector = ExportSectorSettings(level.SecondarySector),
                OverloadSector = ExportSectorSettings(level.OverloadSector),
                ExpeditionBalance = level.ExpeditionBalance.Clone(),
                GridState = level.GridState.ExportStateData()
            };
            project.Levels.Add(levelData);
        }

        return project;
    }

    public static void SaveProject(MainWindowViewModel vm, string filePath)
    {
        var project = ExportProject(vm);
        SaveProject(project, filePath);
    }

    public static void SaveProject(RundownProjectFile project, string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        project.ModifiedAt = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static RundownProjectFile LoadProject(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Rundown file not found: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<RundownProjectFile>(json, JsonOptions);
        if (project == null)
        {
            throw new InvalidOperationException("Failed to deserialize rundown project file.");
        }

        return project;
    }

    public static void ApplyProject(MainWindowViewModel vm, RundownProjectFile project, string? filePath = null)
    {
        // 1. Alarms
        vm.Alarms.Alarms.Clear();
        if (project.Alarms != null)
        {
            foreach (var alarm in project.Alarms)
            {
                vm.Alarms.Alarms.Add(alarm.Clone());
            }
        }

        if (vm.Alarms.Alarms.Count > 0)
        {
            vm.Alarms.SelectedAlarm = vm.Alarms.Alarms[0];
        }

        // 2. Lighting
        vm.Lighting.Blocks.Clear();
        if (project.Lighting != null)
        {
            foreach (var lightBlock in project.Lighting)
            {
                vm.Lighting.Blocks.Add(lightBlock.Clone());
            }
        }

        if (vm.Lighting.Blocks.Count > 0)
        {
            vm.Lighting.SelectedBlock = vm.Lighting.Blocks[0];
        }

        // 3. Fog Settings
        vm.FogSettings.Blocks.Clear();
        if (project.FogSettings != null)
        {
            foreach (var fogBlock in project.FogSettings)
            {
                vm.FogSettings.Blocks.Add(fogBlock.Clone());
            }
        }

        if (vm.FogSettings.Blocks.Count > 0)
        {
            vm.FogSettings.SelectedBlock = vm.FogSettings.Blocks[0];
        }

        // 3.5. Consumable Distributions
        vm.Consumables.Blocks.Clear();
        if (project.ConsumableDistributions != null)
        {
            foreach (var consumableBlock in project.ConsumableDistributions)
            {
                vm.Consumables.Blocks.Add(consumableBlock.Clone());
            }
        }

        if (vm.Consumables.Blocks.Count > 0)
        {
            vm.Consumables.SelectedBlock = vm.Consumables.Blocks[0];
        }

        // 4. Level Layouts
        vm.LevelLayout.Levels.Clear();
        if (project.Levels != null)
        {
            foreach (var levelData in project.Levels)
            {
                var levelBlock = new LevelLayoutBlock
                {
                    Name = levelData.Name,
                    PersistentId = levelData.PersistentId,
                    ComplexType = levelData.ComplexType,
                    ComplexResourceSet = levelData.ComplexResourceSet,
                    ElevatorTile = levelData.ElevatorTile,
                    FogSettings = levelData.FogSettings,
                    InternalEnabled = levelData.InternalEnabled,
                    Dimensions = new ObservableCollection<DimensionLayoutBlock>(
                        levelData.Dimensions?.Select(d => d.Clone()) ?? Enumerable.Empty<DimensionLayoutBlock>()),
                    MainSector = ImportSectorSettings(levelData.MainSector),
                    SecondarySector = ImportSectorSettings(levelData.SecondarySector),
                    OverloadSector = ImportSectorSettings(levelData.OverloadSector),
                    ExpeditionBalance = levelData.ExpeditionBalance?.Clone() ?? new ExpeditionBalanceSettings()
                };

                if (levelData.GridState != null)
                {
                    levelBlock.GridState.ImportStateData(levelData.GridState);
                }

                vm.LevelLayout.Levels.Add(levelBlock);
            }
        }

        if (vm.LevelLayout.Levels.Count > 0)
        {
            vm.LevelLayout.SelectedLevel = vm.LevelLayout.Levels[0];
        }

        // 5. Rundown
        if (project.Rundown != null)
        {
            vm.Rundown.SelectedRundown = project.Rundown.Clone();
            vm.Rundown.RebuildTiers();
            vm.Rundown.SyncAllExpeditionsWithLevelLayouts();
        }

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            vm.CurrentFilePath = filePath;
            AddRecentFile(filePath);
        }

        vm.LastExportDirectory = project.LastExportDirectory;
    }

    private static WardenObjectiveData ExportWardenObjective(WardenObjective obj)
    {
        return new WardenObjectiveData
        {
            WinCondition = obj.WinCondition,
            ExitZone = obj.ExitZone,
            ObjectiveType = obj.ObjectiveType,
            HsuPlacementZone = obj.HsuPlacementZone,
            HsuPlacementStartWeight = obj.HsuPlacementStartWeight,
            HsuPlacementMiddleWeight = obj.HsuPlacementMiddleWeight,
            HsuPlacementEndWeight = obj.HsuPlacementEndWeight,
            ReactorPlacementZone = obj.ReactorPlacementZone,
            ReactorStartupStages = obj.ReactorStartupStages.Select(s => s.Clone()).ToList(),
            ChainedPuzzleToActive = obj.ChainedPuzzleToActive,
            ChainedPuzzleMidObjective = obj.ChainedPuzzleMidObjective,
            DoNotSolveObjectiveOnReactorComplete = obj.DoNotSolveObjectiveOnReactorComplete,
            SpecialTerminalCommandZone = obj.SpecialTerminalCommandZone,
            SpecialTerminalCommand = obj.SpecialTerminalCommand,
            SpecialTerminalCommandDesc = obj.SpecialTerminalCommandDesc,
            PostCommandOutput = obj.PostCommandOutput,
            SpecialCommandRule = obj.SpecialCommandRule,
            GatherItem = obj.GatherItem,
            GatherMaxPerZone = obj.GatherMaxPerZone,
            RetrieveItem = obj.RetrieveItem,
            RetrieveMaxPerZone = obj.RetrieveMaxPerZone,
            GatherItemPlacements = obj.GatherItemPlacements.Select(p => p.Clone()).ToList(),
            RetrieveItemPlacements = obj.RetrieveItemPlacements.Select(p => p.Clone()).ToList(),
            DistributeGeneratorPlacements = obj.DistributeGeneratorPlacements.Select(g => g.Clone()).ToList(),
            UplinkNumberOfVerificationRounds = obj.UplinkNumberOfVerificationRounds,
            TerminalUplinkPlacements = obj.TerminalUplinkPlacements.Select(p => p.Clone()).ToList(),
            CentralGeneratorClusterZone = obj.CentralGeneratorClusterZone,
            CentralGeneratorClusterFogTransitions =
                obj.CentralGeneratorClusterFogTransitions.Select(s => s.Clone()).ToList(),
            CentralGeneratorClusterPowerCellPlacements =
                obj.CentralGeneratorClusterPowerCellPlacements.Select(p => p.Clone()).ToList(),
            GatherTerminalSpawnCount = obj.GatherTerminalSpawnCount,
            GatherTerminalRequiredCount = obj.GatherTerminalRequiredCount,
            GatherTerminalCommand = obj.GatherTerminalCommand,
            GatherTerminalCommandHelp = obj.GatherTerminalCommandHelp,
            GatherTerminalDownloadingText = obj.GatherTerminalDownloadingText,
            GatherTerminalDownloadCompleteText = obj.GatherTerminalDownloadCompleteText,
            GatherTerminalDownloadTime = obj.GatherTerminalDownloadTime,
            GatherTerminalPlacements = obj.GatherTerminalPlacements.Select(p => p.Clone()).ToList(),
            TimedTerminalSequenceMainLocation = obj.TimedTerminalSequenceMainLocation.Clone(),
            TimedTerminalSequenceNumberOfRounds = obj.TimedTerminalSequenceNumberOfRounds,
            TimedTerminalSequenceTimePerRound = obj.TimedTerminalSequenceTimePerRound,
            TimedTerminalSequenceTimeForConfirmation = obj.TimedTerminalSequenceTimeForConfirmation,
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = obj.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter,
            TimedTerminalSequenceNumberOfTerminals = obj.TimedTerminalSequenceNumberOfTerminals,
            TimedTerminalSequencePlacements = obj.TimedTerminalSequencePlacements.Select(p => p.Clone()).ToList(),
            ActivateSmallHsuZone = obj.ActivateSmallHsuZone,
            ItemFromStart = obj.ItemFromStart,
            ItemAfterActivation = obj.ItemAfterActivation,
            BringItemInElevator = obj.BringItemInElevator,
            StopEnemyWavesOnActivation = obj.StopEnemyWavesOnActivation,
            ObjectiveCompleteAfterInsertion = obj.ObjectiveCompleteAfterInsertion,
            RequireItemAfterActivationInExitScan = obj.RequireItemAfterActivationInExitScan,
            TimeToActivate = obj.TimeToActivate,
            TimeToSurvive = obj.TimeToSurvive,
            TimerTitle = obj.TimerTitle,
            TimerToActivateTitle = obj.TimerToActivateTitle,
            Header = obj.Header,
            MainObjective = obj.MainObjective,
            FindLocationInfo = obj.FindLocationInfo,
            FindLocationInfoHelp = obj.FindLocationInfoHelp,
            GoToZone = obj.GoToZone,
            InZoneFindItem = obj.InZoneFindItem,
            SolveItem = obj.SolveItem,
            WavesOnGotoWin = obj.WavesOnGotoWin.Clone(),
            WaveOnGotoWinTrigger = obj.WaveOnGotoWinTrigger,
            StopAllWavesBeforeGotoWin = obj.StopAllWavesBeforeGotoWin,
            WavesOnElevatorLand = obj.WavesOnElevatorLand.Clone(),
            WavesOnActivate = obj.WavesOnActivate.Clone(),
            FogTransitionDataOnGotoWin = obj.FogTransitionDataOnGotoWin,
            FogTransitionDataOnGotoWinDuration = obj.FogTransitionDataOnGotoWinDuration,
            EventsOnActivation = obj.EventsOnActivation.Select(e => e.Clone()).ToList(),
            EventsOnSequenceStart = obj.EventsOnSequenceStart.Select(e => e.Clone()).ToList(),
            EventsOnSequenceDone = obj.EventsOnSequenceDone.Select(e => e.Clone()).ToList(),
            EventsOnSequenceFail = obj.EventsOnSequenceFail.Select(e => e.Clone()).ToList(),
            EventsOnElevatorLand = obj.EventsOnElevatorLand.Select(e => e.Clone()).ToList(),
            EventsOnGotoWin = obj.EventsOnGotoWin.Select(e => e.Clone()).ToList(),
            EventsOnActivate = obj.EventsOnActivate.Select(e => e.Clone()).ToList()
        };
    }

    private static WardenObjective ImportWardenObjective(WardenObjectiveData data, int objectiveNumber = 1)
    {
        var obj = new WardenObjective
        {
            ObjectiveNumber = objectiveNumber,
            WinCondition = data.WinCondition,
            ExitZone = data.ExitZone,
            ObjectiveType = data.ObjectiveType,
            HsuPlacementZone = data.HsuPlacementZone,
            HsuPlacementStartWeight = data.HsuPlacementStartWeight,
            HsuPlacementMiddleWeight = data.HsuPlacementMiddleWeight,
            HsuPlacementEndWeight = data.HsuPlacementEndWeight,
            ReactorPlacementZone = data.ReactorPlacementZone,
            ChainedPuzzleToActive = data.ChainedPuzzleToActive,
            ChainedPuzzleMidObjective = data.ChainedPuzzleMidObjective,
            DoNotSolveObjectiveOnReactorComplete = data.DoNotSolveObjectiveOnReactorComplete,
            SpecialTerminalCommandZone = data.SpecialTerminalCommandZone,
            SpecialTerminalCommand = data.SpecialTerminalCommand ?? string.Empty,
            SpecialTerminalCommandDesc = data.SpecialTerminalCommandDesc ?? string.Empty,
            PostCommandOutput = data.PostCommandOutput ?? string.Empty,
            SpecialCommandRule = data.SpecialCommandRule,
            GatherItem = data.GatherItem,
            GatherMaxPerZone = data.GatherMaxPerZone,
            RetrieveItem = data.RetrieveItem,
            RetrieveMaxPerZone = data.RetrieveMaxPerZone,
            Header = data.Header ?? string.Empty,
            MainObjective = data.MainObjective ?? string.Empty,
            FindLocationInfo = data.FindLocationInfo ?? string.Empty,
            FindLocationInfoHelp = data.FindLocationInfoHelp ?? string.Empty,
            GoToZone = data.GoToZone ?? string.Empty,
            InZoneFindItem = data.InZoneFindItem ?? string.Empty,
            SolveItem = data.SolveItem ?? string.Empty,
            WavesOnGotoWin = data.WavesOnGotoWin?.Clone() ?? new GenericWaveSettings(),
            WaveOnGotoWinTrigger = data.WaveOnGotoWinTrigger,
            StopAllWavesBeforeGotoWin = data.StopAllWavesBeforeGotoWin,
            WavesOnElevatorLand = data.WavesOnElevatorLand?.Clone() ?? new GenericWaveSettings(),
            WavesOnActivate = data.WavesOnActivate?.Clone() ?? new GenericWaveSettings(),
            FogTransitionDataOnGotoWin = data.FogTransitionDataOnGotoWin,
            FogTransitionDataOnGotoWinDuration = data.FogTransitionDataOnGotoWinDuration,
            UplinkNumberOfVerificationRounds = data.UplinkNumberOfVerificationRounds,
            CentralGeneratorClusterZone = data.CentralGeneratorClusterZone,
            ActivateSmallHsuZone = data.ActivateSmallHsuZone,
            ItemFromStart = data.ItemFromStart,
            ItemAfterActivation = data.ItemAfterActivation,
            BringItemInElevator = data.BringItemInElevator,
            StopEnemyWavesOnActivation = data.StopEnemyWavesOnActivation,
            ObjectiveCompleteAfterInsertion = data.ObjectiveCompleteAfterInsertion,
            RequireItemAfterActivationInExitScan = data.RequireItemAfterActivationInExitScan,
            TimeToActivate = data.TimeToActivate,
            TimeToSurvive = data.TimeToSurvive,
            TimerTitle = data.TimerTitle ?? string.Empty,
            TimerToActivateTitle = data.TimerToActivateTitle ?? string.Empty,
            GatherTerminalSpawnCount = data.GatherTerminalSpawnCount,
            GatherTerminalRequiredCount = data.GatherTerminalRequiredCount,
            GatherTerminalCommand = data.GatherTerminalCommand ?? string.Empty,
            GatherTerminalCommandHelp = data.GatherTerminalCommandHelp ?? string.Empty,
            GatherTerminalDownloadingText = data.GatherTerminalDownloadingText ?? string.Empty,
            GatherTerminalDownloadCompleteText = data.GatherTerminalDownloadCompleteText ?? string.Empty,
            GatherTerminalDownloadTime = data.GatherTerminalDownloadTime,
            TimedTerminalSequenceMainLocation = data.TimedTerminalSequenceMainLocation?.Clone() ?? new CandidateZonePlacement(),
            TimedTerminalSequenceNumberOfRounds = data.TimedTerminalSequenceNumberOfRounds,
            TimedTerminalSequenceTimePerRound = data.TimedTerminalSequenceTimePerRound,
            TimedTerminalSequenceTimeForConfirmation = data.TimedTerminalSequenceTimeForConfirmation,
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = data.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter ?? "MainTerminal",
            TimedTerminalSequenceNumberOfTerminals = data.TimedTerminalSequenceNumberOfTerminals
        };

        obj.ReactorStartupStages.Clear();
        if (data.ReactorStartupStages != null)
        {
            foreach (var stage in data.ReactorStartupStages)
            {
                obj.ReactorStartupStages.Add(stage.Clone());
            }
        }

        obj.GatherItemPlacements.Clear();
        if (data.GatherItemPlacements != null)
        {
            int idx = 1;
            foreach (var placement in data.GatherItemPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                obj.GatherItemPlacements.Add(clone);
            }
        }

        obj.RetrieveItemPlacements.Clear();
        if (data.RetrieveItemPlacements != null)
        {
            int idx = 1;
            foreach (var placement in data.RetrieveItemPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                obj.RetrieveItemPlacements.Add(clone);
            }
        }

        obj.DistributeGeneratorPlacements.Clear();
        if (data.DistributeGeneratorPlacements != null)
        {
            int idx = 1;
            foreach (var generator in data.DistributeGeneratorPlacements)
            {
                var clone = generator.Clone();
                clone.GeneratorNumber = idx++;
                obj.DistributeGeneratorPlacements.Add(clone);
            }
        }

        obj.TerminalUplinkPlacements.Clear();
        if (data.TerminalUplinkPlacements != null)
        {
            int idx = 1;
            foreach (var placement in data.TerminalUplinkPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Terminal";
                obj.TerminalUplinkPlacements.Add(clone);
            }
        }

        obj.CentralGeneratorClusterFogTransitions.Clear();
        if (data.CentralGeneratorClusterFogTransitions != null)
        {
            int idx = 1;
            foreach (var stage in data.CentralGeneratorClusterFogTransitions)
            {
                var clone = stage.Clone();
                clone.StageNumber = idx++;
                obj.CentralGeneratorClusterFogTransitions.Add(clone);
            }
        }

        obj.CentralGeneratorClusterPowerCellPlacements.Clear();
        if (data.CentralGeneratorClusterPowerCellPlacements != null)
        {
            int idx = 1;
            foreach (var placement in data.CentralGeneratorClusterPowerCellPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Power Cell";
                obj.CentralGeneratorClusterPowerCellPlacements.Add(clone);
            }
        }

        obj.GatherTerminalPlacements.Clear();
        if (data.GatherTerminalPlacements != null)
        {
            int idx = 1;
            foreach (var placement in data.GatherTerminalPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Terminal";
                obj.GatherTerminalPlacements.Add(clone);
            }
        }

        obj.TimedTerminalSequencePlacements.Clear();
        if (data.TimedTerminalSequencePlacements != null)
        {
            int idx = 1;
            foreach (var placement in data.TimedTerminalSequencePlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Terminal";
                obj.TimedTerminalSequencePlacements.Add(clone);
            }
        }

        obj.EventsOnActivation.Clear();
        if (data.EventsOnActivation != null)
        {
            foreach (var ev in data.EventsOnActivation)
            {
                obj.EventsOnActivation.Add(ev.Clone());
            }
        }

        obj.EventsOnSequenceStart.Clear();
        if (data.EventsOnSequenceStart != null)
        {
            foreach (var ev in data.EventsOnSequenceStart)
            {
                obj.EventsOnSequenceStart.Add(ev.Clone());
            }
        }

        obj.EventsOnSequenceDone.Clear();
        if (data.EventsOnSequenceDone != null)
        {
            foreach (var ev in data.EventsOnSequenceDone)
            {
                obj.EventsOnSequenceDone.Add(ev.Clone());
            }
        }

        obj.EventsOnSequenceFail.Clear();
        if (data.EventsOnSequenceFail != null)
        {
            foreach (var ev in data.EventsOnSequenceFail)
            {
                obj.EventsOnSequenceFail.Add(ev.Clone());
            }
        }

        obj.EventsOnElevatorLand.Clear();
        if (data.EventsOnElevatorLand != null)
        {
            foreach (var ev in data.EventsOnElevatorLand)
            {
                obj.EventsOnElevatorLand.Add(ev.Clone());
            }
        }

        obj.EventsOnGotoWin.Clear();
        if (data.EventsOnGotoWin != null)
        {
            foreach (var ev in data.EventsOnGotoWin)
            {
                obj.EventsOnGotoWin.Add(ev.Clone());
            }
        }

        obj.EventsOnActivate.Clear();
        if (data.EventsOnActivate != null)
        {
            foreach (var ev in data.EventsOnActivate)
            {
                obj.EventsOnActivate.Add(ev.Clone());
            }
        }

        return obj;
    }

    private static SectorSettingsData ExportSectorSettings(SectorSettings sector)
    {
        var primaryData = ExportWardenObjective(sector.PrimaryObjective);
        var data = new SectorSettingsData
        {
            ArtifactAmountMultiplier = sector.ArtifactAmountMultiplier,
            ZoneAliasStart = sector.ZoneAliasStart,
            BulkheadKeyPlacements = sector.BulkheadKeyPlacements.Select(p => p.Clone()).ToList(),
            Objectives = sector.Objectives.Select(ExportWardenObjective).ToList(),

            // Copy flat properties for backwards compatibility
            WinCondition = primaryData.WinCondition,
            ExitZone = primaryData.ExitZone,
            ObjectiveType = primaryData.ObjectiveType,
            HsuPlacementZone = primaryData.HsuPlacementZone,
            HsuPlacementStartWeight = primaryData.HsuPlacementStartWeight,
            HsuPlacementMiddleWeight = primaryData.HsuPlacementMiddleWeight,
            HsuPlacementEndWeight = primaryData.HsuPlacementEndWeight,
            ReactorPlacementZone = primaryData.ReactorPlacementZone,
            ReactorStartupStages = primaryData.ReactorStartupStages,
            ChainedPuzzleToActive = primaryData.ChainedPuzzleToActive,
            ChainedPuzzleMidObjective = primaryData.ChainedPuzzleMidObjective,
            DoNotSolveObjectiveOnReactorComplete = primaryData.DoNotSolveObjectiveOnReactorComplete,
            SpecialTerminalCommandZone = primaryData.SpecialTerminalCommandZone,
            SpecialTerminalCommand = primaryData.SpecialTerminalCommand,
            SpecialTerminalCommandDesc = primaryData.SpecialTerminalCommandDesc,
            PostCommandOutput = primaryData.PostCommandOutput,
            SpecialCommandRule = primaryData.SpecialCommandRule,
            GatherItem = primaryData.GatherItem,
            GatherMaxPerZone = primaryData.GatherMaxPerZone,
            RetrieveItem = primaryData.RetrieveItem,
            RetrieveMaxPerZone = primaryData.RetrieveMaxPerZone,
            GatherItemPlacements = primaryData.GatherItemPlacements,
            RetrieveItemPlacements = primaryData.RetrieveItemPlacements,
            DistributeGeneratorPlacements = primaryData.DistributeGeneratorPlacements,
            UplinkNumberOfVerificationRounds = primaryData.UplinkNumberOfVerificationRounds,
            TerminalUplinkPlacements = primaryData.TerminalUplinkPlacements,
            CentralGeneratorClusterZone = primaryData.CentralGeneratorClusterZone,
            CentralGeneratorClusterFogTransitions = primaryData.CentralGeneratorClusterFogTransitions,
            CentralGeneratorClusterPowerCellPlacements = primaryData.CentralGeneratorClusterPowerCellPlacements,
            GatherTerminalSpawnCount = primaryData.GatherTerminalSpawnCount,
            GatherTerminalRequiredCount = primaryData.GatherTerminalRequiredCount,
            GatherTerminalCommand = primaryData.GatherTerminalCommand,
            GatherTerminalCommandHelp = primaryData.GatherTerminalCommandHelp,
            GatherTerminalDownloadingText = primaryData.GatherTerminalDownloadingText,
            GatherTerminalDownloadCompleteText = primaryData.GatherTerminalDownloadCompleteText,
            GatherTerminalDownloadTime = primaryData.GatherTerminalDownloadTime,
            GatherTerminalPlacements = primaryData.GatherTerminalPlacements,
            TimedTerminalSequenceMainLocation = primaryData.TimedTerminalSequenceMainLocation,
            TimedTerminalSequenceNumberOfRounds = primaryData.TimedTerminalSequenceNumberOfRounds,
            TimedTerminalSequenceTimePerRound = primaryData.TimedTerminalSequenceTimePerRound,
            TimedTerminalSequenceTimeForConfirmation = primaryData.TimedTerminalSequenceTimeForConfirmation,
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = primaryData.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter,
            TimedTerminalSequenceNumberOfTerminals = primaryData.TimedTerminalSequenceNumberOfTerminals,
            TimedTerminalSequencePlacements = primaryData.TimedTerminalSequencePlacements,
            ActivateSmallHsuZone = primaryData.ActivateSmallHsuZone,
            ItemFromStart = primaryData.ItemFromStart,
            ItemAfterActivation = primaryData.ItemAfterActivation,
            BringItemInElevator = primaryData.BringItemInElevator,
            StopEnemyWavesOnActivation = primaryData.StopEnemyWavesOnActivation,
            ObjectiveCompleteAfterInsertion = primaryData.ObjectiveCompleteAfterInsertion,
            RequireItemAfterActivationInExitScan = primaryData.RequireItemAfterActivationInExitScan,
            TimeToActivate = primaryData.TimeToActivate,
            TimeToSurvive = primaryData.TimeToSurvive,
            TimerTitle = primaryData.TimerTitle,
            TimerToActivateTitle = primaryData.TimerToActivateTitle,
            Header = primaryData.Header,
            MainObjective = primaryData.MainObjective,
            FindLocationInfo = primaryData.FindLocationInfo,
            FindLocationInfoHelp = primaryData.FindLocationInfoHelp,
            GoToZone = primaryData.GoToZone,
            InZoneFindItem = primaryData.InZoneFindItem,
            SolveItem = primaryData.SolveItem,
            WavesOnGotoWin = primaryData.WavesOnGotoWin,
            WaveOnGotoWinTrigger = primaryData.WaveOnGotoWinTrigger,
            StopAllWavesBeforeGotoWin = primaryData.StopAllWavesBeforeGotoWin,
            WavesOnElevatorLand = primaryData.WavesOnElevatorLand,
            WavesOnActivate = primaryData.WavesOnActivate,
            FogTransitionDataOnGotoWin = primaryData.FogTransitionDataOnGotoWin,
            FogTransitionDataOnGotoWinDuration = primaryData.FogTransitionDataOnGotoWinDuration,
            EventsOnActivation = primaryData.EventsOnActivation,
            EventsOnSequenceStart = primaryData.EventsOnSequenceStart,
            EventsOnSequenceDone = primaryData.EventsOnSequenceDone,
            EventsOnSequenceFail = primaryData.EventsOnSequenceFail,
            EventsOnElevatorLand = primaryData.EventsOnElevatorLand,
            EventsOnGotoWin = primaryData.EventsOnGotoWin,
            EventsOnActivate = primaryData.EventsOnActivate
        };
        return data;
    }

    private static SectorSettings ImportSectorSettings(SectorSettingsData? data)
    {
        if (data == null) return new SectorSettings();
        var sector = new SectorSettings
        {
            ArtifactAmountMultiplier = data.ArtifactAmountMultiplier,
            ZoneAliasStart = data.ZoneAliasStart
        };

        if (data.BulkheadKeyPlacements != null)
        {
            sector.BulkheadKeyPlacements.Clear();
            int idx = 1;
            foreach (var placement in data.BulkheadKeyPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                if (string.IsNullOrWhiteSpace(clone.ItemPrefix)) clone.ItemPrefix = "Bulkhead Key";
                sector.BulkheadKeyPlacements.Add(clone);
            }
        }

        var importedObjectives = new ObservableCollection<WardenObjective>();
        if (data.Objectives != null && data.Objectives.Count > 0)
        {
            int objNum = 1;
            foreach (var objData in data.Objectives)
            {
                importedObjectives.Add(ImportWardenObjective(objData, objNum++));
            }
        }
        else
        {
            importedObjectives.Add(ImportWardenObjective(data, 1));
        }
        sector.Objectives = importedObjectives;

        return sector;
    }

    public static List<RecentFileItem> LoadRecentFiles()
    {
        try
        {
            string configPath = RecentFilesConfigPath;
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var items = JsonSerializer.Deserialize<List<RecentFileItem>>(json, JsonOptions);
                if (items != null)
                {
                    return items.Where(i => !string.IsNullOrWhiteSpace(i.FilePath)).ToList();
                }
            }
        }
        catch
        {
            // Ignore error reading recent files
        }

        return new List<RecentFileItem>();
    }

    public static void SaveRecentFiles(IEnumerable<RecentFileItem> files)
    {
        try
        {
            string configPath = RecentFilesConfigPath;
            string json = JsonSerializer.Serialize(files.Take(10).ToList(), JsonOptions);
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // Ignore error saving recent files
        }
    }

    public static List<RecentFileItem> AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return LoadRecentFiles();
        }

        var files = LoadRecentFiles();
        files.RemoveAll(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        files.Insert(0, new RecentFileItem
        {
            FilePath = filePath,
            DisplayName = Path.GetFileName(filePath),
            LastOpened = DateTime.UtcNow
        });

        var trimmed = files.Take(10).ToList();
        SaveRecentFiles(trimmed);
        return trimmed;
    }
}