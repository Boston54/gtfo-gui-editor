using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class DistributePowerCellsTests
{
    [Fact]
    public void ObjectiveType_DistributePowerCells_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "DistributePowerCells"));
    }

    [Fact]
    public void SectorSettings_DistributePowerCells_DefaultAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Single(settings.DistributeGeneratorPlacements);
        Assert.Equal(1, settings.DistributeGeneratorCount);

        settings.ObjectiveType = ObjectiveType.DistributePowerCells;
        settings.DistributeGeneratorCount = 3;
        settings.DistributeGeneratorPlacements[0].Zone = 1;
        settings.DistributeGeneratorPlacements[1].Zone = 2;
        settings.DistributeGeneratorPlacements[2].Zone = 3;

        var clone = settings.Clone();
        Assert.Equal(ObjectiveType.DistributePowerCells, clone.ObjectiveType);
        Assert.Equal(3, clone.DistributeGeneratorCount);
        Assert.Equal(3, clone.DistributeGeneratorPlacements.Count);
        Assert.Equal(2, clone.DistributeGeneratorPlacements[1].Zone);
        Assert.Equal("Generator 2 (Zone 2)", clone.DistributeGeneratorPlacements[1].DisplayTitle);
    }

    [Fact]
    public void ViewModel_DistributePowerCells_AddRemoveAndSelectZone()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.DistributePowerCells;
        Assert.True(layout.IsDistributePowerCellsObjective);

        layout.DistributeGeneratorCount = 3;
        Assert.Equal(3, layout.EditingSectorSettings.DistributeGeneratorPlacements.Count);

        layout.SelectedDistributeGeneratorPlacement = layout.EditingSectorSettings.DistributeGeneratorPlacements[1];
        Assert.True(layout.HasSelectedDistributeGeneratorPlacement);

        var zone = layout.AvailableObjectiveZones.LastOrDefault();
        if (zone != null)
        {
            layout.SelectedDistributeGeneratorPlacement.Zone = zone.ZoneIndex;
            Assert.Equal(zone.ZoneIndex, layout.SelectedDistributeGeneratorPlacement.Zone);
        }

        layout.AddDistributeGeneratorPlacement();
        Assert.Equal(4, layout.EditingSectorSettings.DistributeGeneratorPlacements.Count);
        Assert.Equal(4, layout.DistributeGeneratorCount);

        layout.RemoveDistributeGeneratorPlacement();
        Assert.Equal(3, layout.EditingSectorSettings.DistributeGeneratorPlacements.Count);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_DistributePowerCells_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_distributecells_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.DistributePowerCells;
            mainSector.DistributeGeneratorCount = 2;
            mainSector.DistributeGeneratorPlacements[0].Zone = 1;
            mainSector.DistributeGeneratorPlacements[1].Zone = 3;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.DistributePowerCells, sector.ObjectiveType);
            Assert.Equal(2, sector.DistributeGeneratorPlacements?.Count);
            Assert.Equal(3, sector.DistributeGeneratorPlacements?[1].Zone);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.DistributePowerCells, applied.ObjectiveType);
            Assert.Equal(2, applied.DistributeGeneratorCount);
            Assert.Equal(3, applied.DistributeGeneratorPlacements[1].Zone);
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
