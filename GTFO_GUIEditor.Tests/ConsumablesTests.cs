using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ConsumablesTests
{
    [Fact]
    public void ConsumableDistributionBlock_DefaultsAndCloning()
    {
        var block = ConsumableDistributionService.CreateFreshBlock(101, "Test_Consumables");
        Assert.Equal((uint)101, block.PersistentId);
        Assert.Equal("Test_Consumables", block.Name);
        Assert.True(block.InternalEnabled);
        Assert.Equal(5, block.SpawnsPerZone);
        Assert.Equal(0.8, block.ChanceToSpawnInResourceContainer);
        Assert.NotEmpty(block.SpawnData);

        var clone = block.Clone();
        Assert.Equal(block.PersistentId, clone.PersistentId);
        Assert.Equal(block.Name, clone.Name);
        Assert.Equal(block.SpawnsPerZone, clone.SpawnsPerZone);
        Assert.Equal(block.ChanceToSpawnInResourceContainer, clone.ChanceToSpawnInResourceContainer);
        Assert.Equal(block.SpawnData.Count, clone.SpawnData.Count);

        // Modifying clone does not alter original
        clone.SpawnsPerZone = 12;
        clone.SpawnData.Add(new ConsumableDistributionItem(Consumables.Syringe_Health, 5.0));
        Assert.Equal(5, block.SpawnsPerZone);
        Assert.NotEqual(block.SpawnData.Count, clone.SpawnData.Count);
    }

    [Fact]
    public void ConsumableDistributionItem_ItemAndWeight_Binding()
    {
        var item = new ConsumableDistributionItem(Consumables.CFoamGrenade, 3.5);
        Assert.Equal((uint)115, item.ItemId);
        Assert.Equal(Consumables.CFoamGrenade, item.Item);
        Assert.Equal(3.5, item.Weight);

        item.Item = Consumables.FogRepeller;
        Assert.Equal((uint)117, item.ItemId);

        item.ItemId = (uint)Consumables.ExplosiveTripmine;
        Assert.Equal(Consumables.ExplosiveTripmine, item.Item);
    }

    [Fact]
    public void ConsumablesViewModel_AddRemoveAndValidation()
    {
        var vm = new ConsumablesViewModel();
        Assert.True(vm.Blocks.Count >= 0);

        vm.OpenCreateBlockDialog();
        vm.NewBlockName = "Custom_Pack_1";
        vm.NewBlockPersistentId = 555;
        bool created = vm.ConfirmCreateBlock();
        Assert.True(created);
        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal((uint)555, vm.SelectedBlock!.PersistentId);
        Assert.Equal("Custom_Pack_1", vm.SelectedBlock.Name);

        int initialCount = vm.SelectedBlock.SpawnData.Count;
        vm.AddSpawnDataItem();
        Assert.Equal(initialCount + 1, vm.SelectedBlock.SpawnData.Count);

        var addedItem = vm.SelectedBlock.SpawnData.Last();
        vm.RemoveSpawnDataItem(addedItem);
        Assert.Equal(initialCount, vm.SelectedBlock.SpawnData.Count);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_Consumables_RoundTrip()
    {
        var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
        mainVm.Consumables.Blocks.Clear();

        var customBlock = new ConsumableDistributionBlock
        {
            PersistentId = 4242,
            Name = "Special_Consumables",
            SpawnsPerZone = 8,
            ChanceToSpawnInResourceContainer = 0.65,
            SpawnData = new ObservableCollection<ConsumableDistributionItem>
            {
                new ConsumableDistributionItem(Consumables.LockMelter, 4.0),
                new ConsumableDistributionItem(Consumables.CFoamTripmine, 2.5)
            }
        };
        mainVm.Consumables.Blocks.Add(customBlock);
        mainVm.Consumables.SelectedBlock = customBlock;

        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_consumables_{Guid.NewGuid():N}.rundown");
        try
        {
            RundownProjectService.SaveProject(mainVm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            Assert.NotNull(loadedProject.ConsumableDistributions);
            Assert.Single(loadedProject.ConsumableDistributions);

            var loadedBlock = loadedProject.ConsumableDistributions[0];
            Assert.Equal((uint)4242, loadedBlock.PersistentId);
            Assert.Equal("Special_Consumables", loadedBlock.Name);
            Assert.Equal(8, loadedBlock.SpawnsPerZone);
            Assert.Equal(0.65, loadedBlock.ChanceToSpawnInResourceContainer);
            Assert.Equal(2, loadedBlock.SpawnData.Count);
            Assert.Equal(Consumables.LockMelter, loadedBlock.SpawnData[0].Item);
            Assert.Equal(4.0, loadedBlock.SpawnData[0].Weight);

            var newVm = new MainWindowViewModel(autoReopenLastRundown: false);
            RundownProjectService.ApplyProject(newVm, loadedProject);

            Assert.Single(newVm.Consumables.Blocks);
            Assert.Equal((uint)4242, newVm.Consumables.Blocks[0].PersistentId);
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
    public void DatablockExportService_ExportsConsumableDistributionDataBlock()
    {
        var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
        mainVm.Consumables.Blocks.Clear();

        var customBlock = new ConsumableDistributionBlock
        {
            PersistentId = 9999,
            Name = "Export_Test_Distribution",
            SpawnsPerZone = 10,
            ChanceToSpawnInResourceContainer = 0.5,
            SpawnData = new ObservableCollection<ConsumableDistributionItem>
            {
                new ConsumableDistributionItem(Consumables.Flashlight, 1.0),
                new ConsumableDistributionItem(Consumables.Syringe_MeleeBuff, 3.0)
            }
        };
        mainVm.Consumables.Blocks.Add(customBlock);

        string tempExportDir = Path.Combine(Path.GetTempPath(), $"gtfo_export_consumables_{Guid.NewGuid():N}");
        try
        {
            var result = DatablockExportService.ExportAll(mainVm, tempExportDir);
            Assert.True(result.Success, result.Message);

            string exportedFile = Path.Combine(tempExportDir, "GameData_ConsumableDistributionDataBlock_bin.json");
            Assert.True(File.Exists(exportedFile));

            string json = File.ReadAllText(exportedFile);
            var rootNode = JsonNode.Parse(json)?.AsObject();
            Assert.NotNull(rootNode);

            var blocksArray = rootNode!["Blocks"]?.AsArray();
            Assert.NotNull(blocksArray);
            Assert.Single(blocksArray!);

            var exportedBlock = blocksArray![0]?.AsObject();
            Assert.NotNull(exportedBlock);
            Assert.Equal(10, exportedBlock!["SpawnsPerZone"]?.GetValue<int>());
            Assert.Equal(0.5, exportedBlock!["ChanceToSpawnInResourceContainer"]?.GetValue<double>());
            Assert.Equal("Export_Test_Distribution", exportedBlock!["name"]?.GetValue<string>());
            Assert.Equal((uint)9999, exportedBlock!["persistentID"]?.GetValue<uint>());

            var spawnData = exportedBlock!["SpawnData"]?.AsArray();
            Assert.NotNull(spawnData);
            Assert.Equal(2, spawnData!.Count);
            Assert.Equal((uint)30, spawnData[0]!["ItemID"]?.GetValue<uint>());
            Assert.Equal(1.0, spawnData[0]!["Weight"]?.GetValue<double>());
            Assert.Equal((uint)142, spawnData[1]!["ItemID"]?.GetValue<uint>());
            Assert.Equal(3.0, spawnData[1]!["Weight"]?.GetValue<double>());
        }
        finally
        {
            if (Directory.Exists(tempExportDir))
            {
                Directory.Delete(tempExportDir, true);
            }
        }
    }

    [Fact]
    public void ZoneSettings_ConsumableDistributionInZone_DefaultsAndLevelLayoutExport()
    {
        var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
        mainVm.Consumables.Blocks.Clear();

        var dist = new ConsumableDistributionBlock
        {
            PersistentId = 48,
            Name = "Zone_Consumable_Dist_48"
        };
        mainVm.Consumables.Blocks.Add(dist);

        var level = mainVm.LevelLayout.Levels.First();
        var zoneSettings = level.GridState.GetOrCreateZoneSettings(SectorType.Main, 0);
        zoneSettings.ConsumableDistributionInZone = 48;

        // Test LevelLayoutViewModel SelectedZoneConsumableDistributionOption binding
        var room = new PlacedRoomNode { Sector = SectorType.Main, Zone = 0 };
        mainVm.LevelLayout.EditingRoom = room;

        Assert.Equal(dist, mainVm.LevelLayout.SelectedZoneConsumableDistributionOption);

        var dist2 = new ConsumableDistributionBlock
        {
            PersistentId = 99,
            Name = "Zone_Consumable_Dist_99"
        };
        mainVm.Consumables.Blocks.Add(dist2);
        mainVm.LevelLayout.SelectedZoneConsumableDistributionOption = dist2;
        Assert.Equal((uint)99, zoneSettings.ConsumableDistributionInZone);

        // Test Save and Load roundtrip
        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_zone_consumables_{Guid.NewGuid():N}.rundown");
        string tempExportDir = Path.Combine(Path.GetTempPath(), $"gtfo_export_zone_consumables_{Guid.NewGuid():N}");
        try
        {
            RundownProjectService.SaveProject(mainVm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            var newVm = new MainWindowViewModel(autoReopenLastRundown: false);
            RundownProjectService.ApplyProject(newVm, loadedProject);

            var loadedZoneSettings = newVm.LevelLayout.Levels.First().GridState.GetOrCreateZoneSettings(SectorType.Main, 0);
            Assert.Equal((uint)99, loadedZoneSettings.ConsumableDistributionInZone);

            // Test Datablock Export
            var exportResult = DatablockExportService.ExportAll(mainVm, tempExportDir);
            Assert.True(exportResult.Success, exportResult.Message);

            string exportedLevelLayoutFile = Path.Combine(tempExportDir, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(exportedLevelLayoutFile));

            string json = File.ReadAllText(exportedLevelLayoutFile);
            var rootNode = JsonNode.Parse(json)?.AsObject();
            Assert.NotNull(rootNode);

            var blocksArray = rootNode!["Blocks"]?.AsArray();
            Assert.NotNull(blocksArray);
            var firstBlock = blocksArray![0]?.AsObject();
            Assert.NotNull(firstBlock);
            var zonesArray = firstBlock!["Zones"]?.AsArray();
            Assert.NotNull(zonesArray);
            var firstZone = zonesArray![0]?.AsObject();
            Assert.NotNull(firstZone);

            Assert.Equal((uint)99, firstZone!["ConsumableDistributionInZone"]?.GetValue<uint>());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            if (Directory.Exists(tempExportDir))
            {
                Directory.Delete(tempExportDir, true);
            }
        }
    }
}
