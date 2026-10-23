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

    [Fact]
    public void PlacedDoorNode_BloodDoorProperties_SyncWithConnectedDoor()
    {
        var door1 = new PlacedDoorNode { Id = "door-1" };
        var door2 = new PlacedDoorNode { Id = "door-2" };

        door1.ConnectedDoor = door2;
        door2.ConnectedDoor = door1;

        door1.HasBloodDoor = true;
        Assert.True(door2.HasBloodDoor);

        var itemInArea = new EnemyGroupItem { EnemyId = 30, Count = 3 };
        door1.EnemyGroupInArea.Add(itemInArea);

        var itemInFront = new EnemyGroupItem { EnemyId = 76, Count = 5 };
        door1.EnemyGroupInFrontOfDoor.Add(itemInFront);

        Assert.Equal(door1.EnemyGroupInArea, door2.EnemyGroupInArea);
        Assert.Equal(door1.EnemyGroupInFrontOfDoor, door2.EnemyGroupInFrontOfDoor);
        Assert.Single(door2.EnemyGroupInArea);
        Assert.Equal(30u, door2.EnemyGroupInArea[0].EnemyId);
        Assert.Equal(3, door2.EnemyGroupInArea[0].Count);
        Assert.Single(door2.EnemyGroupInFrontOfDoor);
        Assert.Equal(76u, door2.EnemyGroupInFrontOfDoor[0].EnemyId);
        Assert.Equal(5, door2.EnemyGroupInFrontOfDoor[0].Count);
    }

    [Fact]
    public void GridState_ExportAndImport_PreservesBloodDoorSettings()
    {
        var grid = new LevelLayoutGridState();
        var geoNode = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_03.prefab", 0, 0);
        grid.PlacedGeomorphs.Add(geoNode);

        var door = new PlacedDoorNode
        {
            Id = "door-blood-1",
            Geomorph = geoNode,
            HasBloodDoor = true
        };
        door.EnemyGroupInArea.Add(new EnemyGroupItem { EnemyId = 30, Count = 2 });
        door.EnemyGroupInFrontOfDoor.Add(new EnemyGroupItem { EnemyId = 76, Count = 4 });
        grid.PlacedDoors.Add(door);
        geoNode.Doors.Add(door);

        var exported = grid.ExportStateData();
        var newGrid = new LevelLayoutGridState();
        newGrid.ImportStateData(exported);

        Assert.Single(newGrid.PlacedDoors);
        var importedDoor = newGrid.PlacedDoors[0];
        Assert.True(importedDoor.HasBloodDoor);
        Assert.Single(importedDoor.EnemyGroupInArea);
        Assert.Equal(30u, importedDoor.EnemyGroupInArea[0].EnemyId);
        Assert.Equal(2, importedDoor.EnemyGroupInArea[0].Count);
        Assert.Single(importedDoor.EnemyGroupInFrontOfDoor);
        Assert.Equal(76u, importedDoor.EnemyGroupInFrontOfDoor[0].EnemyId);
        Assert.Equal(4, importedDoor.EnemyGroupInFrontOfDoor[0].Count);
    }

    [Fact]
    public void LevelLayoutViewModel_BloodDoorEnemyGroups_AddAndRemove()
    {
        var vm = new LevelLayoutViewModel();
        var door = new PlacedDoorNode { Id = "door-test-1", HasBloodDoor = true };
        vm.OpenEditDoorDialog(door);

        Assert.True(vm.IsEditingDoor);
        Assert.Same(door, vm.EditingDoor);

        vm.AddEnemyGroupInAreaItem();
        Assert.Single(door.EnemyGroupInArea);

        vm.AddEnemyGroupInFrontOfDoorItem();
        Assert.Single(door.EnemyGroupInFrontOfDoor);

        var itemArea = door.EnemyGroupInArea[0];
        vm.RemoveEnemyGroupInAreaItem(itemArea);
        Assert.Empty(door.EnemyGroupInArea);

        var itemFront = door.EnemyGroupInFrontOfDoor[0];
        vm.RemoveEnemyGroupInFrontOfDoorItem(itemFront);
        Assert.Empty(door.EnemyGroupInFrontOfDoor);
    }

    [Fact]
    public void PlacedDoorNode_ApexDoor_SyncsWithConnectedDoor()
    {
        var door1 = new PlacedDoorNode { Id = "door-1", IsSecurityDoor = true };
        var door2 = new PlacedDoorNode { Id = "door-2", IsSecurityDoor = true };

        door1.ConnectedDoor = door2;
        door2.ConnectedDoor = door1;

        door1.IsApexDoor = true;
        Assert.True(door2.IsApexDoor);

        door2.IsApexDoor = false;
        Assert.False(door1.IsApexDoor);
    }

    [Fact]
    public void GridState_ExportAndImport_PreservesApexDoor()
    {
        var grid = new LevelLayoutGridState();
        var geoNode = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_03.prefab", 0, 0);
        grid.PlacedGeomorphs.Add(geoNode);

        var door = new PlacedDoorNode
        {
            Id = "door-apex-1",
            Geomorph = geoNode,
            IsSecurityDoor = true,
            IsApexDoor = true
        };
        grid.PlacedDoors.Add(door);
        geoNode.Doors.Add(door);

        var exported = grid.ExportStateData();
        var newGrid = new LevelLayoutGridState();
        newGrid.ImportStateData(exported);

        Assert.Single(newGrid.PlacedDoors);
        var importedDoor = newGrid.PlacedDoors[0];
        Assert.True(importedDoor.IsApexDoor);
    }

    [Fact]
    public void LevelLayoutBlock_FogSettings_ClonesAndPersists()
    {
        var level = new LevelLayoutBlock
        {
            Name = "Test Level",
            PersistentId = 42,
            FogSettings = 136
        };

        var clone = level.Clone();
        Assert.Equal(136u, clone.FogSettings);
        Assert.Equal("Test Level", clone.Name);
        Assert.Equal(42u, clone.PersistentId);
    }

    [Fact]
    public void LevelLayoutViewModel_CreateAndEditLevel_FogSettingsAndProperties()
    {
        var vm = new LevelLayoutViewModel();
        var fog = new FogSettingsBlock { PersistentId = 136, Name = "Dense Fog" };
        vm.AvailableFogSettings.Add(fog);

        // Test create level flow
        vm.OpenCreateLevelDialog();
        Assert.True(vm.IsCreatingLevel);
        Assert.False(vm.IsEditingExistingLevel);
        Assert.Equal("Create New Level", vm.LevelDialogTitle);
        Assert.Equal("Create", vm.LevelDialogConfirmButtonText);

        vm.NewLevelName = "Level 1";
        vm.NewLevelPersistentId = 101;
        vm.SelectedFogSetting = fog;

        bool created = vm.ConfirmCreateLevel();
        Assert.True(created);
        Assert.False(vm.IsCreatingLevel);
        Assert.NotNull(vm.SelectedLevel);
        Assert.Equal(101u, vm.SelectedLevel.PersistentId);
        Assert.Equal("Level 1", vm.SelectedLevel.Name);
        Assert.Equal(136u, vm.SelectedLevel.FogSettings);

        // Test edit level flow
        var fog2 = new FogSettingsBlock { PersistentId = 200, Name = "Clear Fog" };
        vm.AvailableFogSettings.Add(fog2);

        vm.OpenEditLevelDialog();
        Assert.True(vm.IsCreatingLevel);
        Assert.True(vm.IsEditingExistingLevel);
        Assert.Equal("Edit Level", vm.LevelDialogTitle);
        Assert.Equal("Save", vm.LevelDialogConfirmButtonText);
        Assert.Equal(101u, vm.NewLevelPersistentId);
        Assert.Equal("Level 1", vm.NewLevelName);

        vm.NewLevelName = "Level 1 Renamed";
        vm.SelectedFogSetting = fog2;

        bool edited = vm.ConfirmCreateLevel();
        Assert.True(edited);
        Assert.False(vm.IsCreatingLevel);
        Assert.Equal("Level 1 Renamed", vm.SelectedLevel.Name);
        Assert.Equal(200u, vm.SelectedLevel.FogSettings);
    }

    [Fact]
    public void PlacedDoorNode_ProgressionPuzzle_SyncsWithConnectedDoor()
    {
        var door1 = new PlacedDoorNode { Id = "door-1", IsSecurityDoor = true };
        var door2 = new PlacedDoorNode { Id = "door-2", IsSecurityDoor = true };

        door1.ConnectedDoor = door2;
        door2.ConnectedDoor = door1;

        door1.ProgressionPuzzleToEnter = ProgressionPuzzleType.Keycard_SecurityBox;
        door1.ProgressionPuzzleCustomText = "Custom Keycard Door Text";
        door1.ProgressionPuzzlePlacementCount = 1;

        var cz1 = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 3,
            PlacementStartWeight = 0.2,
            PlacementMiddleWeight = 0.5,
            PlacementEndWeight = 0.3
        };
        door1.ProgressionPuzzleCandidateZones.Add(cz1);

        Assert.Equal(ProgressionPuzzleType.Keycard_SecurityBox, door2.ProgressionPuzzleToEnter);
        Assert.Equal("Custom Keycard Door Text", door2.ProgressionPuzzleCustomText);
        Assert.True(door2.IsKeycardPuzzle);
        Assert.True(door2.IsKeycardOrGeneratorPuzzle);
        Assert.False(door2.IsGeneratorPuzzle);
        Assert.False(door2.IsLockedNoKey);
        Assert.True(door2.HasProgressionPuzzle);

        door2.ProgressionPuzzleToEnter = ProgressionPuzzleType.PowerGenerator_And_PowerCell;
        door2.ProgressionPuzzlePlacementCount = 3;

        Assert.Equal(ProgressionPuzzleType.PowerGenerator_And_PowerCell, door1.ProgressionPuzzleToEnter);
        Assert.Equal(3, door1.ProgressionPuzzlePlacementCount);
        Assert.True(door1.IsGeneratorPuzzle);
        Assert.True(door1.IsKeycardOrGeneratorPuzzle);
        Assert.False(door1.IsKeycardPuzzle);
    }

    [Fact]
    public void GridState_ExportAndImport_PreservesProgressionPuzzleSettings()
    {
        var grid = new LevelLayoutGridState();
        var geoNode = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_HA_03.prefab", 0, 0);
        grid.PlacedGeomorphs.Add(geoNode);

        var door = new PlacedDoorNode
        {
            Id = "door-lock-1",
            Geomorph = geoNode,
            IsSecurityDoor = true,
            ProgressionPuzzleToEnter = ProgressionPuzzleType.Keycard_SecurityBox,
            ProgressionPuzzleCustomText = "Insert Blue Keycard",
            ProgressionPuzzlePlacementCount = 1
        };
        door.ProgressionPuzzleCandidateZones.Add(new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 2,
            PlacementStartWeight = 0.1,
            PlacementMiddleWeight = 0.2,
            PlacementEndWeight = 0.7
        });

        grid.PlacedDoors.Add(door);
        geoNode.Doors.Add(door);

        var exported = grid.ExportStateData();
        var newGrid = new LevelLayoutGridState();
        newGrid.ImportStateData(exported);

        Assert.Single(newGrid.PlacedDoors);
        var importedDoor = newGrid.PlacedDoors[0];
        Assert.Equal(ProgressionPuzzleType.Keycard_SecurityBox, importedDoor.ProgressionPuzzleToEnter);
        Assert.Equal("Insert Blue Keycard", importedDoor.ProgressionPuzzleCustomText);
        Assert.Equal(1, importedDoor.ProgressionPuzzlePlacementCount);
        Assert.Single(importedDoor.ProgressionPuzzleCandidateZones);
        Assert.Equal(2, importedDoor.ProgressionPuzzleCandidateZones[0].Zone);
        Assert.Equal(0.7, importedDoor.ProgressionPuzzleCandidateZones[0].PlacementEndWeight);
    }

    [Fact]
    public void LevelLayoutViewModel_DoorCandidateZones_Commands()
    {
        var vm = new LevelLayoutViewModel();
        var door = new PlacedDoorNode { Id = "door-test", IsSecurityDoor = true };

        vm.OpenEditDoorDialog(door);
        Assert.True(vm.IsEditingDoor);
        Assert.Equal(door, vm.EditingDoor);

        vm.AddDoorCandidateZone();
        Assert.Single(door.ProgressionPuzzleCandidateZones);
        Assert.NotNull(vm.SelectedDoorCandidateZonePlacement);
        Assert.True(vm.HasSelectedDoorCandidateZonePlacement);

        var cz = vm.SelectedDoorCandidateZonePlacement;
        cz.PlacementStartWeight = 0.4;
        cz.PlacementMiddleWeight = 0.4;
        cz.PlacementEndWeight = 0.2;

        vm.RemoveDoorCandidateZone(cz);
        Assert.Empty(door.ProgressionPuzzleCandidateZones);
        Assert.Null(vm.SelectedDoorCandidateZonePlacement);
        Assert.False(vm.HasSelectedDoorCandidateZonePlacement);

        vm.CloseEditDoorDialog();
        Assert.False(vm.IsEditingDoor);
        Assert.Null(vm.EditingDoor);
    }

    [Fact]
    public void LevelLayoutViewModel_DoorsOpenedByThisDc_ListsReachableDoors_Correctly()
    {
        var vm = new LevelLayoutViewModel();
        var level = new LevelLayoutBlock { PersistentId = 1, Name = "Test Level" };
        var grid = level.GridState;

        var geo0 = new PlacedGeomorphNode { Id = "geo-0" };
        geo0.SetRoomZone(0, 0);
        geo0.GenerateRoom(0);
        var room0 = new PlacedRoomNode { Geomorph = geo0, AreaIndex = 0, Zone = 0, Sector = SectorType.Main };
        geo0.Rooms.Add(room0);
        grid.PlacedGeomorphs.Add(geo0);
        grid.PlacedRooms.Add(room0);

        var geo1 = new PlacedGeomorphNode { Id = "geo-1" };
        geo1.SetRoomZone(0, 1);
        geo1.GenerateRoom(0);
        var room1 = new PlacedRoomNode { Geomorph = geo1, AreaIndex = 0, Zone = 1, Sector = SectorType.Main };
        geo1.Rooms.Add(room1);
        grid.PlacedGeomorphs.Add(geo1);
        grid.PlacedRooms.Add(room1);

        var door = new PlacedDoorNode
        {
            Id = "door-0-1",
            Geomorph = geo0,
            AreaFrom = 0,
            IsConnected = true,
            DoorType = DoorType.SecurityDoor
        };
        var doorTarget = new PlacedDoorNode
        {
            Id = "door-1-0",
            Geomorph = geo1,
            AreaFrom = 0,
            IsConnected = true,
            DoorType = DoorType.SecurityDoor,
            ConnectedDoor = door
        };
        door.ConnectedDoor = doorTarget;
        geo0.Doors.Add(door);
        geo1.Doors.Add(doorTarget);
        grid.PlacedDoors.Add(door);
        grid.PlacedDoors.Add(doorTarget);

        vm.Levels.Add(level);
        vm.SelectedLevel = level;

        vm.EditingRoom = room0;
        vm.ActiveZoneSettings!.ContainsDC = true;
        vm.UpdateDoorsOpenedByThisDc();

        Assert.True(vm.HasDoorsOpenedByThisDc);
        Assert.Single(vm.DoorsOpenedByThisDc);
        Assert.Contains("Security Door to Zone 1", vm.DoorsOpenedByThisDc[0]);
    }

    [Fact]
    public void LevelLayoutViewModel_BulkheadKeys_AddRemoveAndSelect()
    {
        var vm = new LevelLayoutViewModel();
        var level = new LevelLayoutBlock { PersistentId = 1, Name = "Test Level" };
        vm.Levels.Add(level);
        vm.SelectedLevel = level;

        vm.OpenSectorSettings(SectorType.Main);
        Assert.True(vm.IsEditingSector);
        Assert.NotNull(vm.EditingSectorSettings);

        vm.AddBulkheadKey();
        Assert.Single(vm.EditingSectorSettings.BulkheadKeyPlacements);
        Assert.NotNull(vm.SelectedBulkheadKeyPlacement);
        Assert.True(vm.HasSelectedBulkheadKeyPlacement);

        vm.AddBulkheadKeyCandidateZone();
        Assert.Equal(2, vm.SelectedBulkheadKeyPlacement.CandidateZones.Count);
        Assert.NotNull(vm.SelectedBulkheadKeyCandidateZonePlacement);

        vm.RemoveBulkheadKeyCandidateZone();
        Assert.Single(vm.SelectedBulkheadKeyPlacement.CandidateZones);

        vm.RemoveBulkheadKey();
        Assert.Empty(vm.EditingSectorSettings.BulkheadKeyPlacements);
        Assert.Null(vm.SelectedBulkheadKeyPlacement);
        Assert.False(vm.HasSelectedBulkheadKeyPlacement);

        vm.CloseSectorSettings();
        Assert.False(vm.IsEditingSector);
    }
}
