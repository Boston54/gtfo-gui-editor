using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class GatherSmallItemsTests
{
    [Fact]
    public void GatherSmallItemType_HasCorrectInternalIntegerValues()
    {
        Assert.Equal(128, (int)GatherSmallItemType.PersonnelID);
        Assert.Equal(129, (int)GatherSmallItemType.PartialDecoder);
        Assert.Equal(147, (int)GatherSmallItemType.HardDrive);
        Assert.Equal(183, (int)GatherSmallItemType.Microdrive);
        Assert.Equal(149, (int)GatherSmallItemType.GLP_1);
        Assert.Equal(150, (int)GatherSmallItemType.OSIP);
        Assert.Equal(169, (int)GatherSmallItemType.GLP_2);
        Assert.Equal(168, (int)GatherSmallItemType.DataCubeTampered);
        Assert.Equal(165, (int)GatherSmallItemType.DataCube);
        Assert.Equal(179, (int)GatherSmallItemType.DataCubes);
        Assert.Equal(153, (int)GatherSmallItemType.PlantSample);
        Assert.Equal(171, (int)GatherSmallItemType.MemoryStick);
    }

    [Fact]
    public void SectorSettings_GatherSmallItems_DefaultValues_AndCloning()
    {
        var settings = new SectorSettings();
        Assert.Equal(GatherSmallItemType.PersonnelID, settings.GatherItem);
        Assert.Single(settings.GatherItemPlacements);
        Assert.Equal(1, settings.GatherSpawnCount);
        Assert.Equal("Reality", settings.GatherItemPlacements[0].DimensionIndex);
        Assert.Equal(0, settings.GatherItemPlacements[0].Zone);
        Assert.Equal(0.0, settings.GatherItemPlacements[0].PlacementStartWeight);
        Assert.Equal(0.0, settings.GatherItemPlacements[0].PlacementMiddleWeight);
        Assert.Equal(0.0, settings.GatherItemPlacements[0].PlacementEndWeight);

        settings.ObjectiveType = ObjectiveType.GatherSmallItems;
        settings.GatherItem = GatherSmallItemType.GLP_2;
        settings.GatherSpawnCount = 3;
        Assert.Equal(3, settings.GatherItemPlacements.Count);
        Assert.Equal(3, settings.GatherSpawnCount);

        settings.GatherItemPlacements[0].DimensionIndex = "Reality";
        settings.GatherItemPlacements[0].Zone = 0;
        settings.GatherItemPlacements[0].PlacementStartWeight = 1.0;

        settings.GatherItemPlacements[1].DimensionIndex = "Dimension_1";
        settings.GatherItemPlacements[1].Zone = 1;
        settings.GatherItemPlacements[1].PlacementMiddleWeight = 0.5;

        settings.GatherItemPlacements[2].DimensionIndex = "Dimension_2";
        settings.GatherItemPlacements[2].Zone = 2;
        settings.GatherItemPlacements[2].PlacementEndWeight = 0.8;

        var cloned = settings.Clone();
        Assert.Equal(ObjectiveType.GatherSmallItems, cloned.ObjectiveType);
        Assert.Equal(GatherSmallItemType.GLP_2, cloned.GatherItem);
        Assert.Equal(3, cloned.GatherItemPlacements.Count);
        Assert.Equal(3, cloned.GatherSpawnCount);

        Assert.Equal("Reality", cloned.GatherItemPlacements[0].DimensionIndex);
        Assert.Equal(0, cloned.GatherItemPlacements[0].Zone);
        Assert.Equal(1.0, cloned.GatherItemPlacements[0].PlacementStartWeight);

        Assert.Equal("Dimension_1", cloned.GatherItemPlacements[1].DimensionIndex);
        Assert.Equal(1, cloned.GatherItemPlacements[1].Zone);
        Assert.Equal(0.5, cloned.GatherItemPlacements[1].PlacementMiddleWeight);

        Assert.Equal("Dimension_2", cloned.GatherItemPlacements[2].DimensionIndex);
        Assert.Equal(2, cloned.GatherItemPlacements[2].Zone);
        Assert.Equal(0.8, cloned.GatherItemPlacements[2].PlacementEndWeight);

        // Verify independent instance
        cloned.GatherItem = GatherSmallItemType.HardDrive;
        cloned.GatherItemPlacements[0].PlacementStartWeight = 0.2;
        Assert.Equal(GatherSmallItemType.GLP_2, settings.GatherItem);
        Assert.Equal(1.0, settings.GatherItemPlacements[0].PlacementStartWeight);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_ObjectiveVisibilityFlags()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();

        Assert.NotNull(vm.EditingSectorSettings);
        Assert.True(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);
        Assert.False(vm.IsGatherSmallItemsObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;
        Assert.False(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);
        Assert.False(vm.IsReactorShutdownObjective);
        Assert.True(vm.IsGatherSmallItemsObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Shutdown;
        Assert.False(vm.IsGatherSmallItemsObjective);
        Assert.True(vm.IsReactorShutdownObjective);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_ItemAndSpawnCountManagement()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;

        Assert.Equal(12, vm.AvailableGatherSmallItemOptions.Count);
        Assert.Equal(GatherSmallItemType.PersonnelID, vm.SelectedGatherItem);

        var hardDriveOpt = vm.AvailableGatherSmallItemOptions.First(o => o.Type == GatherSmallItemType.HardDrive);
        vm.SelectedGatherSmallItemOption = hardDriveOpt;
        Assert.Equal(GatherSmallItemType.HardDrive, vm.EditingSectorSettings!.GatherItem);
        Assert.Equal(GatherSmallItemType.HardDrive, vm.SelectedGatherItem);

        // Test Spawn count modifications
        vm.GatherSpawnCount = 3;
        Assert.Equal(3, vm.EditingSectorSettings.GatherItemPlacements.Count);
        Assert.NotNull(vm.SelectedGatherItemPlacement);
        Assert.True(vm.HasSelectedGatherItemPlacement);

        // Add item
        vm.AddGatherItemPlacement();
        Assert.Equal(4, vm.GatherSpawnCount);
        Assert.Equal(4, vm.EditingSectorSettings.GatherItemPlacements.Count);
        Assert.Equal(4, vm.SelectedGatherItemPlacement.ItemNumber);

        // Remove item
        vm.RemoveGatherItemPlacement();
        Assert.Equal(3, vm.GatherSpawnCount);
        Assert.Equal(3, vm.EditingSectorSettings.GatherItemPlacements.Count);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_DimensionAndZoneOptionBindings()
    {
        var vm = new LevelLayoutViewModel();
        // Add dimension
        var level = vm.SelectedLevel;
        Assert.NotNull(level);
        level.Dimensions.Add(new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Dimension 1"
        });
        vm.RefreshAvailableDimensions();

        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;
        vm.GatherSpawnCount = 2;

        var placement1 = vm.EditingSectorSettings!.GatherItemPlacements[0];
        var placement2 = vm.EditingSectorSettings.GatherItemPlacements[1];

        vm.SelectedGatherItemPlacement = placement1;
        var realityOpt = vm.AvailableDimensions.First(d => d.DimensionIndex == "Reality");
        vm.SelectedGatherItemPlacementDimensionOption = realityOpt;
        Assert.Equal("Reality", placement1.DimensionIndex);

        vm.SelectedGatherItemPlacement = placement2;
        var dim1Opt = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");
        vm.SelectedGatherItemPlacementDimensionOption = dim1Opt;
        Assert.Equal("Dimension_1", placement2.DimensionIndex);

        var zoneOpt = vm.AvailableObjectiveZones.FirstOrDefault();
        if (zoneOpt != null)
        {
            vm.SelectedGatherItemPlacementZoneOption = zoneOpt;
            Assert.Equal(zoneOpt.ZoneIndex, placement2.Zone);
        }
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_PersistsGatherSmallItemsSettings()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_gather_{Guid.NewGuid()}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var mainSector = vm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(mainSector);

            mainSector.ObjectiveType = ObjectiveType.GatherSmallItems;
            mainSector.GatherItem = GatherSmallItemType.PlantSample;
            mainSector.GatherSpawnCount = 2;

            mainSector.GatherItemPlacements[0].DimensionIndex = "Reality";
            mainSector.GatherItemPlacements[0].Zone = 0;
            mainSector.GatherItemPlacements[0].PlacementStartWeight = 0.9;
            mainSector.GatherItemPlacements[0].PlacementMiddleWeight = 0.5;
            mainSector.GatherItemPlacements[0].PlacementEndWeight = 0.1;

            mainSector.GatherItemPlacements[1].DimensionIndex = "Dimension_1";
            mainSector.GatherItemPlacements[1].Zone = 2;
            mainSector.GatherItemPlacements[1].PlacementStartWeight = 0.2;
            mainSector.GatherItemPlacements[1].PlacementMiddleWeight = 0.4;
            mainSector.GatherItemPlacements[1].PlacementEndWeight = 0.8;

            RundownProjectService.SaveProject(vm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            var loadedSector = loadedProject.Levels?.FirstOrDefault()?.MainSector;

            Assert.NotNull(loadedSector);
            Assert.Equal(ObjectiveType.GatherSmallItems, loadedSector.ObjectiveType);
            Assert.Equal(GatherSmallItemType.PlantSample, loadedSector.GatherItem);
            Assert.NotNull(loadedSector.GatherItemPlacements);
            Assert.Equal(2, loadedSector.GatherItemPlacements.Count);

            Assert.Equal("Reality", loadedSector.GatherItemPlacements[0].DimensionIndex);
            Assert.Equal(0, loadedSector.GatherItemPlacements[0].Zone);
            Assert.Equal(0.9, loadedSector.GatherItemPlacements[0].PlacementStartWeight);
            Assert.Equal(0.5, loadedSector.GatherItemPlacements[0].PlacementMiddleWeight);
            Assert.Equal(0.1, loadedSector.GatherItemPlacements[0].PlacementEndWeight);

            Assert.Equal("Dimension_1", loadedSector.GatherItemPlacements[1].DimensionIndex);
            Assert.Equal(2, loadedSector.GatherItemPlacements[1].Zone);
            Assert.Equal(0.2, loadedSector.GatherItemPlacements[1].PlacementStartWeight);
            Assert.Equal(0.4, loadedSector.GatherItemPlacements[1].PlacementMiddleWeight);
            Assert.Equal(0.8, loadedSector.GatherItemPlacements[1].PlacementEndWeight);

            // Apply project to fresh ViewModel
            var freshVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(freshVm, loadedProject, tempFile);

            var appliedSector = freshVm.LevelLayout.SelectedLevel?.MainSector;
            Assert.NotNull(appliedSector);
            Assert.Equal(ObjectiveType.GatherSmallItems, appliedSector.ObjectiveType);
            Assert.Equal(GatherSmallItemType.PlantSample, appliedSector.GatherItem);
            Assert.Equal(2, appliedSector.GatherItemPlacements.Count);
            Assert.Equal(2, appliedSector.GatherSpawnCount);

            Assert.Equal("Dimension_1", appliedSector.GatherItemPlacements[1].DimensionIndex);
            Assert.Equal(2, appliedSector.GatherItemPlacements[1].Zone);
            Assert.Equal(0.8, appliedSector.GatherItemPlacements[1].PlacementEndWeight);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GatherPickupEventStep_DisplayTitle_AndCloning()
    {
        var step1 = new GatherPickupEventStep { PickupNumber = 1 };
        Assert.Equal("Pickup #1 (1st Item) - 0 Events", step1.DisplayTitle);

        step1.Events.Add(new WardenEvent { Type = EventType.SpawnEnemyWave });
        Assert.Equal("Pickup #1 (1st Item) - 1 Event", step1.DisplayTitle);

        step1.Events.Add(new WardenEvent { Type = EventType.DimensionFlashTeam });
        Assert.Equal("Pickup #1 (1st Item) - 2 Events", step1.DisplayTitle);

        var step2 = new GatherPickupEventStep { PickupNumber = 2 };
        Assert.Equal("Pickup #2 (2nd Item) - 0 Events", step2.DisplayTitle);

        var step3 = new GatherPickupEventStep { PickupNumber = 3 };
        Assert.Equal("Pickup #3 (3rd Item) - 0 Events", step3.DisplayTitle);

        var step11 = new GatherPickupEventStep { PickupNumber = 11 };
        Assert.Equal("Pickup #11 (11th Item) - 0 Events", step11.DisplayTitle);

        var cloned = step1.Clone();
        Assert.Equal(1, cloned.PickupNumber);
        Assert.Equal(2, cloned.Events.Count);
        Assert.Equal(EventType.SpawnEnemyWave, cloned.Events[0].Type);
        Assert.Equal(EventType.DimensionFlashTeam, cloned.Events[1].Type);

        // Independent clone
        cloned.Events.Clear();
        Assert.Equal(2, step1.Events.Count);
    }

    [Fact]
    public void WardenEvent_AvailableEventTypes_ExcludesEventBreak()
    {
        var ev = new WardenEvent();
        Assert.DoesNotContain(EventType.EventBreak, ev.AvailableEventTypes);
    }

    [Fact]
    public void LevelLayoutViewModel_GatherSmallItems_PickupEvents_Management()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;

        Assert.True(vm.IsGatherSmallItemsObjective);
        Assert.Equal("Events On Activate (Locked for Gather Small Items)", vm.EventsOnActivateHeaderTitle);

        Assert.Single(vm.EditingSectorSettings!.GatherPickupEvents);
        Assert.NotNull(vm.SelectedGatherPickupEvent);
        Assert.Equal(1, vm.SelectedGatherPickupEvent.PickupNumber);

        // Add second pickup step
        vm.AddGatherPickupEvent();
        Assert.Equal(2, vm.EditingSectorSettings.GatherPickupEvents.Count);
        Assert.Equal(2, vm.SelectedGatherPickupEvent.PickupNumber);

        // Add event to 2nd pickup
        vm.AddEventOnGatherPickup();
        Assert.Single(vm.SelectedGatherPickupEvent.Events);

        // Add 3rd pickup step
        vm.AddGatherPickupEvent();
        Assert.Equal(3, vm.EditingSectorSettings.GatherPickupEvents.Count);
        Assert.Equal(3, vm.SelectedGatherPickupEvent.PickupNumber);

        // Remove 2nd step
        var secondStep = vm.EditingSectorSettings.GatherPickupEvents[1];
        vm.RemoveGatherPickupEvent(secondStep);
        Assert.Equal(2, vm.EditingSectorSettings.GatherPickupEvents.Count);
        Assert.Equal(1, vm.EditingSectorSettings.GatherPickupEvents[0].PickupNumber);
        Assert.Equal(2, vm.EditingSectorSettings.GatherPickupEvents[1].PickupNumber);
    }

    [Fact]
    public void WardenObjectiveDataBlockExporter_GatherSmallItems_PickupEventsExport()
    {
        var sector = new SectorSettings
        {
            ObjectiveType = ObjectiveType.GatherSmallItems,
            GatherItem = GatherSmallItemType.DataCubeTampered
        };

        // Pickup 1 has 2 events
        var step1 = new GatherPickupEventStep { PickupNumber = 1 };
        step1.Events.Add(new WardenEvent { Type = EventType.DimensionFlashTeam });
        step1.Events.Add(new WardenEvent { Type = EventType.SpawnEnemyWave });

        // Pickup 2 has 1 event
        var step2 = new GatherPickupEventStep { PickupNumber = 2 };
        step2.Events.Add(new WardenEvent { Type = EventType.UnlockSecurityDoor });

        // Pickup 3 has 0 events
        var step3 = new GatherPickupEventStep { PickupNumber = 3 };

        // Pickup 4 has 1 event
        var step4 = new GatherPickupEventStep { PickupNumber = 4 };
        step4.Events.Add(new WardenEvent { Type = EventType.UpdateCustomSubObjective });

        sector.GatherPickupEvents.Clear();
        sector.GatherPickupEvents.Add(step1);
        sector.GatherPickupEvents.Add(step2);
        sector.GatherPickupEvents.Add(step3);
        sector.GatherPickupEvents.Add(step4);

        var waveSettingsAlloc = new DatablockAllocator();
        var wavePopAlloc = new DatablockAllocator();
        var waveSettingsBlocks = new System.Text.Json.Nodes.JsonArray();
        var wavePopBlocks = new System.Text.Json.Nodes.JsonArray();

        var exported = GTFO_GUIEditor.Services.DatablockManager.Exporters.WardenObjectiveDataBlockExporter.CompileWardenObjective(
            sector,
            100,
            (k, v) => 1,
            waveSettingsAlloc,
            wavePopAlloc,
            waveSettingsBlocks,
            wavePopBlocks);

        Assert.True(exported["OnActivateOnSolveItem"]?.GetValue<bool>());

        var eventsArray = exported["EventsOnActivate"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(eventsArray);

        // Expected sequence:
        // 0: DimensionFlashTeam (Pickup 1)
        // 1: SpawnEnemyWave (Pickup 1)
        // 2: EventBreak (Separator after Pickup 1)
        // 3: UnlockSecurityDoor (Pickup 2)
        // 4: EventBreak (Separator after Pickup 2)
        // 5: EventBreak (Separator after Pickup 3)
        // 6: UpdateCustomSubObjective (Pickup 4)
        Assert.Equal(7, eventsArray.Count);

        Assert.Equal("DimensionFlashTeam", eventsArray[0]?["Type"]?.ToString());
        Assert.Equal("SpawnEnemyWave", eventsArray[1]?["Type"]?.ToString());
        Assert.Equal("EventBreak", eventsArray[2]?["Type"]?.ToString());
        Assert.Equal("UnlockSecurityDoor", eventsArray[3]?["Type"]?.ToString());
        Assert.Equal("EventBreak", eventsArray[4]?["Type"]?.ToString());
        Assert.Equal("EventBreak", eventsArray[5]?["Type"]?.ToString());
        Assert.Equal("UpdateCustomSubObjective", eventsArray[6]?["Type"]?.ToString());
    }

    [Fact]
    public void LevelLayoutDataBlockImporter_GatherSmallItems_PickupEventsImport()
    {
        var rawJson = new System.Text.Json.Nodes.JsonObject
        {
            ["Type"] = "GatherSmallItems",
            ["Gather_RequiredCount"] = 3,
            ["Gather_SpawnCount"] = 3,
            ["Gather_ItemId"] = 168,
            ["EventsOnActivate"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject { ["Type"] = "DimensionFlashTeam", ["Trigger"] = "OnStart" },
                new System.Text.Json.Nodes.JsonObject { ["Type"] = "EventBreak", ["Trigger"] = "None" },
                new System.Text.Json.Nodes.JsonObject { ["Type"] = "SpawnEnemyWave", ["Trigger"] = "OnStart" },
                new System.Text.Json.Nodes.JsonObject { ["Type"] = "EventBreak", ["Trigger"] = "None" },
                new System.Text.Json.Nodes.JsonObject { ["Type"] = "EventBreak", ["Trigger"] = "None" },
                new System.Text.Json.Nodes.JsonObject { ["Type"] = "UnlockSecurityDoor", ["Trigger"] = "OnStart" }
            }
        };

        var context = new GTFO_GUIEditor.Services.DatablockManager.Importers.ImportContext();
        var objective = new WardenObjective();
        GTFO_GUIEditor.Services.DatablockManager.Importers.LevelLayoutDataBlockImporter.ImportWardenObjective(rawJson, objective, context);

        Assert.Equal(ObjectiveType.GatherSmallItems, objective.ObjectiveType);
        Assert.Empty(objective.EventsOnActivate);
        Assert.Equal(4, objective.GatherPickupEvents.Count);

        // Pickup 1 has 1 event (DimensionFlashTeam)
        Assert.Equal(1, objective.GatherPickupEvents[0].PickupNumber);
        Assert.Single(objective.GatherPickupEvents[0].Events);
        Assert.Equal(EventType.DimensionFlashTeam, objective.GatherPickupEvents[0].Events[0].Type);

        // Pickup 2 has 1 event (SpawnEnemyWave)
        Assert.Equal(2, objective.GatherPickupEvents[1].PickupNumber);
        Assert.Single(objective.GatherPickupEvents[1].Events);
        Assert.Equal(EventType.SpawnEnemyWave, objective.GatherPickupEvents[1].Events[0].Type);

        // Pickup 3 has 0 events
        Assert.Equal(3, objective.GatherPickupEvents[2].PickupNumber);
        Assert.Empty(objective.GatherPickupEvents[2].Events);

        // Pickup 4 has 1 event (UnlockSecurityDoor)
        Assert.Equal(4, objective.GatherPickupEvents[3].PickupNumber);
        Assert.Single(objective.GatherPickupEvents[3].Events);
        Assert.Equal(EventType.UnlockSecurityDoor, objective.GatherPickupEvents[3].Events[0].Type);
    }

    [Fact]
    public void EventsOnActivate_PreservedWhenSwappingToAndFromGatherSmallItems_AndLockedDuringGatherSmallItems()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();

        // 1. Start with HSU_FindTakeSample, add an event to EventsOnActivate
        vm.SelectedSectorObjectiveType = ObjectiveType.HSU_FindTakeSample;
        Assert.False(vm.IsGatherSmallItemsObjective);

        vm.AddEventOnActivate();
        Assert.Single(vm.EditingSectorSettings!.EventsOnActivate);
        vm.EditingSectorSettings.EventsOnActivate[0].Type = EventType.CloseSecurityDoor;

        // 2. Swap to GatherSmallItems
        vm.SelectedSectorObjectiveType = ObjectiveType.GatherSmallItems;
        Assert.True(vm.IsGatherSmallItemsObjective);

        // Verify EventsOnActivate was NOT deleted and still contains the event
        Assert.Single(vm.EditingSectorSettings.EventsOnActivate);
        Assert.Equal(EventType.CloseSecurityDoor, vm.EditingSectorSettings.EventsOnActivate[0].Type);

        // Verify users cannot add events to EventsOnActivate while in GatherSmallItems mode
        vm.AddEventOnActivate();
        Assert.Single(vm.EditingSectorSettings.EventsOnActivate);

        // Verify datablock exporter ignores manual EventsOnActivate and exports GatherPickupEvents
        var waveSettingsAlloc = new DatablockAllocator();
        var wavePopAlloc = new DatablockAllocator();
        var waveSettingsBlocks = new System.Text.Json.Nodes.JsonArray();
        var wavePopBlocks = new System.Text.Json.Nodes.JsonArray();

        var exportedGather = GTFO_GUIEditor.Services.DatablockManager.Exporters.WardenObjectiveDataBlockExporter.CompileWardenObjective(
            vm.EditingSectorSettings,
            100,
            (k, v) => 1,
            waveSettingsAlloc,
            wavePopAlloc,
            waveSettingsBlocks,
            wavePopBlocks);

        Assert.True(exportedGather["OnActivateOnSolveItem"]?.GetValue<bool>());
        var eventsGather = exportedGather["EventsOnActivate"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(eventsGather);
        // GatherPickupEvents has 1 step with 0 events, so exported array is empty (no CloseSecurityDoor)
        Assert.Empty(eventsGather);

        // 3. Swap back to HSU_FindTakeSample
        vm.SelectedSectorObjectiveType = ObjectiveType.HSU_FindTakeSample;
        Assert.False(vm.IsGatherSmallItemsObjective);

        // Verify EventsOnActivate is still intact
        Assert.Single(vm.EditingSectorSettings.EventsOnActivate);
        Assert.Equal(EventType.CloseSecurityDoor, vm.EditingSectorSettings.EventsOnActivate[0].Type);

        // Verify adding events to EventsOnActivate works again
        vm.AddEventOnActivate();
        Assert.Equal(2, vm.EditingSectorSettings.EventsOnActivate.Count);

        // Verify exporter exports EventsOnActivate now
        var exportedHsu = GTFO_GUIEditor.Services.DatablockManager.Exporters.WardenObjectiveDataBlockExporter.CompileWardenObjective(
            vm.EditingSectorSettings,
            101,
            (k, v) => 1,
            waveSettingsAlloc,
            wavePopAlloc,
            waveSettingsBlocks,
            wavePopBlocks);

        Assert.False(exportedHsu["OnActivateOnSolveItem"]?.GetValue<bool>());
        var eventsHsu = exportedHsu["EventsOnActivate"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(eventsHsu);
        Assert.Equal(2, eventsHsu.Count);
        Assert.Equal("CloseSecurityDoor", eventsHsu[0]?["Type"]?.ToString());
    }
}
