using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

[Collection("RecentFiles")]
public class RundownFileTests : IDisposable
{
    private readonly string _tempTestDir;

    public RundownFileTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), $"RundownFileTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempTestDir);
        RundownProjectService.CustomRecentFilesConfigPath = Path.Combine(_tempTestDir, "recent_files.json");
    }

    public void Dispose()
    {
        RundownProjectService.CustomRecentFilesConfigPath = null;
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, true);
            }
        }
        catch { }
    }

    private class FakeFileDialogService : IFileDialogService
    {
        public string DefaultRundownsDirectory => Path.Combine(Path.GetTempPath(), "GTFO_Test_Rundowns");
        public string? NextOpenPath { get; set; }
        public string? NextSavePath { get; set; }
        public string? NextFolderPath { get; set; }

        public Task<string?> ShowOpenFileDialogAsync(string? defaultDirectory = null, string? filter = null)
        {
            return Task.FromResult(NextOpenPath);
        }

        public Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null, string? defaultDirectory = null, string? defaultExtension = "rundown")
        {
            return Task.FromResult(NextSavePath);
        }

        public Task<string?> ShowOpenFolderDialogAsync(string? defaultDirectory = null, string? title = "Select Export Destination Directory")
        {
            return Task.FromResult(NextFolderPath);
        }
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_PreservesAllConfigsAndCanvasState()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_rundown_{Guid.NewGuid():N}.rundown");
        try
        {
            var fakeDialog = new FakeFileDialogService();
            var vm = new MainWindowViewModel(fakeDialog);

            // 1. Modify Alarms
            var customAlarm = new AlarmBlock
            {
                Name = "Custom Test Alarm",
                PersistentId = 999111,
                PublicAlarmName = "TEST ALARM DETECTED",
                PauseBeforeStart = 12.5,
                WavePauseMin = 7.0,
                WavePauseMax = 15.0,
                PopulationPointsTotal = 100.0,
                FilterType = WaveFilterType.Include,
                FilterWeakling = true,
                FilterStandard = false
            };
            vm.Alarms.Alarms.Add(customAlarm);

            // 2. Modify Lighting
            var customLighting = new LightSettingsBlock
            {
                Name = "Custom Test Lighting",
                PersistentId = 888222
            };
            customLighting.LightCategorySettings.Add(new LightCategorySettingItem
            {
                Category = LightCategory.Emergency,
                On = true,
                IntensityMul = 2.5,
                Weight = 1.0,
                Chance = 0.9,
                Color = new LightColor { R = 1.0, G = 0.2, B = 0.1, A = 1.0 }
            });
            vm.Lighting.Blocks.Add(customLighting);

            // 3. Modify Fog Settings
            var customFog = new FogSettingsBlock
            {
                Name = "Custom Test Fog",
                PersistentId = 666444,
                FogDensity = 0.75,
                Infection = 0.5,
                FogColor = new LightColor { R = 0.1, G = 0.8, B = 0.3, A = 1.0 },
                DensityNoiseDirection = new Vector3Data(1.0, 2.0, 3.0),
                DensityNoiseSpeed = 4.5,
                DensityNoiseScale = 0.25,
                DensityHeightAltitude = 10.0,
                DensityHeightRange = 5.0,
                DensityHeightMaxBoost = 2.0
            };
            vm.FogSettings.Blocks.Add(customFog);

            // 4. Modify Level Layout and Canvas Grid State
            var customLevel = new LevelLayoutBlock
            {
                Name = "Complex Level Test",
                PersistentId = 555444,
                ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab",
                InternalEnabled = true
            };
            customLevel.GridState.AltitudeOffset = 12.0;
            customLevel.GridState.InitializeStartingTile(customLevel.ElevatorTile, 0, 0);

            // Configure Sector Settings
            customLevel.MainSector.ArtifactAmountMultiplier = 0.85;
            customLevel.MainSector.ZoneAliasStart = 40;
            customLevel.MainSector.WinCondition = WinCondition.GoToExitGeo;
            customLevel.MainSector.ExitZone = 2;
            customLevel.MainSector.ObjectiveType = ObjectiveType.HSU_FindTakeSample;
            customLevel.MainSector.HsuPlacementZone = 1;
            customLevel.MainSector.HsuPlacementStartWeight = 0.5;
            customLevel.MainSector.HsuPlacementMiddleWeight = 0.3;
            customLevel.MainSector.HsuPlacementEndWeight = 0.2;
            customLevel.MainSector.FogTransitionDataOnGotoWin = customFog.PersistentId;
            customLevel.MainSector.FogTransitionDataOnGotoWinDuration = 12.5;

            customLevel.SecondarySector.ArtifactAmountMultiplier = 1.45;
            customLevel.SecondarySector.ZoneAliasStart = 100;
            customLevel.OverloadSector.ArtifactAmountMultiplier = 2.15;
            customLevel.OverloadSector.ZoneAliasStart = 200;

            // Place an adjacent geomorph via Security Door into Zone 1
            var geoPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
            var forwardDoor = customLevel.GridState.PlacedDoors.First(d => d.Direction == "Forwards");
            var placedGeo = customLevel.GridState.ExpandFromDoor(
                forwardDoor,
                geoPrefab,
                DoorType.SecurityDoor,
                altitude: GeomorphAltitude.High,
                lighting: customLighting.PersistentId);
            Assert.NotNull(placedGeo);

            // Configure Zone Settings for Zone 1
            var zs = customLevel.GridState.GetOrCreateZoneSettings(1);
            zs.Lighting = customLighting.PersistentId;
            zs.MedKitUses = 6;
            zs.AmmoKitUses = 8;
            zs.ToolKitUses = 4;
            zs.DisinfectKitUses = 2;
            zs.PowerCells = 2;
            zs.FogTurbines = 1;
            zs.ContainsDC = true;
            zs.DcOpensSecondary = true;
            zs.DcOpensOverload = true;

            // Mark a door as Main Door
            var testDoor = customLevel.GridState.PlacedDoors.FirstOrDefault(d => !d.IsConnected);
            if (testDoor != null)
            {
                testDoor.IsMainDoor = true;
                zs.DcOpenedMainDoorIds.Add(testDoor.Id);
            }

            vm.LevelLayout.Levels.Add(customLevel);
            vm.LevelLayout.SelectedLevel = customLevel;

            // 5. Modify Rundown
            if (vm.Rundown.SelectedRundown != null)
            {
                vm.Rundown.SelectedRundown.Name = "Test Campaign Rundown";
                vm.Rundown.SelectedRundown.PersistentId = 123456;
                vm.Rundown.SelectedRundown.StorytellingData.Title = "RUNDOWN #99.0\nTITLE: TESTING";
                vm.Rundown.SelectedRundown.StorytellingData.SurfaceDescription = "Confidential Warden Intel Briefing.";
            }

            // Save to file
            RundownProjectService.SaveProject(vm, tempFile);
            Assert.True(File.Exists(tempFile));

            // Load into a fresh VM
            var loadedVm = new MainWindowViewModel(fakeDialog);
            var project = RundownProjectService.LoadProject(tempFile);
            RundownProjectService.ApplyProject(loadedVm, project, tempFile);

            // Verify Alarms
            var loadedAlarm = loadedVm.Alarms.Alarms.FirstOrDefault(a => a.PersistentId == 999111);
            Assert.NotNull(loadedAlarm);
            Assert.Equal("Custom Test Alarm", loadedAlarm.Name);
            Assert.Equal("TEST ALARM DETECTED", loadedAlarm.PublicAlarmName);
            Assert.Equal(12.5, loadedAlarm.PauseBeforeStart);
            Assert.Equal(100.0, loadedAlarm.PopulationPointsTotal);
            Assert.True(loadedAlarm.FilterWeakling);
            Assert.False(loadedAlarm.FilterStandard);

            // Verify Lighting
            var loadedLighting = loadedVm.Lighting.Blocks.FirstOrDefault(l => l.PersistentId == 888222);
            Assert.NotNull(loadedLighting);
            Assert.Equal("Custom Test Lighting", loadedLighting.Name);
            Assert.Single(loadedLighting.LightCategorySettings);
            Assert.Equal(LightCategory.Emergency, loadedLighting.LightCategorySettings[0].Category);
            Assert.Equal(2.5, loadedLighting.LightCategorySettings[0].IntensityMul);
            Assert.Equal(1.0, loadedLighting.LightCategorySettings[0].Color.R);

            // Verify Fog Settings
            var loadedFog = loadedVm.FogSettings.Blocks.FirstOrDefault(f => f.PersistentId == 666444);
            Assert.NotNull(loadedFog);
            Assert.Equal("Custom Test Fog", loadedFog.Name);
            Assert.Equal(0.75, loadedFog.FogDensity);
            Assert.Equal(0.5, loadedFog.Infection);
            Assert.Equal(0.1, loadedFog.FogColor.R);
            Assert.Equal(1.0, loadedFog.DensityNoiseDirection.X);
            Assert.Equal(4.5, loadedFog.DensityNoiseSpeed);
            Assert.Equal(10.0, loadedFog.DensityHeightAltitude);

            // Verify Level Layout and Grid State
            var loadedLevel = loadedVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 555444);
            Assert.NotNull(loadedLevel);
            Assert.Equal("Complex Level Test", loadedLevel.Name);
            Assert.Equal(12.0, loadedLevel.GridState.AltitudeOffset);
            Assert.True(loadedLevel.GridState.PlacedGeomorphs.Count >= 2);

            // Verify Sector Settings
            Assert.Equal(0.85, loadedLevel.MainSector.ArtifactAmountMultiplier);
            Assert.Equal(40, loadedLevel.MainSector.ZoneAliasStart);
            Assert.Equal(WinCondition.GoToExitGeo, loadedLevel.MainSector.WinCondition);
            Assert.Equal(2, loadedLevel.MainSector.ExitZone);
            Assert.Equal(ObjectiveType.HSU_FindTakeSample, loadedLevel.MainSector.ObjectiveType);
            Assert.Equal(1, loadedLevel.MainSector.HsuPlacementZone);
            Assert.Equal(0.5, loadedLevel.MainSector.HsuPlacementStartWeight);
            Assert.Equal(0.3, loadedLevel.MainSector.HsuPlacementMiddleWeight);
            Assert.Equal(0.2, loadedLevel.MainSector.HsuPlacementEndWeight);
            Assert.Equal((uint)666444, loadedLevel.MainSector.FogTransitionDataOnGotoWin);
            Assert.Equal(12.5, loadedLevel.MainSector.FogTransitionDataOnGotoWinDuration);

            Assert.Equal(1.45, loadedLevel.SecondarySector.ArtifactAmountMultiplier);
            Assert.Equal(100, loadedLevel.SecondarySector.ZoneAliasStart);
            Assert.Equal(2.15, loadedLevel.OverloadSector.ArtifactAmountMultiplier);
            Assert.Equal(200, loadedLevel.OverloadSector.ZoneAliasStart);

            var loadedGeo = loadedLevel.GridState.PlacedGeomorphs.FirstOrDefault(g => g.PrefabPath == geoPrefab);
            Assert.NotNull(loadedGeo);
            Assert.Equal(GeomorphAltitude.High, loadedGeo.Altitude);
            Assert.Equal(customLighting.PersistentId, loadedGeo.Lighting);

            // Verify Zone Settings
            var loadedZs = loadedLevel.GridState.GetOrCreateZoneSettings(1);
            Assert.NotNull(loadedZs);
            Assert.Equal(6, loadedZs.MedKitUses);
            Assert.Equal(8, loadedZs.AmmoKitUses);
            Assert.Equal(4, loadedZs.ToolKitUses);
            Assert.Equal(2, loadedZs.DisinfectKitUses);
            Assert.Equal(2, loadedZs.PowerCells);
            Assert.Equal(1, loadedZs.FogTurbines);
            Assert.True(loadedZs.ContainsDC);
            Assert.True(loadedZs.DcOpensSecondary);
            Assert.True(loadedZs.DcOpensOverload);

            // Verify Rundown
            Assert.Equal("Test Campaign Rundown", loadedVm.Rundown.SelectedRundown?.Name);
            Assert.Equal((uint)123456, loadedVm.Rundown.SelectedRundown?.PersistentId);
            Assert.Equal("RUNDOWN #99.0\nTITLE: TESTING", loadedVm.Rundown.SelectedRundown?.StorytellingData?.Title);
            Assert.Equal("Confidential Warden Intel Briefing.", loadedVm.Rundown.SelectedRundown?.StorytellingData?.SurfaceDescription);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task SaveAs_UpdatesCurrentFilePathAndRecentFiles()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_saveas_{Guid.NewGuid():N}.rundown");
        try
        {
            var fakeDialog = new FakeFileDialogService { NextSavePath = tempFile };
            var vm = new MainWindowViewModel(fakeDialog);

            Assert.Null(vm.CurrentFilePath);
            Assert.False(vm.HasCurrentFilePath);
            Assert.Equal("Untitled Rundown", vm.CurrentFileTitle);

            await vm.SaveAsRundownAsync();

            Assert.Equal(tempFile, vm.CurrentFilePath);
            Assert.True(vm.HasCurrentFilePath);
            Assert.Equal(Path.GetFileName(tempFile), vm.CurrentFileTitle);
            Assert.True(File.Exists(tempFile));
            Assert.Contains(vm.RecentFiles, f => f.FilePath == tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task OpenRundown_LoadsSelectedFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_open_{Guid.NewGuid():N}.rundown");
        try
        {
            var fakeDialog = new FakeFileDialogService();
            var vm1 = new MainWindowViewModel(fakeDialog);
            if (vm1.Rundown.SelectedRundown != null)
            {
                vm1.Rundown.SelectedRundown.Name = "Saved Rundown For Open";
            }
            RundownProjectService.SaveProject(vm1, tempFile);

            fakeDialog.NextOpenPath = tempFile;
            var vm2 = new MainWindowViewModel(fakeDialog);
            await vm2.OpenRundownAsync();

            Assert.Equal(tempFile, vm2.CurrentFilePath);
            Assert.Equal("Saved Rundown For Open", vm2.Rundown.SelectedRundown?.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void NewRundown_ResetsStateToDefaults()
    {
        var vm = new MainWindowViewModel(new FakeFileDialogService());
        vm.CurrentFilePath = "C:\\some\\path.rundown";
        if (vm.Rundown.SelectedRundown != null)
        {
            vm.Rundown.SelectedRundown.Name = "Old Custom Rundown";
        }

        vm.NewRundown();

        Assert.Null(vm.CurrentFilePath);
        Assert.Equal("Untitled Rundown", vm.CurrentFileTitle);
        Assert.NotEmpty(vm.LevelLayout.Levels);
        Assert.Equal("Created new rundown.", vm.StatusMessage);
    }

    [Fact]
    public void InitialLoadAndNewRundown_DoesNotLoadVanillaRundownFogLightingOrAlarms_ButKeepsEnemies()
    {
        var vm = new MainWindowViewModel(new FakeFileDialogService());

        // Alarms: empty alarms collection, but Enemies are loaded in the background
        Assert.Empty(vm.Alarms.Alarms);
        Assert.Null(vm.Alarms.SelectedAlarm);
        Assert.NotEmpty(vm.Alarms.Enemies);

        // Lighting: empty collection
        Assert.Empty(vm.Lighting.Blocks);
        Assert.Null(vm.Lighting.SelectedBlock);

        // Fog: empty collection
        Assert.Empty(vm.FogSettings.Blocks);
        Assert.Null(vm.FogSettings.SelectedBlock);

        // Rundown: clean fresh default rundown, not loaded from vanilla datablocks
        Assert.NotNull(vm.Rundown.SelectedRundown);
        Assert.Equal("New Rundown", vm.Rundown.SelectedRundown?.Name);
        Assert.Equal((uint)1, vm.Rundown.SelectedRundown?.PersistentId);

        // Call NewRundown() and ensure it remains clean
        vm.NewRundown();

        Assert.Empty(vm.Alarms.Alarms);
        Assert.Null(vm.Alarms.SelectedAlarm);
        Assert.NotEmpty(vm.Alarms.Enemies);

        Assert.Empty(vm.Lighting.Blocks);
        Assert.Null(vm.Lighting.SelectedBlock);

        Assert.Empty(vm.FogSettings.Blocks);
        Assert.Null(vm.FogSettings.SelectedBlock);

        Assert.NotNull(vm.Rundown.SelectedRundown);
        Assert.Equal("New Rundown", vm.Rundown.SelectedRundown?.Name);
    }

    [Fact]
    public void RecentFiles_LimitsAndOrdersCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"RecentFiles_Limits_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string tempConfig = Path.Combine(tempDir, "recent_files.json");
        try
        {
            RundownProjectService.CustomRecentFilesConfigPath = tempConfig;
            string baseDir = Path.GetTempPath();
            for (int i = 0; i < 15; i++)
            {
                string path = Path.Combine(baseDir, $"rundown_{i}.rundown");
                var list = RundownProjectService.AddRecentFile(path);
                Assert.True(list.Count <= 10);
                Assert.Equal(path, list[0].FilePath);
            }
        }
        finally
        {
            RundownProjectService.CustomRecentFilesConfigPath = null;
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void RundownBlock_ExpeditionsProperty_ReturnsAllExpeditionsAcrossTiers()
    {
        var rundown = new RundownBlock();
        rundown.TierA.Clear();
        rundown.TierB.Clear();
        rundown.TierC.Clear();
        rundown.TierD.Clear();
        rundown.TierE.Clear();

        rundown.TierA.Add(new ExpeditionBlock());
        rundown.TierB.Add(new ExpeditionBlock());
        rundown.TierC.Add(new ExpeditionBlock());

        Assert.Equal(3, rundown.Expeditions.Count());
        Assert.Equal(3, rundown.GetAllExpeditions().Count);
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_PreservesReactorStagesAndEvents()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_reactor_events_{Guid.NewGuid():N}.rundown");
        try
        {
            var fakeDialog = new FakeFileDialogService();
            var vm = new MainWindowViewModel(fakeDialog);

            var customLevel = new LevelLayoutBlock
            {
                Name = "Reactor Level Test",
                PersistentId = 444333,
                ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab"
            };

            customLevel.MainSector.ObjectiveType = ObjectiveType.Reactor_Startup;
            customLevel.MainSector.ReactorStartupStages.Clear();
            customLevel.MainSector.ReactorStartupStages.Add(new ReactorStartupStage
            {
                StageNumber = 1,
                Warmup = 45.0,
                Wave = 60.0,
                Verify = 30.0,
                VerifyInOtherZone = true,
                ZoneForVerification = 2
            });

            customLevel.MainSector.EventsOnActivation.Add(new WardenEvent
            {
                Type = EventType.SpawnEnemyWave,
                Trigger = EventTrigger.OnEnd,
                Delay = 5.0
            });

            customLevel.MainSector.EventsOnActivate.Add(new WardenEvent
            {
                Type = EventType.UpdateCustomSubObjective,
                Trigger = EventTrigger.OnStart,
                CustomSubObjective = "Test Activate Sub Objective"
            });

            vm.LevelLayout.Levels.Add(customLevel);

            RundownProjectService.SaveProject(vm, tempFile);

            var loadedVm = new MainWindowViewModel(fakeDialog);
            var project = RundownProjectService.LoadProject(tempFile);
            RundownProjectService.ApplyProject(loadedVm, project, tempFile);

            var loadedLevel = loadedVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 444333);
            Assert.NotNull(loadedLevel);
            Assert.Single(loadedLevel.MainSector.ReactorStartupStages);
            Assert.Equal(45.0, loadedLevel.MainSector.ReactorStartupStages[0].Warmup);
            Assert.Equal(2, loadedLevel.MainSector.ReactorStartupStages[0].ZoneForVerification);

            Assert.Single(loadedLevel.MainSector.EventsOnActivation);
            Assert.Equal(EventType.SpawnEnemyWave, loadedLevel.MainSector.EventsOnActivation[0].Type);

            Assert.Single(loadedLevel.MainSector.EventsOnActivate);
            Assert.Equal("Test Activate Sub Objective", loadedLevel.MainSector.EventsOnActivate[0].CustomSubObjective);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_PreservesAlarmScanTypes()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_alarm_scans_{Guid.NewGuid():N}.rundown");
        try
        {
            var fakeDialog = new FakeFileDialogService();
            var vm = new MainWindowViewModel(fakeDialog);

            var customAlarm = new AlarmBlock
            {
                PersistentId = 9988,
                Name = "Custom Test Alarm"
            };
            customAlarm.ChainedPuzzle.Clear();
            customAlarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = 12 });
            customAlarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = 24 });
            customAlarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = 35 });

            vm.Alarms.Alarms.Add(customAlarm);

            RundownProjectService.SaveProject(vm, tempFile);

            var loadedVm = new MainWindowViewModel(fakeDialog);
            var project = RundownProjectService.LoadProject(tempFile);
            RundownProjectService.ApplyProject(loadedVm, project, tempFile);

            var loadedAlarm = loadedVm.Alarms.Alarms.FirstOrDefault(a => a.PersistentId == 9988);
            Assert.NotNull(loadedAlarm);
            Assert.Equal(3, loadedAlarm.ChainedPuzzle.Count);
            Assert.Equal((uint)12, loadedAlarm.ChainedPuzzle[0].PuzzleType);
            Assert.Equal((uint)24, loadedAlarm.ChainedPuzzle[1].PuzzleType);
            Assert.Equal((uint)35, loadedAlarm.ChainedPuzzle[2].PuzzleType);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadProject_ParsesIntegerArrayAndObjectArrayScans()
    {
        string rawJson = @"
{
  ""fileVersion"": 1,
  ""alarms"": [
    {
      ""persistentID"": 101,
      ""name"": ""IntegerScansAlarm"",
      ""ChainedPuzzle"": [ 2, 6, 22 ]
    },
    {
      ""persistentID"": 102,
      ""name"": ""ObjectScansAlarm"",
      ""ChainedPuzzle"": [
        { ""PuzzleType"": 3 },
        { ""PuzzleType"": 4 }
      ]
    }
  ]
}";
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_compat_scans_{Guid.NewGuid():N}.rundown");
        try
        {
            File.WriteAllText(tempFile, rawJson);

            var fakeDialog = new FakeFileDialogService();
            var loadedVm = new MainWindowViewModel(fakeDialog);
            var project = RundownProjectService.LoadProject(tempFile);
            RundownProjectService.ApplyProject(loadedVm, project, tempFile);

            var alarm101 = loadedVm.Alarms.Alarms.FirstOrDefault(a => a.PersistentId == 101);
            Assert.NotNull(alarm101);
            Assert.Equal(3, alarm101.ChainedPuzzle.Count);
            Assert.Equal((uint)2, alarm101.ChainedPuzzle[0].PuzzleType);
            Assert.Equal((uint)6, alarm101.ChainedPuzzle[1].PuzzleType);
            Assert.Equal((uint)22, alarm101.ChainedPuzzle[2].PuzzleType);

            var alarm102 = loadedVm.Alarms.Alarms.FirstOrDefault(a => a.PersistentId == 102);
            Assert.NotNull(alarm102);
            Assert.Equal(2, alarm102.ChainedPuzzle.Count);
            Assert.Equal((uint)3, alarm102.ChainedPuzzle[0].PuzzleType);
            Assert.Equal((uint)4, alarm102.ChainedPuzzle[1].PuzzleType);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_PreservesTerminalSettingsAndCounts()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_terminal_counts_{Guid.NewGuid():N}.rundown");
        try
        {
            var fakeDialog = new FakeFileDialogService();
            var vm = new MainWindowViewModel(fakeDialog);

            var customLevel = new LevelLayoutBlock
            {
                Name = "Terminal Level Test",
                PersistentId = 555666,
                ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab"
            };

            customLevel.MainSector.GatherTerminalSpawnCount = 7;
            customLevel.MainSector.GatherTerminalRequiredCount = 4;
            customLevel.MainSector.TimedTerminalSequenceNumberOfTerminals = 5;
            customLevel.MainSector.TimedTerminalSequenceNumberOfRounds = 3;

            vm.LevelLayout.Levels.Add(customLevel);

            RundownProjectService.SaveProject(vm, tempFile);

            var loadedVm = new MainWindowViewModel(fakeDialog);
            var project = RundownProjectService.LoadProject(tempFile);
            RundownProjectService.ApplyProject(loadedVm, project, tempFile);

            var loadedLevel = loadedVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 555666);
            Assert.NotNull(loadedLevel);
            Assert.Equal(7, loadedLevel.MainSector.GatherTerminalSpawnCount);
            Assert.Equal(4, loadedLevel.MainSector.GatherTerminalRequiredCount);
            Assert.Equal(5, loadedLevel.MainSector.TimedTerminalSequenceNumberOfTerminals);
            Assert.Equal(3, loadedLevel.MainSector.TimedTerminalSequenceNumberOfRounds);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
