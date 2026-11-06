using System;
using System.IO;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ModManagerTests
{
    public ModManagerTests()
    {
        ModManager.Instance.Reset();
        ModManager.Instance.ClearAllFeatureRegistrations();
    }

    [Fact]
    public void AdvancedWardenObjective_Toggle_WorksCorrectly()
    {
        var manager = ModManager.Instance;
        Assert.False(manager.AdvancedWardenObjective);
        Assert.False(manager.IsModEnabled(ModManager.AdvancedWardenObjectiveModId));

        manager.AdvancedWardenObjective = true;
        Assert.True(manager.AdvancedWardenObjective);
        Assert.True(manager.IsModEnabled(ModManager.AdvancedWardenObjectiveModId));

        manager.AdvancedWardenObjective = false;
        Assert.False(manager.AdvancedWardenObjective);
    }

    [Fact]
    public void ModStateChanged_EventFiresOnToggle()
    {
        var manager = ModManager.Instance;
        string? changedMod = null;
        bool? changedState = null;

        manager.ModStateChanged += (mod, enabled) =>
        {
            changedMod = mod;
            changedState = enabled;
        };

        manager.SetModEnabled("CustomMod", true);
        Assert.Equal("CustomMod", changedMod);
        Assert.True(changedState);

        manager.SetModEnabled("CustomMod", false);
        Assert.Equal("CustomMod", changedMod);
        Assert.False(changedState);
    }

    [Fact]
    public void FeatureRegistration_BlocksDisablingWhenInUse()
    {
        var manager = ModManager.Instance;
        manager.AdvancedWardenObjective = true;

        bool featureIsActive = true;
        manager.RegisterFeature(ModManager.AdvancedWardenObjectiveModId, "Custom Objective Types", () => featureIsActive);

        Assert.True(manager.IsModInUse(ModManager.AdvancedWardenObjectiveModId));
        Assert.False(manager.CanDisableMod(ModManager.AdvancedWardenObjectiveModId));

        bool trySuccess = manager.TrySetModEnabled(ModManager.AdvancedWardenObjectiveModId, false, out string? error);
        Assert.False(trySuccess);
        Assert.NotNull(error);
        Assert.Contains("Custom Objective Types", error);
        Assert.True(manager.AdvancedWardenObjective); // Still true

        Assert.Throws<InvalidOperationException>(() =>
        {
            manager.AdvancedWardenObjective = false;
        });

        // Deactivate feature usage
        featureIsActive = false;
        Assert.False(manager.IsModInUse(ModManager.AdvancedWardenObjectiveModId));
        Assert.True(manager.CanDisableMod(ModManager.AdvancedWardenObjectiveModId));

        bool trySuccessAfter = manager.TrySetModEnabled(ModManager.AdvancedWardenObjectiveModId, false, out string? errorAfter);
        Assert.True(trySuccessAfter);
        Assert.Null(errorAfter);
        Assert.False(manager.AdvancedWardenObjective);
    }

    [Fact]
    public void ModdedViewModel_ReflectsAndGuardsModState()
    {
        var manager = ModManager.Instance;
        manager.Reset();
        manager.ClearAllFeatureRegistrations();

        var vm = new ModdedViewModel();
        Assert.False(vm.AdvancedWardenObjective);
        Assert.False(vm.HasErrorMessage);

        vm.AdvancedWardenObjective = true;
        Assert.True(vm.AdvancedWardenObjective);
        Assert.True(manager.AdvancedWardenObjective);

        bool inUse = true;
        manager.RegisterFeature(ModManager.AdvancedWardenObjectiveModId, "AWO Timers", () => inUse);

        Assert.True(vm.IsAdvancedWardenObjectiveInUse);
        Assert.False(vm.CanDisableAdvancedWardenObjective);

        // Try disabling via VM
        vm.AdvancedWardenObjective = false;
        Assert.True(vm.AdvancedWardenObjective); // Should stay true
        Assert.True(vm.HasErrorMessage);
        Assert.Contains("AWO Timers", vm.ErrorMessage);

        // Turn off in-use flag and disable
        inUse = false;
        vm.AdvancedWardenObjective = false;
        Assert.False(vm.AdvancedWardenObjective);
        Assert.False(vm.HasErrorMessage);
    }

    [Fact]
    public void ExportAndLoad_RoundTripsCorrectly()
    {
        var manager = ModManager.Instance;
        manager.Reset();
        manager.AdvancedWardenObjective = true;
        manager.SetModEnabled("ExtraMod1", true);

        var data = manager.ExportData();
        Assert.True(data.AdvancedWardenObjective);
        Assert.Contains(ModManager.AdvancedWardenObjectiveModId, data.EnabledMods);
        Assert.Contains("ExtraMod1", data.EnabledMods);

        manager.Reset();
        Assert.False(manager.AdvancedWardenObjective);
        Assert.False(manager.IsModEnabled("ExtraMod1"));

        manager.LoadFrom(data);
        Assert.True(manager.AdvancedWardenObjective);
        Assert.True(manager.IsModEnabled("ExtraMod1"));
    }

    [Fact]
    public void RundownLoader_SavesAndLoadsModdedSettings()
    {
        var vm = new MainWindowViewModel();
        vm.Modded.Reset();
        vm.Modded.AdvancedWardenObjective = true;

        var loader = new RundownLoader();
        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_{Guid.NewGuid():N}.json");

        try
        {
            loader.SaveProject(vm, tempFile);
            Assert.True(File.Exists(tempFile));

            var loadedProject = loader.LoadProject(tempFile);
            Assert.NotNull(loadedProject.ModdedSettings);
            Assert.True(loadedProject.ModdedSettings.AdvancedWardenObjective);

            var newVm = new MainWindowViewModel();
            newVm.Modded.Reset();
            Assert.False(newVm.Modded.AdvancedWardenObjective);

            loader.ApplyProject(newVm, loadedProject);
            Assert.True(newVm.Modded.AdvancedWardenObjective);
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
