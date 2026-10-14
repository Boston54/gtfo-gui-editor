using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class TimedTerminalSequenceTests
{
    [Fact]
    public void ObjectiveType_TimedTerminalSequence_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "TimedTerminalSequence"));
    }

    [Fact]
    public void SectorSettings_TimedTerminalSequence_DefaultsAndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal("Reality", settings.TimedTerminalSequenceMainLocation.DimensionIndex);
        Assert.Equal(0, settings.TimedTerminalSequenceMainLocation.Zone);
        Assert.Equal(1, settings.TimedTerminalSequenceNumberOfRounds);
        Assert.Equal(0.0, settings.TimedTerminalSequenceTimePerRound);
        Assert.Equal(0.0, settings.TimedTerminalSequenceTimeForConfirmation);
        Assert.Equal("MainTerminal", settings.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter);
        Assert.Equal(1, settings.TimedTerminalSequenceNumberOfTerminals);
        Assert.Single(settings.TimedTerminalSequencePlacements);

        settings.ObjectiveType = ObjectiveType.TimedTerminalSequence;
        settings.TimedTerminalSequenceMainLocation.DimensionIndex = "Dimension_1";
        settings.TimedTerminalSequenceMainLocation.Zone = 3;
        settings.TimedTerminalSequenceMainLocation.PlacementStartWeight = 0.2;
        settings.TimedTerminalSequenceMainLocation.PlacementMiddleWeight = 0.5;
        settings.TimedTerminalSequenceMainLocation.PlacementEndWeight = 0.8;
        settings.TimedTerminalSequenceNumberOfRounds = 4;
        settings.TimedTerminalSequenceTimePerRound = 60.0;
        settings.TimedTerminalSequenceTimeForConfirmation = 15.0;
        settings.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = "PrimaryTerminal";
        settings.TimedTerminalSequenceNumberOfTerminals = 2;

        settings.TimedTerminalSequencePlacements[0].DimensionIndex = "Reality";
        settings.TimedTerminalSequencePlacements[0].Zone = 1;
        settings.TimedTerminalSequencePlacements[1].DimensionIndex = "Dimension_1";
        settings.TimedTerminalSequencePlacements[1].Zone = 2;

        var clone = settings.Clone();

        Assert.Equal(ObjectiveType.TimedTerminalSequence, clone.ObjectiveType);
        Assert.Equal("Dimension_1", clone.TimedTerminalSequenceMainLocation.DimensionIndex);
        Assert.Equal(3, clone.TimedTerminalSequenceMainLocation.Zone);
        Assert.Equal(0.2, clone.TimedTerminalSequenceMainLocation.PlacementStartWeight);
        Assert.Equal(0.5, clone.TimedTerminalSequenceMainLocation.PlacementMiddleWeight);
        Assert.Equal(0.8, clone.TimedTerminalSequenceMainLocation.PlacementEndWeight);
        Assert.Equal(4, clone.TimedTerminalSequenceNumberOfRounds);
        Assert.Equal(60.0, clone.TimedTerminalSequenceTimePerRound);
        Assert.Equal(15.0, clone.TimedTerminalSequenceTimeForConfirmation);
        Assert.Equal("PrimaryTerminal", clone.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter);
        Assert.Equal(2, clone.TimedTerminalSequenceNumberOfTerminals);
        Assert.Equal(2, clone.TimedTerminalSequencePlacements.Count);
        Assert.Equal("Dimension_1", clone.TimedTerminalSequencePlacements[1].DimensionIndex);
        Assert.Equal(2, clone.TimedTerminalSequencePlacements[1].Zone);
    }

    [Fact]
    public void ViewModel_TimedTerminalSequence_VisibilityAndPlacementManagement()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        Assert.NotNull(layout.EditingSectorSettings);

        layout.SelectedSectorObjectiveType = ObjectiveType.TimedTerminalSequence;
        Assert.True(layout.IsTimedTerminalSequenceObjective);
        Assert.False(layout.IsGatherTerminalObjective);

        var dimOption = layout.AvailableDimensions.FirstOrDefault(d => d.DimensionIndex == "Reality");
        if (dimOption != null)
        {
            layout.SelectedTimedTerminalSequenceMainDimensionOption = dimOption;
            Assert.Equal("Reality", layout.EditingSectorSettings.TimedTerminalSequenceMainLocation.DimensionIndex);
        }

        var zoneOption = layout.AvailableObjectiveZones.FirstOrDefault();
        if (zoneOption != null)
        {
            layout.SelectedTimedTerminalSequenceMainZoneOption = zoneOption;
            Assert.Equal(zoneOption.ZoneIndex, layout.EditingSectorSettings.TimedTerminalSequenceMainLocation.Zone);
        }

        layout.TimedTerminalSequenceNumberOfTerminals = 3;
        Assert.Equal(3, layout.EditingSectorSettings.TimedTerminalSequencePlacements.Count);

        layout.EditingSectorSettings.TimedTerminalSequenceNumberOfRounds = 3;
        layout.EditingSectorSettings.TimedTerminalSequenceTimePerRound = 45.0;
        layout.EditingSectorSettings.TimedTerminalSequenceTimeForConfirmation = 10.0;
        layout.EditingSectorSettings.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = "SeqTerm";

        layout.SelectedTimedTerminalSequencePlacement = layout.EditingSectorSettings.TimedTerminalSequencePlacements[0];
        Assert.True(layout.HasSelectedTimedTerminalSequencePlacement);

        layout.AddTimedTerminalSequenceCandidateZone();
        Assert.Equal(2, layout.SelectedTimedTerminalSequencePlacement.CandidateZones.Count);

        layout.AddTimedTerminalSequencePlacement();
        Assert.Equal(4, layout.EditingSectorSettings.TimedTerminalSequencePlacements.Count);
        Assert.Equal(4, layout.TimedTerminalSequenceNumberOfTerminals);

        layout.RemoveTimedTerminalSequencePlacement();
        Assert.Equal(3, layout.EditingSectorSettings.TimedTerminalSequencePlacements.Count);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_TimedTerminalSequence_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_timedsequence_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.TimedTerminalSequence;
            mainSector.TimedTerminalSequenceMainLocation.DimensionIndex = "Dimension_1";
            mainSector.TimedTerminalSequenceMainLocation.Zone = 2;
            mainSector.TimedTerminalSequenceMainLocation.PlacementStartWeight = 0.1;
            mainSector.TimedTerminalSequenceMainLocation.PlacementMiddleWeight = 0.5;
            mainSector.TimedTerminalSequenceMainLocation.PlacementEndWeight = 0.9;

            mainSector.TimedTerminalSequenceNumberOfRounds = 3;
            mainSector.TimedTerminalSequenceTimePerRound = 90.0;
            mainSector.TimedTerminalSequenceTimeForConfirmation = 20.0;
            mainSector.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = "SequenceMaster";
            mainSector.TimedTerminalSequenceNumberOfTerminals = 2;

            mainSector.TimedTerminalSequencePlacements[0].CandidateZones[0].DimensionIndex = "Reality";
            mainSector.TimedTerminalSequencePlacements[0].CandidateZones[0].Zone = 1;
            mainSector.TimedTerminalSequencePlacements[0].CandidateZones[0].PlacementStartWeight = 0.5;

            mainSector.TimedTerminalSequencePlacements[1].CandidateZones[0].DimensionIndex = "Dimension_1";
            mainSector.TimedTerminalSequencePlacements[1].CandidateZones[0].Zone = 3;
            mainSector.TimedTerminalSequencePlacements[1].CandidateZones[0].PlacementEndWeight = 1.0;

            RundownProjectService.SaveProject(vm, tempFile);
            var loaded = RundownProjectService.LoadProject(tempFile);

            var sector = loaded.Levels?.FirstOrDefault()?.MainSector;
            Assert.NotNull(sector);

            Assert.Equal(ObjectiveType.TimedTerminalSequence, sector.ObjectiveType);
            Assert.NotNull(sector.TimedTerminalSequenceMainLocation);
            Assert.Equal("Dimension_1", sector.TimedTerminalSequenceMainLocation.DimensionIndex);
            Assert.Equal(2, sector.TimedTerminalSequenceMainLocation.Zone);
            Assert.Equal(0.1, sector.TimedTerminalSequenceMainLocation.PlacementStartWeight);
            Assert.Equal(0.5, sector.TimedTerminalSequenceMainLocation.PlacementMiddleWeight);
            Assert.Equal(0.9, sector.TimedTerminalSequenceMainLocation.PlacementEndWeight);
            Assert.Equal(3, sector.TimedTerminalSequenceNumberOfRounds);
            Assert.Equal(90.0, sector.TimedTerminalSequenceTimePerRound);
            Assert.Equal(20.0, sector.TimedTerminalSequenceTimeForConfirmation);
            Assert.Equal("SequenceMaster", sector.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter);
            Assert.Equal(2, sector.TimedTerminalSequenceNumberOfTerminals);
            Assert.Equal(2, sector.TimedTerminalSequencePlacements?.Count);
            Assert.Equal("Dimension_1", sector.TimedTerminalSequencePlacements?[1].CandidateZones[0].DimensionIndex);

            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loaded, tempFile);

            var applied = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(applied);
            Assert.Equal(ObjectiveType.TimedTerminalSequence, applied.ObjectiveType);
            Assert.Equal(3, applied.TimedTerminalSequenceNumberOfRounds);
            Assert.Equal(2, applied.TimedTerminalSequenceNumberOfTerminals);
            Assert.Equal("SequenceMaster", applied.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter);
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
