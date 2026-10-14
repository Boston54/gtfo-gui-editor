using System;
using System.IO;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ObjectiveDefaultsTests
{
    [Fact]
    public void ObjectiveDefaults_JsonFile_ContainsAllObjectiveTypes()
    {
        ObjectiveDefaultsService.ResetCache();
        string? defaultsPath = ObjectiveDefaultsService.FindDefaultsFile();
        Assert.NotNull(defaultsPath);
        Assert.True(File.Exists(defaultsPath));

        foreach (var type in Enum.GetValues<ObjectiveType>())
        {
            var defaults = ObjectiveDefaultsService.GetDefaults(type);
            Assert.NotNull(defaults);
            Assert.NotNull(defaults.Header);
            Assert.NotNull(defaults.MainObjective);
            Assert.NotNull(defaults.FindLocationInfo);
            Assert.NotNull(defaults.FindLocationInfoHelp);
            Assert.NotNull(defaults.GoToZone);
            Assert.NotNull(defaults.InZoneFindItem);
            Assert.NotNull(defaults.SolveItem);
        }
    }

    [Fact]
    public void ObjectiveDefaultsService_GetAndSetDefaults_WorksCorrectly()
    {
        ObjectiveDefaultsService.ResetCache();
        var custom = new ObjectiveTextDefaults
        {
            Header = "Custom Header",
            MainObjective = "Custom Main Objective",
            FindLocationInfo = "Find Info",
            FindLocationInfoHelp = "Find Help Info",
            GoToZone = "Go To Zone 1",
            InZoneFindItem = "In Zone Item",
            SolveItem = "Solve Item"
        };

        ObjectiveDefaultsService.SetDefaults(ObjectiveType.ClearAPath, custom);

        var retrieved = ObjectiveDefaultsService.GetDefaults(ObjectiveType.ClearAPath);
        Assert.Equal("Custom Header", retrieved.Header);
        Assert.Equal("Custom Main Objective", retrieved.MainObjective);
        Assert.Equal("Find Info", retrieved.FindLocationInfo);
        Assert.Equal("Find Help Info", retrieved.FindLocationInfoHelp);
        Assert.Equal("Go To Zone 1", retrieved.GoToZone);
        Assert.Equal("In Zone Item", retrieved.InZoneFindItem);
        Assert.Equal("Solve Item", retrieved.SolveItem);

        ObjectiveDefaultsService.ResetCache();
    }

    [Fact]
    public void SectorSettings_AppliesDefaults_AndClonesCorrectly()
    {
        ObjectiveDefaultsService.ResetCache();
        var custom = new ObjectiveTextDefaults
        {
            Header = "Hsu Header",
            MainObjective = "Hsu Main Obj",
            FindLocationInfo = "Hsu Find",
            FindLocationInfoHelp = "Hsu Help",
            GoToZone = "Hsu GoTo",
            InZoneFindItem = "Hsu InZone",
            SolveItem = "Hsu Solve"
        };
        ObjectiveDefaultsService.SetDefaults(ObjectiveType.HSU_FindTakeSample, custom);

        var sector = new SectorSettings();
        sector.ApplyDefaultsForObjectiveType(ObjectiveType.HSU_FindTakeSample);

        Assert.Equal("Hsu Header", sector.Header);
        Assert.Equal("Hsu Main Obj", sector.MainObjective);
        Assert.Equal("Hsu Find", sector.FindLocationInfo);
        Assert.Equal("Hsu Help", sector.FindLocationInfoHelp);
        Assert.Equal("Hsu GoTo", sector.GoToZone);
        Assert.Equal("Hsu InZone", sector.InZoneFindItem);
        Assert.Equal("Hsu Solve", sector.SolveItem);

        var cloned = sector.Clone();
        Assert.Equal("Hsu Header", cloned.Header);
        Assert.Equal("Hsu Main Obj", cloned.MainObjective);
        Assert.Equal("Hsu Find", cloned.FindLocationInfo);
        Assert.Equal("Hsu Help", cloned.FindLocationInfoHelp);
        Assert.Equal("Hsu GoTo", cloned.GoToZone);
        Assert.Equal("Hsu InZone", cloned.InZoneFindItem);
        Assert.Equal("Hsu Solve", cloned.SolveItem);

        cloned.Header = "Modified Header";
        cloned.FindLocationInfo = "Modified Find";
        cloned.FindLocationInfoHelp = "Modified Help";
        Assert.Equal("Hsu Header", sector.Header);
        Assert.Equal("Hsu Find", sector.FindLocationInfo);
        Assert.Equal("Hsu Help", sector.FindLocationInfoHelp);

        ObjectiveDefaultsService.ResetCache();
    }

    [Fact]
    public void SectorSettings_PropertyChange_FiresForNewFields()
    {
        var sector = new SectorSettings();
        string? changedProp = null;
        sector.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        sector.FindLocationInfo = "New Find Info";
        Assert.Equal(nameof(SectorSettings.FindLocationInfo), changedProp);

        sector.FindLocationInfoHelp = "New Find Info Help";
        Assert.Equal(nameof(SectorSettings.FindLocationInfoHelp), changedProp);

        sector.GoToZone = "New Go To Zone";
        Assert.Equal(nameof(SectorSettings.GoToZone), changedProp);

        sector.InZoneFindItem = "New In Zone Find Item";
        Assert.Equal(nameof(SectorSettings.InZoneFindItem), changedProp);

        sector.SolveItem = "New Solve Item";
        Assert.Equal(nameof(SectorSettings.SolveItem), changedProp);
    }

    [Fact]
    public void LevelLayoutViewModel_SelectingObjectiveType_AppliesDefaults()
    {
        ObjectiveDefaultsService.ResetCache();
        var reactorDefaults = new ObjectiveTextDefaults
        {
            Header = "Startup Reactor",
            MainObjective = "Initialize startup sequence",
            FindLocationInfo = "Find reactor terminal",
            FindLocationInfoHelp = "Find reactor terminal help",
            GoToZone = "Navigate to reactor zone",
            InZoneFindItem = "Locate reactor unit",
            SolveItem = "Initiate startup"
        };
        ObjectiveDefaultsService.SetDefaults(ObjectiveType.Reactor_Startup, reactorDefaults);

        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Startup;

        Assert.NotNull(vm.EditingSectorSettings);
        Assert.Equal("Startup Reactor", vm.EditingSectorSettings.Header);
        Assert.Equal("Initialize startup sequence", vm.EditingSectorSettings.MainObjective);
        Assert.Equal("Find reactor terminal", vm.EditingSectorSettings.FindLocationInfo);
        Assert.Equal("Find reactor terminal help", vm.EditingSectorSettings.FindLocationInfoHelp);
        Assert.Equal("Navigate to reactor zone", vm.EditingSectorSettings.GoToZone);
        Assert.Equal("Locate reactor unit", vm.EditingSectorSettings.InZoneFindItem);
        Assert.Equal("Initiate startup", vm.EditingSectorSettings.SolveItem);

        ObjectiveDefaultsService.ResetCache();
    }

    [Fact]
    public void RundownProjectService_RoundTrips_ObjectiveDefaultsAndTextSections()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rundown_obj_defaults_{Guid.NewGuid()}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var layoutVm = vm.LevelLayout;

            var level = layoutVm.SelectedLevel;
            Assert.NotNull(level);

            var mainSector = level.GetSectorSettings(SectorType.Main);
            mainSector.ObjectiveType = ObjectiveType.Survival;
            mainSector.Header = "Survive the Onslaught";
            mainSector.MainObjective = "Defend the extraction area for 120 seconds";
            mainSector.FindLocationInfo = "Find defensive perimeter";
            mainSector.FindLocationInfoHelp = "Find defensive perimeter help";
            mainSector.GoToZone = "Proceed to Zone 3";
            mainSector.InZoneFindItem = "Locate defense terminal";
            mainSector.SolveItem = "Activate defense matrix";

            RundownProjectService.SaveProject(vm, tempFile);

            var project = RundownProjectService.LoadProject(tempFile);
            var newVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(newVm, project, tempFile);

            var loadedLevel = newVm.LevelLayout.SelectedLevel;
            Assert.NotNull(loadedLevel);

            var loadedMain = loadedLevel.GetSectorSettings(SectorType.Main);
            Assert.Equal(ObjectiveType.Survival, loadedMain.ObjectiveType);
            Assert.Equal("Survive the Onslaught", loadedMain.Header);
            Assert.Equal("Defend the extraction area for 120 seconds", loadedMain.MainObjective);
            Assert.Equal("Find defensive perimeter", loadedMain.FindLocationInfo);
            Assert.Equal("Find defensive perimeter help", loadedMain.FindLocationInfoHelp);
            Assert.Equal("Proceed to Zone 3", loadedMain.GoToZone);
            Assert.Equal("Locate defense terminal", loadedMain.InZoneFindItem);
            Assert.Equal("Activate defense matrix", loadedMain.SolveItem);
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
