using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ClipboardCopyPasteTests
{
    [Fact]
    public async Task CopyWardenEvent_SerializesToJsonStringInClipboard()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;

        var ev = new WardenEvent
        {
            Type = EventType.SpawnEnemyWave,
            Trigger = EventTrigger.OnStart,
            Delay = 5.5,
            Layer = SectorType.Secondary,
            Dimension = "Dimension_1",
            Zone = 2,
            WardenIntel = "Test intel message",
            SoundID = 123456,
            SoundSubtitle = "Subtitle_123",
            Enabled = true
        };

        await vm.CopyWardenEventAsync(ev);

        Assert.NotNull(mockClipboard.Content);
        Assert.Contains("SpawnEnemyWave", mockClipboard.Content);
        Assert.Contains("Test intel message", mockClipboard.Content);

        // Verify valid JSON deserialization
        var deserialized = JsonSerializer.Deserialize<WardenEvent>(mockClipboard.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(deserialized);
        Assert.Equal(EventType.SpawnEnemyWave, deserialized.Type);
        Assert.Equal(5.5, deserialized.Delay);
        Assert.Equal("Test intel message", deserialized.WardenIntel);
        Assert.Equal(123456u, deserialized.SoundID);
    }

    [Fact]
    public async Task CopyTerminalPlacement_SerializesToJsonStringInClipboard()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;

        var term = new TerminalPlacement
        {
            PlacementStartWeight = 0.2,
            PlacementMiddleWeight = 0.5,
            PlacementEndWeight = 0.8,
            AreaSeedOffset = 12,
            MarkerSeedOffset = 34,
            StartingState = TerminalStartingState.Awake,
            UseCustomInfoText = true,
            CustomInfoText = "TERMINAL ACCESS GRANTED",
            KeepShowingLocalLogCount = true,
            PasswordProtected = true,
            PasswordHintText = "SECRET_PASS",
            GeneratePassword = true,
            PasswordPartCount = 3,
            ShowPasswordLength = true,
            ShowPasswordPartPositions = true
        };
        term.LocalLogFiles.Add(new TerminalLocalLogFile { FileName = "LOG_01", FileContent = "CLASSIFIED_INFO" });
        var cmd = new TerminalUniqueCommand { Command = "OVERRIDE", CommandDesc = "Override door locks" };
        cmd.CommandEvents.Add(new WardenEvent { Type = EventType.OpenSecurityDoor, Delay = 1.0 });
        term.UniqueCommands.Add(cmd);

        await vm.CopyTerminalPlacementAsync(term);

        Assert.NotNull(mockClipboard.Content);
        Assert.Contains("TERMINAL ACCESS GRANTED", mockClipboard.Content);
        Assert.Contains("LOG_01", mockClipboard.Content);
        Assert.Contains("OVERRIDE", mockClipboard.Content);

        var deserialized = JsonSerializer.Deserialize<TerminalPlacement>(mockClipboard.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(deserialized);
        Assert.Equal(TerminalStartingState.Awake, deserialized.StartingState);
        Assert.Equal("SECRET_PASS", deserialized.PasswordHintText);
        Assert.Single(deserialized.LocalLogFiles);
        Assert.Equal("LOG_01", deserialized.LocalLogFiles[0].FileName);
        Assert.Single(deserialized.UniqueCommands);
        Assert.Equal("OVERRIDE", deserialized.UniqueCommands[0].Command);
        Assert.Single(deserialized.UniqueCommands[0].CommandEvents);
        Assert.Equal(EventType.OpenSecurityDoor, deserialized.UniqueCommands[0].CommandEvents[0].Type);
    }

    [Fact]
    public async Task AddDoorEventsFromClipboard_PopulatesDoorEventCollections()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;

        var door = new PlacedDoorNode();
        vm.EditingDoor = door;

        var json = JsonSerializer.Serialize(new WardenEvent
        {
            Type = EventType.OpenSecurityDoor,
            Delay = 2.0,
            WardenIntel = "Door breached"
        });
        mockClipboard.Content = json;

        // Test each door event command
        await vm.AddEventOnPortalWarpFromClipboardCommand();
        Assert.Single(door.EventsOnPortalWarp);
        Assert.Equal(EventType.OpenSecurityDoor, door.EventsOnPortalWarp[0].Type);

        await vm.AddEventOnApproachDoorFromClipboardCommand();
        Assert.Single(door.EventsOnApproachDoor);
        Assert.Equal(EventType.OpenSecurityDoor, door.EventsOnApproachDoor[0].Type);

        await vm.AddEventOnUnlockDoorFromClipboardCommand();
        Assert.Single(door.EventsOnUnlockDoor);

        await vm.AddEventOnOpenDoorFromClipboardCommand();
        Assert.Single(door.EventsOnOpenDoor);

        await vm.AddEventOnDoorScanStartFromClipboardCommand();
        Assert.Single(door.EventsOnDoorScanStart);

        await vm.AddEventOnDoorScanDoneFromClipboardCommand();
        Assert.Single(door.EventsOnDoorScanDone);

        await vm.AddEventOnBossDeathFromClipboardCommand();
        Assert.Single(door.EventsOnBossDeath);

        await vm.AddEventOnTriggerFromClipboardCommand();
        Assert.Single(door.EventsOnTrigger);

        await vm.AddEventOnTerminalDeactivateAlarmFromClipboardCommand();
        Assert.Single(door.EventsOnTerminalDeactivateAlarm);
    }

    [Fact]
    public async Task AddObjectiveEventsFromClipboard_PopulatesObjectiveEventCollections()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;
        vm.ActiveEditingObjective = new WardenObjective();

        var json = JsonSerializer.Serialize(new WardenEvent
        {
            Type = EventType.SpawnEnemyWave,
            Delay = 10.0,
            WardenIntel = "Wave incoming"
        });
        mockClipboard.Content = json;

        await vm.AddEventOnElevatorLandFromClipboardCommand();
        Assert.Single(vm.CurrentObjective!.EventsOnElevatorLand);
        Assert.Equal(EventType.SpawnEnemyWave, vm.CurrentObjective.EventsOnElevatorLand[0].Type);

        await vm.AddEventOnActivateFromClipboardCommand();
        Assert.Single(vm.CurrentObjective.EventsOnActivate);

        await vm.AddEventOnGotoWinFromClipboardCommand();
        Assert.Single(vm.CurrentObjective.EventsOnGotoWin);

        await vm.AddEventOnActivationFromClipboardCommand();
        Assert.Single(vm.CurrentObjective.EventsOnActivation);

        await vm.AddEventOnSequenceStartFromClipboardCommand();
        Assert.Single(vm.CurrentObjective.EventsOnSequenceStart);

        await vm.AddEventOnSequenceDoneFromClipboardCommand();
        Assert.Single(vm.CurrentObjective.EventsOnSequenceDone);

        await vm.AddEventOnSequenceFailFromClipboardCommand();
        Assert.Single(vm.CurrentObjective.EventsOnSequenceFail);
    }

    [Fact]
    public async Task AddTerminalPlacementsAndCommandEventsFromClipboard_PopulatesCorrectCollections()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;

        var room = new PlacedRoomNode { Sector = SectorType.Main, Zone = 0 };
        vm.EditingRoom = room;

        var term = new TerminalPlacement
        {
            CustomInfoText = "ZONE TERMINAL",
            PasswordProtected = true
        };
        mockClipboard.Content = JsonSerializer.Serialize(term);

        await vm.AddTerminalPlacementFromClipboardCommand();
        Assert.NotNull(vm.ActiveZoneSettings);
        Assert.Single(vm.ActiveZoneSettings.TerminalPlacements);
        Assert.Equal("ZONE TERMINAL", vm.ActiveZoneSettings.TerminalPlacements[0].CustomInfoText);

        // Static dimension terminal placements
        var dim = new DimensionLayoutBlock();
        vm.EditingDimension = dim;

        var dimTerm = new TerminalPlacement
        {
            CustomInfoText = "DIMENSION TERMINAL"
        };
        mockClipboard.Content = JsonSerializer.Serialize(dimTerm);

        await vm.AddDimensionTerminalPlacementFromClipboardCommand();
        Assert.Single(dim.StaticTerminalPlacements);
        Assert.Equal("DIMENSION TERMINAL", dim.StaticTerminalPlacements[0].CustomInfoText);

        // Command events inside Unique Commands
        var cmd = new TerminalUniqueCommand { Command = "TEST" };
        var ev = new WardenEvent { Type = EventType.ActivateChainedPuzzle, Delay = 3.0 };
        mockClipboard.Content = JsonSerializer.Serialize(ev);

        await vm.AddUniqueCommandEventFromClipboardCommand(cmd);
        Assert.Single(cmd.CommandEvents);
        Assert.Equal(EventType.ActivateChainedPuzzle, cmd.CommandEvents[0].Type);
    }

    [Fact]
    public async Task AddFromClipboard_SupportsJsonArray()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;

        var events = new List<WardenEvent>
        {
            new WardenEvent { Type = EventType.OpenSecurityDoor, Delay = 1.0 },
            new WardenEvent { Type = EventType.UnlockSecurityDoor, Delay = 2.0 },
            new WardenEvent { Type = EventType.LockSecurityDoor, Delay = 3.0 }
        };
        mockClipboard.Content = JsonSerializer.Serialize(events);

        var target = new ObservableCollection<WardenEvent>();
        await vm.AddWardenEventsToCollectionFromClipboardAsync(target);

        Assert.Equal(3, target.Count);
        Assert.Equal(EventType.OpenSecurityDoor, target[0].Type);
        Assert.Equal(EventType.UnlockSecurityDoor, target[1].Type);
        Assert.Equal(EventType.LockSecurityDoor, target[2].Type);
    }

    [Fact]
    public async Task AddFromClipboard_HandlesInvalidOrEmptyJsonGracefully()
    {
        var vm = new LevelLayoutViewModel();
        var mockClipboard = new MockClipboardService();
        vm.ClipboardService = mockClipboard;

        var target = new ObservableCollection<WardenEvent>();

        // Null / whitespace
        mockClipboard.Content = "";
        await vm.AddWardenEventsToCollectionFromClipboardAsync(target);
        Assert.Empty(target);

        mockClipboard.Content = "   ";
        await vm.AddWardenEventsToCollectionFromClipboardAsync(target);
        Assert.Empty(target);

        // Invalid JSON
        mockClipboard.Content = "INVALID_NOT_JSON";
        await vm.AddWardenEventsToCollectionFromClipboardAsync(target);
        Assert.Empty(target);

        // Invalid JSON for TerminalPlacement
        var termTarget = await vm.GetTerminalPlacementsFromClipboardAsync();
        Assert.Empty(termTarget);
    }
}
