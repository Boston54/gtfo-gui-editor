using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class CentralGeneratorClusterTests
{
    [Fact]
    public void ObjectiveType_CentralGeneratorCluster_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "CentralGeneratorCluster"));
    }

    [Fact]
    public void SectorSettings_CentralGeneratorCluster_DefaultAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(0, settings.CentralGeneratorClusterZone);
        Assert.Equal(4, settings.CentralGeneratorClusterGeneratorCount);
        Assert.Equal(4, settings.CentralGeneratorClusterFogTransitions.Count);
        Assert.Equal(4, settings.CentralGeneratorClusterPowerCellCount);
        Assert.Equal(4, settings.CentralGeneratorClusterPowerCellPlacements.Count);

        settings.ObjectiveType = ObjectiveType.CentralGeneratorCluster;
        settings.CentralGeneratorClusterZone = 2;
        settings.CentralGeneratorClusterGeneratorCount = 2;
        settings.CentralGeneratorClusterFogTransitions[0].FogSetting = 10;
        settings.CentralGeneratorClusterFogTransitions[0].TransitionDuration = 15.5;

        var clone = settings.Clone();
        Assert.Equal(ObjectiveType.CentralGeneratorCluster, clone.ObjectiveType);
        Assert.Equal(2, clone.CentralGeneratorClusterZone);
        Assert.Equal(2, clone.CentralGeneratorClusterGeneratorCount);
        Assert.Equal(2, clone.CentralGeneratorClusterFogTransitions.Count);
        Assert.Equal(10u, clone.CentralGeneratorClusterFogTransitions[0].FogSetting);
        Assert.Equal(15.5, clone.CentralGeneratorClusterFogTransitions[0].TransitionDuration);
    }

    [Fact]
    public void ViewModel_CentralGeneratorCluster_VisibilityAndSync()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.CentralGeneratorCluster;
        Assert.True(layout.IsCentralGeneratorClusterObjective);

        layout.CentralGeneratorClusterGeneratorCount = 3;
        Assert.Equal(3, layout.EditingSectorSettings.CentralGeneratorClusterFogTransitions.Count);

        layout.CentralGeneratorClusterPowerCellCount = 2;
        Assert.Equal(2, layout.EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count);

        var zone = layout.AvailableObjectiveZones.FirstOrDefault();
        if (zone != null)
        {
            layout.SelectedCentralGeneratorClusterZoneOption = zone;
            Assert.Equal(zone.ZoneIndex, layout.EditingSectorSettings.CentralGeneratorClusterZone);
        }
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_CentralGeneratorCluster_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_centralgen_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.CentralGeneratorCluster;
            mainSector.CentralGeneratorClusterZone = 1;
            mainSector.CentralGeneratorClusterGeneratorCount = 2;
            mainSector.CentralGeneratorClusterFogTransitions[0].FogSetting = 101;
            mainSector.CentralGeneratorClusterFogTransitions[0].TransitionDuration = 20.0;
            mainSector.CentralGeneratorClusterFogTransitions[1].FogSetting = 102;
            mainSector.CentralGeneratorClusterFogTransitions[1].TransitionDuration = 30.0;

            mainSector.CentralGeneratorClusterPowerCellCount = 2;
            mainSector.CentralGeneratorClusterPowerCellPlacements[0].CandidateZones[0].DimensionIndex = "Reality";
            mainSector.CentralGeneratorClusterPowerCellPlacements[0].CandidateZones[0].Zone = 0;
            mainSector.CentralGeneratorClusterPowerCellPlacements[0].CandidateZones[0].PlacementStartWeight = 0.5;

            mainSector.CentralGeneratorClusterPowerCellPlacements[1].CandidateZones[0].DimensionIndex = "Dimension_1";
            mainSector.CentralGeneratorClusterPowerCellPlacements[1].CandidateZones[0].Zone = 1;
            mainSector.CentralGeneratorClusterPowerCellPlacements[1].CandidateZones[0].PlacementEndWeight = 1.0;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.CentralGeneratorCluster, sector.ObjectiveType);
            Assert.Equal(1, sector.CentralGeneratorClusterZone);
            Assert.Equal(2, sector.CentralGeneratorClusterFogTransitions?.Count);
            Assert.Equal(2, sector.CentralGeneratorClusterFogTransitions?.Count);
            Assert.Equal(101u, sector.CentralGeneratorClusterFogTransitions?[0].FogSetting);
            Assert.Equal(20.0, sector.CentralGeneratorClusterFogTransitions?[0].TransitionDuration);
            Assert.Equal(2, sector.CentralGeneratorClusterPowerCellPlacements?.Count);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.CentralGeneratorCluster, applied.ObjectiveType);
            Assert.Equal(1, applied.CentralGeneratorClusterZone);
            Assert.Equal(2, applied.CentralGeneratorClusterGeneratorCount);
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
