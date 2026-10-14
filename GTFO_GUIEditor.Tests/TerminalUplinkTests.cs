using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class TerminalUplinkTests
{
    [Fact]
    public void ObjectiveType_TerminalUplink_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "TerminalUplink"));
    }

    [Fact]
    public void SectorSettings_TerminalUplink_DefaultAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(1, settings.UplinkNumberOfTerminals);
        Assert.Equal(1, settings.UplinkNumberOfVerificationRounds);
        Assert.Single(settings.TerminalUplinkPlacements);

        settings.ObjectiveType = ObjectiveType.TerminalUplink;
        settings.UplinkNumberOfTerminals = 2;
        settings.UplinkNumberOfVerificationRounds = 4;
        settings.TerminalUplinkPlacements[0].DimensionIndex = "Reality";
        settings.TerminalUplinkPlacements[0].Zone = 1;
        settings.TerminalUplinkPlacements[1].DimensionIndex = "Dimension_1";
        settings.TerminalUplinkPlacements[1].Zone = 2;

        var clone = settings.Clone();
        Assert.Equal(ObjectiveType.TerminalUplink, clone.ObjectiveType);
        Assert.Equal(2, clone.UplinkNumberOfTerminals);
        Assert.Equal(4, clone.UplinkNumberOfVerificationRounds);
        Assert.Equal(2, clone.TerminalUplinkPlacements.Count);
        Assert.Equal("Dimension_1", clone.TerminalUplinkPlacements[1].DimensionIndex);
        Assert.Equal(2, clone.TerminalUplinkPlacements[1].Zone);
    }

    [Fact]
    public void ViewModel_TerminalUplink_VisibilityAndPlacementManagement()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.TerminalUplink;
        Assert.True(layout.IsTerminalUplinkObjective);

        layout.UplinkNumberOfTerminals = 3;
        Assert.Equal(3, layout.EditingSectorSettings.TerminalUplinkPlacements.Count);

        layout.UplinkNumberOfVerificationRounds = 5;
        Assert.Equal(5, layout.EditingSectorSettings.UplinkNumberOfVerificationRounds);

        layout.SelectedTerminalUplinkPlacement = layout.EditingSectorSettings.TerminalUplinkPlacements[0];
        Assert.True(layout.HasSelectedTerminalUplinkPlacement);

        layout.AddTerminalUplinkCandidateZone();
        Assert.Equal(2, layout.SelectedTerminalUplinkPlacement.CandidateZones.Count);

        layout.RemoveTerminalUplinkPlacement();
        Assert.Equal(2, layout.EditingSectorSettings.TerminalUplinkPlacements.Count);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_TerminalUplink_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_uplink_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.TerminalUplink;
            mainSector.UplinkNumberOfTerminals = 2;
            mainSector.UplinkNumberOfVerificationRounds = 3;
            mainSector.TerminalUplinkPlacements[0].CandidateZones[0].DimensionIndex = "Reality";
            mainSector.TerminalUplinkPlacements[0].CandidateZones[0].Zone = 1;
            mainSector.TerminalUplinkPlacements[0].CandidateZones[0].PlacementStartWeight = 0.5;

            mainSector.TerminalUplinkPlacements[1].CandidateZones[0].DimensionIndex = "Dimension_1";
            mainSector.TerminalUplinkPlacements[1].CandidateZones[0].Zone = 2;
            mainSector.TerminalUplinkPlacements[1].CandidateZones[0].PlacementMiddleWeight = 0.8;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.TerminalUplink, sector.ObjectiveType);
            Assert.Equal(2, sector.TerminalUplinkPlacements?.Count);
            Assert.Equal(3, sector.UplinkNumberOfVerificationRounds);
            Assert.Equal(2, sector.TerminalUplinkPlacements?.Count);
            Assert.Equal("Dimension_1", sector.TerminalUplinkPlacements?[1].CandidateZones[0].DimensionIndex);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.TerminalUplink, applied.ObjectiveType);
            Assert.Equal(2, applied.UplinkNumberOfTerminals);
            Assert.Equal(3, applied.UplinkNumberOfVerificationRounds);
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
