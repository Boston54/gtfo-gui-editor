using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class DatablockImportTests : IDisposable
{
    private readonly string _tempDir;

    public DatablockImportTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GTFO_ImportTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup failure
        }
    }

    [Fact]
    public void ExportAndImport_Roundtrip_PreservesRundownState()
    {
        // 1. Arrange a populated MainWindowViewModel
        var originalVm = new MainWindowViewModel();

        // Custom Light Block
        var light = new LightSettingsBlock
        {
            PersistentId = 101,
            Name = "Test Custom Light"
        };
        light.LightCategorySettings.Add(new LightCategorySettingItem
        {
            Category = LightCategory.General,
            On = true,
            Chance = 0.8,
            IntensityMul = 1.5,
            Color = new LightColor { A = 1.0, R = 0.2, G = 0.5, B = 0.8 }
        });
        originalVm.Lighting.Blocks.Add(light);

        // Custom Fog Block
        var fog = new FogSettingsBlock
        {
            PersistentId = 202,
            Name = "Test Custom Fog",
            FogDensity = 0.05,
            FogAmbience = 0.2,
            FogColor = new LightColor { A = 1.0, R = 0.1, G = 0.2, B = 0.3 }
        };
        originalVm.FogSettings.Blocks.Add(fog);

        // Custom Consumables
        var cons = new ConsumableDistributionBlock
        {
            PersistentId = 303,
            Name = "Test Consumables",
            SpawnsPerZone = 8,
            ChanceToSpawnInResourceContainer = 0.9
        };
        cons.SpawnData.Add(new ConsumableDistributionItem { ItemId = 127, Weight = 2.0 });
        originalVm.Consumables.Blocks.Add(cons);

        // Custom Alarm
        var alarm = new AlarmBlock
        {
            PersistentId = 404,
            Name = "Test Custom Alarm",
            PublicAlarmName = "Class IV Alarm",
            SpawnsEnemies = true,
            WaveRoleStandard = 15,
            PauseBeforeStart = 4.0
        };
        alarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = 4 });
        alarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = 5 });
        originalVm.Alarms.Alarms.Add(alarm);

        // Level Layout
        var level = originalVm.LevelLayout.Levels[0];
        level.PersistentId = 505;
        level.Name = "Test_A1";
        level.ComplexType = ComplexType.Tech;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();

        grid.InitializeStartingTile(level.ElevatorTile, 0, 0);

        // Add a second geomorph
        var geo2 = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_01.prefab", 1, 0, initialAreaIndex: 0);
        grid.PlacedGeomorphs.Add(geo2);

        // Generate rooms
        geo2.GenerateRoom(0);
        geo2.SetRoomSector(0, SectorType.Main);
        geo2.SetRoomZone(0, 1);
        grid.CreateRoomNode(geo2, 0);

        // Connect doors
        var doorA = grid.PlacedDoors.FirstOrDefault(d => d.LocalX == 0 && d.LocalY == 0);
        var doorB = grid.PlacedDoors.FirstOrDefault(d => d.LocalX == 1 && d.LocalY == 0);
        if (doorA != null && doorB != null)
        {
            doorA.IsConnected = true;
            doorA.ConnectedDoor = doorB;
            doorB.IsConnected = true;
            doorB.ConnectedDoor = doorA;
            doorA.IsSecurityDoor = true;
            doorA.ChainedPuzzleToSolve = alarm.PersistentId;
        }

        // Zone settings
        var zs1 = grid.GetOrCreateZoneSettings(SectorType.Main, 1);
        zs1.Lighting = light.PersistentId;
        zs1.ConsumableDistributionInZone = cons.PersistentId;
        zs1.SubSeed = 42;
        zs1.MedKitUses = 4;
        zs1.AmmoKitUses = 3;

        // Objective
        level.MainSector.Objectives[0].ObjectiveType = ObjectiveType.HSU_FindTakeSample;
        level.MainSector.Objectives[0].Header = "Find the HSU Sample";
        level.MainSector.Objectives[0].MainObjective = "Retrieve DNA container from Zone 1";

        // Rundown
        var rundown = originalVm.Rundown.SelectedRundown!;
        rundown.Name = "Rundown_Test";
        rundown.TierA[0].Descriptive.PublicName = level.Name;
        rundown.TierA[0].LevelLayoutData = level.PersistentId;
        rundown.TierA[0].Expedition.LightSettings = light.PersistentId;
        rundown.TierA[0].Expedition.FogSettings = fog.PersistentId;

        // Save original rundown file
        string originalRundownPath = Path.Combine(_tempDir, "Original.rundown");
        RundownLoader.Instance.SaveProject(originalVm, originalRundownPath);
        string originalRundownJson = File.ReadAllText(originalRundownPath);

        // Export Datablocks
        string exportDir = Path.Combine(_tempDir, "ExportedDatablocks");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        // 2. Import into a fresh MainWindowViewModel
        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);
        Assert.True(importResult.LevelsImportedCount >= 1);

        // 3. Save imported rundown file
        string importedRundownPath = Path.Combine(_tempDir, "Imported.rundown");
        RundownLoader.Instance.SaveProject(importedVm, importedRundownPath);
        string importedRundownJson = File.ReadAllText(importedRundownPath);

        // 4. Parse both JSON documents and assert key equivalence
        var origDoc = JsonNode.Parse(originalRundownJson) as JsonObject;
        var importedDoc = JsonNode.Parse(importedRundownJson) as JsonObject;

        Assert.NotNull(origDoc);
        Assert.NotNull(importedDoc);

        // Check Rundown
        Assert.Equal(
            origDoc["rundown"]?["name"]?.ToString(),
            importedDoc["rundown"]?["name"]?.ToString());

        // Check Level Layouts count
        var origLevels = origDoc["levels"] as JsonArray;
        var importedLevels = importedDoc["levels"] as JsonArray;
        Assert.NotNull(origLevels);
        Assert.NotNull(importedLevels);
        Assert.Equal(origLevels.Count, importedLevels.Count);

        // Check first level properties
        var origL0 = origLevels[0] as JsonObject;
        var impL0 = importedLevels[0] as JsonObject;
        Assert.Equal(origL0?["name"]?.ToString(), impL0?["name"]?.ToString());
        Assert.Equal(origL0?["complexType"]?.ToString(), impL0?["complexType"]?.ToString());
        Assert.Equal(origL0?["ElevatorTile"]?.ToString(), impL0?["ElevatorTile"]?.ToString());

        // Check lighting and fog imported
        Assert.Contains(importedVm.Lighting.Blocks, b => b.PersistentId == light.PersistentId);
        Assert.Contains(importedVm.FogSettings.Blocks, b => b.PersistentId == fog.PersistentId);
        Assert.Contains(importedVm.Consumables.Blocks, b => b.PersistentId == cons.PersistentId);
        Assert.Contains(importedVm.Alarms.Alarms, a => a.PersistentId == alarm.PersistentId);
    }

    [Fact]
    public void Import_MissingLgTunerConfig_SkipsLevelAndNotifiesUser()
    {
        var vm = new MainWindowViewModel();
        var level = vm.LevelLayout.Levels[0];
        level.PersistentId = 999;
        level.Name = "NoConfig_Level";

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.PublicName = level.Name;
        rundown.TierA[0].LevelLayoutData = level.PersistentId;

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedNoConfig");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success);

        // Delete LGTuner file
        string lgTunerDir = Path.Combine(exportDir, "Custom", "LGTunerAdvanced");
        if (Directory.Exists(lgTunerDir))
        {
            foreach (var f in Directory.GetFiles(lgTunerDir, "*.json"))
            {
                File.Delete(f);
            }
        }

        // Import into fresh VM
        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);

        Assert.Contains(importResult.SkippedLevels, s => s.Contains("NoConfig_Level") || s.Contains("999"));
        Assert.NotEmpty(importResult.FailedLevels);
        var failure = importResult.FailedLevels[0];
        Assert.Equal("NoConfig_Level", failure.PublicName);
        Assert.Equal(999u, failure.MainLayoutId);
        Assert.Contains(failure.MissingConfigs, mc => mc.Contains("Main Sector") && mc.Contains("999"));

        Assert.True(importedVm.IsShowingImportNotification);
        Assert.NotEmpty(importedVm.ImportFailedLevels);
        Assert.Empty(importedVm.LevelLayout.Levels);

        // Closing notification
        importedVm.CloseImportNotification();
        Assert.False(importedVm.IsShowingImportNotification);
    }

    [Fact]
    public void Import_MissingSecondarySectorConfig_IdentifiesMissingSectorAndSkipsLevel()
    {
        var vm = new MainWindowViewModel();
        var level = vm.LevelLayout.Levels[0];
        level.PersistentId = 801;
        level.Name = "SecConfigTest";

        var geo = level.GridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_02.prefab", 2, 0, initialAreaIndex: 0);
        level.GridState.PlacedGeomorphs.Add(geo);
        geo.GenerateRoom(0);
        geo.SetRoomSector(0, SectorType.Secondary);
        geo.SetRoomZone(0, 0);
        level.GridState.CreateRoomNode(geo, 0);

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.PublicName = level.Name;
        rundown.TierA[0].LevelLayoutData = level.PersistentId;
        rundown.TierA[0].SecondaryLayerEnabled = true;

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedMissingSecConfig");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success);

        // Delete Secondary LGTuner file (the one that is not 801)
        string lgTunerDir = Path.Combine(exportDir, "Custom", "LGTunerAdvanced");
        var secFile = Directory.GetFiles(lgTunerDir, "*.json")
            .FirstOrDefault(f => !Path.GetFileName(f).Contains("801"));
        Assert.NotNull(secFile);
        File.Delete(secFile);

        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);

        Assert.NotEmpty(importResult.FailedLevels);
        var failure = importResult.FailedLevels[0];
        Assert.Contains(failure.MissingConfigs, mc => mc.Contains("Secondary Sector"));
        Assert.True(importedVm.IsShowingImportNotification);
        Assert.Empty(importedVm.LevelLayout.Levels);
    }

    [Fact]
    public void Import_MissingOverloadSectorConfig_IdentifiesMissingSectorAndSkipsLevel()
    {
        var vm = new MainWindowViewModel();
        var level = vm.LevelLayout.Levels[0];
        level.PersistentId = 811;
        level.Name = "OverloadConfigTest";

        var geo = level.GridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_03.prefab", 2, 0, initialAreaIndex: 0);
        level.GridState.PlacedGeomorphs.Add(geo);
        geo.GenerateRoom(0);
        geo.SetRoomSector(0, SectorType.Overload);
        geo.SetRoomZone(0, 0);
        level.GridState.CreateRoomNode(geo, 0);

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.PublicName = level.Name;
        rundown.TierA[0].LevelLayoutData = level.PersistentId;
        rundown.TierA[0].ThirdLayerEnabled = true;

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedMissingOverloadConfig");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success);

        // Delete Overload LGTuner file (the one that is not 811)
        string lgTunerDir = Path.Combine(exportDir, "Custom", "LGTunerAdvanced");
        var ovFile = Directory.GetFiles(lgTunerDir, "*.json")
            .FirstOrDefault(f => !Path.GetFileName(f).Contains("811"));
        Assert.NotNull(ovFile);
        File.Delete(ovFile);

        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);

        Assert.NotEmpty(importResult.FailedLevels);
        var failure = importResult.FailedLevels[0];
        Assert.Contains(failure.MissingConfigs, mc => mc.Contains("Overload Sector"));
        Assert.True(importedVm.IsShowingImportNotification);
        Assert.Empty(importedVm.LevelLayout.Levels);
    }

    [Fact]
    public void Import_MissingDimensionConfig_IdentifiesMissingDimensionAndSkipsLevel()
    {
        var vm = new MainWindowViewModel();
        var level = vm.LevelLayout.Levels[0];
        level.PersistentId = 821;
        level.Name = "DimensionConfigTest";
        level.Dimensions.Add(new DimensionLayoutBlock
        {
            Name = "Dimension_Beta",
            DimensionGeomorph = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_dim_02.prefab",
            LevelLayoutData = 822
        });

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.PublicName = level.Name;
        rundown.TierA[0].LevelLayoutData = level.PersistentId;

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedMissingDimConfig");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success);

        // Delete Dimension LGTuner file (LevelGen_822.json)
        string dimFile = Path.Combine(exportDir, "Custom", "LGTunerAdvanced", "LevelGen_822.json");
        if (File.Exists(dimFile)) File.Delete(dimFile);

        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);

        Assert.NotEmpty(importResult.FailedLevels);
        var failure = importResult.FailedLevels[0];
        Assert.Contains(failure.MissingConfigs, mc => mc.Contains("Dimension") && mc.Contains("822"));
        Assert.True(importedVm.IsShowingImportNotification);
        Assert.Empty(importedVm.LevelLayout.Levels);
    }

    [Fact]
    public void Import_MultipleFailedLevels_ReportsAllFailedLevelsWithSpecificMissingConfigs()
    {
        var vm = new MainWindowViewModel();

        // Level 1: A1 (missing Main config)
        var level1 = vm.LevelLayout.Levels[0];
        level1.PersistentId = 910;
        level1.Name = "Level_A1";

        // Level 2: A2 (missing Secondary config)
        var level2 = new LevelLayoutBlock { PersistentId = 920, Name = "Level_A2" };
        level2.GridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_01.prefab", 0, 0);
        var geo2 = level2.GridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_02.prefab", 2, 0, initialAreaIndex: 0);
        level2.GridState.PlacedGeomorphs.Add(geo2);
        geo2.GenerateRoom(0);
        geo2.SetRoomSector(0, SectorType.Secondary);
        geo2.SetRoomZone(0, 0);
        level2.GridState.CreateRoomNode(geo2, 0);
        vm.LevelLayout.Levels.Add(level2);

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.Prefix = "A1";
        rundown.TierA[0].Descriptive.PublicName = "Level_A1";
        rundown.TierA[0].LevelLayoutData = 910;

        var expA2 = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = 920,
            SecondaryLayerEnabled = true,
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = "A2",
                PublicName = "Level_A2"
            }
        };
        rundown.TierA.Add(expA2);

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedMultipleFailures");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success);

        // Delete Main config for A1 (LevelGen_910.json) and Main config for A2 (LevelGen_920.json)
        string a1MainFile = Path.Combine(exportDir, "Custom", "LGTunerAdvanced", "LevelGen_910.json");
        if (File.Exists(a1MainFile)) File.Delete(a1MainFile);

        string a2MainFile = Path.Combine(exportDir, "Custom", "LGTunerAdvanced", "LevelGen_920.json");
        if (File.Exists(a2MainFile)) File.Delete(a2MainFile);

        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);

        Assert.Equal(2, importResult.FailedLevels.Count);

        var failA1 = importResult.FailedLevels.FirstOrDefault(f => f.Prefix == "A1" || f.PublicName == "Level_A1");
        Assert.NotNull(failA1);
        Assert.Contains(failA1.MissingConfigs, mc => mc.Contains("Main Sector") && mc.Contains("910"));

        var failA2 = importResult.FailedLevels.FirstOrDefault(f => f.Prefix == "A2" || f.PublicName == "Level_A2");
        Assert.NotNull(failA2);
        Assert.Contains(failA2.MissingConfigs, mc => mc.Contains("Main Sector") && mc.Contains("920"));

        Assert.True(importedVm.IsShowingImportNotification);
        Assert.Equal(2, importedVm.ImportFailedLevels.Count);
        Assert.Empty(importedVm.LevelLayout.Levels);
    }

    [Fact]
    public void Import_MixedSuccessAndFailures_LoadsValidLevelsAndReportsDetailedNotification()
    {
        var vm = new MainWindowViewModel();

        // Level 1: A1 (valid level)
        var level1 = vm.LevelLayout.Levels[0];
        level1.PersistentId = 951;
        level1.Name = "Valid_A1";

        // Level 2: A2 (missing Main config)
        var level2 = new LevelLayoutBlock { PersistentId = 952, Name = "Fail_A2" };
        vm.LevelLayout.Levels.Add(level2);

        // Level 3: A3 (missing Dimension config)
        var level3 = new LevelLayoutBlock { PersistentId = 953, Name = "Fail_A3" };
        level3.Dimensions.Add(new DimensionLayoutBlock
        {
            Name = "Dimension_Gamma",
            DimensionGeomorph = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_dim_03.prefab",
            LevelLayoutData = 954
        });
        vm.LevelLayout.Levels.Add(level3);

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.Prefix = "A1";
        rundown.TierA[0].Descriptive.PublicName = "Valid_A1";
        rundown.TierA[0].LevelLayoutData = 951;

        var expA2 = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = 952,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A2", PublicName = "Fail_A2" }
        };
        rundown.TierA.Add(expA2);

        var expA3 = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = 953,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A3", PublicName = "Fail_A3" }
        };
        rundown.TierA.Add(expA3);

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedMixed");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success);

        // Delete Main config for A2 (LevelGen_952.json) and Dimension config for A3 (LevelGen_954.json)
        string a2MainFile = Path.Combine(exportDir, "Custom", "LGTunerAdvanced", "LevelGen_952.json");
        if (File.Exists(a2MainFile)) File.Delete(a2MainFile);

        string a3DimFile = Path.Combine(exportDir, "Custom", "LGTunerAdvanced", "LevelGen_954.json");
        if (File.Exists(a3DimFile)) File.Delete(a3DimFile);

        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);

        Assert.Equal(2, importResult.FailedLevels.Count);

        var failA2 = importResult.FailedLevels.FirstOrDefault(f => f.Prefix == "A2" || f.PublicName == "Fail_A2");
        Assert.NotNull(failA2);
        Assert.Contains(failA2.MissingConfigs, mc => mc.Contains("Main Sector") && mc.Contains("952"));

        var failA3 = importResult.FailedLevels.FirstOrDefault(f => f.Prefix == "A3" || f.PublicName == "Fail_A3");
        Assert.NotNull(failA3);
        Assert.Contains(failA3.MissingConfigs, mc => mc.Contains("Dimension") && mc.Contains("954"));

        Assert.True(importedVm.IsShowingImportNotification);
        Assert.Equal(2, importedVm.ImportFailedLevels.Count);

        // Valid level A1 should be successfully imported and loaded!
        Assert.Single(importedVm.LevelLayout.Levels);
        Assert.Equal("Valid_A1", importedVm.LevelLayout.Levels[0].Name);
    }

    [Fact]
    public void ExportAndImport_MultiSectorAndDimensions_PreservesConfiguration()
    {
        var vm = new MainWindowViewModel();
        var level = vm.LevelLayout.Levels[0];
        level.PersistentId = 701;
        level.Name = "Test_MultiSector";
        level.Dimensions.Add(new DimensionLayoutBlock
        {
            Name = "Dimension_Alpha",
            DimensionGeomorph = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_dim_01.prefab",
            LevelLayoutData = 701
        });

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();

        grid.InitializeStartingTile(level.ElevatorTile, 0, 0);

        // Place Main Geomorph
        var geoMain = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_01.prefab", 1, 0, initialAreaIndex: 0);
        grid.PlacedGeomorphs.Add(geoMain);
        geoMain.GenerateRoom(0);
        geoMain.SetRoomSector(0, SectorType.Main);
        geoMain.SetRoomZone(0, 0);
        grid.CreateRoomNode(geoMain, 0);

        // Place Secondary Geomorph
        var geoSec = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_02.prefab", 2, 0, initialAreaIndex: 0);
        grid.PlacedGeomorphs.Add(geoSec);
        geoSec.GenerateRoom(0);
        geoSec.SetRoomSector(0, SectorType.Secondary);
        geoSec.SetRoomZone(0, 0);
        grid.CreateRoomNode(geoSec, 0);

        // Connect Main to Secondary with Bulkhead door
        var doorMain = grid.PlacedDoors.FirstOrDefault(d => d.LocalX == 1 && d.LocalY == 0);
        var doorSec = grid.PlacedDoors.FirstOrDefault(d => d.LocalX == 2 && d.LocalY == 0);
        if (doorMain != null && doorSec != null)
        {
            doorMain.IsConnected = true;
            doorMain.ConnectedDoor = doorSec;
            doorSec.IsConnected = true;
            doorSec.ConnectedDoor = doorMain;
            doorMain.IsSecondaryDoor = true;
            doorSec.IsSecondaryDoor = true;
        }

        level.SecondarySector.ZoneAliasStart = 100;
        level.SecondarySector.Objectives[0].ObjectiveType = ObjectiveType.ClearAPath;
        level.SecondarySector.Objectives[0].Header = "Secondary Sector Clear Path";

        var rundown = vm.Rundown.SelectedRundown!;
        rundown.TierA[0].Descriptive.PublicName = level.Name;
        rundown.TierA[0].LevelLayoutData = level.PersistentId;
        rundown.TierA[0].SecondaryLayerEnabled = true;

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedMultiSector");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        // Import
        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        var impLevel = importedVm.LevelLayout.Levels.FirstOrDefault();
        Assert.NotNull(impLevel);
        Assert.Equal(level.Name, impLevel.Name);
        Assert.NotEmpty(impLevel.Dimensions);
        Assert.Equal("Dimension_Alpha", impLevel.Dimensions[0].Name);
    }

    [Fact]
    public void TestRundown_ExportAndImport_Cycle_MatchesOriginal()
    {
        string testRundownPath = Path.Combine(_tempDir, "Test Rundown.rundown");
        File.WriteAllText(testRundownPath, TestRundownJson);

        var originalVm = new MainWindowViewModel();
        var origProject = RundownLoader.Instance.LoadProject(testRundownPath);
        RundownLoader.Instance.ApplyProject(originalVm, origProject, testRundownPath);

        var origLevel = originalVm.LevelLayout.Levels[0];
        Assert.Equal(5, origLevel.GridState.PlacedGeomorphs.Count);

        string exportDir = Path.Combine(_tempDir, "ExportedTestRundown");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        var importedVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        Assert.Single(importedVm.LevelLayout.Levels);
        var impLevel = importedVm.LevelLayout.Levels[0];

        // Check geomorph count
        Assert.Equal(origLevel.GridState.PlacedGeomorphs.Count, impLevel.GridState.PlacedGeomorphs.Count);

        // Check rooms count
        Assert.Equal(origLevel.GridState.PlacedRooms.Count, impLevel.GridState.PlacedRooms.Count);

        // Check geomorph 3 rooms
        var origGeo3 = origLevel.GridState.PlacedGeomorphs.First(g => g.GridX == -1 && g.GridY == -1);
        var impGeo3 = impLevel.GridState.PlacedGeomorphs.First(g => g.GridX == -1 && g.GridY == -1);
        Assert.Equal(origGeo3.GeneratedRooms.OrderBy(r => r), impGeo3.GeneratedRooms.OrderBy(r => r));
        foreach (int area in origGeo3.GeneratedRooms)
        {
            Assert.Equal(origGeo3.GetRoomSector(area), impGeo3.GetRoomSector(area));
            Assert.Equal(origGeo3.GetRoomZone(area), impGeo3.GetRoomZone(area));
        }

        // Check overload geomorph
        var overloadGeo = impLevel.GridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == -1 && g.GridY == -2);
        Assert.NotNull(overloadGeo);
        Assert.Equal(SectorType.Overload, overloadGeo.GetRoomSector(0));

        // Check geomorph 5 zone
        var geo5 = impLevel.GridState.PlacedGeomorphs.First(g => g.GridX == 0 && g.GridY == -2);
        Assert.Equal(1, geo5.GetRoomZone(0));

        // Check security doors
        var secDoors = impLevel.GridState.PlacedDoors.Where(d => d.IsSecurityDoor).ToList();
        Assert.NotEmpty(secDoors);

        // Check internal security door (between room 0 and room 5 in Geo3)
        var internalSecDoor = impGeo3.Doors.FirstOrDefault(d => d.IsInternal && d.IsSecurityDoor);
        Assert.NotNull(internalSecDoor);

        // Check overload door
        var overloadDoors = impLevel.GridState.PlacedDoors.Where(d => d.IsOverloadDoor).ToList();
        Assert.NotEmpty(overloadDoors);

        // Save imported VM back to a .rundown file
        string savedRundownPath = Path.Combine(_tempDir, "Imported_Test_Rundown.rundown");
        RundownProjectService.SaveProject(importedVm, savedRundownPath);
        Assert.True(File.Exists(savedRundownPath));

        // Reload saved project and verify
        var reloadedVm = new MainWindowViewModel();
        var reloadedProject = RundownLoader.Instance.LoadProject(savedRundownPath);
        RundownLoader.Instance.ApplyProject(reloadedVm, reloadedProject, savedRundownPath);

        Assert.Single(reloadedVm.LevelLayout.Levels);
        var relLevel = reloadedVm.LevelLayout.Levels[0];
        Assert.Equal(origLevel.GridState.PlacedGeomorphs.Count, relLevel.GridState.PlacedGeomorphs.Count);
        Assert.Equal(origLevel.GridState.PlacedRooms.Count, relLevel.GridState.PlacedRooms.Count);

        var relOverloadGeo = relLevel.GridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == -1 && g.GridY == -2);
        Assert.NotNull(relOverloadGeo);
        Assert.Equal(SectorType.Overload, relOverloadGeo.GetRoomSector(0));
    }

    private const string TestRundownJson = @"{
  ""fileVersion"": 1,
  ""createdAt"": ""2026-10-23T18:10:32.1171346Z"",
  ""modifiedAt"": ""2026-10-23T18:10:32.1176028Z"",
  ""lastExportDirectory"": ""C:\\Users\\User\\Desktop\\Rundown Tests\\First Test\\"",
  ""rundown"": {
    ""name"": ""New Rundown"",
    ""internalEnabled"": true,
    ""persistentID"": 1,
    ""NeverShowRundownTree"": false,
    ""UseTierUnlockRequirements"": true,
    ""ReqToReachTierA"": {
      ""MainSectors"": 0,
      ""SecondarySectors"": 0,
      ""ThirdSectors"": 0,
      ""AllClearedSectors"": 0
    },
    ""ReqToReachTierB"": {
      ""MainSectors"": 0,
      ""SecondarySectors"": 0,
      ""ThirdSectors"": 0,
      ""AllClearedSectors"": 0
    },
    ""ReqToReachTierC"": {
      ""MainSectors"": 0,
      ""SecondarySectors"": 0,
      ""ThirdSectors"": 0,
      ""AllClearedSectors"": 0
    },
    ""ReqToReachTierD"": {
      ""MainSectors"": 0,
      ""SecondarySectors"": 0,
      ""ThirdSectors"": 0,
      ""AllClearedSectors"": 0
    },
    ""ReqToReachTierE"": {
      ""MainSectors"": 0,
      ""SecondarySectors"": 0,
      ""ThirdSectors"": 0,
      ""AllClearedSectors"": 0
    },
    ""StorytellingData"": {
      ""Title"": ""RUNDOWN EXAMPLE\nTITLE: EXAMPLE"",
      ""TextLog"": 0,
      ""TextLogPos"": {
        ""x"": 0,
        ""y"": 0
      },
      ""Visuals"": {
        ""ColorBackground"": {
          ""a"": 1,
          ""r"": 0,
          ""g"": 0,
          ""b"": 0
        },
        ""TierAVisuals"": {
          ""Color"": {
            ""a"": 1,
            ""r"": 0.97,
            ""g"": 0.88,
            ""b"": 0.79
          },
          ""Scale"": 0.65,
          ""ScaleYModifier"": 0.3
        },
        ""TierBVisuals"": {
          ""Color"": {
            ""a"": 1,
            ""r"": 0.91,
            ""g"": 0.77,
            ""b"": 0.66
          },
          ""Scale"": 0.65,
          ""ScaleYModifier"": 0.3
        },
        ""TierCVisuals"": {
          ""Color"": {
            ""a"": 1,
            ""r"": 0.81,
            ""g"": 0.66,
            ""b"": 0.54
          },
          ""Scale"": 0.65,
          ""ScaleYModifier"": 0.3
        },
        ""TierDVisuals"": {
          ""Color"": {
            ""a"": 1,
            ""r"": 0.68,
            ""g"": 0.49,
            ""b"": 0.32
          },
          ""Scale"": 0.65,
          ""ScaleYModifier"": 0.3
        },
        ""TierEVisuals"": {
          ""Color"": {
            ""a"": 1,
            ""r"": 0.68,
            ""g"": 0.41,
            ""b"": 0.18
          },
          ""Scale"": 0.65,
          ""ScaleYModifier"": 0.3
        }
      },
      ""SurfaceIconPosition"": {
        ""x"": 0,
        ""y"": 0
      },
      ""SurfaceDescription"": """",
      ""ExternalExpTitle"": 0
    },
    ""VanityItemLayerDropDataBlock"": 0,
    ""TierA"": [
      {
        ""Enabled"": true,
        ""ExcludeFromMatchmaking"": false,
        ""IsSinglePlayer"": false,
        ""SkipLobby"": false,
        ""PutIconAboveTier"": false,
        ""DisablePlayerVoicelines"": false,
        ""ExcludeFromProgression"": false,
        ""HideOnLocked"": false,
        ""HideRolePlayDataOnLocked"": false,
        ""Accessibility"": ""UseCustomProgressionLock"",
        ""UnlockedByExpedition"": {
          ""Tier"": ""TierA"",
          ""Exp"": ""Exp1""
        },
        ""CustomProgressionLock"": {
          ""MainSectors"": 0,
          ""SecondarySectors"": 0,
          ""ThirdSectors"": 0,
          ""AllClearedSectors"": 0
        },
        ""Descriptive"": {
          ""Prefix"": ""A"",
          ""PublicName"": ""Level A1"",
          ""IsExtraExpedition"": false,
          ""SkipExpNumberInName"": false,
          ""UseCustomMatchmakingTier"": false,
          ""CustomMatchmakingTier"": ""TierA"",
          ""ProgressionVisualStyle"": ""Normal"",
          ""ExpeditionDepth"": 600,
          ""EstimatedDuration"": 900,
          ""ExpeditionDescription"": ""Test Rundown A1"",
          ""RoleplayedWardenIntel"": "">> It\u0027s almost like we\u0027re in...\r\n>> some kind of <size=200%><color=red>test?</color></size>""
        },
        ""Seeds"": {
          ""BuildSeed"": 0,
          ""FunctionMarkerOffset"": 0,
          ""StandardMarkerOffset"": 0,
          ""LightJobSeedOffset"": 0
        },
        ""Expedition"": {
          ""ComplexResourceData"": 1,
          ""MLSLevelKit"": 0,
          ""LightSettings"": 1,
          ""FogSettings"": 0,
          ""EnemyPopulation"": 0,
          ""ExpeditionBalance"": 0,
          ""ScoutWaveSettings"": 0,
          ""ScoutWavePopulation"": 0,
          ""EnvironmentWetness"": 0,
          ""DustColor"": {
            ""a"": 1,
            ""r"": 0.5,
            ""g"": 0.5,
            ""b"": 0.5
          },
          ""DustTurbulence"": 1
        },
        ""VanityItemsDropData"": null,
        ""UseGearPicker"": false,
        ""GearPicker"": null,
        ""LevelLayoutData"": 1,
        ""MainLayerData"": {
          ""ZonesWithBulkheadEntrance"": null,
          ""BulkheadDoorControllerPlacements"": null,
          ""BulkheadKeyPlacements"": null,
          ""ObjectiveData"": {
            ""DataBlockId"": 0,
            ""WinCondition"": ""GoToElevator"",
            ""ZonePlacementDatas"": null
          },
          ""ChainedObjectiveData"": null,
          ""ArtifactData"": null
        },
        ""SecondaryLayerEnabled"": false,
        ""SecondaryLayout"": 0,
        ""BuildSecondaryFrom"": {
          ""LayerType"": ""MainLayer"",
          ""Zone"": ""Zone_0""
        },
        ""SecondaryLayerData"": {
          ""ZonesWithBulkheadEntrance"": null,
          ""BulkheadDoorControllerPlacements"": null,
          ""BulkheadKeyPlacements"": null,
          ""ObjectiveData"": {
            ""DataBlockId"": 0,
            ""WinCondition"": ""GoToElevator"",
            ""ZonePlacementDatas"": null
          },
          ""ChainedObjectiveData"": null,
          ""ArtifactData"": null
        },
        ""ThirdLayerEnabled"": true,
        ""ThirdLayout"": 0,
        ""BuildThirdFrom"": {
          ""LayerType"": ""MainLayer"",
          ""Zone"": ""Zone_2""
        },
        ""ThirdLayerData"": {
          ""ZonesWithBulkheadEntrance"": null,
          ""BulkheadDoorControllerPlacements"": null,
          ""BulkheadKeyPlacements"": null,
          ""ObjectiveData"": {
            ""DataBlockId"": 0,
            ""WinCondition"": ""GoToElevator"",
            ""ZonePlacementDatas"": null
          },
          ""ChainedObjectiveData"": null,
          ""ArtifactData"": null
        },
        ""DimensionDatas"": null,
        ""SoundEventOnWarpToReality"": 0,
        ""SpecialOverrideData"": {
          ""WeakResourceContainerWithPackChanceForLocked"": -1,
          ""InfectionLevelAtExpeditionStart"": 0,
          ""HealthLevelAtExpeditionStart"": 1,
          ""StandardAmmoAtExpeditionStart"": 1,
          ""SpecialAmmoAtExpeditionStart"": 1,
          ""ToolAmmoAtExpeditionStart"": 1,
          ""ShowCreditsAfterSuccess"": false
        },
        ""HasExternalStyle"": false,
        ""HasStoryStyle"": false
      }
    ],
    ""TierB"": [],
    ""TierC"": [],
    ""TierD"": [],
    ""TierE"": []
  },
  ""levels"": [
    {
      ""name"": ""Level 1"",
      ""persistentID"": 1,
      ""complexType"": ""Mining"",
      ""ComplexResourceSet"": 0,
      ""ElevatorTile"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab"",
      ""fogSettings"": 0,
      ""internalEnabled"": true,
      ""Dimensions"": [],
      ""mainSector"": {
        ""artifactAmountMultiplier"": 1,
        ""zoneAliasStart"": 100,
        ""bulkheadKeyPlacements"": [],
        ""objectives"": []
      },
      ""secondarySector"": {
        ""artifactAmountMultiplier"": 1,
        ""zoneAliasStart"": -1,
        ""bulkheadKeyPlacements"": [],
        ""objectives"": []
      },
      ""overloadSector"": {
        ""artifactAmountMultiplier"": 1,
        ""zoneAliasStart"": 0,
        ""bulkheadKeyPlacements"": [],
        ""objectives"": []
      },
      ""gridState"": {
        ""gridSize"": 40,
        ""cellDimension"": 64,
        ""altitudeOffset"": 6,
        ""zoom"": 4,
        ""panX"": 422.5,
        ""panY"": 512,
        ""currentStartingPrefab"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab"",
        ""nextZoneIndex"": 4,
        ""placedGeomorphs"": [
          {
            ""id"": ""d2f2aa20-5953-46ae-a316-6b08d5e24615"",
            ""prefabPath"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab"",
            ""displayName"": ""geo_32x32_elevator_shaft_dig_site_01"",
            ""gridX"": 0,
            ""gridY"": 0,
            ""rotationAngle"": 0,
            ""altitude"": ""Mid"",
            ""altitudeOffset"": 6,
            ""lighting"": 0,
            ""generatedRooms"": [
              0
            ],
            ""roomZones"": {
              ""0"": 0
            },
            ""roomSectors"": {
              ""0"": ""Main""
            }
          },
          {
            ""id"": ""d47ccfef-2802-428d-b064-644e16a2e9d3"",
            ""prefabPath"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_02_v2.prefab"",
            ""displayName"": ""geo_64x64_mining_dig_site_HA_02_v2"",
            ""gridX"": 0,
            ""gridY"": -1,
            ""rotationAngle"": 0,
            ""altitude"": ""Mid"",
            ""altitudeOffset"": 6,
            ""lighting"": 0,
            ""generatedRooms"": [
              0
            ],
            ""roomZones"": {
              ""0"": 0
            },
            ""roomSectors"": {
              ""0"": ""Main""
            }
          },
          {
            ""id"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""prefabPath"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01_v2.prefab"",
            ""displayName"": ""geo_64x64_mining_dig_site_HA_01_v2"",
            ""gridX"": -1,
            ""gridY"": -1,
            ""rotationAngle"": 180,
            ""altitude"": ""Mid"",
            ""altitudeOffset"": 6,
            ""lighting"": 0,
            ""generatedRooms"": [
              2,
              0,
              5,
              4,
              3
            ],
            ""roomZones"": {
              ""2"": 0,
              ""0"": 0,
              ""5"": 2,
              ""4"": 2,
              ""3"": 2
            },
            ""roomSectors"": {
              ""2"": ""Main"",
              ""0"": ""Main"",
              ""5"": ""Main"",
              ""4"": ""Main"",
              ""3"": ""Main""
            }
          },
          {
            ""id"": ""f91eaa57-b03b-4164-bed4-440d1bcd1f03"",
            ""prefabPath"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_04_AW.prefab"",
            ""displayName"": ""geo_64x64_mining_dig_site_HA_04_AW"",
            ""gridX"": -1,
            ""gridY"": -2,
            ""rotationAngle"": 0,
            ""altitude"": ""Mid"",
            ""altitudeOffset"": 6,
            ""lighting"": 62888294,
            ""generatedRooms"": [
              0
            ],
            ""roomZones"": {
              ""0"": 0
            },
            ""roomSectors"": {
              ""0"": ""Overload""
            }
          },
          {
            ""id"": ""afe60832-0ef9-4c3f-9d9b-b902159e6b4e"",
            ""prefabPath"": ""Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_03_AW.prefab"",
            ""displayName"": ""geo_64x64_mining_dig_site_HA_03_AW"",
            ""gridX"": 0,
            ""gridY"": -2,
            ""rotationAngle"": 0,
            ""altitude"": ""High"",
            ""altitudeOffset"": 6,
            ""lighting"": 62888294,
            ""generatedRooms"": [
              0
            ],
            ""roomZones"": {
              ""0"": 1
            },
            ""roomSectors"": {
              ""0"": ""Main""
            }
          }
        ],
        ""placedDoors"": [
          {
            ""id"": ""7a69a2f8-b69e-4d78-872c-4a40b5a6f8ef"",
            ""geomorphId"": ""d2f2aa20-5953-46ae-a316-6b08d5e24615"",
            ""expanderIndex"": 0,
            ""areaFrom"": 0,
            ""areaTo"": -1,
            ""localX"": 0,
            ""localY"": -32,
            ""worldX"": 32,
            ""worldY"": 0,
            ""direction"": ""Forwards"",
            ""rotationAngle"": 0,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""2436bf89-3246-4c04-bcff-b79307bea181"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""3ddebf7e-b53a-4ce8-bc96-e006078ee864"",
            ""geomorphId"": ""d47ccfef-2802-428d-b064-644e16a2e9d3"",
            ""expanderIndex"": 2,
            ""areaFrom"": 0,
            ""areaTo"": -1,
            ""localX"": 0,
            ""localY"": -32,
            ""worldX"": 32,
            ""worldY"": -64,
            ""direction"": ""Forwards"",
            ""rotationAngle"": 0,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": true,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""4712c488-f1d3-4a28-915f-f438a9ff194c"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""2436bf89-3246-4c04-bcff-b79307bea181"",
            ""geomorphId"": ""d47ccfef-2802-428d-b064-644e16a2e9d3"",
            ""expanderIndex"": 3,
            ""areaFrom"": 0,
            ""areaTo"": -1,
            ""localX"": 0,
            ""localY"": 32,
            ""worldX"": 32,
            ""worldY"": 0,
            ""direction"": ""Backwards"",
            ""rotationAngle"": 179.9997,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""7a69a2f8-b69e-4d78-872c-4a40b5a6f8ef"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""774aa787-541a-455b-bbc6-ee35504395e3"",
            ""geomorphId"": ""d47ccfef-2802-428d-b064-644e16a2e9d3"",
            ""expanderIndex"": 4,
            ""areaFrom"": 0,
            ""areaTo"": -1,
            ""localX"": -32,
            ""localY"": 0,
            ""worldX"": 0,
            ""worldY"": -32,
            ""direction"": ""Left"",
            ""rotationAngle"": 269.9995,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""cef40616-20f6-4d32-947e-68f6bd03c4aa"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""cef40616-20f6-4d32-947e-68f6bd03c4aa"",
            ""geomorphId"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""expanderIndex"": 1,
            ""areaFrom"": 2,
            ""areaTo"": -1,
            ""localX"": 32,
            ""localY"": 0,
            ""worldX"": 0,
            ""worldY"": -32,
            ""direction"": ""Right"",
            ""rotationAngle"": 89.9997,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""774aa787-541a-455b-bbc6-ee35504395e3"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""a7cb3943-2389-4e7f-8232-5fd8d274d595"",
            ""geomorphId"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""expanderIndex"": 2,
            ""areaFrom"": 0,
            ""areaTo"": 2,
            ""localX"": -0.7609997,
            ""localY"": 5.4900017,
            ""worldX"": -32.7609997,
            ""worldY"": -26.5099983,
            ""direction"": ""InZoneArea"",
            ""rotationAngle"": 89.9999,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": null,
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""e80ecb64-f81b-4ee9-9e5b-962baa29ab44"",
            ""geomorphId"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""expanderIndex"": 3,
            ""areaFrom"": 0,
            ""areaTo"": 5,
            ""localX"": -10.933,
            ""localY"": -17.857,
            ""worldX"": -42.933,
            ""worldY"": -49.857,
            ""direction"": ""InZoneArea"",
            ""rotationAngle"": 224.9999,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": true,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": null,
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""986fdd7b-62a3-4324-b591-1e2048aa345f"",
            ""geomorphId"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""expanderIndex"": 2,
            ""areaFrom"": 4,
            ""areaTo"": 5,
            ""localX"": 4.6920013,
            ""localY"": -8.907,
            ""worldX"": -27.3079987,
            ""worldY"": -40.907,
            ""direction"": ""InZoneArea"",
            ""rotationAngle"": 179.9999,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": null,
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""0c0858d2-2ef2-47c9-a731-ff09d3ebdcd4"",
            ""geomorphId"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""expanderIndex"": 4,
            ""areaFrom"": 5,
            ""areaTo"": -1,
            ""localX"": 0,
            ""localY"": -32,
            ""worldX"": -32,
            ""worldY"": -64,
            ""direction"": ""Forwards"",
            ""rotationAngle"": 359.9997,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": true,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""43e1c04f-3f55-441e-809a-2089be88e7a0"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""9bf5080c-3b7b-4904-a34b-36b684e28ad9"",
            ""geomorphId"": ""462e858d-de05-40e0-a778-449d7a9ce1bd"",
            ""expanderIndex"": 0,
            ""areaFrom"": 3,
            ""areaTo"": 4,
            ""localX"": 4.65,
            ""localY"": 16.45,
            ""worldX"": -27.35,
            ""worldY"": -15.55,
            ""direction"": ""InZoneArea"",
            ""rotationAngle"": 0,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": null,
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""43e1c04f-3f55-441e-809a-2089be88e7a0"",
            ""geomorphId"": ""f91eaa57-b03b-4164-bed4-440d1bcd1f03"",
            ""expanderIndex"": 1,
            ""areaFrom"": 0,
            ""areaTo"": -1,
            ""localX"": 2.6702877E-05,
            ""localY"": 32,
            ""worldX"": -31.999973297123,
            ""worldY"": -64,
            ""direction"": ""Backwards"",
            ""rotationAngle"": 179.9999,
            ""altitude"": ""Mid"",
            ""isSecurityDoor"": false,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": true,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""0c0858d2-2ef2-47c9-a731-ff09d3ebdcd4"",
            ""chainedPuzzleToSolve"": 0
          },
          {
            ""id"": ""4712c488-f1d3-4a28-915f-f438a9ff194c"",
            ""geomorphId"": ""afe60832-0ef9-4c3f-9d9b-b902159e6b4e"",
            ""expanderIndex"": 0,
            ""areaFrom"": 0,
            ""areaTo"": -1,
            ""localX"": 0,
            ""localY"": 32,
            ""worldX"": 32,
            ""worldY"": -64,
            ""direction"": ""Backwards"",
            ""rotationAngle"": 180.0001,
            ""altitude"": ""High"",
            ""isSecurityDoor"": true,
            ""isMainDoor"": false,
            ""isSecondaryDoor"": false,
            ""isOverloadDoor"": false,
            ""isConnected"": true,
            ""isBlocked"": false,
            ""connectedDoorId"": ""3ddebf7e-b53a-4ce8-bc96-e006078ee864"",
            ""chainedPuzzleToSolve"": 0
          }
        ],
        ""zoneSettings"": [
          {
            ""sector"": ""Main"",
            ""zoneIndex"": 0,
            ""lighting"": 62888294
          },
          {
            ""sector"": ""Main"",
            ""zoneIndex"": 2,
            ""lighting"": 62888294
          },
          {
            ""sector"": ""Overload"",
            ""zoneIndex"": 0,
            ""lighting"": 62888294
          },
          {
            ""sector"": ""Main"",
            ""zoneIndex"": 1,
            ""lighting"": 62888294
          }
        ]
      }
    }
  ],
  ""alarms"": [],
  ""lighting"": [],
  ""complexResourceSets"": [],
  ""fogSettings"": [],
  ""consumableDistributions"": []
}";

    [Fact]
    public void DimensionLayout_SwitchingAndEditing_MaintainsSeparateLayouts()
    {
        var vm = new LevelLayoutViewModel();
        Assert.NotNull(vm.SelectedLevel);

        var level = vm.SelectedLevel;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";

        // Viewing Reality
        Assert.True(vm.IsViewingReality);
        Assert.Equal("Reality", vm.ActiveDimensionIndex);
        var realityGrid = vm.GridState;
        Assert.Equal(level.GridState, realityGrid);

        // Add Dimension 1
        var dim1 = new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Dimension 1 Alpha",
            DimensionGeomorph = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_mining_room_01.prefab",
            InternalEnabled = true
        };
        level.SetDimension(dim1);
        vm.RefreshAvailableDimensions();

        // Switch to Dimension 1
        var dim1Option = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");
        vm.SelectedDimension = dim1Option;

        Assert.False(vm.IsViewingReality);
        Assert.Equal("Dimension_1", vm.ActiveDimensionIndex);
        var dim1Grid = vm.GridState;
        Assert.NotSame(realityGrid, dim1Grid);
        Assert.Equal(dim1.GridState, dim1Grid);

        // Add a geomorph to Dimension 1
        var geoDim = dim1Grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_mining_room_02.prefab", 1, 0, initialAreaIndex: 0);
        dim1Grid.PlacedGeomorphs.Add(geoDim);

        Assert.Contains(geoDim, dim1Grid.PlacedGeomorphs);
        Assert.DoesNotContain(geoDim, realityGrid.PlacedGeomorphs);

        // Switch back to Reality
        var realityOption = vm.AvailableDimensions.First(d => d.DimensionIndex == "Reality");
        vm.SelectedDimension = realityOption;

        Assert.True(vm.IsViewingReality);
        Assert.Same(realityGrid, vm.GridState);
        Assert.DoesNotContain(geoDim, vm.GridState.PlacedGeomorphs);
    }

    [Fact]
    public void ExportAndImport_Roundtrip_PreservesDCsAndBulkheadKeysAndDimensions()
    {
        var originalVm = new MainWindowViewModel();
        var level = originalVm.LevelLayout.Levels[0];
        level.PersistentId = 601;
        level.Name = "Test_DCs_Keys_Dims";
        level.ComplexType = ComplexType.Tech;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();
        grid.InitializeStartingTile(level.ElevatorTile, 0, 0);

        var geoMain1 = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_01.prefab", 1, 0, initialAreaIndex: 0);
        grid.PlacedGeomorphs.Add(geoMain1);
        geoMain1.SetRoomSector(0, SectorType.Main);
        geoMain1.SetRoomZone(0, 1);
        grid.CreateRoomNode(geoMain1, 0);

        // Enable DC in Zone 1
        var zs1 = grid.GetOrCreateZoneSettings(SectorType.Main, 1);
        zs1.ContainsDC = true;

        // Configure Bulkhead Keys in Main Sector
        var keyPlacement = new GatherItemPlacement();
        keyPlacement.CandidateZones.Clear();
        keyPlacement.CandidateZones.Add(new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 1,
            PlacementStartWeight = 0.5,
            PlacementMiddleWeight = 0.3,
            PlacementEndWeight = 0.2
        });
        level.MainSector.BulkheadKeyPlacements.Add(keyPlacement);

        // Configure Dimension 1
        var dim1 = new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Dimension 1 Secret Area",
            ComplexType = ComplexType.Mining,
            DimensionGeomorph = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/ElevatorShaft_01.prefab",
            InternalEnabled = true
        };
        var dimGrid = dim1.GridState;
        dimGrid.InitializeStartingTile(dim1.DimensionGeomorph, 0, 0);
        var dimGeo2 = dimGrid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_mining_room_01.prefab", 1, 0, initialAreaIndex: 0);
        dimGrid.PlacedGeomorphs.Add(dimGeo2);
        dimGeo2.SetRoomSector(0, SectorType.Main);
        dimGeo2.SetRoomZone(0, 0);
        dimGrid.CreateRoomNode(dimGeo2, 0);

        level.Dimensions.Add(dim1);

        // Rundown Expedition
        var exp = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = level.PersistentId,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = level.Name }
        };
        originalVm.Rundown.SelectedRundown!.TierA.Clear();
        originalVm.Rundown.SelectedRundown!.TierA.Add(exp);

        // Export
        string exportDir = Path.Combine(_tempDir, "ExportedDCsAndDims");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        // Import
        var importVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        // Assert imported level
        var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == level.PersistentId || l.Name == level.Name);
        Assert.NotNull(importedLevel);

        // Assert DC
        var importedZs1 = importedLevel.GridState.GetOrCreateZoneSettings(SectorType.Main, 1);
        Assert.True(importedZs1.ContainsDC);

        // Assert Bulkhead Keys
        Assert.NotEmpty(importedLevel.MainSector.BulkheadKeyPlacements);
        var importedKey = importedLevel.MainSector.BulkheadKeyPlacements[0];
        Assert.NotEmpty(importedKey.CandidateZones);
        Assert.Equal(1, importedKey.CandidateZones[0].Zone);
        Assert.Equal("Reality", importedKey.CandidateZones[0].DimensionIndex);

        // Assert Dimensions
        Assert.NotEmpty(importedLevel.Dimensions);
        var importedDim1 = importedLevel.Dimensions.FirstOrDefault(d => d.DimensionIndex == "Dimension_1");
        Assert.NotNull(importedDim1);
        Assert.Equal("Dimension 1 Secret Area", importedDim1.Name);
        Assert.True(importedDim1.GridState.PlacedGeomorphs.Count >= 2);
    }

    [Fact]
    public void Export_LevelWithMainAndOverloadOnly_ExportsExactlyTwoObjectiveBlocks()
    {
        var vm = new MainWindowViewModel();
        var level = vm.LevelLayout.Levels[0];
        level.PersistentId = 701;
        level.Name = "Test_Main_Overload";
        level.ComplexType = ComplexType.Tech;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();
        grid.InitializeStartingTile(level.ElevatorTile, 0, 0);

        // Add Overload geomorph and zone
        var geoOverload = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_01.prefab", 1, 0, initialAreaIndex: 0);
        grid.PlacedGeomorphs.Add(geoOverload);
        geoOverload.SetRoomSector(0, SectorType.Overload);
        geoOverload.SetRoomZone(0, 0);
        grid.CreateRoomNode(geoOverload, 0);

        var exp = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = level.PersistentId,
            ThirdLayerEnabled = true,
            SecondaryLayerEnabled = false,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = level.Name }
        };
        vm.Rundown.SelectedRundown!.TierA.Clear();
        vm.Rundown.SelectedRundown!.TierA.Add(exp);

        string exportDir = Path.Combine(_tempDir, "ExportMainOverloadOnly");
        var exportResult = DatablockManager.Instance.ExportDatablocks(vm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        string wardenObjPath = Path.Combine(exportDir, "GameData_WardenObjectiveDataBlock_bin.json");
        Assert.True(File.Exists(wardenObjPath));

        var json = File.ReadAllText(wardenObjPath);
        using var doc = JsonDocument.Parse(json);
        var blocks = doc.RootElement.GetProperty("Blocks");
        // Exactly two objective blocks: Main and Overload (no Secondary)
        Assert.Equal(2, blocks.GetArrayLength());

        // Check Rundown layer objective references
        string rundownPath = Path.Combine(exportDir, "GameData_RundownDataBlock_bin.json");
        var rundownJson = File.ReadAllText(rundownPath);
        using var rundownDoc = JsonDocument.Parse(rundownJson);
        var rundownExp = rundownDoc.RootElement.GetProperty("Blocks")[0].GetProperty("TierA")[0];
        Assert.True(rundownExp.GetProperty("MainLayerData").GetProperty("ObjectiveData").GetProperty("DataBlockId").GetUInt32() > 0);
        Assert.Equal(0u, rundownExp.GetProperty("SecondaryLayerData").GetProperty("ObjectiveData").GetProperty("DataBlockId").GetUInt32());
        Assert.False(rundownExp.GetProperty("SecondaryLayerEnabled").GetBoolean());
        Assert.True(rundownExp.GetProperty("ThirdLayerData").GetProperty("ObjectiveData").GetProperty("DataBlockId").GetUInt32() > 0);
        Assert.True(rundownExp.GetProperty("ThirdLayerEnabled").GetBoolean());

        // ChainedObjectiveData should be empty for 1 objective
        Assert.Equal(0, rundownExp.GetProperty("MainLayerData").GetProperty("ChainedObjectiveData").GetArrayLength());
    }

    [Fact]
    public void ExportAndImport_WavesOnElevatorLand_DisabledByDefault_PreservesEnabledState()
    {
        var originalVm = new MainWindowViewModel();
        var level = originalVm.LevelLayout.Levels[0];
        level.PersistentId = 801;
        level.Name = "Test_Waves_Check";
        level.ComplexType = ComplexType.Tech;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();
        grid.InitializeStartingTile(level.ElevatorTile, 0, 0);

        // Verify default is disabled
        Assert.False(level.MainSector.PrimaryObjective.WavesOnElevatorLand.Enabled);
        Assert.False(level.MainSector.PrimaryObjective.WavesOnElevatorLand.HasWaveData());

        var exp = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = level.PersistentId,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = level.Name }
        };
        originalVm.Rundown.SelectedRundown!.TierA.Clear();
        originalVm.Rundown.SelectedRundown!.TierA.Add(exp);

        string exportDir = Path.Combine(_tempDir, "ExportWavesDisabled");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        string wardenObjPath = Path.Combine(exportDir, "GameData_WardenObjectiveDataBlock_bin.json");
        var json = File.ReadAllText(wardenObjPath);
        using var doc = JsonDocument.Parse(json);
        var blocks = doc.RootElement.GetProperty("Blocks");
        var firstBlock = blocks[0];

        // WavesOnElevatorLand should be empty array when disabled
        Assert.Equal(0, firstBlock.GetProperty("WavesOnElevatorLand").GetArrayLength());

        // Import back and verify
        var importVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == level.PersistentId || l.Name == level.Name);
        Assert.NotNull(importedLevel);
        Assert.False(importedLevel.MainSector.PrimaryObjective.WavesOnElevatorLand.Enabled);
        Assert.Single(importedLevel.MainSector.Objectives);
    }

    [Fact]
    public void ExportAndImport_MultipleChainedObjectives_CorrectlyExportedAndImported()
    {
        var originalVm = new MainWindowViewModel();
        var level = originalVm.LevelLayout.Levels[0];
        level.PersistentId = 901;
        level.Name = "Test_Chained_Objectives";
        level.ComplexType = ComplexType.Tech;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();
        grid.InitializeStartingTile(level.ElevatorTile, 0, 0);

        // Configure 3 chained objectives in Main Sector
        level.MainSector.Objectives.Clear();
        var obj1 = new WardenObjective { ObjectiveNumber = 1, Header = "Step 1 Header", MainObjective = "Step 1 Objective" };
        var obj2 = new WardenObjective { ObjectiveNumber = 2, Header = "Step 2 Header", MainObjective = "Step 2 Objective" };
        var obj3 = new WardenObjective { ObjectiveNumber = 3, Header = "Step 3 Header", MainObjective = "Step 3 Objective" };
        level.MainSector.Objectives.Add(obj1);
        level.MainSector.Objectives.Add(obj2);
        level.MainSector.Objectives.Add(obj3);

        var exp = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = level.PersistentId,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = level.Name }
        };
        originalVm.Rundown.SelectedRundown!.TierA.Clear();
        originalVm.Rundown.SelectedRundown!.TierA.Add(exp);

        string exportDir = Path.Combine(_tempDir, "ExportChainedObjectives");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        // Verify exported warden objectives contain 3 blocks
        string wardenObjPath = Path.Combine(exportDir, "GameData_WardenObjectiveDataBlock_bin.json");
        var json = File.ReadAllText(wardenObjPath);
        using var doc = JsonDocument.Parse(json);
        var blocks = doc.RootElement.GetProperty("Blocks");
        Assert.Equal(3, blocks.GetArrayLength());

        // Verify ChainedObjectiveData in rundown datablock has 2 elements
        string rundownPath = Path.Combine(exportDir, "GameData_RundownDataBlock_bin.json");
        var rundownJson = File.ReadAllText(rundownPath);
        using var rundownDoc = JsonDocument.Parse(rundownJson);
        var rundownExp = rundownDoc.RootElement.GetProperty("Blocks")[0].GetProperty("TierA")[0];
        var chainedData = rundownExp.GetProperty("MainLayerData").GetProperty("ChainedObjectiveData");
        Assert.Equal(2, chainedData.GetArrayLength());

        // Import back and verify
        var importVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == level.PersistentId || l.Name == level.Name);
        Assert.NotNull(importedLevel);
        Assert.Equal(3, importedLevel.MainSector.Objectives.Count);
        Assert.Equal("Step 1 Header", importedLevel.MainSector.Objectives[0].Header);
        Assert.Equal("Step 2 Header", importedLevel.MainSector.Objectives[1].Header);
        Assert.Equal("Step 3 Header", importedLevel.MainSector.Objectives[2].Header);
    }

    [Fact]
    public void ExportAndImport_MultiRoomZoneSecurityDoor_PreservesCorrectEntranceDoor()
    {
        var originalVm = new MainWindowViewModel();
        var level = originalVm.LevelLayout.Levels[0];
        level.PersistentId = 1001;
        level.Name = "Test_Security_Door_Position";
        level.ComplexType = ComplexType.Mining;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();

        // 1. Elevator at (0, 0)
        var elevGeo = grid.CreateGeomorphNode(level.ElevatorTile, 0, 0);
        elevGeo.SetRoomZone(0, 0);
        grid.PlacedGeomorphs.Add(elevGeo);

        // 2. Multi-area geomorph at (0, -1) (LGTuner (0, 1))
        var geo1 = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_02.prefab", 0, -1);
        grid.PlacedGeomorphs.Add(geo1);

        foreach (var door in elevGeo.Doors)
        {
            if (!grid.PlacedDoors.Contains(door)) grid.PlacedDoors.Add(door);
        }
        foreach (var door in geo1.Doors)
        {
            if (!grid.PlacedDoors.Contains(door)) grid.PlacedDoors.Add(door);
        }

        // Connect Elevator to geo1 Area 0
        var elevDoor = elevGeo.Doors.FirstOrDefault(d => !d.IsInternal);
        var geo1Door = geo1.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == 0);
        Assert.NotNull(elevDoor);
        Assert.NotNull(geo1Door);
        elevDoor.IsConnected = true;
        geo1Door.IsConnected = true;
        elevDoor.ConnectedDoor = geo1Door;
        geo1Door.ConnectedDoor = elevDoor;

        // Generate rooms: Area 0, 1, 2 in Zone 0
        geo1.GenerateRoom(0);
        geo1.SetRoomZone(0, 0);

        var door0To1 = geo1.Doors.First(d => d.IsInternal && ((d.AreaFrom == 0 && d.AreaTo == 1) || (d.AreaFrom == 1 && d.AreaTo == 0)));
        grid.GenerateRoomFromDoor(door0To1);
        geo1.SetRoomZone(1, 0);

        var door1To2 = geo1.Doors.First(d => d.IsInternal && ((d.AreaFrom == 1 && d.AreaTo == 2) || (d.AreaFrom == 2 && d.AreaTo == 1)));
        grid.GenerateRoomFromDoor(door1To2);
        geo1.SetRoomZone(2, 0);

        // Generate Area 8 in Zone 1 (connected to Area 2 via door2To8)
        var door2To8 = geo1.Doors.First(d => d.IsInternal && !d.IsConnected && (d.AreaFrom == 2 || d.AreaTo == 2));
        int area8 = grid.GenerateRoomFromDoor(door2To8);
        geo1.SetRoomZone(area8, 1);

        // Generate Area 7 in Zone 1 (connected to Area 8 via door8To7)
        var door8To7 = geo1.Doors.First(d => d.IsInternal && !d.IsConnected && (d.AreaFrom == area8 || d.AreaTo == area8));
        int area7 = grid.GenerateRoomFromDoor(door8To7);
        geo1.SetRoomZone(area7, 1);

        // Explicitly configure door2To8 as the Security Door into Zone 1
        door2To8.IsSecurityDoor = true;
        door2To8.ChainedPuzzleToSolve = 456;
        grid.RecalculateSecurityDoorConstraints();

        // Verify door8To7 is not a security door
        door8To7.IsSecurityDoor = false;

        var exp = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = level.PersistentId,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = level.Name }
        };
        originalVm.Rundown.SelectedRundown!.TierA.Clear();
        originalVm.Rundown.SelectedRundown!.TierA.Add(exp);

        string exportDir = Path.Combine(_tempDir, "ExportMultiRoomSecurityDoor");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        string lgPath = Path.Combine(exportDir, "Custom", "LGTunerAdvanced", "LevelGen_1001.json");
        Assert.True(File.Exists(lgPath), "LevelGen file should exist");
        string lgContent = File.ReadAllText(lgPath);

        // Import back
        var importVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == level.PersistentId || l.Name == level.Name);
        Assert.NotNull(importedLevel);

        var impGrid = importedLevel.GridState;
        var impGeo1 = impGrid.PlacedGeomorphs.FirstOrDefault(g => g.GridX == 0 && g.GridY == -1);
        Assert.NotNull(impGeo1);

        // Find the imported door connecting Area 2 and Area 8
        var impDoor2To8 = impGeo1.Doors.FirstOrDefault(d => d.IsInternal &&
            ((d.AreaFrom == 2 && d.AreaTo == area8) || (d.AreaFrom == area8 && d.AreaTo == 2)));
        Assert.NotNull(impDoor2To8);

        string allDoorsInfo = string.Join("; ", impGeo1.Doors.Select(d => $"({d.AreaFrom}->{d.AreaTo}, Exp={d.ExpanderIndex}, Sec={d.IsSecurityDoor}, Chained={d.ChainedPuzzleToSolve})"));
        Assert.True(impDoor2To8.IsSecurityDoor, $"LevelGen: {lgContent}\nAll doors: {allDoorsInfo}");
        Assert.Equal(456u, impDoor2To8.ChainedPuzzleToSolve);

        // Find the imported door connecting Area 8 and Area 7
        var impDoor8To7 = impGeo1.Doors.FirstOrDefault(d => d.IsInternal &&
            ((d.AreaFrom == area8 && d.AreaTo == area7) || (d.AreaFrom == area7 && d.AreaTo == area8)));
        Assert.NotNull(impDoor8To7);
        Assert.False(impDoor8To7.IsSecurityDoor, "Door between Area 8 and Area 7 within Zone 1 should NOT be a Security Door");
    }

    [Fact]
    public void ExportAndImport_Area0ToArea5SecurityDoor_NotMovedToOtherDoor()
    {
        var originalVm = new MainWindowViewModel();
        var level = originalVm.LevelLayout.Levels[0];
        level.PersistentId = 1101;
        level.Name = "Test_Area0_To_Area5_Door";
        level.ComplexType = ComplexType.Tech;
        level.ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_32x32_elevator_shaft_tech_01.prefab";

        var grid = level.GridState;
        grid.PlacedGeomorphs.Clear();
        grid.PlacedRooms.Clear();
        grid.PlacedDoors.Clear();

        // 1. Elevator at (0, 0)
        var elevGeo = grid.CreateGeomorphNode(level.ElevatorTile, 0, 0);
        elevGeo.SetRoomZone(0, 0);
        grid.PlacedGeomorphs.Add(elevGeo);

        // 2. Multi-area geomorph at (0, -1) (LGTuner (0, 1))
        var geo1 = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_64x64_tech_data_center_HA_01.prefab", 0, -1);
        grid.PlacedGeomorphs.Add(geo1);

        foreach (var door in elevGeo.Doors)
        {
            if (!grid.PlacedDoors.Contains(door)) grid.PlacedDoors.Add(door);
        }
        foreach (var door in geo1.Doors)
        {
            if (!grid.PlacedDoors.Contains(door)) grid.PlacedDoors.Add(door);
        }

        // Connect Elevator to geo1 Area 0
        var elevDoor = elevGeo.Doors.FirstOrDefault(d => !d.IsInternal);
        var geo1Door = geo1.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == 0);
        Assert.NotNull(elevDoor);
        Assert.NotNull(geo1Door);
        elevDoor.IsConnected = true;
        geo1Door.IsConnected = true;
        elevDoor.ConnectedDoor = geo1Door;
        geo1Door.ConnectedDoor = elevDoor;

        // Generate rooms: Area 0, 1, 2 in Zone 0
        geo1.GenerateRoom(0);
        geo1.SetRoomZone(0, 0);

        var door0To1 = geo1.Doors.First(d => d.IsInternal && ((d.AreaFrom == 0 && d.AreaTo == 1) || (d.AreaFrom == 1 && d.AreaTo == 0)));
        grid.GenerateRoomFromDoor(door0To1);
        geo1.SetRoomZone(1, 0);

        var door1To2 = geo1.Doors.First(d => d.IsInternal && ((d.AreaFrom == 1 && d.AreaTo == 2) || (d.AreaFrom == 2 && d.AreaTo == 1)));
        grid.GenerateRoomFromDoor(door1To2);
        geo1.SetRoomZone(2, 0);

        // Generate Area 5 in Zone 1 (connected to Area 0 via door0To5)
        var door0To5 = geo1.Doors.First(d => d.IsInternal && !d.IsConnected && (d.AreaFrom == 0 && d.AreaTo == 5 || d.AreaFrom == 5 && d.AreaTo == 0));
        int area5 = grid.GenerateRoomFromDoor(door0To5);
        geo1.SetRoomZone(area5, 1);

        // Explicitly configure door0To5 as the Security Door into Zone 1
        door0To5.IsSecurityDoor = true;
        door0To5.ChainedPuzzleToSolve = 1234;
        grid.RecalculateSecurityDoorConstraints();

        var exp = new ExpeditionBlock
        {
            Enabled = true,
            LevelLayoutData = level.PersistentId,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = level.Name }
        };
        originalVm.Rundown.SelectedRundown!.TierA.Clear();
        originalVm.Rundown.SelectedRundown!.TierA.Add(exp);

        string exportDir = Path.Combine(_tempDir, "ExportArea0To5SecurityDoor");
        var exportResult = DatablockManager.Instance.ExportDatablocks(originalVm, exportDir);
        Assert.True(exportResult.Success, exportResult.Message);

        // Import back
        var importVm = new MainWindowViewModel();
        var importResult = DatablockManager.Instance.ImportDatablocks(importVm, exportDir);
        Assert.True(importResult.Success, importResult.Message);

        var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == level.PersistentId || l.Name == level.Name);
        Assert.NotNull(importedLevel);

        var impGrid = importedLevel.GridState;
        var impGeo1 = impGrid.PlacedGeomorphs.FirstOrDefault(g => g.GridX == 0 && g.GridY == -1);
        Assert.NotNull(impGeo1);

        // Find the imported door connecting Area 0 and Area 5
        var impDoor0To5 = impGeo1.Doors.FirstOrDefault(d => d.IsInternal &&
            ((d.AreaFrom == 0 && d.AreaTo == 5) || (d.AreaFrom == 5 && d.AreaTo == 0)) && d.IsSecurityDoor);
        Assert.NotNull(impDoor0To5);
        Assert.True(impDoor0To5.IsSecurityDoor, "Door from Area 0 to Area 5 must remain the Security Door");
        Assert.Equal(1234u, impDoor0To5.ChainedPuzzleToSolve);

        // Other internal doors must not be security doors
        var impDoor2To3 = impGeo1.Doors.FirstOrDefault(d => d.IsInternal &&
            ((d.AreaFrom == 2 && d.AreaTo == 3) || (d.AreaFrom == 3 && d.AreaTo == 2)));
        if (impDoor2To3 != null)
        {
            Assert.False(impDoor2To3.IsSecurityDoor, "Door from Area 2 to Area 3 should NOT be a Security Door");
        }
    }
}
