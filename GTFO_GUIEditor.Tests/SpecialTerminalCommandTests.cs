using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class SpecialTerminalCommandTests
{
    [Fact]
    public void SpecialCommandRule_HasExpectedValues()
    {
        var values = Enum.GetValues<SpecialCommandRule>();
        Assert.Contains(SpecialCommandRule.Normal, values);
        Assert.Contains(SpecialCommandRule.OnlyOnce, values);
        Assert.Contains(SpecialCommandRule.OnlyOnceDelete, values);
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void SectorSettings_SpecialTerminalCommand_DefaultAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(0, settings.SpecialTerminalCommandZone);
        Assert.Equal(string.Empty, settings.SpecialTerminalCommand);
        Assert.Equal(string.Empty, settings.SpecialTerminalCommandDesc);
        Assert.Equal(string.Empty, settings.PostCommandOutput);
        Assert.Equal(SpecialCommandRule.Normal, settings.SpecialCommandRule);

        settings.ObjectiveType = ObjectiveType.SpecialTerminalCommand;
        settings.SpecialTerminalCommandZone = 3;
        settings.SpecialTerminalCommand = "TERMINAL_OVERRIDE";
        settings.SpecialTerminalCommandDesc = "Override lockdown sequence";
        settings.PostCommandOutput = "OVERRIDE ACCEPTED";
        settings.SpecialCommandRule = SpecialCommandRule.OnlyOnceDelete;

        var clone = settings.Clone();
        Assert.Equal(ObjectiveType.SpecialTerminalCommand, clone.ObjectiveType);
        Assert.Equal(3, clone.SpecialTerminalCommandZone);
        Assert.Equal("TERMINAL_OVERRIDE", clone.SpecialTerminalCommand);
        Assert.Equal("Override lockdown sequence", clone.SpecialTerminalCommandDesc);
        Assert.Equal("OVERRIDE ACCEPTED", clone.PostCommandOutput);
        Assert.Equal(SpecialCommandRule.OnlyOnceDelete, clone.SpecialCommandRule);

        clone.SpecialTerminalCommand = "DIFFERENT_CMD";
        Assert.Equal("TERMINAL_OVERRIDE", settings.SpecialTerminalCommand);
    }

    [Fact]
    public void ViewModel_SpecialTerminalCommand_PropertiesAndOptions()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.SpecialTerminalCommand;
        Assert.True(layout.IsSpecialTerminalCommandObjective);
        Assert.False(layout.IsReactorObjective);

        var zone = layout.AvailableObjectiveZones.FirstOrDefault();
        if (zone != null)
        {
            layout.SelectedSpecialTerminalCommandZoneOption = zone;
            Assert.Equal(zone.ZoneIndex, layout.EditingSectorSettings.SpecialTerminalCommandZone);
        }

        layout.SelectedSpecialCommandRule = SpecialCommandRule.OnlyOnce;
        Assert.Equal(SpecialCommandRule.OnlyOnce, layout.EditingSectorSettings.SpecialCommandRule);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_SpecialTerminalCommand_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_specialcmd_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.SpecialTerminalCommand;
            mainSector.SpecialTerminalCommandZone = 2;
            mainSector.SpecialTerminalCommand = "UNLOCK_REACTOR";
            mainSector.SpecialTerminalCommandDesc = "Unlock the reactor blast doors";
            mainSector.PostCommandOutput = "DOORS UNLOCKED";
            mainSector.SpecialCommandRule = SpecialCommandRule.OnlyOnceDelete;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.SpecialTerminalCommand, sector.ObjectiveType);
            Assert.Equal(2, sector.SpecialTerminalCommandZone);
            Assert.Equal("UNLOCK_REACTOR", sector.SpecialTerminalCommand);
            Assert.Equal("Unlock the reactor blast doors", sector.SpecialTerminalCommandDesc);
            Assert.Equal("DOORS UNLOCKED", sector.PostCommandOutput);
            Assert.Equal(SpecialCommandRule.OnlyOnceDelete, sector.SpecialCommandRule);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.SpecialTerminalCommand, applied.ObjectiveType);
            Assert.Equal(2, applied.SpecialTerminalCommandZone);
            Assert.Equal("UNLOCK_REACTOR", applied.SpecialTerminalCommand);
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
