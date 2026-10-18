using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ZoneAndDoorSettingsTests
{
    [Fact]
    public void ZoneSettings_NewProperties_DefaultAndSetCorrectly()
    {
        var settings = new ZoneSettings();

        // Seeds
        settings.SubSeed = 100;
        settings.MarkerSubSeed = 200;
        settings.LightingSubSeed = 300;
        settings.DcScanSeed = 400;

        Assert.Equal(100, settings.SubSeed);
        Assert.Equal(200, settings.MarkerSubSeed);
        Assert.Equal(300, settings.LightingSubSeed);
        Assert.Equal(400, settings.DcScanSeed);

        // Enemy Respawning
        settings.EnemyRespawning = true;
        settings.RequireOtherZone = false;
        settings.RoomDistance = 4;
        settings.TimeInterval = 15.5;
        settings.CountMultiplier = 2.0;

        Assert.True(settings.EnemyRespawning);
        Assert.False(settings.RequireOtherZone);
        Assert.Equal(4, settings.RoomDistance);
        Assert.Equal(15.5, settings.TimeInterval);
        Assert.Equal(2.0, settings.CountMultiplier);

        // Clusters & Spawners & Pickups
        settings.HSUClusters = 3;
        settings.CorpseClusters = 5;
        settings.ResourceContainerClusters = 7;
        settings.CorpsesInZone = 12;
        settings.GroundSpawnersInZone = ZoneDistributionAmount.Some_10;
        settings.HSUsInZone = ZoneDistributionAmount.Few_5;
        settings.DeconUnitsInZone = ZoneDistributionAmount.Pair_2;

        Assert.Equal(3, settings.HSUClusters);
        Assert.Equal(5, settings.CorpseClusters);
        Assert.Equal(7, settings.ResourceContainerClusters);
        Assert.Equal(12, settings.CorpsesInZone);
        Assert.Equal(ZoneDistributionAmount.Some_10, settings.GroundSpawnersInZone);
        Assert.Equal(ZoneDistributionAmount.Few_5, settings.HSUsInZone);
        Assert.Equal(ZoneDistributionAmount.Pair_2, settings.DeconUnitsInZone);
    }

    [Fact]
    public void PlacedDoorNode_TurnOffAlarmOnTerminal_SyncsWithConnectedDoor()
    {
        var door1 = new PlacedDoorNode { Id = "door-1" };
        var door2 = new PlacedDoorNode { Id = "door-2" };

        door1.ConnectedDoor = door2;
        door2.ConnectedDoor = door1;

        door1.TurnOffAlarmOnTerminal = true;
        door1.TerminalZone = 2;
        door1.TerminalIndex = 1;

        Assert.True(door2.TurnOffAlarmOnTerminal);
        Assert.Equal(2, door2.TerminalZone);
        Assert.Equal(1, door2.TerminalIndex);
    }

    [Fact]
    public void GridState_ExportAndImport_PreservesNewZoneAndDoorSettings()
    {
        var grid = new LevelLayoutGridState();
        var zs = grid.GetOrCreateZoneSettings(1);
        zs.SubSeed = 42;
        zs.MarkerSubSeed = 43;
        zs.LightingSubSeed = 44;
        zs.DcScanSeed = 45;
        zs.EnemyRespawning = true;
        zs.RequireOtherZone = false;
        zs.RoomDistance = 3;
        zs.TimeInterval = 20.0;
        zs.CountMultiplier = 1.5;
        zs.ExcludeList.Add(new EnemyExcludeItem { EnemyId = 12 });
        zs.HSUClusters = 2;
        zs.CorpseClusters = 4;
        zs.ResourceContainerClusters = 6;
        zs.CorpsesInZone = 8;
        zs.GroundSpawnersInZone = ZoneDistributionAmount.Many_20;
        zs.HSUsInZone = ZoneDistributionAmount.SomeMore_15;
        zs.DeconUnitsInZone = ZoneDistributionAmount.Alot_30;
        zs.BigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.CryoHardcase });
        zs.DisinfectionStationPlacements.Add(new DisinfectionStationPlacement
        {
            PlacementStartWeight = 0.2,
            PlacementMiddleWeight = 0.5,
            PlacementEndWeight = 0.3
        });

        var terminal = new TerminalPlacement
        {
            PlacementStartWeight = 0.1,
            PlacementMiddleWeight = 0.8,
            PlacementEndWeight = 0.1,
            AreaSeedOffset = 11,
            MarkerSeedOffset = 22
        };
        var cmd = new TerminalUniqueCommand
        {
            Command = "OVERRIDE_LOCKDOWN",
            CommandDesc = "Unlock the blast door"
        };
        cmd.PostCommandOutputs.Add(new TerminalPostCommandOutput
        {
            LineType = TerminalLineType.SpinningWaitDone,
            Time = 4.5,
            Output = "INITIALIZING OVERRIDE..."
        });
        cmd.CommandEvents.Add(new WardenEvent
        {
            Type = EventType.OpenSecurityDoor,
            Trigger = EventTrigger.OnStart,
            Delay = 5
        });
        terminal.UniqueCommands.Add(cmd);
        zs.TerminalPlacements.Add(terminal);

        var geo = new PlacedGeomorphNode { Id = "geo-1", PrefabPath = "Assets/prefabs/test.prefab" };
        var door = new PlacedDoorNode
        {
            Id = "door-1",
            Geomorph = geo,
            TurnOffAlarmOnTerminal = true,
            TerminalZone = 1,
            TerminalIndex = 0
        };
        geo.Doors.Add(door);
        grid.PlacedGeomorphs.Add(geo);
        grid.PlacedDoors.Add(door);

        // Export
        var data = grid.ExportStateData();

        // Import into new grid
        var newGrid = new LevelLayoutGridState();
        newGrid.ImportStateData(data);

        var importedZs = newGrid.GetOrCreateZoneSettings(1);
        Assert.Equal(42, importedZs.SubSeed);
        Assert.Equal(43, importedZs.MarkerSubSeed);
        Assert.Equal(44, importedZs.LightingSubSeed);
        Assert.Equal(45, importedZs.DcScanSeed);
        Assert.True(importedZs.EnemyRespawning);
        Assert.False(importedZs.RequireOtherZone);
        Assert.Equal(3, importedZs.RoomDistance);
        Assert.Equal(20.0, importedZs.TimeInterval);
        Assert.Equal(1.5, importedZs.CountMultiplier);
        Assert.Single(importedZs.ExcludeList);
        Assert.Equal(12u, importedZs.ExcludeList[0].EnemyId);
        Assert.Equal(2, importedZs.HSUClusters);
        Assert.Equal(4, importedZs.CorpseClusters);
        Assert.Equal(6, importedZs.ResourceContainerClusters);
        Assert.Equal(8, importedZs.CorpsesInZone);
        Assert.Equal(ZoneDistributionAmount.Many_20, importedZs.GroundSpawnersInZone);
        Assert.Equal(ZoneDistributionAmount.SomeMore_15, importedZs.HSUsInZone);
        Assert.Equal(ZoneDistributionAmount.Alot_30, importedZs.DeconUnitsInZone);
        Assert.Single(importedZs.BigPickupDistributionInZone);
        Assert.Equal(RetrieveBigItemType.CryoHardcase, importedZs.BigPickupDistributionInZone[0].ItemType);
        Assert.Single(importedZs.DisinfectionStationPlacements);
        Assert.Equal(0.2, importedZs.DisinfectionStationPlacements[0].PlacementStartWeight);
        Assert.Single(importedZs.TerminalPlacements);
        Assert.Equal(11, importedZs.TerminalPlacements[0].AreaSeedOffset);
        Assert.Single(importedZs.TerminalPlacements[0].UniqueCommands);
        Assert.Equal("OVERRIDE_LOCKDOWN", importedZs.TerminalPlacements[0].UniqueCommands[0].Command);
        Assert.Single(importedZs.TerminalPlacements[0].UniqueCommands[0].PostCommandOutputs);
        Assert.Equal(TerminalLineType.SpinningWaitDone, importedZs.TerminalPlacements[0].UniqueCommands[0].PostCommandOutputs[0].LineType);
        Assert.Equal(4.5, importedZs.TerminalPlacements[0].UniqueCommands[0].PostCommandOutputs[0].Time);
        Assert.Single(importedZs.TerminalPlacements[0].UniqueCommands[0].CommandEvents);
        Assert.Equal(EventType.OpenSecurityDoor, importedZs.TerminalPlacements[0].UniqueCommands[0].CommandEvents[0].Type);

        var importedDoor = newGrid.PlacedDoors.First();
        Assert.True(importedDoor.TurnOffAlarmOnTerminal);
        Assert.Equal(1, importedDoor.TerminalZone);
        Assert.Equal(0, importedDoor.TerminalIndex);
    }

    [Fact]
    public void ViewModel_ZoneCollectionCommands_AddAndRemoveCorrectly()
    {
        var vm = new LevelLayoutViewModel();
        var level = new LevelLayoutBlock { ComplexType = ComplexType.Mining };
        vm.SelectedLevel = level;

        var geo = new PlacedGeomorphNode { Id = "geo-1" };
        var room = new PlacedRoomNode { Geomorph = geo, Zone = 1, AreaIndex = 0 };
        geo.Rooms.Add(room);
        vm.GridState.PlacedGeomorphs.Add(geo);
        vm.GridState.PlacedRooms.Add(room);
        vm.EditingRoom = room;

        Assert.NotNull(vm.ActiveZoneSettings);

        // EnemyExclude via Command
        vm.AddEnemyExcludeItem();
        Assert.Single(vm.ActiveZoneSettings!.ExcludeList);
        var excludeItem = vm.ActiveZoneSettings.ExcludeList[0];
        Assert.True(vm.RemoveEnemyExcludeItemCommand.CanExecute(excludeItem));
        vm.RemoveEnemyExcludeItemCommand.Execute(excludeItem);
        Assert.Empty(vm.ActiveZoneSettings.ExcludeList);

        // BigPickupDistribution via Command
        vm.AddBigPickupDistributionItem();
        Assert.Single(vm.ActiveZoneSettings.BigPickupDistributionInZone);
        var pickupItem = vm.ActiveZoneSettings.BigPickupDistributionInZone[0];
        Assert.True(vm.RemoveBigPickupDistributionItemCommand.CanExecute(pickupItem));
        vm.RemoveBigPickupDistributionItemCommand.Execute(pickupItem);
        Assert.Empty(vm.ActiveZoneSettings.BigPickupDistributionInZone);

        // DisinfectionStationPlacement via Command
        vm.AddDisinfectionStationPlacement();
        Assert.Single(vm.ActiveZoneSettings.DisinfectionStationPlacements);
        var disinfectPlacement = vm.ActiveZoneSettings.DisinfectionStationPlacements[0];
        Assert.True(vm.RemoveDisinfectionStationPlacementCommand.CanExecute(disinfectPlacement));
        vm.RemoveDisinfectionStationPlacementCommand.Execute(disinfectPlacement);
        Assert.Empty(vm.ActiveZoneSettings.DisinfectionStationPlacements);

        // TerminalPlacement & Sublists via Commands
        vm.AddTerminalPlacement();
        Assert.Single(vm.ActiveZoneSettings.TerminalPlacements);
        var termPlacement = vm.ActiveZoneSettings.TerminalPlacements[0];

        termPlacement.AddUniqueCommand();
        Assert.Single(termPlacement.UniqueCommands);
        var uniqueCmd = termPlacement.UniqueCommands[0];

        uniqueCmd.AddPostCommandOutput();
        Assert.Single(uniqueCmd.PostCommandOutputs);
        var postOutput = uniqueCmd.PostCommandOutputs[0];

        uniqueCmd.AddCommandEvent();
        Assert.Single(uniqueCmd.CommandEvents);
        var cmdEvent = uniqueCmd.CommandEvents[0];

        // Remove Command Event via RemoveWardenEventCommand
        Assert.True(vm.RemoveWardenEventCommand.CanExecute(cmdEvent));
        vm.RemoveWardenEventCommand.Execute(cmdEvent);
        Assert.Empty(uniqueCmd.CommandEvents);

        // Remove PostCommandOutput via Command
        Assert.True(vm.RemoveTerminalPostCommandOutputCommand.CanExecute(postOutput));
        vm.RemoveTerminalPostCommandOutputCommand.Execute(postOutput);
        Assert.Empty(uniqueCmd.PostCommandOutputs);

        // Remove UniqueCommand via Command
        Assert.True(vm.RemoveTerminalUniqueCommandCommand.CanExecute(uniqueCmd));
        vm.RemoveTerminalUniqueCommandCommand.Execute(uniqueCmd);
        Assert.Empty(termPlacement.UniqueCommands);

        // Remove TerminalPlacement via Command
        Assert.True(vm.RemoveTerminalPlacementCommand.CanExecute(termPlacement));
        vm.RemoveTerminalPlacementCommand.Execute(termPlacement);
        Assert.Empty(vm.ActiveZoneSettings.TerminalPlacements);
    }

    [Fact]
    public void ViewModel_DoorTerminalOptions_PopulatesOnlyZonesWithTerminals()
    {
        var vm = new LevelLayoutViewModel();
        var level = new LevelLayoutBlock { ComplexType = ComplexType.Mining };
        vm.SelectedLevel = level;

        var geo = new PlacedGeomorphNode { Id = "geo-1" };
        var room1 = new PlacedRoomNode { Geomorph = geo, Zone = 1, AreaIndex = 0 };
        var room2 = new PlacedRoomNode { Geomorph = geo, Zone = 2, AreaIndex = 1 };
        geo.Rooms.Add(room1);
        geo.Rooms.Add(room2);
        vm.GridState.PlacedGeomorphs.Add(geo);
        vm.GridState.PlacedRooms.Add(room1);
        vm.GridState.PlacedRooms.Add(room2);

        // Zone 1 has NO terminals. Zone 2 has 2 terminals.
        var zone2Settings = vm.GridState.GetOrCreateZoneSettings(2);
        zone2Settings.TerminalPlacements.Add(new TerminalPlacement());
        zone2Settings.TerminalPlacements.Add(new TerminalPlacement());

        var door = new PlacedDoorNode
        {
            Id = "door-1",
            Geomorph = geo,
            TurnOffAlarmOnTerminal = true,
            TerminalZone = 2,
            TerminalIndex = 1
        };

        vm.OpenEditDoorDialog(door);

        Assert.True(vm.IsEditingDoor);
        // Only Zone 2 should be in AvailableDoorTerminalZones, Zone 1 should NOT be present
        Assert.Single(vm.AvailableDoorTerminalZones);
        Assert.Contains(vm.AvailableDoorTerminalZones, z => z.ZoneIndex == 2);
        Assert.DoesNotContain(vm.AvailableDoorTerminalZones, z => z.ZoneIndex == 1);
        Assert.Equal(2, vm.SelectedDoorTerminalZoneOption?.ZoneIndex);

        Assert.Equal(2, vm.AvailableDoorTerminals.Count);
        Assert.Equal(1, vm.SelectedDoorTerminalOption?.TerminalIndex);

        // Add a terminal to Zone 1 and check that it now shows up
        vm.EditingRoom = room1;
        vm.AddTerminalPlacement();
        Assert.Equal(2, vm.AvailableDoorTerminalZones.Count);
        Assert.Contains(vm.AvailableDoorTerminalZones, z => z.ZoneIndex == 1);

        // Remove terminal from Zone 1 and verify it disappears again
        vm.RemoveTerminalPlacement(vm.ActiveZoneSettings?.TerminalPlacements[0]);
        Assert.Single(vm.AvailableDoorTerminalZones);
        Assert.DoesNotContain(vm.AvailableDoorTerminalZones, z => z.ZoneIndex == 1);

        vm.CloseEditDoorDialog();
        Assert.False(vm.IsEditingDoor);
    }

    [Fact]
    public void TerminalPlacement_NewProperties_DefaultAndSetCorrectly()
    {
        var term = new TerminalPlacement();

        // Defaults
        Assert.Equal(TerminalStartingState.Sleeping, term.StartingState);
        Assert.False(term.UseCustomInfoText);
        Assert.Equal(string.Empty, term.CustomInfoText);
        Assert.False(term.KeepShowingLocalLogCount);
        Assert.False(term.PasswordProtected);
        Assert.Equal("Password Required.", term.PasswordHintText);
        Assert.False(term.GeneratePassword);
        Assert.Equal(1, term.PasswordPartCount);
        Assert.True(term.ShowPasswordLength);
        Assert.False(term.ShowPasswordPartPositions);
        Assert.False(term.IsPasswordGenerationVisible);
        Assert.Empty(term.TerminalZoneSelectionDatas);

        // Modifying properties
        term.StartingState = TerminalStartingState.Hacked;
        term.UseCustomInfoText = true;
        term.CustomInfoText = "System Overridden";
        term.KeepShowingLocalLogCount = true;
        term.PasswordProtected = true;
        term.PasswordHintText = "Enter Access Code.";
        term.GeneratePassword = true;
        term.PasswordPartCount = 3;
        term.ShowPasswordLength = false;
        term.ShowPasswordPartPositions = true;

        Assert.Equal(TerminalStartingState.Hacked, term.StartingState);
        Assert.True(term.UseCustomInfoText);
        Assert.Equal("System Overridden", term.CustomInfoText);
        Assert.True(term.KeepShowingLocalLogCount);
        Assert.True(term.PasswordProtected);
        Assert.Equal("Enter Access Code.", term.PasswordHintText);
        Assert.True(term.GeneratePassword);
        Assert.Equal(3, term.PasswordPartCount);
        Assert.False(term.ShowPasswordLength);
        Assert.True(term.ShowPasswordPartPositions);
        Assert.True(term.IsPasswordGenerationVisible);

        // Password protected false disables IsPasswordGenerationVisible
        term.PasswordProtected = false;
        Assert.False(term.IsPasswordGenerationVisible);

        term.PasswordProtected = true;
        term.GeneratePassword = false;
        Assert.False(term.IsPasswordGenerationVisible);
    }

    [Fact]
    public void TerminalPlacement_TerminalZoneSelectionDatas_AddRemoveAndClone()
    {
        var term = new TerminalPlacement();
        term.AddTerminalZoneSelectionData();
        Assert.Single(term.TerminalZoneSelectionDatas);

        var item = term.TerminalZoneSelectionDatas[0];
        item.Zone = 5;
        item.Terminal = 2;

        Assert.Equal(5, item.Zone);
        Assert.Equal(2, item.Terminal);

        var cloned = term.Clone();
        Assert.Single(cloned.TerminalZoneSelectionDatas);
        Assert.Equal(5, cloned.TerminalZoneSelectionDatas[0].Zone);
        Assert.Equal(2, cloned.TerminalZoneSelectionDatas[0].Terminal);

        term.RemoveTerminalZoneSelectionData(item);
        Assert.Empty(term.TerminalZoneSelectionDatas);
    }

    [Fact]
    public void GridState_ExportAndImport_PreservesTerminalNewInformation()
    {
        var grid = new LevelLayoutGridState();
        var zs = grid.GetOrCreateZoneSettings(1);

        var terminal = new TerminalPlacement
        {
            PlacementStartWeight = 0.5,
            StartingState = TerminalStartingState.DataMining,
            UseCustomInfoText = true,
            CustomInfoText = "Custom terminal info message",
            KeepShowingLocalLogCount = true,
            PasswordProtected = true,
            PasswordHintText = "Custom Password Prompt",
            GeneratePassword = true,
            PasswordPartCount = 4,
            ShowPasswordLength = true,
            ShowPasswordPartPositions = true
        };

        var tzData = new TerminalZoneSelectionData
        {
            Zone = 2,
            Terminal = 1
        };
        terminal.TerminalZoneSelectionDatas.Add(tzData);
        zs.TerminalPlacements.Add(terminal);

        var exported = grid.ExportStateData();

        var newGrid = new LevelLayoutGridState();
        newGrid.ImportStateData(exported);

        var importedZs = newGrid.GetOrCreateZoneSettings(1);
        Assert.Single(importedZs.TerminalPlacements);
        var importedTerm = importedZs.TerminalPlacements[0];

        Assert.Equal(TerminalStartingState.DataMining, importedTerm.StartingState);
        Assert.True(importedTerm.UseCustomInfoText);
        Assert.Equal("Custom terminal info message", importedTerm.CustomInfoText);
        Assert.True(importedTerm.KeepShowingLocalLogCount);
        Assert.True(importedTerm.PasswordProtected);
        Assert.Equal("Custom Password Prompt", importedTerm.PasswordHintText);
        Assert.True(importedTerm.GeneratePassword);
        Assert.Equal(4, importedTerm.PasswordPartCount);
        Assert.True(importedTerm.ShowPasswordLength);
        Assert.True(importedTerm.ShowPasswordPartPositions);
        Assert.Single(importedTerm.TerminalZoneSelectionDatas);
        Assert.Equal(2, importedTerm.TerminalZoneSelectionDatas[0].Zone);
        Assert.Equal(1, importedTerm.TerminalZoneSelectionDatas[0].Terminal);
    }
}
