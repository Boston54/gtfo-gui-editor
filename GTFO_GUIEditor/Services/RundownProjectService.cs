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

    private static string RecentFilesConfigPath
    {
        get
        {
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
            Rundown = vm.Rundown.SelectedRundown?.Clone() ?? new RundownBlock(),
            Alarms = vm.Alarms.Alarms.Select(a => a.Clone()).ToList(),
            Lighting = vm.Lighting.Blocks.Select(b => b.Clone()).ToList(),
            ComplexResourceSets = vm.ComplexResourceSets.Blocks.Select(s => s.Clone()).ToList(),
            FogSettings = vm.FogSettings.Blocks.Select(b => b.Clone()).ToList()
        };

        foreach (var level in vm.LevelLayout.Levels)
        {
            var levelData = new LevelLayoutBlockData
            {
                Name = level.Name,
                PersistentId = level.PersistentId,
                ComplexResourceSet = level.ComplexResourceSet,
                ElevatorTile = level.ElevatorTile,
                InternalEnabled = level.InternalEnabled,
                Dimensions = level.Dimensions.Select(d => d.Clone()).ToList(),
                MainSector = ExportSectorSettings(level.MainSector),
                SecondarySector = ExportSectorSettings(level.SecondarySector),
                OverloadSector = ExportSectorSettings(level.OverloadSector),
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

        // 3. Complex Resource Sets
        vm.ComplexResourceSets.Blocks.Clear();
        if (project.ComplexResourceSets != null)
        {
            foreach (var compSet in project.ComplexResourceSets)
            {
                vm.ComplexResourceSets.Blocks.Add(compSet.Clone());
            }
        }

        if (vm.ComplexResourceSets.Blocks.Count > 0)
        {
            vm.ComplexResourceSets.SelectedBlock = vm.ComplexResourceSets.Blocks[0];
        }

        // 3.5 Fog Settings
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
                    ComplexResourceSet = levelData.ComplexResourceSet,
                    ElevatorTile = levelData.ElevatorTile,
                    InternalEnabled = levelData.InternalEnabled,
                    Dimensions = new ObservableCollection<DimensionLayoutBlock>(
                        levelData.Dimensions?.Select(d => d.Clone()) ?? Enumerable.Empty<DimensionLayoutBlock>()),
                    MainSector = ImportSectorSettings(levelData.MainSector),
                    SecondarySector = ImportSectorSettings(levelData.SecondarySector),
                    OverloadSector = ImportSectorSettings(levelData.OverloadSector)
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
    }

    private static SectorSettingsData ExportSectorSettings(SectorSettings sector)
    {
        return new SectorSettingsData
        {
            ArtifactAmountMultiplier = sector.ArtifactAmountMultiplier,
            ZoneAliasStart = sector.ZoneAliasStart,
            WinCondition = sector.WinCondition,
            ExitZone = sector.ExitZone,
            ObjectiveType = sector.ObjectiveType,
            HsuPlacementZone = sector.HsuPlacementZone,
            HsuPlacementStartWeight = sector.HsuPlacementStartWeight,
            HsuPlacementMiddleWeight = sector.HsuPlacementMiddleWeight,
            HsuPlacementEndWeight = sector.HsuPlacementEndWeight,
            ReactorPlacementZone = sector.ReactorPlacementZone,
            ReactorStartupStages = sector.ReactorStartupStages.Select(s => s.Clone()).ToList(),
            ChainedPuzzleToActive = sector.ChainedPuzzleToActive,
            ChainedPuzzleMidObjective = sector.ChainedPuzzleMidObjective,
            DoNotSolveObjectiveOnReactorComplete = sector.DoNotSolveObjectiveOnReactorComplete,
            SpecialTerminalCommandZone = sector.SpecialTerminalCommandZone,
            SpecialTerminalCommand = sector.SpecialTerminalCommand,
            SpecialTerminalCommandDesc = sector.SpecialTerminalCommandDesc,
            PostCommandOutput = sector.PostCommandOutput,
            SpecialCommandRule = sector.SpecialCommandRule,
            GatherItem = sector.GatherItem,
            GatherMaxPerZone = sector.GatherMaxPerZone,
            RetrieveItem = sector.RetrieveItem,
            RetrieveMaxPerZone = sector.RetrieveMaxPerZone,
            GatherItemPlacements = sector.GatherItemPlacements.Select(p => p.Clone()).ToList(),
            RetrieveItemPlacements = sector.RetrieveItemPlacements.Select(p => p.Clone()).ToList(),
            DistributeGeneratorPlacements = sector.DistributeGeneratorPlacements.Select(g => g.Clone()).ToList(),
            UplinkNumberOfVerificationRounds = sector.UplinkNumberOfVerificationRounds,
            TerminalUplinkPlacements = sector.TerminalUplinkPlacements.Select(p => p.Clone()).ToList(),
            CentralGeneratorClusterZone = sector.CentralGeneratorClusterZone,
            CentralGeneratorClusterFogTransitions =
                sector.CentralGeneratorClusterFogTransitions.Select(s => s.Clone()).ToList(),
            CentralGeneratorClusterPowerCellPlacements =
                sector.CentralGeneratorClusterPowerCellPlacements.Select(p => p.Clone()).ToList(),
            GatherTerminalSpawnCount = sector.GatherTerminalSpawnCount,
            GatherTerminalRequiredCount = sector.GatherTerminalRequiredCount,
            GatherTerminalCommand = sector.GatherTerminalCommand,
            GatherTerminalCommandHelp = sector.GatherTerminalCommandHelp,
            GatherTerminalDownloadingText = sector.GatherTerminalDownloadingText,
            GatherTerminalDownloadCompleteText = sector.GatherTerminalDownloadCompleteText,
            GatherTerminalDownloadTime = sector.GatherTerminalDownloadTime,
            GatherTerminalPlacements = sector.GatherTerminalPlacements.Select(p => p.Clone()).ToList(),
            TimedTerminalSequenceMainLocation = sector.TimedTerminalSequenceMainLocation.Clone(),
            TimedTerminalSequenceNumberOfRounds = sector.TimedTerminalSequenceNumberOfRounds,
            TimedTerminalSequenceTimePerRound = sector.TimedTerminalSequenceTimePerRound,
            TimedTerminalSequenceTimeForConfirmation = sector.TimedTerminalSequenceTimeForConfirmation,
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = sector.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter,
            TimedTerminalSequenceNumberOfTerminals = sector.TimedTerminalSequenceNumberOfTerminals,
            TimedTerminalSequencePlacements = sector.TimedTerminalSequencePlacements.Select(p => p.Clone()).ToList(),
            ActivateSmallHsuZone = sector.ActivateSmallHsuZone,
            ItemFromStart = sector.ItemFromStart,
            ItemAfterActivation = sector.ItemAfterActivation,
            BringItemInElevator = sector.BringItemInElevator,
            StopEnemyWavesOnActivation = sector.StopEnemyWavesOnActivation,
            ObjectiveCompleteAfterInsertion = sector.ObjectiveCompleteAfterInsertion,
            RequireItemAfterActivationInExitScan = sector.RequireItemAfterActivationInExitScan,
            TimeToActivate = sector.TimeToActivate,
            TimeToSurvive = sector.TimeToSurvive,
            TimerTitle = sector.TimerTitle,
            TimerToActivateTitle = sector.TimerToActivateTitle,
            Header = sector.Header,
            MainObjective = sector.MainObjective,
            FindLocationInfo = sector.FindLocationInfo,
            FindLocationInfoHelp = sector.FindLocationInfoHelp,
            GoToZone = sector.GoToZone,
            InZoneFindItem = sector.InZoneFindItem,
            SolveItem = sector.SolveItem,
            WavesOnGotoWin = sector.WavesOnGotoWin.Clone(),
            WaveOnGotoWinTrigger = sector.WaveOnGotoWinTrigger,
            StopAllWavesBeforeGotoWin = sector.StopAllWavesBeforeGotoWin,
            WavesOnElevatorLand = sector.WavesOnElevatorLand.Clone(),
            FogTransitionDataOnGotoWin = sector.FogTransitionDataOnGotoWin,
            FogTransitionDataOnGotoWinDuration = sector.FogTransitionDataOnGotoWinDuration,
            EventsOnActivation = sector.EventsOnActivation.Select(e => e.Clone()).ToList(),
            EventsOnSequenceStart = sector.EventsOnSequenceStart.Select(e => e.Clone()).ToList(),
            EventsOnSequenceDone = sector.EventsOnSequenceDone.Select(e => e.Clone()).ToList(),
            EventsOnSequenceFail = sector.EventsOnSequenceFail.Select(e => e.Clone()).ToList(),
            EventsOnElevatorLand = sector.EventsOnElevatorLand.Select(e => e.Clone()).ToList(),
            EventsOnGotoWin = sector.EventsOnGotoWin.Select(e => e.Clone()).ToList(),
            EventsOnActivate = sector.EventsOnActivate.Select(e => e.Clone()).ToList()
        };
    }

    private static SectorSettings ImportSectorSettings(SectorSettingsData? data)
    {
        if (data == null) return new SectorSettings();
        var sector = new SectorSettings
        {
            ArtifactAmountMultiplier = data.ArtifactAmountMultiplier,
            ZoneAliasStart = data.ZoneAliasStart,
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
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = data.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter ?? "MainTerminal"
        };

        if (data.ReactorStartupStages != null)
        {
            foreach (var stage in data.ReactorStartupStages)
            {
                sector.ReactorStartupStages.Add(stage.Clone());
            }
        }

        if (data.GatherItemPlacements != null && data.GatherItemPlacements.Count > 0)
        {
            sector.GatherItemPlacements.Clear();
            int idx = 1;
            foreach (var placement in data.GatherItemPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                sector.GatherItemPlacements.Add(clone);
            }
        }

        if (data.RetrieveItemPlacements != null && data.RetrieveItemPlacements.Count > 0)
        {
            sector.RetrieveItemPlacements.Clear();
            int idx = 1;
            foreach (var placement in data.RetrieveItemPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                sector.RetrieveItemPlacements.Add(clone);
            }
        }

        if (data.DistributeGeneratorPlacements != null && data.DistributeGeneratorPlacements.Count > 0)
        {
            sector.DistributeGeneratorPlacements.Clear();
            int idx = 1;
            foreach (var generator in data.DistributeGeneratorPlacements)
            {
                var clone = generator.Clone();
                clone.GeneratorNumber = idx++;
                sector.DistributeGeneratorPlacements.Add(clone);
            }
        }

        if (data.TerminalUplinkPlacements != null && data.TerminalUplinkPlacements.Count > 0)
        {
            sector.TerminalUplinkPlacements.Clear();
            int idx = 1;
            foreach (var placement in data.TerminalUplinkPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Terminal";
                sector.TerminalUplinkPlacements.Add(clone);
            }
        }

        if (data.CentralGeneratorClusterFogTransitions != null && data.CentralGeneratorClusterFogTransitions.Count > 0)
        {
            sector.CentralGeneratorClusterFogTransitions.Clear();
            int idx = 1;
            foreach (var stage in data.CentralGeneratorClusterFogTransitions)
            {
                var clone = stage.Clone();
                clone.StageNumber = idx++;
                sector.CentralGeneratorClusterFogTransitions.Add(clone);
            }
        }

        if (data.CentralGeneratorClusterPowerCellPlacements != null &&
            data.CentralGeneratorClusterPowerCellPlacements.Count > 0)
        {
            sector.CentralGeneratorClusterPowerCellPlacements.Clear();
            int idx = 1;
            foreach (var placement in data.CentralGeneratorClusterPowerCellPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Power Cell";
                sector.CentralGeneratorClusterPowerCellPlacements.Add(clone);
            }
        }

        if (data.GatherTerminalPlacements != null && data.GatherTerminalPlacements.Count > 0)
        {
            sector.GatherTerminalPlacements.Clear();
            int idx = 1;
            foreach (var placement in data.GatherTerminalPlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Terminal";
                sector.GatherTerminalPlacements.Add(clone);
            }
        }

        if (data.TimedTerminalSequencePlacements != null && data.TimedTerminalSequencePlacements.Count > 0)
        {
            sector.TimedTerminalSequencePlacements.Clear();
            int idx = 1;
            foreach (var placement in data.TimedTerminalSequencePlacements)
            {
                var clone = placement.Clone();
                clone.ItemNumber = idx++;
                clone.ItemPrefix = "Terminal";
                sector.TimedTerminalSequencePlacements.Add(clone);
            }
        }

        if (data.EventsOnActivation != null)
        {
            foreach (var ev in data.EventsOnActivation)
            {
                sector.EventsOnActivation.Add(ev.Clone());
            }
        }

        if (data.EventsOnSequenceStart != null)
        {
            foreach (var ev in data.EventsOnSequenceStart)
            {
                sector.EventsOnSequenceStart.Add(ev.Clone());
            }
        }

        if (data.EventsOnSequenceDone != null)
        {
            foreach (var ev in data.EventsOnSequenceDone)
            {
                sector.EventsOnSequenceDone.Add(ev.Clone());
            }
        }

        if (data.EventsOnSequenceFail != null)
        {
            foreach (var ev in data.EventsOnSequenceFail)
            {
                sector.EventsOnSequenceFail.Add(ev.Clone());
            }
        }

        if (data.EventsOnElevatorLand != null)
        {
            foreach (var ev in data.EventsOnElevatorLand)
            {
                sector.EventsOnElevatorLand.Add(ev.Clone());
            }
        }

        if (data.EventsOnGotoWin != null)
        {
            foreach (var ev in data.EventsOnGotoWin)
            {
                sector.EventsOnGotoWin.Add(ev.Clone());
            }
        }

        if (data.EventsOnActivate != null)
        {
            foreach (var ev in data.EventsOnActivate)
            {
                sector.EventsOnActivate.Add(ev.Clone());
            }
        }

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