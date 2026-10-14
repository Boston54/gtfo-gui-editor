using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class SurvivalTests
{
    [Fact]
    public void ObjectiveType_Survival_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "Survival"));
    }

    [Fact]
    public void SectorSettings_Survival_DefaultsAndCloning()
    {
        var settings = new SectorSettings();

        Assert.Equal(0, settings.TimeToActivate);
        Assert.Equal(0, settings.TimeToSurvive);
        Assert.Equal(string.Empty, settings.TimerTitle);
        Assert.Equal(string.Empty, settings.TimerToActivateTitle);

        settings.TimeToActivate = 45;
        settings.TimeToSurvive = 180;
        settings.TimerTitle = "SURVIVE UNTIL EXTRACTION";
        settings.TimerToActivateTitle = "INITIATING OVERRIDE";

        var clone = settings.Clone();

        Assert.Equal(45, clone.TimeToActivate);
        Assert.Equal(180, clone.TimeToSurvive);
        Assert.Equal("SURVIVE UNTIL EXTRACTION", clone.TimerTitle);
        Assert.Equal("INITIATING OVERRIDE", clone.TimerToActivateTitle);
    }

    [Fact]
    public void ViewModel_Survival_VisibilityAndProperties()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.Survival;
        Assert.True(layout.IsSurvivalObjective);
        Assert.False(layout.IsActivateSmallHsuObjective);

        layout.EditingSectorSettings.TimeToActivate = 30;
        layout.EditingSectorSettings.TimeToSurvive = 120;
        layout.EditingSectorSettings.TimerTitle = "DEFEND POSITION";
        layout.EditingSectorSettings.TimerToActivateTitle = "SYSTEM BOOT";

        Assert.Equal(30, layout.EditingSectorSettings.TimeToActivate);
        Assert.Equal(120, layout.EditingSectorSettings.TimeToSurvive);
        Assert.Equal("DEFEND POSITION", layout.EditingSectorSettings.TimerTitle);
        Assert.Equal("SYSTEM BOOT", layout.EditingSectorSettings.TimerToActivateTitle);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_Survival_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_survival_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.Survival;
            mainSector.TimeToActivate = 60;
            mainSector.TimeToSurvive = 300;
            mainSector.TimerTitle = "SURVIVE THE HORDE";
            mainSector.TimerToActivateTitle = "ACTIVATING DEFENSE SYSTEMS";

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.Survival, sector.ObjectiveType);
            Assert.Equal(60, sector.TimeToActivate);
            Assert.Equal(300, sector.TimeToSurvive);
            Assert.Equal("SURVIVE THE HORDE", sector.TimerTitle);
            Assert.Equal("ACTIVATING DEFENSE SYSTEMS", sector.TimerToActivateTitle);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.Survival, applied.ObjectiveType);
            Assert.Equal(60, applied.TimeToActivate);
            Assert.Equal(300, applied.TimeToSurvive);
            Assert.Equal("SURVIVE THE HORDE", applied.TimerTitle);
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
