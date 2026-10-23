using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class DatablockExportTests
{
    private class FakeExportFileDialogService : IFileDialogService
    {
        public string DefaultRundownsDirectory => Path.Combine(Path.GetTempPath(), "GTFO_Test_Export");
        public string? NextOpenPath { get; set; }
        public string? NextSavePath { get; set; }
        public string? NextFolderPath { get; set; }

        public Task<string?> ShowOpenFileDialogAsync(string? defaultDirectory = null, string? filter = null)
            => Task.FromResult(NextOpenPath);

        public Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null, string? defaultDirectory = null, string? defaultExtension = "rundown")
            => Task.FromResult(NextSavePath);

        public Task<string?> ShowOpenFolderDialogAsync(string? defaultDirectory = null, string? title = "Select Export Destination Directory")
            => Task.FromResult(NextFolderPath);
    }
    [Fact]
    public void PersistentIdAllocator_AllocatesSequentialIdsAboveOffsetWithoutDuplicates()
    {
        var allocator = new PersistentIdAllocator(100000);
        allocator.RegisterExisting(new uint[] { 100000, 100002 });

        uint first = allocator.Allocate();
        uint second = allocator.Allocate();

        Assert.Equal(100001u, first);
        Assert.Equal(100003u, second);
    }

    [Fact]
    public void ExportAll_GeneratesFullMTFOStructureAndValidJson()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var vm = new MainWindowViewModel();

            // Set up a level with blood doors, apex door, terminals, and geomorphs
            var level = LevelLayoutService.CreateFreshLevel(
                101,
                "Export_Test_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_03.prefab",
                1
            );
            level.FogSettings = 100;

            var grid = new LevelLayoutGridState();
            var geo = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_03.prefab", 20, 20);
            grid.PlacedGeomorphs.Add(geo);

            var door = new PlacedDoorNode
            {
                Id = "door-exp-1",
                Geomorph = geo,
                IsSecurityDoor = true,
                IsApexDoor = true,
                HasBloodDoor = true,
                IsConnected = true
            };
            door.EnemyGroupInFrontOfDoor.Add(new EnemyGroupItem { EnemyId = 24, Count = 5 }); // 5 Strikers
            door.EnemyGroupInArea.Add(new EnemyGroupItem { EnemyId = 40, Count = 1 }); // 1 Tank
            grid.PlacedDoors.Add(door);
            geo.Doors.Add(door);

            var room = new PlacedRoomNode { AreaIndex = 0, Zone = 0, Geomorph = geo };
            grid.PlacedRooms.Add(room);
            geo.SetRoomZone(0, 0);

            var zoneSettings = grid.GetOrCreateZoneSettings(0);
            zoneSettings.EnemyRespawning = true;
            zoneSettings.ExcludeList.Add(new EnemyExcludeItem { EnemyId = 38 });
            zoneSettings.TerminalPlacements.Add(new TerminalPlacement
            {
                CustomInfoText = "Test Terminal Info",
                UseCustomInfoText = true
            });

            level.GridState = grid;
            vm.LevelLayout.Levels.Clear();
            vm.LevelLayout.Levels.Add(level);

            var exportResult = DatablockExportService.ExportAll(vm, tempDir);

            Assert.True(exportResult.Success, exportResult.Message);
            Assert.False(Directory.Exists(Path.Combine(tempDir, "GameData_Rules")));
            Assert.True(Directory.Exists(Path.Combine(tempDir, "Custom", "LGTunerAdvanced")));

            // Check .schemas/LGTunerAdvanced.json and LevelGen file
            string schemaPath = Path.Combine(tempDir, ".schemas", "LGTunerAdvanced.json");
            Assert.True(File.Exists(schemaPath));

            string levelGenPath = Path.Combine(tempDir, "Custom", "LGTunerAdvanced", "LevelGen_101.json");
            Assert.True(File.Exists(levelGenPath));
            string levelGenJson = File.ReadAllText(levelGenPath);
            var levelGenNode = JsonNode.Parse(levelGenJson);
            Assert.NotNull(levelGenNode);
            Assert.Equal(101, levelGenNode["LevelLayoutID"]?.GetValue<int>());
            Assert.NotEmpty(levelGenNode["GeoOverrides"]!.AsArray());

            // Check GameData_LevelLayoutDataBlock_bin.json
            string levelLayoutPath = Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(levelLayoutPath));
            var layoutNode = JsonNode.Parse(File.ReadAllText(levelLayoutPath));
            var blocks = layoutNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var exportedLevel = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 101);
            Assert.NotNull(exportedLevel);
            var zones = exportedLevel["Zones"]?.AsArray();
            Assert.NotNull(zones);
            Assert.NotEmpty(zones);

            var zone0 = zones[0];
            Assert.NotNull(zone0);
            Assert.Equal("Apex", zone0["SecurityGateToEnter"]?.GetValue<string>());
            Assert.True(zone0["ActiveEnemyWave"]?["HasActiveEnemyWave"]?.GetValue<bool>());

            // Check GameData_EnemyGroupDataBlock_bin.json
            string enemyGroupPath = Path.Combine(tempDir, "GameData_EnemyGroupDataBlock_bin.json");
            Assert.True(File.Exists(enemyGroupPath));
            var enemyGroupNode = JsonNode.Parse(File.ReadAllText(enemyGroupPath));
            var egBlocks = enemyGroupNode?["Blocks"]?.AsArray();
            Assert.NotNull(egBlocks);
            Assert.NotEmpty(egBlocks);

            // Check GameData_TextDataBlock_bin.json
            string textPath = Path.Combine(tempDir, "GameData_TextDataBlock_bin.json");
            Assert.True(File.Exists(textPath));
            var textNode = JsonNode.Parse(File.ReadAllText(textPath));
            var tBlocks = textNode?["Blocks"]?.AsArray();
            Assert.NotNull(tBlocks);
            Assert.NotEmpty(tBlocks);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void ExportAll_ExportsAllDatablocksDirectlyWithGameDataPrefixAndBinSuffix()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Names_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var vm = new MainWindowViewModel();
            var exportResult = DatablockExportService.ExportAll(vm, tempDir);

            Assert.True(exportResult.Success, exportResult.Message);
            Assert.False(Directory.Exists(Path.Combine(tempDir, "GameData_Rules")));

            string[] expectedDatablockNames = new[]
            {
                "GameData_ChainedPuzzleDataBlock_bin.json",
                "GameData_ChainedPuzzleTypeDataBlock_bin.json",
                "GameData_ComplexResourceSetDataBlock_bin.json",
                "GameData_DimensionDataBlock_bin.json",
                "GameData_EnemyDataBlock_bin.json",
                "GameData_EnemyGroupDataBlock_bin.json",
                "GameData_ExpeditionBalanceDataBlock_bin.json",
                "GameData_FogSettingsDataBlock_bin.json",
                "GameData_GameSetupDataBlock_bin.json",
                "GameData_LevelLayoutDataBlock_bin.json",
                "GameData_LightSettingsDataBlock_bin.json",
                "GameData_RundownDataBlock_bin.json",
                "GameData_SurvivalWavePopulationDataBlock_bin.json",
                "GameData_SurvivalWaveSettingsDataBlock_bin.json",
                "GameData_TextDataBlock_bin.json",
                "GameData_WardenObjectiveDataBlock_bin.json"
            };

            foreach (var expectedFile in expectedDatablockNames)
            {
                string fullPath = Path.Combine(tempDir, expectedFile);
                Assert.True(File.Exists(fullPath), $"Expected file {expectedFile} was not found in export root.");
            }

            Assert.False(File.Exists(Path.Combine(tempDir, "GameData_EnemyPopulationDataBlock_bin.json")),
                "GameData_EnemyPopulationDataBlock_bin.json should not be exported as it is not part of vanilla datablocks and causes game crash on startup.");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task QuickExport_WhenLastExportDirectoryIsNull_PromptsFolderDialogAndExports()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_QuickExport_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fakeDialog = new FakeExportFileDialogService { NextFolderPath = tempDir };
            var vm = new MainWindowViewModel(fakeDialog, autoReopenLastRundown: false);

            Assert.Null(vm.LastExportDirectory);

            await vm.QuickExportDatablocksAsync();

            Assert.Equal(tempDir, vm.LastExportDirectory);
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task QuickExport_WhenLastExportDirectoryIsSet_DirectlyOverwritesWithoutPrompting()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_QuickExport_Existing_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fakeDialog = new FakeExportFileDialogService { NextFolderPath = null }; // Dialog should not be called
            var vm = new MainWindowViewModel(fakeDialog, autoReopenLastRundown: false);
            vm.LastExportDirectory = tempDir;

            await vm.QuickExportDatablocksAsync();

            Assert.Equal(tempDir, vm.LastExportDirectory);
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task ExportDatablocksTo_UpdatesLastExportDirectory_AndExports()
    {
        string tempDir1 = Path.Combine(Path.GetTempPath(), "GTFO_ExportTo_Test1_" + Guid.NewGuid().ToString("N"));
        string tempDir2 = Path.Combine(Path.GetTempPath(), "GTFO_ExportTo_Test2_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fakeDialog = new FakeExportFileDialogService { NextFolderPath = tempDir1 };
            var vm = new MainWindowViewModel(fakeDialog, autoReopenLastRundown: false);

            await vm.ExportDatablocksToAsync();
            Assert.Equal(tempDir1, vm.LastExportDirectory);
            Assert.True(File.Exists(Path.Combine(tempDir1, "GameData_LevelLayoutDataBlock_bin.json")));

            fakeDialog.NextFolderPath = tempDir2;
            await vm.ExportDatablocksToAsync();
            Assert.Equal(tempDir2, vm.LastExportDirectory);
            Assert.True(File.Exists(Path.Combine(tempDir2, "GameData_LevelLayoutDataBlock_bin.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir1)) { try { Directory.Delete(tempDir1, true); } catch { } }
            if (Directory.Exists(tempDir2)) { try { Directory.Delete(tempDir2, true); } catch { } }
        }
    }

    [Fact]
    public void RundownProject_PersistsAndRestoresLastExportDirectoryPerRundown()
    {
        string tempRundown1 = Path.Combine(Path.GetTempPath(), $"rundown1_{Guid.NewGuid():N}.rundown");
        string tempRundown2 = Path.Combine(Path.GetTempPath(), $"rundown2_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            vm.LastExportDirectory = @"C:\Mods\RundownA";
            RundownProjectService.SaveProject(vm, tempRundown1);

            vm.NewRundown();
            Assert.Null(vm.LastExportDirectory);

            vm.LastExportDirectory = @"C:\Mods\RundownB";
            RundownProjectService.SaveProject(vm, tempRundown2);

            // Load Rundown 1
            var proj1 = RundownProjectService.LoadProject(tempRundown1);
            RundownProjectService.ApplyProject(vm, proj1, tempRundown1);
            Assert.Equal(@"C:\Mods\RundownA", vm.LastExportDirectory);

            // Load Rundown 2
            var proj2 = RundownProjectService.LoadProject(tempRundown2);
            RundownProjectService.ApplyProject(vm, proj2, tempRundown2);
            Assert.Equal(@"C:\Mods\RundownB", vm.LastExportDirectory);

            // New rundown clears
            vm.NewRundown();
            Assert.Null(vm.LastExportDirectory);
        }
        finally
        {
            if (File.Exists(tempRundown1)) File.Delete(tempRundown1);
            if (File.Exists(tempRundown2)) File.Delete(tempRundown2);
        }
    }

    [Fact]
    public void DatablockExportService_ExportsProgressionPuzzle_Correctly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_ProgPuzzle_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = new LevelLayoutBlock
            {
                Name = "Locked Door Level",
                PersistentId = 205
            };
            mainVm.LevelLayout.Levels.Add(level);

            var gridState = new LevelLayoutGridState();
            level.GridState = gridState;

            var geo = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", 0, 0);
            gridState.PlacedGeomorphs.Add(geo);
            geo.GenerateRoom(0);
            geo.SetRoomZone(0, 1);
            gridState.CreateRoomNode(geo, 0);

            var door = new PlacedDoorNode
            {
                Id = "door-lock",
                Geomorph = geo,
                IsSecurityDoor = true,
                IsConnected = true,
                ProgressionPuzzleToEnter = ProgressionPuzzleType.PowerGenerator_And_PowerCell,
                ProgressionPuzzleCustomText = "Restore Power Cells",
                ProgressionPuzzlePlacementCount = 2
            };
            door.ProgressionPuzzleCandidateZones.Add(new CandidateZonePlacement
            {
                DimensionIndex = "Reality",
                Zone = 0,
                PlacementStartWeight = 0.1,
                PlacementMiddleWeight = 0.3,
                PlacementEndWeight = 0.6
            });
            geo.Doors.Add(door);
            gridState.PlacedDoors.Add(door);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);

            Assert.True(result.Success);

            string levelLayoutPath = Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(levelLayoutPath));
            var rootNode = JsonNode.Parse(File.ReadAllText(levelLayoutPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);

            var exportedLevel = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 205);
            Assert.NotNull(exportedLevel);
            var zones = exportedLevel["Zones"]?.AsArray();
            Assert.NotNull(zones);
            Assert.NotEmpty(zones);

            var zoneObj = zones.FirstOrDefault(z => z?["LocalIndex"]?.GetValue<string>() == "Zone_1");
            Assert.NotNull(zoneObj);
            var progPuzzle = zoneObj["ProgressionPuzzleToEnter"];
            Assert.NotNull(progPuzzle);
            Assert.Equal("PowerGenerator_And_PowerCell", progPuzzle["PuzzleType"]?.GetValue<string>());
            Assert.Equal(2, progPuzzle["PlacementCount"]?.GetValue<int>());
            Assert.True(progPuzzle["CustomText"]?.GetValue<uint>() > 0);

            var czArray = progPuzzle["ZonePlacementData"]?.AsArray();
            Assert.NotNull(czArray);
            Assert.Single(czArray);
            Assert.Equal("Zone_0", czArray[0]?["LocalIndex"]?.GetValue<string>());
            Assert.Equal(0.6, czArray[0]?["Weights"]?["End"]?.GetValue<double>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_SecurityGateToEnter_IsNeverNone()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_SecurityGate_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string levelLayoutPath = Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(levelLayoutPath));
            var rootNode = JsonNode.Parse(File.ReadAllText(levelLayoutPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);

            foreach (var block in blocks)
            {
                var zones = block?["Zones"]?.AsArray();
                if (zones == null) continue;
                foreach (var zone in zones)
                {
                    string? secGate = zone?["SecurityGateToEnter"]?.GetValue<string>();
                    Assert.NotNull(secGate);
                    Assert.NotEqual("None", secGate);
                    Assert.True(secGate == "Security" || secGate == "Apex", $"SecurityGateToEnter must be Security or Apex, but was '{secGate}'");
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_GameSetupDataBlock_ContainsActiveRundownPersistentId()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_GameSetup_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            Assert.NotNull(mainVm.Rundown.SelectedRundown);
            mainVm.Rundown.SelectedRundown.PersistentId = 42;

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string gameSetupPath = Path.Combine(tempDir, "GameData_GameSetupDataBlock_bin.json");
            Assert.True(File.Exists(gameSetupPath));

            var rootNode = JsonNode.Parse(File.ReadAllText(gameSetupPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var firstBlock = blocks[0];
            Assert.NotNull(firstBlock);
            var rundownIds = firstBlock["RundownIdsToLoad"]?.AsArray();
            Assert.NotNull(rundownIds);
            Assert.Single(rundownIds);
            Assert.Equal(42u, rundownIds[0]?.GetValue<uint>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_AlwaysExportsFogId1And21_WhenNoFogConfigured()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Fog21_Empty_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            mainVm.FogSettings.Blocks.Clear();

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string fogPath = Path.Combine(tempDir, "GameData_FogSettingsDataBlock_bin.json");
            Assert.True(File.Exists(fogPath));

            var rootNode = JsonNode.Parse(File.ReadAllText(fogPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var fog1 = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 1);
            Assert.NotNull(fog1);
            Assert.Equal("Fog_medium_white_alt", fog1["name"]?.GetValue<string>());
            Assert.True(fog1["Enabled"]?.GetValue<bool>());

            var fog21 = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 21);
            Assert.NotNull(fog21);
            Assert.Equal("Fog_ELEVATOR", fog21["name"]?.GetValue<string>());
            Assert.True(fog21["Enabled"]?.GetValue<bool>());
            Assert.True(fog21["internalEnabled"]?.GetValue<bool>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_AlwaysExportsFogId1And21_WhenCustomFogConfigured()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Fog21_Custom_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            mainVm.FogSettings.Blocks.Clear();
            mainVm.FogSettings.Blocks.Add(new FogSettingsBlock
            {
                PersistentId = 500,
                Name = "Custom_Fog_500",
                FogDensity = 0.05
            });

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string fogPath = Path.Combine(tempDir, "GameData_FogSettingsDataBlock_bin.json");
            Assert.True(File.Exists(fogPath));

            var rootNode = JsonNode.Parse(File.ReadAllText(fogPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.Equal(3, blocks.Count);

            var customFog = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 500);
            Assert.NotNull(customFog);
            Assert.Equal("Custom_Fog_500", customFog["name"]?.GetValue<string>());

            var fog1 = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 1);
            Assert.NotNull(fog1);
            Assert.Equal("Fog_medium_white_alt", fog1["name"]?.GetValue<string>());

            var fog21 = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 21);
            Assert.NotNull(fog21);
            Assert.Equal("Fog_ELEVATOR", fog21["name"]?.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsLightSettingsWithCorrectCategoryPropertyName()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Lighting_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var lightBlock = EditorDefaultsService.CreateDefaultLightBlock(1, "BlueToOrange_1");
            mainVm.Lighting.Blocks.Clear();
            mainVm.Lighting.Blocks.Add(lightBlock);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string lightPath = Path.Combine(tempDir, "GameData_LightSettingsDataBlock_bin.json");
            Assert.True(File.Exists(lightPath));

            var rootNode = JsonNode.Parse(File.ReadAllText(lightPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var b0 = blocks[0]?.AsObject();
            Assert.NotNull(b0);
            Assert.True(b0.ContainsKey("LightCategorySettings"));
            Assert.False(b0.ContainsKey("Categories"));

            var catSettings = b0["LightCategorySettings"]?.AsArray();
            Assert.NotNull(catSettings);
            Assert.NotEmpty(catSettings);
            var firstCat = catSettings[0]?.AsObject();
            Assert.NotNull(firstCat);
            Assert.True(firstCat.ContainsKey("Category"));
            Assert.True(firstCat.ContainsKey("On"));
            Assert.True(firstCat.ContainsKey("Color"));
            Assert.True(firstCat.ContainsKey("IntensityMul"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_RundownExpeditions_MatchesVanillaSchemaStrictly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Rundown_Schema_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
            Assert.NotNull(mainVm.Rundown.SelectedRundown);
            mainVm.Rundown.SelectedRundown.TierA.Add(EditorDefaultsService.CreateDefaultExpedition("A", 1));

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string rundownPath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            Assert.True(File.Exists(rundownPath));

            var rootNode = JsonNode.Parse(File.ReadAllText(rundownPath));
            var blocks = rootNode?["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var rBlock = blocks[0];
            Assert.NotNull(rBlock);

            // Verify Rundown level properties
            Assert.True(rBlock.AsObject().ContainsKey("NeverShowRundownTree"));
            Assert.True(rBlock.AsObject().ContainsKey("UseTierUnlockRequirements"));
            Assert.True(rBlock.AsObject().ContainsKey("ReqToReachTierA"));
            Assert.True(rBlock.AsObject().ContainsKey("ReqToReachTierB"));
            Assert.True(rBlock.AsObject().ContainsKey("ReqToReachTierC"));
            Assert.True(rBlock.AsObject().ContainsKey("ReqToReachTierD"));
            Assert.True(rBlock.AsObject().ContainsKey("ReqToReachTierE"));
            Assert.True(rBlock.AsObject().ContainsKey("StorytellingData"));
            Assert.True(rBlock.AsObject().ContainsKey("VanityItemLayerDropDataBlock"));
            Assert.True(rBlock.AsObject().ContainsKey("TierA"));
            Assert.True(rBlock.AsObject().ContainsKey("TierB"));
            Assert.True(rBlock.AsObject().ContainsKey("TierC"));
            Assert.True(rBlock.AsObject().ContainsKey("TierD"));
            Assert.True(rBlock.AsObject().ContainsKey("TierE"));

            // Check TierA expeditions
            var tierA = rBlock["TierA"]?.AsArray();
            Assert.NotNull(tierA);
            Assert.NotEmpty(tierA);

            var exp = tierA[0]?.AsObject();
            Assert.NotNull(exp);

            // Mandatory root expedition fields
            string[] requiredRootKeys = new[]
            {
                "Enabled", "ExcludeFromMatchmaking", "IsSinglePlayer", "SkipLobby",
                "PutIconAboveTier", "DisablePlayerVoicelines", "ExcludeFromProgression",
                "HideOnLocked", "HideRolePlayDataOnLocked", "Accessibility",
                "UnlockedByExpedition", "CustomProgressionLock", "Descriptive",
                "Seeds", "Expedition", "VanityItemsDropData", "UseGearPicker",
                "LevelLayoutData", "MainLayerData", "SecondaryLayerEnabled",
                "SecondaryLayout", "BuildSecondaryFrom", "SecondaryLayerData",
                "ThirdLayerEnabled", "ThirdLayout", "BuildThirdFrom",
                "ThirdLayerData", "DimensionDatas", "SoundEventOnWarpToReality",
                "SpecialOverrideData", "HasExternalStyle", "HasStoryStyle"
            };

            foreach (var key in requiredRootKeys)
            {
                Assert.True(exp.ContainsKey(key), $"Expedition is missing required field: {key}");
            }

            // Ensure deprecated/extraneous root fields are NOT present
            string[] forbiddenRootKeys = new[]
            {
                "FogSettings", "LightSettings", "ComplexResourceData", "WardenIntel", "SubTitle"
            };

            foreach (var key in forbiddenRootKeys)
            {
                Assert.False(exp.ContainsKey(key), $"Expedition has invalid root field: {key}");
            }

            // Check Descriptive fields
            var desc = exp["Descriptive"]?.AsObject();
            Assert.NotNull(desc);
            string[] requiredDescKeys = new[]
            {
                "Prefix", "PublicName", "IsExtraExpedition", "SkipExpNumberInName",
                "UseCustomMatchmakingTier", "CustomMatchmakingTier", "ProgressionVisualStyle",
                "ExpeditionDepth", "EstimatedDuration", "ExpeditionDescription", "RoleplayedWardenIntel"
            };
            foreach (var key in requiredDescKeys)
            {
                Assert.True(desc.ContainsKey(key), $"Descriptive is missing required field: {key}");
            }

            // Check Expedition sub-object fields
            var expSub = exp["Expedition"]?.AsObject();
            Assert.NotNull(expSub);
            string[] requiredExpSubKeys = new[]
            {
                "ComplexResourceData", "MLSLevelKit", "LightSettings", "FogSettings",
                "EnemyPopulation", "ExpeditionBalance", "ScoutWaveSettings", "ScoutWavePopulation",
                "EnvironmentWetness", "DustColor", "DustTurbulence"
            };
            foreach (var key in requiredExpSubKeys)
            {
                Assert.True(expSub.ContainsKey(key), $"Expedition sub-object is missing required field: {key}");
            }

            // Check MainLayerData fields
            var mainLayer = exp["MainLayerData"]?.AsObject();
            Assert.NotNull(mainLayer);
            string[] requiredLayerKeys = new[]
            {
                "ZonesWithBulkheadEntrance", "BulkheadDoorControllerPlacements",
                "BulkheadKeyPlacements", "ObjectiveData", "ChainedObjectiveData", "ArtifactData"
            };
            foreach (var key in requiredLayerKeys)
            {
                Assert.True(mainLayer.ContainsKey(key), $"MainLayerData is missing required field: {key}");
            }

            var objData = mainLayer["ObjectiveData"]?.AsObject();
            Assert.NotNull(objData);
            Assert.True(objData.ContainsKey("DataBlockId"));
            Assert.True(objData.ContainsKey("WinCondition"));
            Assert.True(objData.ContainsKey("ZonePlacementDatas"));

            var artifactData = mainLayer["ArtifactData"]?.AsObject();
            Assert.NotNull(artifactData);
            Assert.True(artifactData.ContainsKey("ArtifactAmountMulti"));
            Assert.True(artifactData.ContainsKey("ArtifactLayerDistributionDataID"));
            Assert.True(artifactData.ContainsKey("ArtifactZoneDistributions"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_BuildSecondaryAndThirdFrom_ResolvesCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_BuildFrom_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
            Assert.NotNull(mainVm.Rundown.SelectedRundown);

            // Create level layout with secondary and overload doors
            var level = new LevelLayoutBlock { PersistentId = 100, Name = "Test Level" };
            var gridState = level.GridState;

            // Geo 0 (Zone 0, Main)
            var geo0 = new PlacedGeomorphNode
            {
                Id = "geo0",
                PrefabPath = "Assets/Test_HA_01.prefab",
                GridX = 20,
                GridY = 20
            };
            geo0.SetRoomZone(0, 0);
            geo0.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo0);

            // Geo 1 (Zone 1, Secondary) connected via Secondary Door from Geo 0 Area 0
            var geo1 = new PlacedGeomorphNode
            {
                Id = "geo1",
                PrefabPath = "Assets/Test_HA_02.prefab",
                GridX = 21,
                GridY = 20
            };
            geo1.SetRoomZone(0, 1);
            geo1.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo1);

            // Geo 2 (Zone 2, Overload) connected via Overload Door from Geo 1 Area 0 (Secondary Layer)
            var geo2 = new PlacedGeomorphNode
            {
                Id = "geo2",
                PrefabPath = "Assets/Test_HA_03.prefab",
                GridX = 22,
                GridY = 20
            };
            geo2.SetRoomZone(0, 2);
            geo2.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo2);

            // Door from Geo0 to Geo1 is Secondary Door
            var door0 = new PlacedDoorNode
            {
                Id = "door0",
                Geomorph = geo0,
                AreaFrom = 0,
                IsConnected = true,
                DoorType = DoorType.SecondaryDoor
            };
            var door1 = new PlacedDoorNode
            {
                Id = "door1",
                Geomorph = geo1,
                AreaFrom = 0,
                IsConnected = true,
                ConnectedDoor = door0
            };
            door0.ConnectedDoor = door1;
            gridState.PlacedDoors.Add(door0);
            gridState.PlacedDoors.Add(door1);

            // Door from Geo1 to Geo2 is Overload Door
            var door2 = new PlacedDoorNode
            {
                Id = "door2",
                Geomorph = geo1,
                AreaFrom = 0,
                IsConnected = true,
                DoorType = DoorType.OverloadDoor
            };
            var door3 = new PlacedDoorNode
            {
                Id = "door3",
                Geomorph = geo2,
                AreaFrom = 0,
                IsConnected = true,
                ConnectedDoor = door2
            };
            door2.ConnectedDoor = door3;
            gridState.PlacedDoors.Add(door2);
            gridState.PlacedDoors.Add(door3);

            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var exp = EditorDefaultsService.CreateDefaultExpedition("A", 1);
            exp.LevelLayoutData = 100;
            mainVm.Rundown.SelectedRundown.TierA.Clear();
            mainVm.Rundown.SelectedRundown.TierA.Add(exp);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success);

            string rundownPath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            var rootNode = JsonNode.Parse(File.ReadAllText(rundownPath));
            var exportedExp = rootNode?["Blocks"]?[0]?["TierA"]?[0]?.AsObject();
            Assert.NotNull(exportedExp);

            Assert.True(exportedExp["SecondaryLayerEnabled"]?.GetValue<bool>());
            Assert.True(exportedExp["ThirdLayerEnabled"]?.GetValue<bool>());

            var secBuild = exportedExp["BuildSecondaryFrom"]?.AsObject();
            Assert.NotNull(secBuild);
            Assert.Equal("MainLayer", secBuild["LayerType"]?.GetValue<string>());
            Assert.Equal("Zone_0", secBuild["Zone"]?.GetValue<string>());

            var thirdBuild = exportedExp["BuildThirdFrom"]?.AsObject();
            Assert.NotNull(thirdBuild);
            Assert.Equal("SecondaryLayer", thirdBuild["LayerType"]?.GetValue<string>());
            Assert.Equal("Zone_1", thirdBuild["Zone"]?.GetValue<string>());

            // Check Bulkhead entrance zones - empty since no Main Doors lead into zones
            var mainLayer = exportedExp["MainLayerData"]?.AsObject();
            var zonesWithBulkhead = mainLayer?["ZonesWithBulkheadEntrance"]?.AsArray();
            Assert.NotNull(zonesWithBulkhead);
            Assert.Empty(zonesWithBulkhead);

            var secLayer = exportedExp["SecondaryLayerData"]?.AsObject();
            var secZonesWithBulkhead = secLayer?["ZonesWithBulkheadEntrance"]?.AsArray();
            Assert.NotNull(secZonesWithBulkhead);
            Assert.Empty(secZonesWithBulkhead);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsLevelGenSchema_CorrectCoordinateSystemAndStructure()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_LevelGen_Schema_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                1,
                "Schema_Test_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab",
                1
            );
            level.ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";

            var gridState = new LevelLayoutGridState();

            // Elevator at (0, 0)
            var elevatorGeo = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);
            elevatorGeo.SetRoomZone(0, 0);
            gridState.PlacedGeomorphs.Add(elevatorGeo);

            // Geo 1 at (0, -1) (above Elevator on grid) connected to Elevator
            var geo1 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_02.prefab", 0, -1);
            geo1.RotationAngle = 180;
            geo1.Altitude = GeomorphAltitude.Mid;
            geo1.SetRoomZone(0, 0);
            gridState.PlacedGeomorphs.Add(geo1);

            var elevDoor = new PlacedDoorNode
            {
                Id = "elev_door",
                Geomorph = elevatorGeo,
                AreaFrom = 0,
                ExpanderIndex = 0,
                IsConnected = true
            };
            var geo1Door = new PlacedDoorNode
            {
                Id = "geo1_door",
                Geomorph = geo1,
                AreaFrom = 0,
                ExpanderIndex = 0,
                IsConnected = true,
                ConnectedDoor = elevDoor
            };
            elevDoor.ConnectedDoor = geo1Door;
            elevatorGeo.Doors.Add(elevDoor);
            geo1.Doors.Add(geo1Door);
            gridState.PlacedDoors.Add(elevDoor);
            gridState.PlacedDoors.Add(geo1Door);

            // Geo 2 at (0, -2) (two tiles above Elevator on grid) connected to Geo 1
            var geo2 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", 0, -2);
            geo2.RotationAngle = 0;
            geo2.Altitude = GeomorphAltitude.Mid;
            geo2.SetRoomZone(0, 0);
            gridState.PlacedGeomorphs.Add(geo2);

            var geo1ToGeo2Door = new PlacedDoorNode
            {
                Id = "geo1_to_geo2_door",
                Geomorph = geo1,
                AreaFrom = 0,
                ExpanderIndex = 1,
                IsConnected = true
            };
            var geo2Door = new PlacedDoorNode
            {
                Id = "geo2_door",
                Geomorph = geo2,
                AreaFrom = 0,
                ExpanderIndex = 0,
                IsConnected = true,
                ConnectedDoor = geo1ToGeo2Door
            };
            geo1ToGeo2Door.ConnectedDoor = geo2Door;
            geo1.Doors.Add(geo1ToGeo2Door);
            geo2.Doors.Add(geo2Door);
            gridState.PlacedDoors.Add(geo1ToGeo2Door);
            gridState.PlacedDoors.Add(geo2Door);

            level.GridState = gridState;
            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            // 1. Schema file exists in .schemas
            string rootSchemaPath = Path.Combine(tempDir, ".schemas", "LGTunerAdvanced.json");
            string customSchemaPath = Path.Combine(tempDir, "Custom", "LGTunerAdvanced", "schema.json");
            Assert.True(File.Exists(rootSchemaPath), "Expected .schemas/LGTunerAdvanced.json to exist");
            Assert.False(File.Exists(customSchemaPath), "Custom/LGTunerAdvanced/schema.json should not exist to prevent mod loader conflicts");

            // 2. LevelGen file is valid and contains $schema
            string levelGenPath = Path.Combine(tempDir, "Custom", "LGTunerAdvanced", "LevelGen_1.json");
            Assert.True(File.Exists(levelGenPath), "Expected LevelGen_1.json to exist");

            string levelGenContent = File.ReadAllText(levelGenPath);
            var levelGenObj = JsonNode.Parse(levelGenContent)?.AsObject();
            Assert.NotNull(levelGenObj);

            Assert.Equal("../../.schemas/LGTunerAdvanced.json", levelGenObj["$schema"]?.GetValue<string>());
            Assert.Equal(1, levelGenObj["LevelLayoutID"]?.GetValue<int>());

            // 3. Elevator at (0, 0) should not be in GeoOverrides; geo1 at (0, -1) grid -> (0, 1) LGTuner, geo2 at (0, -2) grid -> (0, 2) LGTuner
            var geoOverrides = levelGenObj["GeoOverrides"]?.AsArray();
            Assert.NotNull(geoOverrides);
            Assert.Equal(2, geoOverrides.Count);

            var override0 = geoOverrides[0]?.AsObject();
            Assert.NotNull(override0);
            Assert.Equal("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_02.prefab", override0["Geomorph"]?.GetValue<string>());
            Assert.Equal(0, override0["Altitude"]?.GetValue<int>());
            Assert.Equal(2, override0["Rotation"]?.GetValue<int>());

            var pos0 = override0["Position"]?.AsObject();
            Assert.NotNull(pos0);
            Assert.Equal(0, pos0["x"]?.GetValue<int>());
            Assert.Equal(1, pos0["z"]?.GetValue<int>());

            var override1 = geoOverrides[1]?.AsObject();
            Assert.NotNull(override1);
            Assert.Equal("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", override1["Geomorph"]?.GetValue<string>());
            Assert.Equal(0, override1["Rotation"]?.GetValue<int>());
            var pos1 = override1["Position"]?.AsObject();
            Assert.NotNull(pos1);
            Assert.Equal(0, pos1["x"]?.GetValue<int>());
            Assert.Equal(2, pos1["z"]?.GetValue<int>());

            // 4. AreaOverrides should contain root elevator (0, 0), geo1 (0, 0) -> (0, 1), and geo2 (0, 1) -> (0, 2)
            var zoneOverrides = levelGenObj["ZoneOverrides"]?.AsArray();
            Assert.NotNull(zoneOverrides);
            Assert.NotEmpty(zoneOverrides);

            var areaOverrides = zoneOverrides[0]?["AreaOverrides"]?.AsArray();
            Assert.NotNull(areaOverrides);
            Assert.Equal(3, areaOverrides.Count);

            var area0 = areaOverrides[0]?.AsObject();
            Assert.NotNull(area0);
            var prevPos0 = area0["PreviousCellPosition"]?.AsObject();
            Assert.NotNull(prevPos0);
            Assert.Equal(0, prevPos0["x"]?.GetValue<int>());
            Assert.Equal(0, prevPos0["z"]?.GetValue<int>());

            var area1 = areaOverrides[1]?.AsObject();
            Assert.NotNull(area1);
            var prevPos1 = area1["PreviousCellPosition"]?.AsObject();
            Assert.NotNull(prevPos1);
            Assert.Equal(0, prevPos1["x"]?.GetValue<int>());
            Assert.Equal(0, prevPos1["z"]?.GetValue<int>());
            Assert.Equal(0, area1["LG_ExpanderID"]?.GetValue<int>());

            var area2 = areaOverrides[2]?.AsObject();
            Assert.NotNull(area2);
            var prevPos2 = area2["PreviousCellPosition"]?.AsObject();
            Assert.NotNull(prevPos2);
            Assert.Equal(0, prevPos2["x"]?.GetValue<int>());
            Assert.Equal(1, prevPos2["z"]?.GetValue<int>());
            Assert.Equal(1, area2["LG_ExpanderID"]?.GetValue<int>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsLGTunerAdvanced_ResolvesInternalAreaExpandersCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_LGTuner_InternalExpanders_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                1,
                "Internal_Expanders_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_02.prefab",
                1
            );
            level.ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";

            var gridState = new LevelLayoutGridState();

            // Elevator at (0, 0)
            var elevatorGeo = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);
            elevatorGeo.SetRoomZone(0, 0);
            gridState.PlacedGeomorphs.Add(elevatorGeo);

            // Multi-area geomorph AS_02 at (0, -1) (LGTuner (0, 1))
            var geo1 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_02.prefab", 0, -1);
            gridState.PlacedGeomorphs.Add(geo1);

            foreach (var door in elevatorGeo.Doors)
            {
                if (!gridState.PlacedDoors.Contains(door)) gridState.PlacedDoors.Add(door);
            }
            foreach (var door in geo1.Doors)
            {
                if (!gridState.PlacedDoors.Contains(door)) gridState.PlacedDoors.Add(door);
            }

            // Connect Elevator (Area 0) to geo1 (Area 0)
            var elevDoor = elevatorGeo.Doors.FirstOrDefault(d => !d.IsInternal);
            var geo1Door = geo1.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == 0);
            Assert.NotNull(elevDoor);
            Assert.NotNull(geo1Door);
            elevDoor.IsConnected = true;
            geo1Door.IsConnected = true;
            elevDoor.ConnectedDoor = geo1Door;
            geo1Door.ConnectedDoor = elevDoor;

            // Expand internal rooms sequentially along available unexpanded doors
            var door0To1 = geo1.Doors.First(d => d.IsInternal && (d.AreaFrom == 0 && d.AreaTo == 1 || d.AreaFrom == 1 && d.AreaTo == 0));
            gridState.GenerateRoomFromDoor(door0To1);

            var door1To2 = geo1.Doors.First(d => d.IsInternal && (d.AreaFrom == 1 && d.AreaTo == 2 || d.AreaFrom == 2 && d.AreaTo == 1));
            gridState.GenerateRoomFromDoor(door1To2);

            var door2ToNext = geo1.Doors.First(d => d.IsInternal && !d.IsConnected && (d.AreaFrom == 2 || d.AreaTo == 2));
            int nextArea = gridState.GenerateRoomFromDoor(door2ToNext);

            var doorNextTo3 = geo1.Doors.First(d => d.IsInternal && !d.IsConnected && (d.AreaFrom == nextArea || d.AreaTo == nextArea));
            int nextArea2 = gridState.GenerateRoomFromDoor(doorNextTo3);

            var doorNextTo4 = geo1.Doors.First(d => d.IsInternal && !d.IsConnected && (d.AreaFrom == nextArea2 || d.AreaTo == nextArea2));
            gridState.GenerateRoomFromDoor(doorNextTo4);

            level.GridState = gridState;
            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string levelGenPath = Path.Combine(tempDir, "Custom", "LGTunerAdvanced", "LevelGen_1.json");
            Assert.True(File.Exists(levelGenPath));

            var levelGenObj = JsonNode.Parse(File.ReadAllText(levelGenPath))?.AsObject();
            Assert.NotNull(levelGenObj);

            var zoneOverrides = levelGenObj["ZoneOverrides"]?.AsArray();
            Assert.NotNull(zoneOverrides);
            var areaOverrides = zoneOverrides[0]?["AreaOverrides"]?.AsArray();
            Assert.NotNull(areaOverrides);

            // Verify area overrides count is 7 (1 elevator + 6 rooms in geo1)
            Assert.Equal(7, areaOverrides.Count);

            // Area 0 in geo1: previous is elevator (0, 0), area 0, expander 0
            var areaGeo0 = areaOverrides[1]?.AsObject();
            Assert.NotNull(areaGeo0);
            Assert.Equal(0, areaGeo0["PreviousCellPosition"]?["x"]?.GetValue<int>());
            Assert.Equal(0, areaGeo0["PreviousCellPosition"]?["z"]?.GetValue<int>());
            Assert.Equal(0, areaGeo0["PreviousCellInternalAreaID"]?.GetValue<int>());
            Assert.Equal(0, areaGeo0["LG_ExpanderID"]?.GetValue<int>());

            // Verify all expected internal connections exist with correct Expander IDs:
            // Area 0 -> Area 1: previous area 0, expander 0
            Assert.Contains(areaOverrides, a => a?["PreviousCellPosition"]?["z"]?.GetValue<int>() == 1 &&
                                                a?["PreviousCellInternalAreaID"]?.GetValue<int>() == 0 &&
                                                a?["LG_ExpanderID"]?.GetValue<int>() == 0);

            // Area 1 -> Area 2: previous area 1, expander 0
            Assert.Contains(areaOverrides, a => a?["PreviousCellPosition"]?["z"]?.GetValue<int>() == 1 &&
                                                a?["PreviousCellInternalAreaID"]?.GetValue<int>() == 1 &&
                                                a?["LG_ExpanderID"]?.GetValue<int>() == 0);

            // Area 2 -> Area 8: previous area 2, expander 0
            Assert.Contains(areaOverrides, a => a?["PreviousCellPosition"]?["z"]?.GetValue<int>() == 1 &&
                                                a?["PreviousCellInternalAreaID"]?.GetValue<int>() == 2 &&
                                                a?["LG_ExpanderID"]?.GetValue<int>() == 0);

            // Area 8 -> Area 7: previous area 8, expander 1 (or 7 -> 8)
            Assert.Contains(areaOverrides, a => a?["PreviousCellPosition"]?["z"]?.GetValue<int>() == 1 &&
                                                (a?["PreviousCellInternalAreaID"]?.GetValue<int>() == 8 || a?["PreviousCellInternalAreaID"]?.GetValue<int>() == 7));

            // Area 7 -> Area 9: previous area 7, expander 1 (or 8 -> 9)
            Assert.Contains(areaOverrides, a => a?["PreviousCellPosition"]?["z"]?.GetValue<int>() == 1 &&
                                                a?["LG_ExpanderID"]?.GetValue<int>() == 1);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsLevelLayoutDataBlock_VanillaFieldComplianceAndDisinfectionStationList()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_LevelLayout_Vanilla_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                201,
                "Layout_Test_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab",
                1
            );
            level.ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";

            var gridState = new LevelLayoutGridState();
            var elevatorGeo = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);
            elevatorGeo.SetRoomZone(0, 0);
            elevatorGeo.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(elevatorGeo);

            var elevatorRoom = new PlacedRoomNode { AreaIndex = 0, Zone = 0, Geomorph = elevatorGeo };
            gridState.PlacedRooms.Add(elevatorRoom);

            var zs0 = gridState.GetOrCreateZoneSettings(0);
            zs0.MedKitUses = 4;
            zs0.AmmoKitUses = 8;
            zs0.ToolKitUses = 2;
            zs0.DisinfectKitUses = 1;
            zs0.CorpsesInZone = 10;
            zs0.GroundSpawnersInZone = ZoneDistributionAmount.Many_20;
            zs0.HSUsInZone = ZoneDistributionAmount.Few_5;
            zs0.DeconUnitsInZone = ZoneDistributionAmount.None_0;
            zs0.HSUClusters = 2;
            zs0.CorpseClusters = 1;
            zs0.ResourceContainerClusters = 3;
            zs0.ContainsGenCluster = true;

            // Disinfection station placement list
            zs0.DisinfectionStationPlacements.Add(new DisinfectionStationPlacement
            {
                PlacementStartWeight = 0.5,
                PlacementMiddleWeight = 0.3,
                PlacementEndWeight = 0.2
            });

            // Terminal placement with StartingStateData
            zs0.TerminalPlacements.Add(new TerminalPlacement
            {
                PlacementStartWeight = 1.0,
                PlacementMiddleWeight = 0.0,
                PlacementEndWeight = 0.0,
                StartingState = TerminalStartingState.Awake,
                PasswordProtected = true,
                PasswordHintText = "SECRET_CODE"
            });

            level.GridState = gridState;
            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string layoutPath = Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(layoutPath));

            string jsonContent = File.ReadAllText(layoutPath);
            var rootObj = JsonNode.Parse(jsonContent)?.AsObject();
            Assert.NotNull(rootObj);

            var blocks = rootObj["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var layoutBlock = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 201)?.AsObject();
            Assert.NotNull(layoutBlock);

            Assert.True(layoutBlock.ContainsKey("ZoneAliasStart"));
            Assert.True(layoutBlock.ContainsKey("Zones"));
            Assert.True(layoutBlock.ContainsKey("name"));
            Assert.True(layoutBlock.ContainsKey("internalEnabled"));
            Assert.True(layoutBlock.ContainsKey("persistentID"));

            var zones = layoutBlock["Zones"]?.AsArray();
            Assert.NotNull(zones);
            Assert.NotEmpty(zones);

            var zone0 = zones[0]?.AsObject();
            Assert.NotNull(zone0);

            // 1. Should NOT contain obsolete ResourceContainerData or boolean ContainsDisinfectStation
            Assert.False(zone0.ContainsKey("ResourceContainerData"), "LevelLayoutDataBlock must not contain ResourceContainerData object");
            Assert.False(zone0.ContainsKey("ContainsDisinfectStation"), "LevelLayoutDataBlock must not contain ContainsDisinfectStation boolean");

            // 2. Should contain all required vanilla fields
            Assert.Equal("Zone_0", zone0["LocalIndex"]?.GetValue<string>());
            Assert.Equal(-1, zone0["AliasOverride"]?.GetValue<int>());
            Assert.False(zone0["OverrideAliasPrefix"]?.GetValue<bool>());
            Assert.True(zone0.ContainsKey("SubSeed"));
            Assert.True(zone0.ContainsKey("MarkerSubSeed"));
            Assert.True(zone0.ContainsKey("LightsSubSeed"));
            Assert.True(zone0.ContainsKey("BulkheadDCScanSeed"));
            Assert.Equal("DigSite", zone0["SubComplex"]?.GetValue<string>());
            Assert.False(zone0["IgnoreRandomGeomorphRotation"]?.GetValue<bool>());
            Assert.NotNull(zone0["CoverageMinMax"]);
            Assert.Equal("Zone_0", zone0["BuildFromLocalIndex"]?.GetValue<string>());
            Assert.Equal("From_AverageCenter", zone0["StartPosition"]?.GetValue<string>());
            Assert.Equal("Towards_Forward", zone0["StartExpansion"]?.GetValue<string>());
            Assert.Equal("Expansional", zone0["ZoneExpansion"]?.GetValue<string>());
            Assert.NotNull(zone0["AltitudeData"]);

            // Door and Zone Events
            Assert.NotNull(zone0["EventsOnEnter"]);
            Assert.NotNull(zone0["EventsOnPortalWarp"]);
            Assert.NotNull(zone0["EventsOnApproachDoor"]);
            Assert.NotNull(zone0["EventsOnUnlockDoor"]);
            Assert.NotNull(zone0["EventsOnOpenDoor"]);
            Assert.NotNull(zone0["EventsOnDoorScanStart"]);
            Assert.NotNull(zone0["EventsOnDoorScanDone"]);

            // Security & Puzzle
            Assert.NotNull(zone0["ProgressionPuzzleToEnter"]);
            Assert.True(zone0.ContainsKey("ChainedPuzzleToEnter"));
            Assert.False(zone0["IsCheckpointDoor"]?.GetValue<bool>());
            Assert.True(zone0["PlayScannerVoiceAudio"]?.GetValue<bool>());
            Assert.False(zone0["SkipAutomaticProgressionObjective"]?.GetValue<bool>());
            Assert.Equal("Security", zone0["SecurityGateToEnter"]?.GetValue<string>());
            Assert.False(zone0["UseStaticBioscanPointsInZone"]?.GetValue<bool>());
            Assert.False(zone0["TurnOffAlarmOnTerminal"]?.GetValue<bool>());
            Assert.NotNull(zone0["TerminalPuzzleZone"]);
            Assert.NotNull(zone0["EventsOnTerminalDeactivateAlarm"]);

            // Enemies & Spawning
            Assert.NotNull(zone0["ActiveEnemyWave"]);
            Assert.NotNull(zone0["EnemySpawningInZone"]);
            Assert.False(zone0["EnemyRespawning"]?.GetValue<bool>());
            Assert.NotNull(zone0["EnemyRespawnExcludeList"]);

            // Clusters & Pickups
            Assert.Equal(2, zone0["HSUClustersInZone"]?.GetValue<int>());
            Assert.Equal(1, zone0["CorpseClustersInZone"]?.GetValue<int>());
            Assert.Equal(3, zone0["ResourceContainerClustersInZone"]?.GetValue<int>());
            Assert.Equal(1, zone0["GeneratorClustersInZone"]?.GetValue<int>());
            Assert.Equal("Some_10", zone0["CorpsesInZone"]?.GetValue<string>());
            Assert.Equal("Many_20", zone0["GroundSpawnersInZone"]?.GetValue<string>());
            Assert.Equal("Few_5", zone0["HSUsInZone"]?.GetValue<string>());
            Assert.Equal("None_0", zone0["DeconUnitsInZone"]?.GetValue<string>());
            Assert.True(zone0["AllowSmallPickupsAllocation"]?.GetValue<bool>());
            Assert.True(zone0["AllowResourceContainerAllocation"]?.GetValue<bool>());
            Assert.False(zone0["ForceBigPickupsAllocation"]?.GetValue<bool>());

            // Disinfection station placements list
            var disinfPlacements = zone0["DisinfectionStationPlacements"]?.AsArray();
            Assert.NotNull(disinfPlacements);
            Assert.Single(disinfPlacements);
            var dsp0 = disinfPlacements[0]?.AsObject();
            Assert.NotNull(dsp0);
            Assert.Equal(0.5, dsp0["PlacementWeights"]?["Start"]?.GetValue<double>());
            Assert.Equal(0.3, dsp0["PlacementWeights"]?["Middle"]?.GetValue<double>());
            Assert.Equal(0.2, dsp0["PlacementWeights"]?["End"]?.GetValue<double>());

            // Terminal placements with StartingStateData
            var termPlacements = zone0["TerminalPlacements"]?.AsArray();
            Assert.NotNull(termPlacements);
            Assert.Single(termPlacements);
            var term0 = termPlacements[0]?.AsObject();
            Assert.NotNull(term0);
            var stateData = term0["StartingStateData"]?.AsObject();
            Assert.NotNull(stateData);
            Assert.Equal("Awake", stateData["StartingState"]?.GetValue<string>());
            Assert.True(stateData["PasswordProtected"]?.GetValue<bool>());
            Assert.Equal("SECRET_CODE", stateData["PasswordHintText"]?.GetValue<string>());

            // Resource multipliers and placements
            Assert.Equal(1.0, zone0["HealthMulti"]?.GetValue<double>()); // 4 uses * 0.25 = 1.0
            Assert.NotNull(zone0["HealthPlacement"]);
            Assert.Equal(2.0, zone0["WeaponAmmoMulti"]?.GetValue<double>()); // 8 uses * 0.25 = 2.0
            Assert.NotNull(zone0["WeaponAmmoPlacement"]);
            Assert.Equal(0.5, zone0["ToolAmmoMulti"]?.GetValue<double>()); // 2 uses * 0.25 = 0.5
            Assert.NotNull(zone0["ToolAmmoPlacement"]);
            Assert.Equal(0.25, zone0["DisinfectionMulti"]?.GetValue<double>()); // 1 use * 0.25 = 0.25
            Assert.NotNull(zone0["DisinfectionPlacement"]);
            Assert.NotNull(zone0["StaticSpawnDataContainers"]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsLGTunerAdvanced_RotationMapping()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_LGTuner_Rotation_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                1,
                "Rotation_Test_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab",
                1
            );
            level.ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";

            var gridState = new LevelLayoutGridState();

            // Elevator at (0, 0)
            var elevatorGeo = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);
            gridState.PlacedGeomorphs.Add(elevatorGeo);

            // Geo 0 deg at (0, -1) -> rotation 0
            var geo0 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", 0, -1);
            geo0.RotationAngle = 0;
            gridState.PlacedGeomorphs.Add(geo0);

            // Geo 90 deg at (1, 0) -> rotation 1
            var geo90 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", 1, 0);
            geo90.RotationAngle = 90;
            gridState.PlacedGeomorphs.Add(geo90);

            // Geo 180 deg at (0, 1) -> rotation 2
            var geo180 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", 0, 1);
            geo180.RotationAngle = 180;
            gridState.PlacedGeomorphs.Add(geo180);

            // Geo 270 deg at (-1, 0) -> rotation 3
            var geo270 = gridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab", -1, 0);
            geo270.RotationAngle = 270;
            gridState.PlacedGeomorphs.Add(geo270);

            level.GridState = gridState;
            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string levelGenPath = Path.Combine(tempDir, "Custom", "LGTunerAdvanced", "LevelGen_1.json");
            Assert.True(File.Exists(levelGenPath));

            var levelGenObj = JsonNode.Parse(File.ReadAllText(levelGenPath))?.AsObject();
            Assert.NotNull(levelGenObj);

            var geoOverrides = levelGenObj["GeoOverrides"]?.AsArray();
            Assert.NotNull(geoOverrides);
            Assert.Equal(4, geoOverrides.Count);

            var item0 = geoOverrides.FirstOrDefault(g => g?["Position"]?["x"]?.GetValue<int>() == 0 && g?["Position"]?["z"]?.GetValue<int>() == 1);
            Assert.NotNull(item0);
            Assert.Equal(0, item0["Rotation"]?.GetValue<int>());

            var item90 = geoOverrides.FirstOrDefault(g => g?["Position"]?["x"]?.GetValue<int>() == 1 && g?["Position"]?["z"]?.GetValue<int>() == 0);
            Assert.NotNull(item90);
            Assert.Equal(1, item90["Rotation"]?.GetValue<int>());

            var item180 = geoOverrides.FirstOrDefault(g => g?["Position"]?["x"]?.GetValue<int>() == 0 && g?["Position"]?["z"]?.GetValue<int>() == -1);
            Assert.NotNull(item180);
            Assert.Equal(2, item180["Rotation"]?.GetValue<int>());

            var item270 = geoOverrides.FirstOrDefault(g => g?["Position"]?["x"]?.GetValue<int>() == -1 && g?["Position"]?["z"]?.GetValue<int>() == 0);
            Assert.NotNull(item270);
            Assert.Equal(3, item270["Rotation"]?.GetValue<int>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsComplexResourceSetDataBlock_IncludesAllPlugsLaddersAndGates()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_ComplexResourceSet_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                1,
                "Complex_Test_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab",
                1
            );
            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string complexPath = Path.Combine(tempDir, "GameData_ComplexResourceSetDataBlock_bin.json");
            Assert.True(File.Exists(complexPath));

            var doc = JsonNode.Parse(File.ReadAllText(complexPath))?.AsObject();
            Assert.NotNull(doc);
            var blocks = doc["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var miningBlock = blocks[0]?.AsObject();
            Assert.NotNull(miningBlock);
            Assert.Equal("Mining", miningBlock["ComplexType"]?.GetValue<string>());
            Assert.Equal("Complex_Mining", miningBlock["BundleName"]?.GetValue<string>());

            // Gates
            var smallWeakGates = miningBlock["SmallWeakGates"]?.AsArray();
            Assert.NotNull(smallWeakGates);
            Assert.NotEmpty(smallWeakGates);
            Assert.Contains(smallWeakGates, g => g?["Prefab"]?.GetValue<string>() == "Assets/AssetPrefabs/Complex/Generic/Gates/4x4/gate_4x4_weak_door.prefab");

            var smallSecurityGates = miningBlock["SmallSecurityGates"]?.AsArray();
            Assert.NotNull(smallSecurityGates);
            Assert.NotEmpty(smallSecurityGates);
            Assert.Contains(smallSecurityGates, g => g?["Prefab"]?.GetValue<string>() == "Assets/AssetPrefabs/Complex/Generic/Gates/4x4/gate_4x4_security_door.prefab");

            var mediumWeakGates = miningBlock["MediumWeakGates"]?.AsArray();
            Assert.NotNull(mediumWeakGates);
            Assert.NotEmpty(mediumWeakGates);

            var largeWeakGates = miningBlock["LargeWeakGates"]?.AsArray();
            Assert.NotNull(largeWeakGates);
            Assert.NotEmpty(largeWeakGates);

            // Plugs
            var straightPlugsNoGates = miningBlock["StraightPlugsNoGates"]?.AsArray();
            Assert.NotNull(straightPlugsNoGates);
            Assert.NotEmpty(straightPlugsNoGates);
            Assert.Contains(straightPlugsNoGates, p => p?["Prefab"]?.GetValue<string>()?.Contains("env_plug_8mheight_flat_mining_01") == true);

            var straightPlugsWithGates = miningBlock["StraightPlugsWithGates"]?.AsArray();
            Assert.NotNull(straightPlugsWithGates);
            Assert.NotEmpty(straightPlugsWithGates);

            var singleDropPlugsNoGates = miningBlock["SingleDropPlugsNoGates"]?.AsArray();
            Assert.NotNull(singleDropPlugsNoGates);
            Assert.NotEmpty(singleDropPlugsNoGates);

            var doubleDropPlugsNoGates = miningBlock["DoubleDropPlugsNoGates"]?.AsArray();
            Assert.NotNull(doubleDropPlugsNoGates);
            Assert.NotEmpty(doubleDropPlugsNoGates);

            var plugCaps = miningBlock["PlugCaps"]?.AsArray();
            Assert.NotNull(plugCaps);
            Assert.NotEmpty(plugCaps);

            // Ladders
            var ladders4m = miningBlock["Ladders_4m"]?.AsArray();
            Assert.NotNull(ladders4m);
            Assert.NotEmpty(ladders4m);

            var laddersTop = miningBlock["Ladders_Top"]?.AsArray();
            Assert.NotNull(laddersTop);
            Assert.NotEmpty(laddersTop);

            // Geomorphs
            var geos1x1 = miningBlock["GeomorphTiles_1x1"]?.AsArray();
            Assert.NotNull(geos1x1);
            Assert.NotEmpty(geos1x1);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsWardenObjectiveDataBlock_MatchesVanillaStructure()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_WardenObjective_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                1,
                "Objective_Test_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab",
                1
            );
            level.MainSector.ObjectiveType = ObjectiveType.RetrieveBigItems;
            level.MainSector.RetrieveItem = RetrieveBigItemType.CryoHardcase;
            level.MainSector.RetrieveItemPlacements.Clear();
            level.MainSector.RetrieveItemPlacements.Add(new GatherItemPlacement { Zone = 1, ItemNumber = 1 });
            level.MainSector.RetrieveItemPlacements.Add(new GatherItemPlacement { Zone = 2, ItemNumber = 2 });

            level.MainSector.EventsOnGotoWin.Add(new WardenEvent
            {
                Type = EventType.AllLightsOn,
                Trigger = EventTrigger.OnStart,
                Zone = 1
            });

            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string objPath = Path.Combine(tempDir, "GameData_WardenObjectiveDataBlock_bin.json");
            Assert.True(File.Exists(objPath));

            var doc = JsonNode.Parse(File.ReadAllText(objPath))?.AsObject();
            Assert.NotNull(doc);
            var blocks = doc["Blocks"]?.AsArray();
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var objBlock = blocks[0]?.AsObject();
            Assert.NotNull(objBlock);

            // Compare properties against vanilla WardenObjectiveDataBlock.json
            string vanillaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks", "WardenObjectiveDataBlock.json");
            if (!File.Exists(vanillaPath))
            {
                vanillaPath = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "WardenObjectiveDataBlock.json");
            }

            Assert.True(File.Exists(vanillaPath));
            var vanillaDoc = JsonNode.Parse(File.ReadAllText(vanillaPath))?.AsObject();
            Assert.NotNull(vanillaDoc);
            var vanillaBlock0 = vanillaDoc["Blocks"]?.AsArray()?[0]?.AsObject();
            Assert.NotNull(vanillaBlock0);

            var vanillaPropNames = vanillaBlock0.Select(kv => kv.Key).ToList();
            var exportedPropNames = objBlock.Select(kv => kv.Key).ToList();

            Assert.Equal(vanillaPropNames.Count, exportedPropNames.Count);
            for (int i = 0; i < vanillaPropNames.Count; i++)
            {
                Assert.Equal(vanillaPropNames[i], exportedPropNames[i]);
            }

            // Verify content
            Assert.Equal("RetrieveBigItems", objBlock["Type"]?.GetValue<string>());
            var retrieveItems = objBlock["Retrieve_Items"]?.AsArray();
            Assert.NotNull(retrieveItems);
            Assert.Equal(2, retrieveItems.Count);
            Assert.Equal(148, retrieveItems[0]?.GetValue<int>());

            var eventsOnGotoWin = objBlock["EventsOnGotoWin"]?.AsArray();
            Assert.NotNull(eventsOnGotoWin);
            Assert.Single(eventsOnGotoWin);
            var ev = eventsOnGotoWin[0]?.AsObject();
            Assert.NotNull(ev);
            Assert.Equal("AllLightsOn", ev["Type"]?.GetValue<string>());
            Assert.Equal("OnStart", ev["Trigger"]?.GetValue<string>());
            Assert.Equal("Zone_1", ev["LocalIndex"]?.GetValue<string>());
            Assert.Equal("MainLayer", ev["Layer"]?.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsSeparateLayoutsAndConfigsForSectors_AndLinksInRundown()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_Sectors_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel();
            var level = LevelLayoutService.CreateFreshLevel(
                10,
                "MultiSector_Level",
                ComplexType.Mining,
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_01.prefab",
                10
            );

            // Add secondary and overload sectors
            level.SecondarySector = new SectorSettings { ZoneAliasStart = 100, ObjectiveType = ObjectiveType.GatherTerminal };
            level.OverloadSector = new SectorSettings { ZoneAliasStart = 200, ObjectiveType = ObjectiveType.Reactor_Startup };

            // Create rooms in Main (zone 0), Secondary (zone 1), and Overload (zone 2)
            var gridState = level.GridState;
            var geo0 = gridState.PlacedGeomorphs[0];
            geo0.SetRoomZone(0, 0);

            var geo1 = new PlacedGeomorphNode
            {
                Id = "geo_sec",
                GridX = 0,
                GridY = 1,
                PrefabPath = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Storage/geo_64x64_mining_storage_HA_06.prefab"
            };
            geo1.SetRoomZone(0, 1);
            geo1.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo1);

            var geo2 = new PlacedGeomorphNode
            {
                Id = "geo_third",
                GridX = 1,
                GridY = 0,
                PrefabPath = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/Lab/geo_64x64_tech_lab_HA_01.prefab"
            };
            geo2.SetRoomZone(0, 2);
            geo2.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo2);

            var geo_main_1 = new PlacedGeomorphNode
            {
                Id = "geo_main_1",
                GridX = 0,
                GridY = -1,
                PrefabPath = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Storage/geo_64x64_mining_storage_HA_06.prefab"
            };
            geo_main_1.SetRoomZone(0, 1);
            geo_main_1.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo_main_1);

            gridState.PlacedRooms.Clear();
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo0, AreaIndex = 0, Zone = 0, Sector = SectorType.Main });
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo_main_1, AreaIndex = 0, Zone = 1, Sector = SectorType.Main });
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo1, AreaIndex = 0, Zone = 1, Sector = SectorType.Secondary });
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo2, AreaIndex = 0, Zone = 2, Sector = SectorType.Overload });

            // Main door from Main zone 0 (geo0) to Main zone 1 (geo_main_1)
            var mainDoor0 = new PlacedDoorNode
            {
                Geomorph = geo0,
                AreaFrom = 0,
                AreaTo = -1,
                DoorType = DoorType.MainDoor,
                IsConnected = true
            };
            var mainDoor1 = new PlacedDoorNode
            {
                Geomorph = geo_main_1,
                AreaFrom = 0,
                AreaTo = -1,
                DoorType = DoorType.MainDoor,
                IsConnected = true
            };
            mainDoor0.ConnectedDoor = mainDoor1;
            mainDoor1.ConnectedDoor = mainDoor0;
            gridState.PlacedDoors.Add(mainDoor0);
            gridState.PlacedDoors.Add(mainDoor1);

            // Secondary door from Main (geo0) to Secondary (geo1)
            var secDoor0 = new PlacedDoorNode
            {
                Geomorph = geo0,
                AreaFrom = 0,
                AreaTo = -1,
                DoorType = DoorType.SecondaryDoor,
                IsConnected = true
            };
            var secDoor1 = new PlacedDoorNode
            {
                Geomorph = geo1,
                AreaFrom = 0,
                AreaTo = -1,
                DoorType = DoorType.SecondaryDoor,
                IsConnected = true
            };
            secDoor0.ConnectedDoor = secDoor1;
            secDoor1.ConnectedDoor = secDoor0;
            gridState.PlacedDoors.Add(secDoor0);
            gridState.PlacedDoors.Add(secDoor1);

            // Overload door from Main (geo0) to Overload (geo2)
            var thirdDoor0 = new PlacedDoorNode
            {
                Geomorph = geo0,
                AreaFrom = 0,
                AreaTo = -1,
                DoorType = DoorType.OverloadDoor,
                IsConnected = true
            };
            var thirdDoor1 = new PlacedDoorNode
            {
                Geomorph = geo2,
                AreaFrom = 0,
                AreaTo = -1,
                DoorType = DoorType.OverloadDoor,
                IsConnected = true
            };
            thirdDoor0.ConnectedDoor = thirdDoor1;
            thirdDoor1.ConnectedDoor = thirdDoor0;
            gridState.PlacedDoors.Add(thirdDoor0);
            gridState.PlacedDoors.Add(thirdDoor1);

            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var exp = EditorDefaultsService.CreateDefaultExpedition("A", 1);
            exp.LevelLayoutData = level.PersistentId;
            mainVm.Rundown.SelectedRundown.TierA.Clear();
            mainVm.Rundown.SelectedRundown.TierA.Add(exp);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            // 1. Verify LevelLayoutDataBlock has 3 layout blocks (Main, Secondary, Third)
            string layoutPath = Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(layoutPath));
            var layoutDoc = JsonNode.Parse(File.ReadAllText(layoutPath))?.AsObject();
            Assert.NotNull(layoutDoc);
            var layoutBlocks = layoutDoc["Blocks"]?.AsArray();
            Assert.NotNull(layoutBlocks);
            Assert.Equal(3, layoutBlocks.Count);

            var mainLayout = layoutBlocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == level.PersistentId);
            Assert.NotNull(mainLayout);
            Assert.Equal(level.Name, mainLayout["name"]?.GetValue<string>());

            var secLayout = layoutBlocks.FirstOrDefault(b => b?["name"]?.GetValue<string>() == $"{level.Name}_Secondary");
            Assert.NotNull(secLayout);
            uint secLayoutId = secLayout["persistentID"]?.GetValue<uint>() ?? 0;
            Assert.True(secLayoutId > 0 && secLayoutId != level.PersistentId);

            var thirdLayout = layoutBlocks.FirstOrDefault(b => b?["name"]?.GetValue<string>() == $"{level.Name}_Third");
            Assert.NotNull(thirdLayout);
            uint thirdLayoutId = thirdLayout["persistentID"]?.GetValue<uint>() ?? 0;
            Assert.True(thirdLayoutId > 0 && thirdLayoutId != level.PersistentId && thirdLayoutId != secLayoutId);

            // 2. Verify LGTunerAdvanced configs exist for each layout
            string lgTunerDir = Path.Combine(tempDir, "Custom", "LGTunerAdvanced");
            Assert.True(File.Exists(Path.Combine(lgTunerDir, $"LevelGen_{level.PersistentId}.json")));
            Assert.True(File.Exists(Path.Combine(lgTunerDir, $"LevelGen_{secLayoutId}.json")));
            Assert.True(File.Exists(Path.Combine(lgTunerDir, $"LevelGen_{thirdLayoutId}.json")));

            // 3. Verify RundownDataBlock linking
            string rundownPath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            Assert.True(File.Exists(rundownPath));
            var rundownDoc = JsonNode.Parse(File.ReadAllText(rundownPath))?.AsObject();
            Assert.NotNull(rundownDoc);
            var rBlock = rundownDoc["Blocks"]?.AsArray()?[0]?.AsObject();
            Assert.NotNull(rBlock);
            var expExported = rBlock["TierA"]?.AsArray()?[0]?.AsObject();
            Assert.NotNull(expExported);

            Assert.Equal(level.PersistentId, expExported["LevelLayoutData"]?.GetValue<uint>());

            // Main layer bulkhead entrance should have Zone_1 (from mainDoor leading into Zone 1)
            var mainBulkheads = expExported["MainLayerData"]?["ZonesWithBulkheadEntrance"]?.AsArray();
            Assert.NotNull(mainBulkheads);
            Assert.Contains(mainBulkheads, z => z?.GetValue<string>() == "Zone_1");

            // Secondary
            Assert.True(expExported["SecondaryLayerEnabled"]?.GetValue<bool>());
            Assert.Equal(secLayoutId, expExported["SecondaryLayout"]?.GetValue<uint>());
            Assert.Equal("MainLayer", expExported["BuildSecondaryFrom"]?["LayerType"]?.GetValue<string>());
            Assert.Equal("Zone_0", expExported["BuildSecondaryFrom"]?["Zone"]?.GetValue<string>());

            // Third
            Assert.True(expExported["ThirdLayerEnabled"]?.GetValue<bool>());
            Assert.Equal(thirdLayoutId, expExported["ThirdLayout"]?.GetValue<uint>());
            Assert.Equal("MainLayer", expExported["BuildThirdFrom"]?["LayerType"]?.GetValue<string>());
            Assert.Equal("Zone_0", expExported["BuildThirdFrom"]?["Zone"]?.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExportService_ExportsBulkheadDCAndKeyPlacements_Correctly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_DC_Key_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
            var level = new LevelLayoutBlock { PersistentId = 200, Name = "DC and Key Test Level" };
            var gridState = level.GridState;

            // Geo 0 (Zone 0, Main) with DC
            var geo0 = new PlacedGeomorphNode
            {
                Id = "geo0",
                PrefabPath = "Assets/Test_01.prefab",
                GridX = 10,
                GridY = 10
            };
            geo0.SetRoomZone(0, 0);
            geo0.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo0);

            var zs0 = gridState.GetOrCreateZoneSettings(SectorType.Main, 0);
            zs0.ContainsDC = true;
            zs0.DcScanSeed = 12345;

            // Geo 1 (Zone 0, Secondary)
            var geo1 = new PlacedGeomorphNode
            {
                Id = "geo1",
                PrefabPath = "Assets/Test_02.prefab",
                GridX = 11,
                GridY = 10
            };
            geo1.SetRoomSector(0, SectorType.Secondary);
            geo1.SetRoomZone(0, 0);
            geo1.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo1);

            // Configure Bulkhead Key Placements in Secondary Sector
            var secKey = new GatherItemPlacement
            {
                ItemNumber = 1,
                ItemPrefix = "Bulkhead Key",
                CandidateZones = new System.Collections.ObjectModel.ObservableCollection<CandidateZonePlacement>
                {
                    new CandidateZonePlacement
                    {
                        DimensionIndex = "Reality",
                        Zone = 0,
                        PlacementStartWeight = 100.0,
                        PlacementMiddleWeight = 200.0,
                        PlacementEndWeight = 300.0
                    }
                }
            };
            level.SecondarySector.BulkheadKeyPlacements.Add(secKey);

            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var exp = EditorDefaultsService.CreateDefaultExpedition("A", 1);
            exp.LevelLayoutData = level.PersistentId;
            mainVm.Rundown.SelectedRundown!.TierA.Clear();
            mainVm.Rundown.SelectedRundown.TierA.Add(exp);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string rundownPath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            Assert.True(File.Exists(rundownPath));
            var rundownDoc = JsonNode.Parse(File.ReadAllText(rundownPath))?.AsObject();
            Assert.NotNull(rundownDoc);
            var expExported = rundownDoc["Blocks"]?.AsArray()?[0]?["TierA"]?.AsArray()?[0]?.AsObject();
            Assert.NotNull(expExported);

            // Verify MainLayerData has BulkheadDoorControllerPlacements containing Zone_0
            var mainDCPlacements = expExported["MainLayerData"]?["BulkheadDoorControllerPlacements"]?.AsArray();
            Assert.NotNull(mainDCPlacements);
            Assert.Single(mainDCPlacements);
            Assert.Equal("Zone_0", mainDCPlacements[0]?["ZoneIndex"]?.GetValue<string>());

            // Verify SecondaryLayerData has BulkheadKeyPlacements
            var secKeyPlacements = expExported["SecondaryLayerData"]?["BulkheadKeyPlacements"]?.AsArray();
            Assert.NotNull(secKeyPlacements);
            Assert.Single(secKeyPlacements);
            var candidates = secKeyPlacements[0]?.AsArray();
            Assert.NotNull(candidates);
            Assert.Single(candidates);
            Assert.Equal("Reality", candidates[0]?["DimensionIndex"]?.GetValue<string>());
            Assert.Equal("Zone_0", candidates[0]?["LocalIndex"]?.GetValue<string>());
            Assert.Equal(100.0, candidates[0]?["Weights"]?["Start"]?.GetValue<double>());
            Assert.Equal(200.0, candidates[0]?["Weights"]?["Middle"]?.GetValue<double>());
            Assert.Equal(300.0, candidates[0]?["Weights"]?["End"]?.GetValue<double>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void SectorSettings_ZoneAliasStart_DefaultsAndRoundtrips_Correctly()
    {
        var level = new LevelLayoutBlock();
        Assert.Equal(0, level.MainSector.ZoneAliasStart);
        Assert.Equal(100, level.SecondarySector.ZoneAliasStart);
        Assert.Equal(200, level.OverloadSector.ZoneAliasStart);
    }

    [Fact]
    public void DatablockExportService_ZonesWithBulkheadEntrance_OnlyIncludesZoneEnteredViaMainDoor()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Export_MainBulkheadEntrance_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
            var level = new LevelLayoutBlock { PersistentId = 300, Name = "Main Bulkhead Entrance Test Level" };
            var gridState = level.GridState;

            // Geo 0 (Zone 0, Main)
            var geo0 = new PlacedGeomorphNode { Id = "geo0", GridX = 0, GridY = 0 };
            geo0.SetRoomZone(0, 0);
            geo0.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo0);

            // Geo 1 (Zone 1, Main) - entered via Security Door from Zone 0
            var geo1 = new PlacedGeomorphNode { Id = "geo1", GridX = 0, GridY = 1 };
            geo1.SetRoomZone(0, 1);
            geo1.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo1);

            // Geo 2 (Zone 2, Main) - entered via Main Door from Zone 1
            var geo2 = new PlacedGeomorphNode { Id = "geo2", GridX = 0, GridY = 2 };
            geo2.SetRoomZone(0, 2);
            geo2.GenerateRoom(0);
            gridState.PlacedGeomorphs.Add(geo2);

            gridState.PlacedRooms.Clear();
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo0, AreaIndex = 0, Zone = 0, Sector = SectorType.Main });
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo1, AreaIndex = 0, Zone = 1, Sector = SectorType.Main });
            gridState.PlacedRooms.Add(new PlacedRoomNode { Geomorph = geo2, AreaIndex = 0, Zone = 2, Sector = SectorType.Main });

            // Security door between Zone 0 and Zone 1
            var d0 = new PlacedDoorNode { Geomorph = geo0, AreaFrom = 0, DoorType = DoorType.SecurityDoor, IsConnected = true };
            var d1 = new PlacedDoorNode { Geomorph = geo1, AreaFrom = 0, DoorType = DoorType.SecurityDoor, IsConnected = true };
            d0.ConnectedDoor = d1;
            d1.ConnectedDoor = d0;
            gridState.PlacedDoors.Add(d0);
            gridState.PlacedDoors.Add(d1);

            // Main door between Zone 1 and Zone 2
            var d2 = new PlacedDoorNode { Geomorph = geo1, AreaFrom = 0, DoorType = DoorType.MainDoor, IsConnected = true };
            var d3 = new PlacedDoorNode { Geomorph = geo2, AreaFrom = 0, DoorType = DoorType.MainDoor, IsConnected = true };
            d2.ConnectedDoor = d3;
            d3.ConnectedDoor = d2;
            gridState.PlacedDoors.Add(d2);
            gridState.PlacedDoors.Add(d3);

            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            var exp = EditorDefaultsService.CreateDefaultExpedition("A", 1);
            exp.LevelLayoutData = level.PersistentId;
            mainVm.Rundown.SelectedRundown!.TierA.Clear();
            mainVm.Rundown.SelectedRundown.TierA.Add(exp);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string rundownPath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            var rundownDoc = JsonNode.Parse(File.ReadAllText(rundownPath))?.AsObject();
            Assert.NotNull(rundownDoc);
            var expExported = rundownDoc["Blocks"]?.AsArray()?[0]?["TierA"]?.AsArray()?[0]?.AsObject();
            Assert.NotNull(expExported);

            var mainBulkheads = expExported["MainLayerData"]?["ZonesWithBulkheadEntrance"]?.AsArray();
            Assert.NotNull(mainBulkheads);
            Assert.Single(mainBulkheads);
            Assert.Equal("Zone_2", mainBulkheads[0]?.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
