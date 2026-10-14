using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ActivateSmallHsuTests
{
    [Fact]
    public void ObjectiveType_ActivateSmallHSU_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "ActivateSmallHSU"));
    }

    [Fact]
    public void SectorSettings_ActivateSmallHsu_DefaultsAndCloning()
    {
        var settings = new SectorSettings();

        Assert.Equal(0, settings.ActivateSmallHsuZone);
        Assert.Equal(RetrieveBigItemType.PowerCell, settings.ItemFromStart);
        Assert.Equal(RetrieveBigItemType.PowerCell, settings.ItemAfterActivation);
        Assert.False(settings.BringItemInElevator);
        Assert.False(settings.StopEnemyWavesOnActivation);
        Assert.False(settings.ObjectiveCompleteAfterInsertion);
        Assert.False(settings.RequireItemAfterActivationInExitScan);

        settings.ActivateSmallHsuZone = 3;
        settings.ItemFromStart = RetrieveBigItemType.FogTurbine;
        settings.ItemAfterActivation = RetrieveBigItemType.DataSphere;
        settings.BringItemInElevator = true;
        settings.StopEnemyWavesOnActivation = true;
        settings.ObjectiveCompleteAfterInsertion = true;
        settings.RequireItemAfterActivationInExitScan = true;

        var clone = settings.Clone();

        Assert.Equal(3, clone.ActivateSmallHsuZone);
        Assert.Equal(RetrieveBigItemType.FogTurbine, clone.ItemFromStart);
        Assert.Equal(RetrieveBigItemType.DataSphere, clone.ItemAfterActivation);
        Assert.True(clone.BringItemInElevator);
        Assert.True(clone.StopEnemyWavesOnActivation);
        Assert.True(clone.ObjectiveCompleteAfterInsertion);
        Assert.True(clone.RequireItemAfterActivationInExitScan);
    }

    [Fact]
    public void ViewModel_ActivateSmallHsu_VisibilityAndBindings()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.ActivateSmallHSU;
        Assert.True(layout.IsActivateSmallHsuObjective);
        Assert.False(layout.IsSurvivalObjective);

        var zoneOption = layout.AvailableObjectiveZones.FirstOrDefault();
        if (zoneOption != null)
        {
            layout.SelectedActivateSmallHsuZoneOption = zoneOption;
            Assert.Equal(zoneOption.ZoneIndex, layout.EditingSectorSettings.ActivateSmallHsuZone);
        }

        var startItem = layout.AvailableRetrieveBigItemOptions.FirstOrDefault(o => o.Type == RetrieveBigItemType.CargoCrate);
        if (startItem != null)
        {
            layout.SelectedItemFromStartOption = startItem;
            Assert.Equal(RetrieveBigItemType.CargoCrate, layout.EditingSectorSettings.ItemFromStart);
        }

        var afterItem = layout.AvailableRetrieveBigItemOptions.FirstOrDefault(o => o.Type == RetrieveBigItemType.SedativesCase);
        if (afterItem != null)
        {
            layout.SelectedItemAfterActivationOption = afterItem;
            Assert.Equal(RetrieveBigItemType.SedativesCase, layout.EditingSectorSettings.ItemAfterActivation);
        }
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_ActivateSmallHsu_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_activatesmallhsu_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.ActivateSmallHSU;
            mainSector.ActivateSmallHsuZone = 2;
            mainSector.ItemFromStart = RetrieveBigItemType.CryoHardcase;
            mainSector.ItemAfterActivation = RetrieveBigItemType.PortalKey;
            mainSector.BringItemInElevator = true;
            mainSector.StopEnemyWavesOnActivation = true;
            mainSector.ObjectiveCompleteAfterInsertion = true;
            mainSector.RequireItemAfterActivationInExitScan = true;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.ActivateSmallHSU, sector.ObjectiveType);
            Assert.Equal(2, sector.ActivateSmallHsuZone);
            Assert.Equal(RetrieveBigItemType.CryoHardcase, sector.ItemFromStart);
            Assert.Equal(RetrieveBigItemType.PortalKey, sector.ItemAfterActivation);
            Assert.True(sector.BringItemInElevator);
            Assert.True(sector.StopEnemyWavesOnActivation);
            Assert.True(sector.ObjectiveCompleteAfterInsertion);
            Assert.True(sector.RequireItemAfterActivationInExitScan);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.ActivateSmallHSU, applied.ObjectiveType);
            Assert.Equal(2, applied.ActivateSmallHsuZone);
            Assert.Equal(RetrieveBigItemType.CryoHardcase, applied.ItemFromStart);
            Assert.Equal(RetrieveBigItemType.PortalKey, applied.ItemAfterActivation);
            Assert.True(applied.BringItemInElevator);
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
