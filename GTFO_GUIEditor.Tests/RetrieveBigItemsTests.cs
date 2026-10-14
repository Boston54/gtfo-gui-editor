using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class RetrieveBigItemsTests
{
    [Fact]
    public void RetrieveBigItemType_HasCorrectInternalIntegerValues()
    {
        Assert.Equal(131, (int)RetrieveBigItemType.PowerCell);
        Assert.Equal(133, (int)RetrieveBigItemType.FogTurbine);
        Assert.Equal(137, (int)RetrieveBigItemType.HSUNeonateStage1);
        Assert.Equal(141, (int)RetrieveBigItemType.HSUNeonateStage2);
        Assert.Equal(143, (int)RetrieveBigItemType.HSUNeonateStage3);
        Assert.Equal(145, (int)RetrieveBigItemType.HSUNeonateStage4);
        Assert.Equal(170, (int)RetrieveBigItemType.HSUNeonateStage5);
        Assert.Equal(164, (int)RetrieveBigItemType.MWP);
        Assert.Equal(151, (int)RetrieveBigItemType.DataSphere);
        Assert.Equal(166, (int)RetrieveBigItemType.PortalKey);
        Assert.Equal(138, (int)RetrieveBigItemType.CargoCrate);
        Assert.Equal(148, (int)RetrieveBigItemType.CryoHardcase);
        Assert.Equal(154, (int)RetrieveBigItemType.CargoCrateHighSecurity);
        Assert.Equal(155, (int)RetrieveBigItemType.CargoCrateHighSecurityOpen);
        Assert.Equal(173, (int)RetrieveBigItemType.SedativesCase);
    }

    [Fact]
    public void SectorSettings_RetrieveBigItems_DefaultAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(RetrieveBigItemType.PowerCell, settings.RetrieveItem);
        Assert.Single(settings.RetrieveItemPlacements);
        Assert.Equal(1, settings.RetrieveSpawnCount);

        settings.ObjectiveType = ObjectiveType.RetrieveBigItems;
        settings.RetrieveItem = RetrieveBigItemType.FogTurbine;
        settings.RetrieveSpawnCount = 2;
        settings.RetrieveMaxPerZone = 2;

        settings.RetrieveItemPlacements[0].DimensionIndex = "Reality";
        settings.RetrieveItemPlacements[0].Zone = 1;
        settings.RetrieveItemPlacements[0].PlacementStartWeight = 0.5;
        settings.RetrieveItemPlacements[1].DimensionIndex = "Dimension_1";
        settings.RetrieveItemPlacements[1].Zone = 2;
        settings.RetrieveItemPlacements[1].PlacementMiddleWeight = 0.8;

        var clone = settings.Clone();
        Assert.Equal(ObjectiveType.RetrieveBigItems, clone.ObjectiveType);
        Assert.Equal(RetrieveBigItemType.FogTurbine, clone.RetrieveItem);
        Assert.Equal(2, clone.RetrieveSpawnCount);
        Assert.Equal(2, clone.RetrieveMaxPerZone);
        Assert.Equal(2, clone.RetrieveItemPlacements.Count);
        Assert.Equal("Dimension_1", clone.RetrieveItemPlacements[1].DimensionIndex);
        Assert.Equal(2, clone.RetrieveItemPlacements[1].Zone);
        Assert.Equal(0.8, clone.RetrieveItemPlacements[1].PlacementMiddleWeight);
    }

    [Fact]
    public void ViewModel_RetrieveBigItems_AddRemovePlacementAndCandidates()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.RetrieveBigItems;
        Assert.True(layout.IsRetrieveBigItemsObjective);

        layout.SelectedRetrieveItem = RetrieveBigItemType.CargoCrate;
        Assert.Equal(RetrieveBigItemType.CargoCrate, layout.EditingSectorSettings.RetrieveItem);

        layout.RetrieveSpawnCount = 3;
        Assert.Equal(3, layout.EditingSectorSettings.RetrieveItemPlacements.Count);

        layout.SelectedRetrieveItemPlacement = layout.EditingSectorSettings.RetrieveItemPlacements[1];
        Assert.True(layout.HasSelectedRetrieveItemPlacement);

        layout.AddRetrieveCandidateZone();
        Assert.Equal(2, layout.SelectedRetrieveItemPlacement.CandidateZones.Count);

        layout.RemoveRetrieveItemPlacement();
        Assert.Equal(2, layout.EditingSectorSettings.RetrieveItemPlacements.Count);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_RetrieveBigItems_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_retrievebig_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.RetrieveBigItems;
            mainSector.RetrieveItem = RetrieveBigItemType.SedativesCase;
            mainSector.RetrieveMaxPerZone = 3;
            mainSector.RetrieveSpawnCount = 2;

            mainSector.RetrieveItemPlacements[0].CandidateZones[0].DimensionIndex = "Reality";
            mainSector.RetrieveItemPlacements[0].CandidateZones[0].Zone = 0;
            mainSector.RetrieveItemPlacements[0].CandidateZones[0].PlacementStartWeight = 0.5;

            mainSector.RetrieveItemPlacements[1].CandidateZones[0].DimensionIndex = "Dimension_1";
            mainSector.RetrieveItemPlacements[1].CandidateZones[0].Zone = 1;
            mainSector.RetrieveItemPlacements[1].CandidateZones[0].PlacementEndWeight = 1.0;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.RetrieveBigItems, sector.ObjectiveType);
            Assert.Equal(RetrieveBigItemType.SedativesCase, sector.RetrieveItem);
            Assert.Equal(3, sector.RetrieveMaxPerZone);
            Assert.Equal(2, sector.RetrieveItemPlacements?.Count);
            Assert.Equal(2, sector.RetrieveItemPlacements?.Count);
            Assert.Equal("Dimension_1", sector.RetrieveItemPlacements?[1].CandidateZones[0].DimensionIndex);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.RetrieveBigItems, applied.ObjectiveType);
            Assert.Equal(RetrieveBigItemType.SedativesCase, applied.RetrieveItem);
            Assert.Equal(3, applied.RetrieveMaxPerZone);
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
