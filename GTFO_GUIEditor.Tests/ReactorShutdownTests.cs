using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ReactorShutdownTests
{
    [Fact]
    public void SectorSettings_ReactorShutdown_DefaultValues_AndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(0u, settings.ChainedPuzzleToActive);
        Assert.Equal(0u, settings.ChainedPuzzleMidObjective);
        Assert.Equal(0, settings.ReactorPlacementZone);
        Assert.Equal(ObjectiveType.HSU_FindTakeSample, settings.ObjectiveType);

        settings.ObjectiveType = ObjectiveType.Reactor_Shutdown;
        settings.ReactorPlacementZone = 2;
        settings.ChainedPuzzleToActive = 10;
        settings.ChainedPuzzleMidObjective = 200;

        var cloned = settings.Clone();
        Assert.Equal(ObjectiveType.Reactor_Shutdown, cloned.ObjectiveType);
        Assert.Equal(2, cloned.ReactorPlacementZone);
        Assert.Equal(10u, cloned.ChainedPuzzleToActive);
        Assert.Equal(200u, cloned.ChainedPuzzleMidObjective);

        // Verify independent instance
        cloned.ChainedPuzzleToActive = 12;
        cloned.ChainedPuzzleMidObjective = 201;
        Assert.Equal(10u, settings.ChainedPuzzleToActive);
        Assert.Equal(200u, settings.ChainedPuzzleMidObjective);
    }

    [Fact]
    public void LevelLayoutViewModel_ReactorShutdown_ObjectiveVisibilityFlags()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();

        Assert.NotNull(vm.EditingSectorSettings);
        Assert.True(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Shutdown;
        Assert.False(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.True(vm.IsReactorShutdownObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Startup;
        Assert.False(vm.IsHsuObjective);
        Assert.True(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.HSU_FindTakeSample;
        Assert.True(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);
    }

    [Fact]
    public void LevelLayoutViewModel_ReactorShutdown_AlarmSelections()
    {
        var alarms = new ObservableCollection<AlarmBlock>
        {
            new() { PersistentId = 10, Name = "Class II Alarm" },
            new() { PersistentId = 11, Name = "Class III Alarm" },
            new() { PersistentId = 200, Name = "Class V Error Alarm" }
        };

        var vm = new LevelLayoutViewModel(alarms: alarms);
        vm.OpenMainSectorSettings();

        Assert.Equal(3, vm.AvailableAlarms.Count);

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Shutdown;
        Assert.NotNull(vm.EditingSectorSettings);

        // Select before shutdown alarm
        vm.SelectedAlarmBeforeShutdownOption = alarms[1]; // PersistentId 11
        Assert.Equal(11u, vm.EditingSectorSettings.ChainedPuzzleToActive);
        Assert.Equal(alarms[1], vm.SelectedAlarmBeforeShutdownOption);

        // Select after shutdown alarm
        vm.SelectedAlarmAfterShutdownOption = alarms[2]; // PersistentId 200
        Assert.Equal(200u, vm.EditingSectorSettings.ChainedPuzzleMidObjective);
        Assert.Equal(alarms[2], vm.SelectedAlarmAfterShutdownOption);

        // Select reactor zone
        var zoneOpt = vm.AvailableObjectiveZones.FirstOrDefault(z => z.ZoneIndex == 0);
        if (zoneOpt != null)
        {
            vm.SelectedReactorPlacementZoneOption = zoneOpt;
            Assert.Equal(0, vm.EditingSectorSettings.ReactorPlacementZone);
        }
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_PersistsReactorShutdownSettings()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_shutdown_{System.Guid.NewGuid()}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.Reactor_Shutdown;
            mainSector.ReactorPlacementZone = 3;
            mainSector.ChainedPuzzleToActive = 10;
            mainSector.ChainedPuzzleMidObjective = 200;

            RundownProjectService.SaveProject(vm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            var loadedSector = loadedProject.Levels?.FirstOrDefault()?.MainSector;

            Assert.NotNull(loadedSector);
            Assert.Equal(ObjectiveType.Reactor_Shutdown, loadedSector.ObjectiveType);
            Assert.Equal(3, loadedSector.ReactorPlacementZone);
            Assert.Equal(10u, loadedSector.ChainedPuzzleToActive);
            Assert.Equal(200u, loadedSector.ChainedPuzzleMidObjective);

            // Apply project to fresh ViewModel
            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loadedProject, tempFile);

            var appliedSector = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(appliedSector);
            Assert.Equal(ObjectiveType.Reactor_Shutdown, appliedSector.ObjectiveType);
            Assert.Equal(3, appliedSector.ReactorPlacementZone);
            Assert.Equal(10u, appliedSector.ChainedPuzzleToActive);
            Assert.Equal(200u, appliedSector.ChainedPuzzleMidObjective);
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
