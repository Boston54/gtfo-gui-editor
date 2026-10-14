using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class GatherTerminalTests
{
    [Fact]
    public void ObjectiveType_GatherTerminal_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "GatherTerminal"));
    }

    [Fact]
    public void SectorSettings_GatherTerminal_DefaultsAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(1, settings.GatherTerminalSpawnCount);
        Assert.Equal(1, settings.GatherTerminalRequiredCount);
        Assert.Equal(string.Empty, settings.GatherTerminalCommand);
        Assert.Equal(string.Empty, settings.GatherTerminalCommandHelp);
        Assert.Equal(string.Empty, settings.GatherTerminalDownloadingText);
        Assert.Equal(string.Empty, settings.GatherTerminalDownloadCompleteText);
        Assert.Equal(0.0, settings.GatherTerminalDownloadTime);
        Assert.Single(settings.GatherTerminalPlacements);

        settings.ObjectiveType = ObjectiveType.GatherTerminal;
        settings.GatherTerminalSpawnCount = 3;
        settings.GatherTerminalRequiredCount = 2;
        settings.GatherTerminalCommand = "DOWNLOAD_DATA";
        settings.GatherTerminalCommandHelp = "Download security files";
        settings.GatherTerminalDownloadingText = "DOWNLOADING...";
        settings.GatherTerminalDownloadCompleteText = "DOWNLOAD COMPLETE";
        settings.GatherTerminalDownloadTime = 25.5;

        settings.GatherTerminalPlacements[0].DimensionIndex = "Reality";
        settings.GatherTerminalPlacements[0].Zone = 1;
        settings.GatherTerminalPlacements[1].DimensionIndex = "Dimension_1";
        settings.GatherTerminalPlacements[1].Zone = 2;
        settings.GatherTerminalPlacements[2].DimensionIndex = "Reality";
        settings.GatherTerminalPlacements[2].Zone = 3;

        var clone = settings.Clone();

        Assert.Equal(ObjectiveType.GatherTerminal, clone.ObjectiveType);
        Assert.Equal(3, clone.GatherTerminalSpawnCount);
        Assert.Equal(2, clone.GatherTerminalRequiredCount);
        Assert.Equal("DOWNLOAD_DATA", clone.GatherTerminalCommand);
        Assert.Equal("Download security files", clone.GatherTerminalCommandHelp);
        Assert.Equal("DOWNLOADING...", clone.GatherTerminalDownloadingText);
        Assert.Equal("DOWNLOAD COMPLETE", clone.GatherTerminalDownloadCompleteText);
        Assert.Equal(25.5, clone.GatherTerminalDownloadTime);
        Assert.Equal(3, clone.GatherTerminalPlacements.Count);
        Assert.Equal("Dimension_1", clone.GatherTerminalPlacements[1].DimensionIndex);
        Assert.Equal(2, clone.GatherTerminalPlacements[1].Zone);
    }

    [Fact]
    public void ViewModel_GatherTerminal_VisibilityAndPlacementManagement()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.GatherTerminal;
        Assert.True(layout.IsGatherTerminalObjective);
        Assert.False(layout.IsTimedTerminalSequenceObjective);

        layout.GatherTerminalSpawnCount = 3;
        Assert.Equal(3, layout.EditingSectorSettings.GatherTerminalPlacements.Count);

        layout.EditingSectorSettings.GatherTerminalRequiredCount = 2;
        layout.EditingSectorSettings.GatherTerminalCommand = "READ_LOGS";
        layout.EditingSectorSettings.GatherTerminalDownloadTime = 15.0;

        layout.SelectedGatherTerminalPlacement = layout.EditingSectorSettings.GatherTerminalPlacements[0];
        Assert.True(layout.HasSelectedGatherTerminalPlacement);

        layout.AddGatherTerminalCandidateZone();
        Assert.Equal(2, layout.SelectedGatherTerminalPlacement.CandidateZones.Count);

        layout.AddGatherTerminalPlacement();
        Assert.Equal(4, layout.EditingSectorSettings.GatherTerminalPlacements.Count);
        Assert.Equal(4, layout.GatherTerminalSpawnCount);

        layout.RemoveGatherTerminalPlacement();
        Assert.Equal(3, layout.EditingSectorSettings.GatherTerminalPlacements.Count);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_GatherTerminal_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_gatherterminal_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.GatherTerminal;
            mainSector.GatherTerminalSpawnCount = 2;
            mainSector.GatherTerminalRequiredCount = 1;
            mainSector.GatherTerminalCommand = "EXTRACT_KEYS";
            mainSector.GatherTerminalCommandHelp = "Extract cryptographic keys";
            mainSector.GatherTerminalDownloadingText = "DECRYPTING...";
            mainSector.GatherTerminalDownloadCompleteText = "KEYS ACQUIRED";
            mainSector.GatherTerminalDownloadTime = 30.0;

            mainSector.GatherTerminalPlacements[0].CandidateZones[0].DimensionIndex = "Reality";
            mainSector.GatherTerminalPlacements[0].CandidateZones[0].Zone = 1;
            mainSector.GatherTerminalPlacements[0].CandidateZones[0].PlacementStartWeight = 0.5;

            mainSector.GatherTerminalPlacements[1].CandidateZones[0].DimensionIndex = "Dimension_1";
            mainSector.GatherTerminalPlacements[1].CandidateZones[0].Zone = 2;
            mainSector.GatherTerminalPlacements[1].CandidateZones[0].PlacementEndWeight = 1.0;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.GatherTerminal, sector.ObjectiveType);
            Assert.Equal(2, sector.GatherTerminalSpawnCount);
            Assert.Equal(1, sector.GatherTerminalRequiredCount);
            Assert.Equal("EXTRACT_KEYS", sector.GatherTerminalCommand);
            Assert.Equal("Extract cryptographic keys", sector.GatherTerminalCommandHelp);
            Assert.Equal("DECRYPTING...", sector.GatherTerminalDownloadingText);
            Assert.Equal("KEYS ACQUIRED", sector.GatherTerminalDownloadCompleteText);
            Assert.Equal(30.0, sector.GatherTerminalDownloadTime);
            Assert.Equal(2, sector.GatherTerminalPlacements?.Count);
            Assert.Equal("Dimension_1", sector.GatherTerminalPlacements?[1].CandidateZones[0].DimensionIndex);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.GatherTerminal, applied.ObjectiveType);
            Assert.Equal(2, applied.GatherTerminalSpawnCount);
            Assert.Equal(1, applied.GatherTerminalRequiredCount);
            Assert.Equal("EXTRACT_KEYS", applied.GatherTerminalCommand);
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
