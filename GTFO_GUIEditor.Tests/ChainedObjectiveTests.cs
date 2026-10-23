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

public class ChainedObjectiveTests
{
    [Fact]
    public void SectorSettings_InitializesWithSingleObjective()
    {
        var sector = new SectorSettings();
        Assert.Single(sector.Objectives);
        Assert.NotNull(sector.PrimaryObjective);
        Assert.Same(sector.Objectives[0], sector.PrimaryObjective);
        Assert.Equal(1, sector.Objectives[0].ObjectiveNumber);
    }

    [Fact]
    public void SectorSettings_MultipleObjectives_RenumberingAndCloning()
    {
        var sector = new SectorSettings();
        var obj2 = new WardenObjective
        {
            ObjectiveType = ObjectiveType.Reactor_Startup,
            WinCondition = WinCondition.GoToElevator,
            Header = "Second Objective"
        };
        var obj3 = new WardenObjective
        {
            ObjectiveType = ObjectiveType.Survival,
            WinCondition = WinCondition.GoToExitGeo,
            Header = "Third Objective"
        };

        sector.Objectives.Add(obj2);
        sector.Objectives.Add(obj3);

        Assert.Equal(3, sector.Objectives.Count);
        Assert.Equal(1, sector.Objectives[0].ObjectiveNumber);
        Assert.Equal(2, sector.Objectives[1].ObjectiveNumber);
        Assert.Equal(3, sector.Objectives[2].ObjectiveNumber);

        var clone = sector.Clone();
        Assert.Equal(3, clone.Objectives.Count);
        Assert.Equal(1, clone.Objectives[0].ObjectiveNumber);
        Assert.Equal(2, clone.Objectives[1].ObjectiveNumber);
        Assert.Equal("Second Objective", clone.Objectives[1].Header);
        Assert.Equal(3, clone.Objectives[2].ObjectiveNumber);
        Assert.Equal("Third Objective", clone.Objectives[2].Header);
    }

    [Fact]
    public void ViewModel_AddAndRemoveObjectives_OnlyForMainSector()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;

        // Open Main sector
        layout.OpenSectorSettings(SectorType.Main);
        Assert.True(layout.IsMainSector);
        Assert.NotNull(layout.EditingSectorSettings);
        Assert.Single(layout.EditingSectorSettings.Objectives);
        Assert.False(layout.CanRemoveObjective);

        // Add objective
        layout.AddObjective();
        Assert.Equal(2, layout.EditingSectorSettings.Objectives.Count);
        Assert.Same(layout.EditingSectorSettings.Objectives[1], layout.ActiveEditingObjective);
        Assert.True(layout.CanRemoveObjective);

        // Add third objective
        layout.AddObjective();
        Assert.Equal(3, layout.EditingSectorSettings.Objectives.Count);
        Assert.Equal(3, layout.ActiveEditingObjective?.ObjectiveNumber);

        // Remove active objective
        layout.RemoveObjective();
        Assert.Equal(2, layout.EditingSectorSettings.Objectives.Count);
        Assert.NotNull(layout.ActiveEditingObjective);

        // Open Secondary sector
        layout.OpenSectorSettings(SectorType.Secondary);
        Assert.False(layout.IsMainSector);
        Assert.False(layout.CanRemoveObjective);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_ChainedObjectives_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_chained_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.Objectives[0].ObjectiveType = ObjectiveType.HSU_FindTakeSample;
            mainSector.Objectives[0].Header = "Primary HSU";
            mainSector.Objectives[0].WinCondition = WinCondition.GoToElevator;

            var obj2 = new WardenObjective
            {
                ObjectiveNumber = 2,
                ObjectiveType = ObjectiveType.Reactor_Startup,
                Header = "Chained Reactor",
                WinCondition = WinCondition.GoToExitGeo
            };
            mainSector.Objectives.Add(obj2);

            var obj3 = new WardenObjective
            {
                ObjectiveNumber = 3,
                ObjectiveType = ObjectiveType.Survival,
                Header = "Chained Survival",
                TimeToSurvive = 180,
                WinCondition = WinCondition.GoToElevator
            };
            mainSector.Objectives.Add(obj3);

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);
            Assert.Equal(3, sector.Objectives?.Count);
            Assert.Equal("Primary HSU", sector.Objectives?[0].Header);
            Assert.Equal(ObjectiveType.HSU_FindTakeSample, sector.Objectives?[0].ObjectiveType);
            Assert.Equal("Chained Reactor", sector.Objectives?[1].Header);
            Assert.Equal(ObjectiveType.Reactor_Startup, sector.Objectives?[1].ObjectiveType);
            Assert.Equal("Chained Survival", sector.Objectives?[2].Header);
            Assert.Equal(ObjectiveType.Survival, sector.Objectives?[2].ObjectiveType);
            Assert.Equal(180, sector.Objectives?[2].TimeToSurvive);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(3, applied.Objectives.Count);
            Assert.Equal("Primary HSU", applied.Objectives[0].Header);
            Assert.Equal("Chained Reactor", applied.Objectives[1].Header);
            Assert.Equal("Chained Survival", applied.Objectives[2].Header);
            Assert.Equal(180, applied.Objectives[2].TimeToSurvive);
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
    public void DatablockExportService_ExportsChainedObjectiveData_Correctly()
    {
        var tempExportDir = Path.Combine(Path.GetTempPath(), $"gtfo_export_chained_{Guid.NewGuid():N}");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.Objectives[0].ObjectiveType = ObjectiveType.HSU_FindTakeSample;
            mainSector.Objectives[0].Header = "HSU Step 1";
            mainSector.Objectives[0].WinCondition = WinCondition.GoToElevator;

            var obj2 = new WardenObjective
            {
                ObjectiveNumber = 2,
                ObjectiveType = ObjectiveType.Reactor_Startup,
                Header = "Reactor Step 2",
                WinCondition = WinCondition.GoToExitGeo
            };
            mainSector.Objectives.Add(obj2);

            var obj3 = new WardenObjective
            {
                ObjectiveNumber = 3,
                ObjectiveType = ObjectiveType.Survival,
                Header = "Survival Step 3",
                WinCondition = WinCondition.GoToElevator
            };
            mainSector.Objectives.Add(obj3);

            DatablockExportService.ExportAll(vm, tempExportDir);

            string rundownDataBlockPath = Path.Combine(tempExportDir, "GameData_RundownDataBlock_bin.json");
            Assert.True(File.Exists(rundownDataBlockPath));

            string json = File.ReadAllText(rundownDataBlockPath);
            using var doc = JsonDocument.Parse(json);
            var blocks = doc.RootElement.GetProperty("Blocks");
            Assert.True(blocks.GetArrayLength() > 0);

            var firstBlock = blocks[0];
            var tierA = firstBlock.GetProperty("TierA");
            Assert.True(tierA.GetArrayLength() > 0);

            var firstExpedition = tierA[0];
            var mainLayer = firstExpedition.GetProperty("MainLayerData");

            // ObjectiveData contains primary objective (first item)
            var objectiveData = mainLayer.GetProperty("ObjectiveData");
            Assert.Equal("GoToElevator", objectiveData.GetProperty("WinCondition").GetString());
            uint primaryId = objectiveData.GetProperty("DataBlockId").GetUInt32();
            Assert.True(primaryId > 0);

            // ChainedObjectiveData contains remaining items in order
            var chainedObjectives = mainLayer.GetProperty("ChainedObjectiveData");
            Assert.Equal(2, chainedObjectives.GetArrayLength());

            var chained1 = chainedObjectives[0];
            Assert.Equal("GoToExitGeo", chained1.GetProperty("WinCondition").GetString());
            uint chained1Id = chained1.GetProperty("DataBlockId").GetUInt32();
            Assert.True(chained1Id > 0);
            Assert.NotEqual(primaryId, chained1Id);

            var chained2 = chainedObjectives[1];
            Assert.Equal("GoToElevator", chained2.GetProperty("WinCondition").GetString());
            uint chained2Id = chained2.GetProperty("DataBlockId").GetUInt32();
            Assert.True(chained2Id > 0);
            Assert.NotEqual(chained1Id, chained2Id);

            // Verify WardenObjectiveDataBlock has all 3 generated objective blocks
            string wardenDataBlockPath = Path.Combine(tempExportDir, "GameData_WardenObjectiveDataBlock_bin.json");
            Assert.True(File.Exists(wardenDataBlockPath));
            string wardenJson = File.ReadAllText(wardenDataBlockPath);
            using var wardenDoc = JsonDocument.Parse(wardenJson);
            var wardenBlocks = wardenDoc.RootElement.GetProperty("Blocks");

            var exportedIds = wardenBlocks.EnumerateArray().Select(b => b.GetProperty("persistentID").GetUInt32()).ToList();
            Assert.Contains(primaryId, exportedIds);
            Assert.Contains(chained1Id, exportedIds);
            Assert.Contains(chained2Id, exportedIds);
        }
        finally
        {
            if (Directory.Exists(tempExportDir))
            {
                Directory.Delete(tempExportDir, true);
            }
        }
    }
}
