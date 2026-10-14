using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class GatherSmallItemsTests
{
    [Fact]
    public void GatherSmallItemType_HasCorrectInternalIntegerValues()
    {
        Assert.Equal(128, (int)GatherSmallItemType.PersonnelID);
        Assert.Equal(129, (int)GatherSmallItemType.PartialDecoder);
        Assert.Equal(147, (int)GatherSmallItemType.HardDrive);
        Assert.Equal(183, (int)GatherSmallItemType.Microdrive);
        Assert.Equal(149, (int)GatherSmallItemType.GLP_1);
        Assert.Equal(150, (int)GatherSmallItemType.OSIP);
        Assert.Equal(169, (int)GatherSmallItemType.GLP_2);
        Assert.Equal(168, (int)GatherSmallItemType.DataCubeTampered);
        Assert.Equal(165, (int)GatherSmallItemType.DataCube);
        Assert.Equal(179, (int)GatherSmallItemType.DataCubes);
        Assert.Equal(153, (int)GatherSmallItemType.PlantSample);
        Assert.Equal(171, (int)GatherSmallItemType.MemoryStick);
    }

    [Fact]
    public void SectorSettings_GatherSmallItems_DefaultValues_AndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(GatherSmallItemType.PersonnelID, settings.GatherItem);
        Assert.Single(settings.GatherItemPlacements);
        Assert.Equal(1, settings.GatherSpawnCount);
        Assert.Equal("Reality", settings.GatherItemPlacements[0].DimensionIndex);
        Assert.Equal(0, settings.GatherItemPlacements[0].Zone);
        Assert.Equal(0.0, settings.GatherItemPlacements[0].PlacementStartWeight);
        Assert.Equal(0.0, settings.GatherItemPlacements[0].PlacementMiddleWeight);
        Assert.Equal(0.0, settings.GatherItemPlacements[0].PlacementEndWeight);

        settings.ObjectiveType = ObjectiveType.GatherSmallItems;
        settings.GatherItem = GatherSmallItemType.GLP_2;
        settings.GatherSpawnCount = 3;
        Assert.Equal(3, settings.GatherItemPlacements.Count);
        Assert.Equal(3, settings.GatherSpawnCount);

        settings.GatherItemPlacements[0].DimensionIndex = "Reality";
        settings.GatherItemPlacements[0].Zone = 0;
        settings.GatherItemPlacements[0].PlacementStartWeight = 1.0;

        settings.GatherItemPlacements[1].DimensionIndex = "Dimension_1";
        settings.GatherItemPlacements[1].Zone = 1;
        settings.GatherItemPlacements[1].PlacementMiddleWeight = 0.5;

        settings.GatherItemPlacements[2].DimensionIndex = "Dimension_2";
        settings.GatherItemPlacements[2].Zone = 2;
        settings.GatherItemPlacements[2].PlacementEndWeight = 0.8;

        var cloned = settings.Clone();
        Assert.Equal(ObjectiveType.GatherSmallItems, cloned.ObjectiveType);
        Assert.Equal(GatherSmallItemType.GLP_2, cloned.GatherItem);
        Assert.Equal(3, cloned.GatherItemPlacements.Count);
        Assert.Equal(3, cloned.GatherSpawnCount);

        Assert.Equal("Reality", cloned.GatherItemPlacements[0].DimensionIndex);
        Assert.Equal(0, cloned.GatherItemPlacements[0].Zone);
        Assert.Equal(1.0, cloned.GatherItemPlacements[0].PlacementStartWeight);

        Assert.Equal("Dimension_1", cloned.GatherItemPlacements[1].DimensionIndex);
        Assert.Equal(1, cloned.GatherItemPlacements[1].Zone);
        Assert.Equal(0.5, cloned.GatherItemPlacements[1].PlacementMiddleWeight);

        Assert.Equal("Dimension_2", cloned.GatherItemPlacements[2].DimensionIndex);
        Assert.Equal(2, cloned.GatherItemPlacements[2].Zone);
        Assert.Equal(0.8, cloned.GatherItemPlacements[2].PlacementEndWeight);

        // Verify independent instance
        cloned.GatherItem = GatherSmallItemType.HardDrive;
        cloned.GatherItemPlacements[0].PlacementStartWeight = 0.2;
        Assert.Equal(GatherSmallItemType.GLP_2, settings.GatherItem);
        Assert.Equal(1.0, settings.GatherItemPlacements[0].PlacementStartWeight);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_ObjectiveVisibilityFlags()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();

        Assert.NotNull(vm.EditingSectorSettings);
        Assert.True(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);
        Assert.False(vm.IsGatherSmallItemsObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;
        Assert.False(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);
        Assert.True(vm.IsGatherSmallItemsObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Shutdown;
        Assert.False(vm.IsGatherSmallItemsObjective);
        Assert.True(vm.IsReactorShutdownObjective);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_ItemAndSpawnCountManagement()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;

        Assert.Equal(12, vm.AvailableGatherSmallItemOptions.Count);
        Assert.Equal(GatherSmallItemType.PersonnelID, vm.SelectedGatherItem);

        var hardDriveOpt = vm.AvailableGatherSmallItemOptions.First(o => o.Type == GatherSmallItemType.HardDrive);
        vm.SelectedGatherSmallItemOption = hardDriveOpt;
        Assert.Equal(GatherSmallItemType.HardDrive, vm.EditingSectorSettings!.GatherItem);
        Assert.Equal(GatherSmallItemType.HardDrive, vm.SelectedGatherItem);

        // Test Spawn count modifications
        vm.GatherSpawnCount = 3;
        Assert.Equal(3, vm.EditingSectorSettings.GatherItemPlacements.Count);
        Assert.NotNull(vm.SelectedGatherItemPlacement);
        Assert.True(vm.HasSelectedGatherItemPlacement);

        // Add item
        vm.AddGatherItemPlacement();
        Assert.Equal(4, vm.GatherSpawnCount);
        Assert.Equal(4, vm.EditingSectorSettings.GatherItemPlacements.Count);
        Assert.Equal(4, vm.SelectedGatherItemPlacement.ItemNumber);

        // Remove item
        vm.RemoveGatherItemPlacement();
        Assert.Equal(3, vm.GatherSpawnCount);
        Assert.Equal(3, vm.EditingSectorSettings.GatherItemPlacements.Count);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_DimensionAndZoneOptionBindings()
    {
        var vm = new LevelLayoutViewModel();
        // Add dimension
        var level = vm.SelectedLevel;
        Assert.NotNull(level);
        level.Dimensions.Add(new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Dimension 1"
        });
        vm.RefreshAvailableDimensions();

        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;
        vm.GatherSpawnCount = 2;

        var placement1 = vm.EditingSectorSettings!.GatherItemPlacements[0];
        var placement2 = vm.EditingSectorSettings.GatherItemPlacements[1];

        vm.SelectedGatherItemPlacement = placement1;
        var realityOpt = vm.AvailableDimensions.First(d => d.DimensionIndex == "Reality");
        vm.SelectedGatherItemPlacementDimensionOption = realityOpt;
        Assert.Equal("Reality", placement1.DimensionIndex);

        vm.SelectedGatherItemPlacement = placement2;
        var dim1Opt = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");
        vm.SelectedGatherItemPlacementDimensionOption = dim1Opt;
        Assert.Equal("Dimension_1", placement2.DimensionIndex);

        var zoneOpt = vm.AvailableObjectiveZones.FirstOrDefault();
        if (zoneOpt != null)
        {
            vm.SelectedGatherItemPlacementZoneOption = zoneOpt;
            Assert.Equal(zoneOpt.ZoneIndex, placement2.Zone);
        }
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_PersistsGatherSmallItemsSettings()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_gather_{Guid.NewGuid()}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.GatherSmallItems;
            mainSector.GatherItem = GatherSmallItemType.PlantSample;
            mainSector.GatherSpawnCount = 2;

            mainSector.GatherItemPlacements[0].DimensionIndex = "Reality";
            mainSector.GatherItemPlacements[0].Zone = 0;
            mainSector.GatherItemPlacements[0].PlacementStartWeight = 0.9;
            mainSector.GatherItemPlacements[0].PlacementMiddleWeight = 0.5;
            mainSector.GatherItemPlacements[0].PlacementEndWeight = 0.1;

            mainSector.GatherItemPlacements[1].DimensionIndex = "Dimension_1";
            mainSector.GatherItemPlacements[1].Zone = 2;
            mainSector.GatherItemPlacements[1].PlacementStartWeight = 0.2;
            mainSector.GatherItemPlacements[1].PlacementMiddleWeight = 0.4;
            mainSector.GatherItemPlacements[1].PlacementEndWeight = 0.8;

            RundownProjectService.SaveProject(vm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            var loadedSector = loadedProject.Levels?.FirstOrDefault()?.MainSector;

            Assert.NotNull(loadedSector);
            Assert.Equal(ObjectiveType.GatherSmallItems, loadedSector.ObjectiveType);
            Assert.Equal(GatherSmallItemType.PlantSample, loadedSector.GatherItem);
            Assert.NotNull(loadedSector.GatherItemPlacements);
            Assert.Equal(2, loadedSector.GatherItemPlacements.Count);

            Assert.Equal("Reality", loadedSector.GatherItemPlacements[0].DimensionIndex);
            Assert.Equal(0, loadedSector.GatherItemPlacements[0].Zone);
            Assert.Equal(0.9, loadedSector.GatherItemPlacements[0].PlacementStartWeight);
            Assert.Equal(0.5, loadedSector.GatherItemPlacements[0].PlacementMiddleWeight);
            Assert.Equal(0.1, loadedSector.GatherItemPlacements[0].PlacementEndWeight);

            Assert.Equal("Dimension_1", loadedSector.GatherItemPlacements[1].DimensionIndex);
            Assert.Equal(2, loadedSector.GatherItemPlacements[1].Zone);
            Assert.Equal(0.2, loadedSector.GatherItemPlacements[1].PlacementStartWeight);
            Assert.Equal(0.4, loadedSector.GatherItemPlacements[1].PlacementMiddleWeight);
            Assert.Equal(0.8, loadedSector.GatherItemPlacements[1].PlacementEndWeight);

            // Apply project to fresh ViewModel
            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loadedProject, tempFile);

            var appliedSector = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(appliedSector);
            Assert.Equal(ObjectiveType.GatherSmallItems, appliedSector.ObjectiveType);
            Assert.Equal(GatherSmallItemType.PlantSample, appliedSector.GatherItem);
            Assert.Equal(2, appliedSector.GatherItemPlacements.Count);
            Assert.Equal(2, appliedSector.GatherSpawnCount);

            Assert.Equal("Dimension_1", appliedSector.GatherItemPlacements[1].DimensionIndex);
            Assert.Equal(2, appliedSector.GatherItemPlacements[1].Zone);
            Assert.Equal(0.8, appliedSector.GatherItemPlacements[1].PlacementEndWeight);
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
