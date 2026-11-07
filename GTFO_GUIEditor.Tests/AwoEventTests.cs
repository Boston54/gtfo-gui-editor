using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class AwoEventTests
{
    [Fact]
    public void EventType_EnumValues_MatchExpectedAwoIds()
    {
        Assert.Equal(10000, (int)EventType.CloseSecurityDoor);
        Assert.Equal(10001, (int)EventType.LockSecurityDoor);
        Assert.Equal(10002, (int)EventType.SetDoorInteraction);
        Assert.Equal(10003, (int)EventType.TriggerSecurityDoorAlarm);
        Assert.Equal(10004, (int)EventType.SolveSecurityDoorAlarm);
        Assert.Equal(10005, (int)EventType.StartReactor);
        Assert.Equal(10006, (int)EventType.ModifyReactorWaveState);
        Assert.Equal(10007, (int)EventType.ForceCompleteReactor);
        Assert.Equal(10008, (int)EventType.ForceCompleteLevel);
        Assert.Equal(10009, (int)EventType.ForceFailLevel);
        Assert.Equal(10010, (int)EventType.Countdown);
        Assert.Equal(10011, (int)EventType.SetLevelFailCheckEnabled);
        Assert.Equal(10012, (int)EventType.SetLevelFailWhenAnyPlayerDowned);
        Assert.Equal(10013, (int)EventType.KillAllPlayers);
        Assert.Equal(10014, (int)EventType.KillPlayersInZone);
        Assert.Equal(10015, (int)EventType.SetLightDataInZone);
        Assert.Equal(10017, (int)EventType.AlertEnemiesInZone);
        Assert.Equal(10018, (int)EventType.CleanUpEnemiesInZone);
        Assert.Equal(10019, (int)EventType.SpawnHibernateInZone);
        Assert.Equal(10020, (int)EventType.SpawnScoutInZone);
        Assert.Equal(10021, (int)EventType.SaveCheckpoint);
        Assert.Equal(10022, (int)EventType.MoveExtractionWorldPosition);
        Assert.Equal(10023, (int)EventType.SetBlackoutEnabled);
        Assert.Equal(10024, (int)EventType.AddTerminalCommand);
        Assert.Equal(10025, (int)EventType.HideTerminalCommand);
        Assert.Equal(10026, (int)EventType.UnhideTerminalCommand);
        Assert.Equal(10027, (int)EventType.AddChainPuzzleToSecurityDoor);
        Assert.Equal(10028, (int)EventType.SetActiveEnemyWave);
        Assert.Equal(10029, (int)EventType.GiveResource);
        Assert.Equal(10030, (int)EventType.ForceResetChainPuzzle);
        Assert.Equal(20000, (int)EventType.NestedEvent);
        Assert.Equal(20001, (int)EventType.StartEventLoop);
        Assert.Equal(20002, (int)EventType.StopEventLoop);
        Assert.Equal(20003, (int)EventType.TeleportPlayer);
        Assert.Equal(20004, (int)EventType.InfectPlayer);
        Assert.Equal(20005, (int)EventType.DamagePlayer);
        Assert.Equal(20006, (int)EventType.RevivePlayer);
        Assert.Equal(20007, (int)EventType.AdjustAWOTimer);
        Assert.Equal(20008, (int)EventType.Countup);
        Assert.Equal(20009, (int)EventType.ForceCompleteChainPuzzle);
        Assert.Equal(20010, (int)EventType.SpawnNavMarker);
        Assert.Equal(20011, (int)EventType.ShakeScreen);
        Assert.Equal(20012, (int)EventType.StartPortalMachine);
        Assert.Equal(20013, (int)EventType.SetSuccessScreen);
        Assert.Equal(20014, (int)EventType.PlaySubtitles);
        Assert.Equal(20015, (int)EventType.MultiProgression);
        Assert.Equal(20016, (int)EventType.PlayWaveRoarSound);
        Assert.Equal(20017, (int)EventType.CustomHudText);
        Assert.Equal(20018, (int)EventType.SpecialHudTimer);
        Assert.Equal(20019, (int)EventType.ForcePlayPlayerDialogue);
        Assert.Equal(20020, (int)EventType.SetTerminalLog);
        Assert.Equal(20021, (int)EventType.SetPocketItem);
        Assert.Equal(20022, (int)EventType.DoInteractWeakDoorsInZone);
        Assert.Equal(20023, (int)EventType.ToggleInteractWeakDoorsInZone);
        Assert.Equal(20024, (int)EventType.PickupSentries);
        Assert.Equal(20026, (int)EventType.SetExpeditionEnvironment);
        Assert.Equal(20027, (int)EventType.ClearWardenIntelQueue);
        Assert.Equal(20028, (int)EventType.ResetOnDoorApproach);
        Assert.Equal(20029, (int)EventType.FadeScreenInOut);
    }

    [Fact]
    public void WardenEvents_AwoCustomFields_ExportAndImportRoundTrip()
    {
        var textMap = new Dictionary<uint, string>();
        uint nextTextId = 1000;
        Func<string?, string, uint> textSynth = (str, ctx) =>
        {
            if (string.IsNullOrEmpty(str)) return 0;
            uint id = nextTextId++;
            textMap[id] = str;
            return id;
        };

        var importer = new LevelLayoutDataBlockImporter();
        var importContext = new ImportContext(new MainWindowViewModel(), AppContext.BaseDirectory);
        foreach (var kvp in textMap)
        {
            importContext.TextBlocks[kvp.Key] = new JsonObject { ["Header"] = kvp.Value };
        }

        var events = new ObservableCollection<WardenEvent>
        {
            new()
            {
                Type = EventType.CloseSecurityDoor,
                Layer = SectorType.Main,
                Zone = 1,
                CleanUpEnemiesBehind = true
            },
            new()
            {
                Type = EventType.LockSecurityDoor,
                Layer = SectorType.Secondary,
                Zone = 2,
                SpecialText = "Access Denied by Warden."
            },
            new()
            {
                Type = EventType.SetDoorInteraction,
                Layer = SectorType.Overload,
                Zone = 3,
                SpecialText = "Manual Override Required."
            },
            new()
            {
                Type = EventType.ModifyReactorWaveState,
                Layer = SectorType.Main,
                Reactor = new AwoReactorData
                {
                    State = AwoReactorState.Wave,
                    Wave = 3,
                    Progress = 0.75
                }
            },
            new()
            {
                Type = EventType.Countdown,
                Duration = 120.0,
                Countdown = new AwoCountdownData
                {
                    CanShowHours = false,
                    TitleText = "REACTOR MELTDOWN",
                    TimerColor = "yellow",
                    EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent>
                    {
                        new()
                        {
                            Progress = 0.5,
                            Events = new ObservableCollection<WardenEvent>
                            {
                                new() { Type = EventType.PlaySound, SoundID = 55555 }
                            }
                        }
                    },
                    EventsOnDone = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.ForceFailLevel }
                    }
                }
            },
            new()
            {
                Type = EventType.SetLightDataInZone,
                Layer = SectorType.Main,
                Zone = 0,
                SetZoneLight = new AwoSetZoneLightData
                {
                    Type = AwoZoneLightType.TransitionToOriginal,
                    LightDataID = 69,
                    TransitionDuration = 2.5,
                    Seed = 42
                }
            },
            new()
            {
                Type = EventType.AlertEnemiesInZone,
                Layer = SectorType.Secondary,
                Zone = 4,
                SpecialNumber = 100
            },
            new()
            {
                Type = EventType.CleanUpEnemiesInZone,
                Layer = SectorType.Main,
                Zone = 2,
                CleanupEnemies = new AwoCleanupEnemiesData
                {
                    Type = AwoCleanupType.Kill,
                    AreaIndex = 1,
                    AreaBlacklist = new ObservableCollection<AwoAreaItem>
                    {
                        new() { AreaIndex = 2 },
                        new() { AreaIndex = 3 }
                    },
                    IncludeHibernate = true,
                    IncludeAggressive = false,
                    IncludeScout = true,
                    ExcludeEnemyIDs = new ObservableCollection<AwoEnemyIdItem> { new() { EnemyID = 101 } },
                    IncludeOnlyIDs = new ObservableCollection<AwoEnemyIdItem> { new() { EnemyID = 102 }, new() { EnemyID = 103 } }
                }
            },
            new()
            {
                Type = EventType.SpawnHibernateInZone,
                Layer = SectorType.Main,
                Zone = 5,
                Count = 4,
                Enabled = true,
                WorldEventObjectFilter = "hib_filter",
                SpawnHibernates = new AwoSpawnHibernatesData
                {
                    AreaIndex = 0,
                    AreaBlacklist = new ObservableCollection<AwoAreaItem> { new() { AreaIndex = 1 } },
                    EnemyID = 200,
                    PlacementRandomWeight = 0.5,
                    PlacementScore = 4.0,
                    ResetPlacementInfo = true,
                    Position = new Vector3Data(10, 20, 30),
                    Rotation = new Vector3Data(0, 90, 0),
                    RandomizeRotation = true
                }
            },
            new()
            {
                Type = EventType.SpawnScoutInZone,
                Layer = SectorType.Secondary,
                Zone = 1,
                Count = 2,
                Position = new Vector3Data(5, 15, 25),
                WorldEventObjectFilter = "scout_filter",
                SpawnScouts = new AwoSpawnScoutsData
                {
                    AreaIndex = -1,
                    AreaBlacklist = new ObservableCollection<AwoAreaItem> { new() { AreaIndex = 3 } },
                    ScoutType = ScoutType.Shadow_Scout
                }
            },
            new()
            {
                Type = EventType.AddTerminalCommand,
                Layer = SectorType.Main,
                Zone = 0,
                AddTerminalCommand = new AwoAddTerminalCommandData
                {
                    TerminalIndex = 2,
                    CommandNumber = 1234,
                    Command = "OVERRIDE_SECURITY",
                    CommandDesc = "Disables lockdown.",
                    AutoIndentCommandDesc = true,
                    ProgressWaitBeforeEvents = true,
                    SpecialCommandRule = SpecialCommandRule.OnlyOnceDelete,
                    PostCommandOutputs = new ObservableCollection<TerminalPostCommandOutput>
                    {
                        new() { LineType = TerminalLineType.Warning, Time = 1.0, Output = "Overriding..." }
                    },
                    CommandEvents = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.UnlockSecurityDoor, Layer = SectorType.Main, Zone = 1 }
                    }
                }
            },
            new()
            {
                Type = EventType.HideTerminalCommand,
                Layer = SectorType.Main,
                Zone = 0,
                HideTerminalCommand = new AwoHideTerminalCommandData
                {
                    TerminalIndex = 1,
                    CommandNumber = 1234,
                    CommandEnum = TERM_Command.None,
                    DeleteCommand = true
                }
            },
            new()
            {
                Type = EventType.UnhideTerminalCommand,
                Layer = SectorType.Main,
                Zone = 0,
                UnhideTerminalCommand = new AwoUnhideTerminalCommandData
                {
                    TerminalIndex = 1,
                    CommandNumber = 0,
                    CommandEnum = TERM_Command.Override
                }
            },
            new()
            {
                Type = EventType.AddChainPuzzleToSecurityDoor,
                Layer = SectorType.Main,
                Zone = 3,
                ChainPuzzle = 139
            },
            new()
            {
                Type = EventType.SetActiveEnemyWave,
                Layer = SectorType.Main,
                Zone = 2,
                ActiveEnemyWave = new AwoActiveEnemyWaveData
                {
                    HasActiveEnemyWave = true,
                    EnemyGroupInfrontOfDoor = 10,
                    EnemyGroupInArea = 20,
                    EnemyGroupsInArea = 30
                }
            },
            new()
            {
                Type = EventType.GiveResource,
                GiveResource = new AwoGiveResourceData
                {
                    Health = 25.0,
                    MainAmmo = 50.0,
                    SpecialAmmo = 40.0,
                    ToolAmmo = 30.0,
                    PlayerWoods = true,
                    PlayerDauda = false,
                    PlayerHackett = true,
                    PlayerBishop = false,
                    FullTeamOverflow = false,
                    UseLocation = true,
                    IncludeSupplyEfficiency = true
                }
            },
            new()
            {
                Type = EventType.ForceResetChainPuzzle,
                Layer = SectorType.Main,
                Zone = 4,
                ChainPuzzle = 145,
                Enabled = true
            }
        };

        // Export
        var exportedJson = WardenObjectiveDataBlockExporter.SerializeWardenEvents(events, textSynth);
        Assert.NotNull(exportedJson);
        Assert.Equal(events.Count, exportedJson.Count);

        // Populate text dictionary for importer
        foreach (var kvp in textMap)
        {
            importContext.TextBlocks[kvp.Key] = new JsonObject { ["English"] = kvp.Value };
        }

        // Import
        var importedEvents = new ObservableCollection<WardenEvent>();
        LevelLayoutDataBlockImporter.ImportWardenEvents(exportedJson, importedEvents, importContext);

        Assert.Equal(events.Count, importedEvents.Count);

        // Verify CloseSecurityDoor
        var ev0 = importedEvents[0];
        Assert.Equal(EventType.CloseSecurityDoor, ev0.Type);
        Assert.True(ev0.CleanUpEnemiesBehind);

        // Verify LockSecurityDoor
        var ev1 = importedEvents[1];
        Assert.Equal(EventType.LockSecurityDoor, ev1.Type);
        Assert.Equal("Access Denied by Warden.", ev1.SpecialText);

        // Verify SetDoorInteraction
        var ev2 = importedEvents[2];
        Assert.Equal(EventType.SetDoorInteraction, ev2.Type);
        Assert.Equal("Manual Override Required.", ev2.SpecialText);

        // Verify ModifyReactorWaveState
        var ev3 = importedEvents[3];
        Assert.Equal(EventType.ModifyReactorWaveState, ev3.Type);
        Assert.Equal(AwoReactorState.Wave, ev3.Reactor.State);
        Assert.Equal(3, ev3.Reactor.Wave);
        Assert.Equal(0.75, ev3.Reactor.Progress);

        // Verify Countdown
        var ev4 = importedEvents[4];
        Assert.Equal(EventType.Countdown, ev4.Type);
        Assert.False(ev4.Countdown.CanShowHours);
        Assert.Equal("REACTOR MELTDOWN", ev4.Countdown.TitleText);
        Assert.Equal("yellow", ev4.Countdown.TimerColor);
        Assert.Single(ev4.Countdown.EventsOnProgress);
        Assert.Equal(0.5, ev4.Countdown.EventsOnProgress[0].Progress);
        Assert.Single(ev4.Countdown.EventsOnProgress[0].Events);
        Assert.Equal(EventType.PlaySound, ev4.Countdown.EventsOnProgress[0].Events[0].Type);
        Assert.Single(ev4.Countdown.EventsOnDone);
        Assert.Equal(EventType.ForceFailLevel, ev4.Countdown.EventsOnDone[0].Type);

        // Verify SetZoneLight
        var ev5 = importedEvents[5];
        Assert.Equal(EventType.SetLightDataInZone, ev5.Type);
        Assert.Equal(AwoZoneLightType.TransitionToOriginal, ev5.SetZoneLight.Type);
        Assert.Equal(69u, ev5.SetZoneLight.LightDataID);
        Assert.Equal(2.5, ev5.SetZoneLight.TransitionDuration);
        Assert.Equal(42, ev5.SetZoneLight.Seed);

        // Verify AlertEnemiesInZone
        var ev6 = importedEvents[6];
        Assert.Equal(EventType.AlertEnemiesInZone, ev6.Type);
        Assert.Equal(100, ev6.SpecialNumber);

        // Verify CleanUpEnemiesInZone
        var ev7 = importedEvents[7];
        Assert.Equal(EventType.CleanUpEnemiesInZone, ev7.Type);
        Assert.Equal(AwoCleanupType.Kill, ev7.CleanupEnemies.Type);
        Assert.Equal(1, ev7.CleanupEnemies.AreaIndex);
        Assert.Equal(2, ev7.CleanupEnemies.AreaBlacklist.Count);
        Assert.Equal(2, ev7.CleanupEnemies.AreaBlacklist[0].AreaIndex);
        Assert.Equal(3, ev7.CleanupEnemies.AreaBlacklist[1].AreaIndex);
        Assert.True(ev7.CleanupEnemies.IncludeHibernate);
        Assert.False(ev7.CleanupEnemies.IncludeAggressive);
        Assert.True(ev7.CleanupEnemies.IncludeScout);
        Assert.Single(ev7.CleanupEnemies.ExcludeEnemyIDs);
        Assert.Equal(101u, ev7.CleanupEnemies.ExcludeEnemyIDs[0].EnemyID);
        Assert.Equal(2, ev7.CleanupEnemies.IncludeOnlyIDs.Count);

        // Verify SpawnHibernates
        var ev8 = importedEvents[8];
        Assert.Equal(EventType.SpawnHibernateInZone, ev8.Type);
        Assert.Equal(4, ev8.Count);
        Assert.Equal("hib_filter", ev8.WorldEventObjectFilter);
        Assert.Equal(0, ev8.SpawnHibernates.AreaIndex);
        Assert.Equal(200u, ev8.SpawnHibernates.EnemyID);
        Assert.Equal(0.5, ev8.SpawnHibernates.PlacementRandomWeight);
        Assert.Equal(4.0, ev8.SpawnHibernates.PlacementScore);
        Assert.True(ev8.SpawnHibernates.ResetPlacementInfo);
        Assert.True(ev8.SpawnHibernates.RandomizeRotation);
        Assert.Equal(10, ev8.SpawnHibernates.Position.X);
        Assert.Equal(90, ev8.SpawnHibernates.Rotation.Y);

        // Verify SpawnScouts
        var ev9 = importedEvents[9];
        Assert.Equal(EventType.SpawnScoutInZone, ev9.Type);
        Assert.Equal(-1, ev9.SpawnScouts.AreaIndex);
        Assert.Equal(3, ev9.SpawnScouts.GroupType);
        Assert.Equal(4, ev9.SpawnScouts.Difficulty);
        Assert.Equal(ScoutType.Shadow_Scout, ev9.SpawnScouts.ScoutType);
        Assert.Equal(5, ev9.Position.X);

        // Verify AddTerminalCommand
        var ev10 = importedEvents[10];
        Assert.Equal(EventType.AddTerminalCommand, ev10.Type);
        Assert.Equal(2, ev10.AddTerminalCommand.TerminalIndex);
        Assert.Equal(1234, ev10.AddTerminalCommand.CommandNumber);
        Assert.Equal("OVERRIDE_SECURITY", ev10.AddTerminalCommand.Command);
        Assert.Equal("Disables lockdown.", ev10.AddTerminalCommand.CommandDesc);
        Assert.True(ev10.AddTerminalCommand.AutoIndentCommandDesc);
        Assert.True(ev10.AddTerminalCommand.ProgressWaitBeforeEvents);
        Assert.Equal(SpecialCommandRule.OnlyOnceDelete, ev10.AddTerminalCommand.SpecialCommandRule);
        Assert.Single(ev10.AddTerminalCommand.PostCommandOutputs);
        Assert.Equal("Overriding...", ev10.AddTerminalCommand.PostCommandOutputs[0].Output);
        Assert.Single(ev10.AddTerminalCommand.CommandEvents);
        Assert.Equal(EventType.UnlockSecurityDoor, ev10.AddTerminalCommand.CommandEvents[0].Type);

        // Verify HideTerminalCommand
        var ev11 = importedEvents[11];
        Assert.Equal(EventType.HideTerminalCommand, ev11.Type);
        Assert.Equal(1, ev11.HideTerminalCommand.TerminalIndex);
        Assert.Equal(1234, ev11.HideTerminalCommand.CommandNumber);
        Assert.True(ev11.HideTerminalCommand.DeleteCommand);

        // Verify UnhideTerminalCommand
        var ev12 = importedEvents[12];
        Assert.Equal(EventType.UnhideTerminalCommand, ev12.Type);
        Assert.Equal(1, ev12.UnhideTerminalCommand.TerminalIndex);
        Assert.Equal(TERM_Command.Override, ev12.UnhideTerminalCommand.CommandEnum);

        // Verify AddChainPuzzleToSecurityDoor
        var ev13 = importedEvents[13];
        Assert.Equal(EventType.AddChainPuzzleToSecurityDoor, ev13.Type);
        Assert.Equal(139u, ev13.ChainPuzzle);

        // Verify SetActiveEnemyWave
        var ev14 = importedEvents[14];
        Assert.Equal(EventType.SetActiveEnemyWave, ev14.Type);
        Assert.True(ev14.ActiveEnemyWave.HasActiveEnemyWave);
        Assert.Equal(10u, ev14.ActiveEnemyWave.EnemyGroupInfrontOfDoor);
        Assert.Equal(20u, ev14.ActiveEnemyWave.EnemyGroupInArea);
        Assert.Equal(30u, ev14.ActiveEnemyWave.EnemyGroupsInArea);

        // Verify GiveResource
        var ev15 = importedEvents[15];
        Assert.Equal(EventType.GiveResource, ev15.Type);
        Assert.Equal(25.0, ev15.GiveResource.Health);
        Assert.Equal(50.0, ev15.GiveResource.MainAmmo);
        Assert.Equal(40.0, ev15.GiveResource.SpecialAmmo);
        Assert.Equal(30.0, ev15.GiveResource.ToolAmmo);
        Assert.True(ev15.GiveResource.PlayerWoods);
        Assert.False(ev15.GiveResource.PlayerDauda);
        Assert.True(ev15.GiveResource.PlayerHackett);
        Assert.False(ev15.GiveResource.PlayerBishop);
        Assert.False(ev15.GiveResource.FullTeamOverflow);
        Assert.True(ev15.GiveResource.UseLocation);
        Assert.True(ev15.GiveResource.IncludeSupplyEfficiency);

        // Verify ForceResetChainPuzzle
        var ev16 = importedEvents[16];
        Assert.Equal(EventType.ForceResetChainPuzzle, ev16.Type);
        Assert.Equal(145u, ev16.ChainPuzzle);
        Assert.True(ev16.Enabled);
    }

    [Fact]
    public void WardenEvents_TerminalCommandContext_UsesSpecialNumberForChainPuzzle()
    {
        var textSynth = new Func<string?, string, uint>((s, c) => 0);

        var events = new ObservableCollection<WardenEvent>
        {
            new()
            {
                Type = EventType.AddChainPuzzleToSecurityDoor,
                ChainPuzzle = 777
            },
            new()
            {
                Type = EventType.ForceResetChainPuzzle,
                ChainPuzzle = 888
            }
        };

        // When exported inside terminal command
        var terminalEventsJson = WardenObjectiveDataBlockExporter.SerializeWardenEvents(events, textSynth, isTerminalCommand: true);
        Assert.Equal(777, terminalEventsJson[0]?["SpecialNumber"]?.GetValue<int>());
        Assert.Equal(0, terminalEventsJson[0]?["ChainPuzzle"]?.GetValue<int>());

        Assert.Equal(888, terminalEventsJson[1]?["SpecialNumber"]?.GetValue<int>());
        Assert.Equal(0, terminalEventsJson[1]?["ChainPuzzle"]?.GetValue<int>());

        // Import back
        var importedEvents = new ObservableCollection<WardenEvent>();
        LevelLayoutDataBlockImporter.ImportWardenEvents(terminalEventsJson, importedEvents, new ImportContext(new MainWindowViewModel(), AppContext.BaseDirectory));

        Assert.Equal(777u, importedEvents[0].ChainPuzzle);
        Assert.Equal(888u, importedEvents[1].ChainPuzzle);
    }

    [Fact]
    public void WardenEvent_Clone_CreatesDeepCopyOfAwoStructures()
    {
        var original = new WardenEvent
        {
            Type = EventType.Countdown,
            CleanUpEnemiesBehind = true,
            SpecialText = "Text",
            SpecialNumber = 99,
            Reactor = new AwoReactorData { State = AwoReactorState.Verify, Wave = 2, Progress = 0.5 },
            Countdown = new AwoCountdownData
            {
                TitleText = "Timer",
                EventsOnDone = new ObservableCollection<WardenEvent> { new() { Type = EventType.KillAllPlayers } }
            },
            SetZoneLight = new AwoSetZoneLightData { LightDataID = 10 },
            CleanupEnemies = new AwoCleanupEnemiesData { AreaBlacklist = new ObservableCollection<AwoAreaItem> { new() { AreaIndex = 5 } } },
            SpawnHibernates = new AwoSpawnHibernatesData { EnemyID = 44 },
            SpawnScouts = new AwoSpawnScoutsData { ScoutType = ScoutType.Zoomer_Scout },
            AddTerminalCommand = new AwoAddTerminalCommandData { CommandNumber = 55 },
            HideTerminalCommand = new AwoHideTerminalCommandData { DeleteCommand = true },
            UnhideTerminalCommand = new AwoUnhideTerminalCommandData { CommandEnum = TERM_Command.Cls },
            ActiveEnemyWave = new AwoActiveEnemyWaveData { EnemyGroupInArea = 12 },
            GiveResource = new AwoGiveResourceData { Health = 100 },
            CameraShake = new AwoCameraShakeData { Radius = 10, Amplitude = 0.5, Frequency = 20, Directional = true }
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.NotSame(original.Reactor, clone.Reactor);
        Assert.NotSame(original.Countdown, clone.Countdown);
        Assert.NotSame(original.SetZoneLight, clone.SetZoneLight);
        Assert.NotSame(original.CleanupEnemies, clone.CleanupEnemies);
        Assert.NotSame(original.SpawnHibernates, clone.SpawnHibernates);
        Assert.NotSame(original.SpawnScouts, clone.SpawnScouts);
        Assert.NotSame(original.AddTerminalCommand, clone.AddTerminalCommand);
        Assert.NotSame(original.HideTerminalCommand, clone.HideTerminalCommand);
        Assert.NotSame(original.UnhideTerminalCommand, clone.UnhideTerminalCommand);
        Assert.NotSame(original.ActiveEnemyWave, clone.ActiveEnemyWave);
        Assert.NotSame(original.GiveResource, clone.GiveResource);
        Assert.NotSame(original.CameraShake, clone.CameraShake);

        Assert.Equal(AwoReactorState.Verify, clone.Reactor.State);
        Assert.Equal("Timer", clone.Countdown.TitleText);
        Assert.Single(clone.Countdown.EventsOnDone);
        Assert.Equal(EventType.KillAllPlayers, clone.Countdown.EventsOnDone[0].Type);
        Assert.Equal(10u, clone.SetZoneLight.LightDataID);
        Assert.Equal(5, clone.CleanupEnemies.AreaBlacklist[0].AreaIndex);
        Assert.Equal(44u, clone.SpawnHibernates.EnemyID);
        Assert.Equal(ScoutType.Zoomer_Scout, clone.SpawnScouts.ScoutType);
        Assert.Equal(3, clone.SpawnScouts.GroupType);
        Assert.Equal(5, clone.SpawnScouts.Difficulty);
        Assert.Equal(55, clone.AddTerminalCommand.CommandNumber);
        Assert.True(clone.HideTerminalCommand.DeleteCommand);
        Assert.Equal(TERM_Command.Cls, clone.UnhideTerminalCommand.CommandEnum);
        Assert.Equal(12u, clone.ActiveEnemyWave.EnemyGroupInArea);
        Assert.Equal(100.0, clone.GiveResource.Health);
        Assert.Equal(10.0, clone.CameraShake.Radius);
        Assert.Equal(0.5, clone.CameraShake.Amplitude);
        Assert.Equal(20.0, clone.CameraShake.Frequency);
        Assert.True(clone.CameraShake.Directional);
    }

    [Fact]
    public void ScoutType_Mapping_MatchesExpectedGroupTypeAndDifficulty()
    {
        Assert.Equal((3, 0), ScoutType.Scout.ToGroupTypeAndDifficulty());
        Assert.Equal((3, 3), ScoutType.Charger_Scout.ToGroupTypeAndDifficulty());
        Assert.Equal((3, 4), ScoutType.Shadow_Scout.ToGroupTypeAndDifficulty());
        Assert.Equal((3, 5), ScoutType.Zoomer_Scout.ToGroupTypeAndDifficulty());
        Assert.Equal((3, 6), ScoutType.Nightmare_Scout.ToGroupTypeAndDifficulty());

        Assert.Equal(ScoutType.Scout, ScoutTypeExtensions.FromGroupTypeAndDifficulty(3, 0));
        Assert.Equal(ScoutType.Charger_Scout, ScoutTypeExtensions.FromGroupTypeAndDifficulty(3, 3));
        Assert.Equal(ScoutType.Shadow_Scout, ScoutTypeExtensions.FromGroupTypeAndDifficulty(3, 4));
        Assert.Equal(ScoutType.Zoomer_Scout, ScoutTypeExtensions.FromGroupTypeAndDifficulty(3, 5));
        Assert.Equal(ScoutType.Nightmare_Scout, ScoutTypeExtensions.FromGroupTypeAndDifficulty(3, 6));

        // Test property synchronization
        var scoutData = new AwoSpawnScoutsData();
        scoutData.ScoutType = ScoutType.Nightmare_Scout;
        Assert.Equal(3, scoutData.GroupType);
        Assert.Equal(6, scoutData.Difficulty);

        scoutData.GroupType = 3;
        scoutData.Difficulty = 3;
        Assert.Equal(ScoutType.Charger_Scout, scoutData.ScoutType);
    }

    [Fact]
    public void LevelLayoutViewModel_AvailableScoutTypes_ContainsAllOptions()
    {
        var options = LevelLayoutViewModel.AvailableScoutTypes;
        Assert.Equal(5, options.Length);
        Assert.Contains(options, o => o.Type == ScoutType.Scout && o.DisplayName == "Scout");
        Assert.Contains(options, o => o.Type == ScoutType.Charger_Scout && o.DisplayName == "Charger Scout");
        Assert.Contains(options, o => o.Type == ScoutType.Shadow_Scout && o.DisplayName == "Shadow Scout");
        Assert.Contains(options, o => o.Type == ScoutType.Zoomer_Scout && o.DisplayName == "Zoomer Scout");
        Assert.Contains(options, o => o.Type == ScoutType.Nightmare_Scout && o.DisplayName == "Nightmare Scout");
    }

    [Fact]
    public void EventTypeColorConverter_ReturnsLightBlueForAwoEvents_AndDefaultForVanilla()
    {
        var converter = new GTFO_GUIEditor.Converters.EventTypeColorConverter();

        // Vanilla events should use default brush
        var vanillaBrush = (Avalonia.Media.ISolidColorBrush)converter.Convert(EventType.None, typeof(Avalonia.Media.IBrush), null, System.Globalization.CultureInfo.InvariantCulture)!;
        var unlockBrush = (Avalonia.Media.ISolidColorBrush)converter.Convert(EventType.UnlockSecurityDoor, typeof(Avalonia.Media.IBrush), null, System.Globalization.CultureInfo.InvariantCulture)!;
        Assert.Equal(vanillaBrush.Color, unlockBrush.Color);

        // AWO events should use light blue brush (#87CEEB)
        foreach (var evType in Enum.GetValues<EventType>().Where(e => (int)e >= 10000))
        {
            var awoBrush = (Avalonia.Media.ISolidColorBrush)converter.Convert(evType, typeof(Avalonia.Media.IBrush), null, System.Globalization.CultureInfo.InvariantCulture)!;
            Assert.NotEqual(vanillaBrush.Color, awoBrush.Color);
            Assert.Equal(Avalonia.Media.Color.Parse("#87CEEB"), awoBrush.Color);
        }
    }

    [Fact]
    public void ModManager_AwoFeatureLocking_PreventsDisablingWhenEventsExist()
    {
        ModManager.Instance.Reset();
        ModManager.Instance.AdvancedWardenObjective = true;

        var vm = new LevelLayoutViewModel();
        Assert.True(ModManager.Instance.AdvancedWardenObjective);

        // When no AWO events exist, can disable
        Assert.True(ModManager.Instance.CanDisableMod(ModManager.AdvancedWardenObjectiveModId));

        // Add an AWO event to the active level's main sector
        vm.SelectedLevel!.MainSector.EventsOnActivate.Add(new WardenEvent
        {
            Type = EventType.CloseSecurityDoor,
            CleanUpEnemiesBehind = true
        });

        // Now cannot disable
        Assert.False(ModManager.Instance.CanDisableMod(ModManager.AdvancedWardenObjectiveModId));
        Assert.Throws<InvalidOperationException>(() => ModManager.Instance.AdvancedWardenObjective = false);

        // Remove the event
        vm.SelectedLevel.MainSector.EventsOnActivate.Clear();

        // Now can disable
        Assert.True(ModManager.Instance.CanDisableMod(ModManager.AdvancedWardenObjectiveModId));
        ModManager.Instance.AdvancedWardenObjective = false;
        Assert.False(ModManager.Instance.AdvancedWardenObjective);
    }

    [Fact]
    public void LevelLayoutViewModel_RemoveWardenEvent_RemovesNestedCountdownAndTerminalEvents()
    {
        var vm = new LevelLayoutViewModel();
        var door = new PlacedDoorNode();
        vm.EditingDoor = door;

        var nestedProgressEvent = new WardenEvent { Type = EventType.KillAllPlayers };
        var nestedDoneEvent = new WardenEvent { Type = EventType.SolveSecurityDoorAlarm };
        var progressContainer = new AwoCountdownProgressEvent { Progress = 0.5 };
        progressContainer.Events.Add(nestedProgressEvent);

        var countdownEvent = new WardenEvent
        {
            Type = EventType.Countdown,
            Countdown = new AwoCountdownData
            {
                EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent> { progressContainer },
                EventsOnDone = new ObservableCollection<WardenEvent> { nestedDoneEvent }
            }
        };

        door.EventsOnUnlockDoor.Add(countdownEvent);

        Assert.Single(progressContainer.Events);
        Assert.Single(countdownEvent.Countdown.EventsOnDone);

        // Remove nested progress event
        vm.RemoveWardenEvent(nestedProgressEvent);
        Assert.Empty(progressContainer.Events);

        // Remove nested done event
        vm.RemoveWardenEvent(nestedDoneEvent);
        Assert.Empty(countdownEvent.Countdown.EventsOnDone);

        // Remove countdown event itself
        vm.RemoveWardenEvent(countdownEvent);
        Assert.Empty(door.EventsOnUnlockDoor);
    }

    [Fact]
    public void AwoEventModels_SubListRemovalMethods_WorkCorrectly()
    {
        // Countdown
        var countdown = new AwoCountdownData();
        var progressItem = new AwoCountdownProgressEvent();
        var doneEvent = new WardenEvent();
        countdown.EventsOnProgress.Add(progressItem);
        countdown.EventsOnDone.Add(doneEvent);
        countdown.RemoveProgressEventCommand.Execute(progressItem);
        countdown.RemoveDoneEventCommand.Execute(doneEvent);
        Assert.Empty(countdown.EventsOnProgress);
        Assert.Empty(countdown.EventsOnDone);

        // CountdownProgressEvent
        var progressContainer = new AwoCountdownProgressEvent();
        var nestedEv = new WardenEvent();
        progressContainer.Events.Add(nestedEv);
        progressContainer.RemoveEventCommand.Execute(nestedEv);
        Assert.Empty(progressContainer.Events);

        // CleanupEnemies
        var cleanup = new AwoCleanupEnemiesData();
        var areaItem = new AwoAreaItem { AreaIndex = 1 };
        var excludeEnemy = new AwoEnemyIdItem { EnemyID = 10 };
        var includeEnemy = new AwoEnemyIdItem { EnemyID = 20 };
        cleanup.AreaBlacklist.Add(areaItem);
        cleanup.ExcludeEnemyIDs.Add(excludeEnemy);
        cleanup.IncludeOnlyIDs.Add(includeEnemy);
        cleanup.RemoveAreaBlacklistCommand.Execute(areaItem);
        cleanup.RemoveExcludeEnemyCommand.Execute(excludeEnemy);
        cleanup.RemoveIncludeOnlyEnemyCommand.Execute(includeEnemy);
        Assert.Empty(cleanup.AreaBlacklist);
        Assert.Empty(cleanup.ExcludeEnemyIDs);
        Assert.Empty(cleanup.IncludeOnlyIDs);

        // SpawnHibernates
        var hibernates = new AwoSpawnHibernatesData();
        var hibArea = new AwoAreaItem { AreaIndex = 2 };
        hibernates.AreaBlacklist.Add(hibArea);
        hibernates.RemoveAreaBlacklistCommand.Execute(hibArea);
        Assert.Empty(hibernates.AreaBlacklist);

        // SpawnScouts
        var scouts = new AwoSpawnScoutsData();
        var scoutArea = new AwoAreaItem { AreaIndex = 3 };
        scouts.AreaBlacklist.Add(scoutArea);
        scouts.RemoveAreaBlacklistCommand.Execute(scoutArea);
        Assert.Empty(scouts.AreaBlacklist);

        // AddTerminalCommand
        var addCmd = new AwoAddTerminalCommandData();
        var postOutput = new TerminalPostCommandOutput { Output = "Test" };
        var cmdEvent = new WardenEvent { Type = EventType.KillPlayersInZone };
        addCmd.PostCommandOutputs.Add(postOutput);
        addCmd.CommandEvents.Add(cmdEvent);
        addCmd.RemovePostCommandOutputCommand.Execute(postOutput);
        addCmd.RemoveCommandEventCommand.Execute(cmdEvent);
        Assert.Empty(addCmd.PostCommandOutputs);
        Assert.Empty(addCmd.CommandEvents);
    }

    [Fact]
    public void WardenEvents_Awo20kCustomFields_ExportAndImportRoundTrip()
    {
        var textMap = new Dictionary<uint, string>();
        uint nextTextId = 2000;
        Func<string?, string, uint> textSynth = (str, ctx) =>
        {
            if (string.IsNullOrEmpty(str)) return 0;
            uint id = nextTextId++;
            textMap[id] = str;
            return id;
        };

        var importer = new LevelLayoutDataBlockImporter();
        var importContext = new ImportContext(new MainWindowViewModel(), AppContext.BaseDirectory);

        var events = new ObservableCollection<WardenEvent>
        {
            new()
            {
                Type = EventType.NestedEvent,
                NestedEvent = new AwoNestedEventData
                {
                    Type = AwoNestedEventType.RandomWeighted,
                    MaxRandomEvents = 2,
                    AllowRepeatsInRandom = true,
                    PlayerWoods = true,
                    PlayerDauda = false,
                    PlayerHackett = true,
                    PlayerBishop = false,
                    EventsToActivate = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.StopEventLoop, Count = 3 }
                    },
                    WheelOfEvents = new ObservableCollection<AwoWheelOfEventItem>
                    {
                        new()
                        {
                            DebugName = "Wheel1",
                            Weight = 0.5,
                            RepeatCount = 2,
                            IsInfinite = false,
                            Events = new ObservableCollection<WardenEvent>
                            {
                                new() { Type = EventType.RevivePlayer, RevivePlayer = new AwoRevivePlayerData { PlayerWoods = true } }
                            }
                        }
                    }
                }
            },
            new()
            {
                Type = EventType.StartEventLoop,
                EventLoop = new AwoEventLoopData
                {
                    LoopIndex = 5,
                    LoopDelay = 2.5,
                    LoopCount = 10,
                    EventsToActivate = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.PickupSentries }
                    }
                }
            },
            new()
            {
                Type = EventType.StopEventLoop,
                Count = 5
            },
            new()
            {
                Type = EventType.TeleportPlayer,
                Dimension = "Reality",
                Duration = 2.0,
                Position = new Vector3Data(10, 20, 30),
                WorldEventObjectFilter = "TP_Filter",
                TeleportPlayer = new AwoTeleportPlayerData
                {
                    FlashTeleport = true,
                    WarpSentries = false,
                    WarpBigPickups = true,
                    SendBigPickupsToHost = true,
                    FullTeamOverflow = true,
                    FromLocation = new AwoTeleportFromLocationData
                    {
                        Enabled = true,
                        DimensionIndex = 1,
                        Layer = SectorType.Secondary,
                        LocalIndex = 3
                    },
                    TPData = new ObservableCollection<AwoTPDataItem>
                    {
                        new()
                        {
                            PlayerIndex = 0,
                            DimensionIndex = 2,
                            Position = new Vector3Data(1, 2, 3),
                            WorldEventObjectFilter = "P0_Filter",
                            UseLookDirectionVector = true,
                            LookDirectionV3 = new Vector3Data(0, 1, 0),
                            PlayWarpAnimation = true,
                            FlashDuration = 1.5
                        },
                        new()
                        {
                            PlayerIndex = 1,
                            DimensionIndex = 0,
                            Position = new Vector3Data(4, 5, 6),
                            WorldEventObjectFilter = "P1_Filter",
                            UseLookDirectionVector = false,
                            LookDirection = AwoTeleportLookDirection.Backward,
                            PlayWarpAnimation = false,
                            FlashDuration = 0.5
                        }
                    }
                }
            },
            new()
            {
                Type = EventType.InfectPlayer,
                InfectPlayer = new AwoInfectPlayerData
                {
                    PlayerWoods = true,
                    PlayerDauda = true,
                    PlayerHackett = false,
                    PlayerBishop = false,
                    FullTeamOverflow = false,
                    InfectionAmount = 0.25,
                    InfectOverTime = true,
                    Interval = 2.0,
                    UseZone = true
                }
            },
            new()
            {
                Type = EventType.DamagePlayer,
                DamagePlayer = new AwoDamagePlayerData
                {
                    PlayerWoods = false,
                    PlayerDauda = false,
                    PlayerHackett = true,
                    PlayerBishop = true,
                    FullTeamOverflow = true,
                    DamageAmount = 50.0,
                    DealPercentageDamage = true,
                    DamageOverTime = false,
                    Interval = 1.0,
                    UseZone = false
                }
            },
            new()
            {
                Type = EventType.RevivePlayer,
                RevivePlayer = new AwoRevivePlayerData
                {
                    PlayerWoods = true,
                    PlayerDauda = false,
                    PlayerHackett = false,
                    PlayerBishop = true,
                    FullTeamOverflow = true
                }
            },
            new()
            {
                Type = EventType.AdjustAWOTimer,
                AdjustTimer = new AwoAdjustTimerData
                {
                    Duration = 30.0,
                    Speed = 1.5,
                    UpdateTitleText = true,
                    TitleText = "NEW TIMER TITLE",
                    UpdateBodyText = true,
                    BodyText = "NEW TIMER BODY",
                    UpdateColor = true,
                    TimerColor = "blue",
                    UpdateSpecialHud = true,
                    SpecialHudIndex = 2,
                    SpecialHudTimeModifier = 1.2
                }
            },
            new()
            {
                Type = EventType.Countup,
                Countup = new AwoCountupData
                {
                    StartValue = 10.0,
                    Speed = 2.0,
                    TitleText = "COUNTUP TEST",
                    BodyText = "[COUNTUP TEST]",
                    TimerColor = "green",
                    DecimalPoints = 2,
                    EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent>
                    {
                        new()
                        {
                            Progress = 0.5,
                            Events = new ObservableCollection<WardenEvent>
                            {
                                new() { Type = EventType.PickupSentries }
                            }
                        }
                    },
                    EventsOnDone = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.ForceCompleteLevel }
                    }
                }
            },
            new()
            {
                Type = EventType.ForceCompleteChainPuzzle,
                ChainPuzzle = 456,
                Count = 2
            },
            new()
            {
                Type = EventType.SpawnNavMarker,
                Duration = 60.0,
                Count = 1,
                Enabled = true,
                WorldEventObjectFilter = "NAV_FILTER",
                NavMarker = new AwoNavMarkerData
                {
                    Index = 1,
                    Style = 12,
                    Title = "OBJECTIVE NAV",
                    Color = "#FF00FF",
                    UsePin = true
                }
            },
            new()
            {
                Type = EventType.ShakeScreen,
                Duration = 2.5,
                Position = new Vector3Data(100, 200, 300),
                WorldEventObjectFilter = "SHAKE_FILTER",
                CameraShake = new AwoCameraShakeData
                {
                    Radius = 15.0,
                    Amplitude = 0.8,
                    Frequency = 45.0,
                    Directional = true
                }
            },
            new()
            {
                Type = EventType.StartPortalMachine,
                Portal = new AwoPortalData
                {
                    TargetDimension = 2,
                    TeleportDelay = 3.5,
                    PreventPortalWarpTeamEvent = true
                }
            },
            new()
            {
                Type = EventType.SetSuccessScreen,
                SpecialText = "VICTORY ACHIEVED",
                SuccessScreen = new AwoSuccessScreenData
                {
                    HasSuccessScreen = true,
                    Type = AwoSuccessScreenType.FlashFakeScreen,
                    CustomSuccessScreen = 101,
                    OverrideMusic = 202
                }
            },
            new()
            {
                Type = EventType.PlaySubtitles,
                Duration = 4.0,
                SoundSubtitle = "Voice line subtitle here"
            },
            new()
            {
                Type = EventType.MultiProgression,
                Layer = SectorType.Main,
                MultiProgression = new AwoMultiProgressionData
                {
                    Items = new ObservableCollection<AwoMultiProgressionItem>
                    {
                        new()
                        {
                            CustomSubObjectiveHeader = "MP Header 1",
                            CustomSubObjective = "MP SubObj 1",
                            Index = 1,
                            Priority = 2,
                            Layer = SectorType.Secondary,
                            IsLayerIndependent = false,
                            OverrideTag = 77
                        }
                    }
                }
            },
            new()
            {
                Type = EventType.PlayWaveRoarSound,
                Position = new Vector3Data(10, 20, 30),
                WorldEventObjectFilter = "ROAR_FILTER",
                WaveRoarSound = new AwoWaveRoarSoundData
                {
                    RoarSound = AwoRoarSound.Tank,
                    RoarSize = AwoRoarSize.Big,
                    IsOutside = true
                }
            },
            new()
            {
                Type = EventType.CustomHudText,
                Enabled = true,
                CustomHud = new AwoCustomHudData
                {
                    TitleText = "HUD TITLE",
                    BodyText = "HUD BODY"
                }
            },
            new()
            {
                Type = EventType.SpecialHudTimer,
                Duration = 45.0,
                SpecialHud = new AwoSpecialHudData
                {
                    Type = AwoSpecialHudType.StartPersistent,
                    Index = 3,
                    Message = "SPECIAL HUD MSG",
                    Style = AwoSpecialHudStyle.BioscanAlarm,
                    Priority = -1,
                    ShowTimeInProgressBar = true,
                    InvertProgress = true,
                    EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent>
                    {
                        new()
                        {
                            Progress = 0.8,
                            Events = new ObservableCollection<WardenEvent>
                            {
                                new() { Type = EventType.PickupSentries }
                            }
                        }
                    },
                    EventsOnDone = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.ForceFailLevel }
                    }
                }
            },
            new()
            {
                Type = EventType.ForcePlayPlayerDialogue,
                Duration = 5.0,
                DialogueID = 999,
                Position = new Vector3Data(1, 2, 3),
                WorldEventObjectFilter = "DLG_FILTER",
                PlayerDialogue = new AwoPlayerDialogueData
                {
                    Type = AwoPlayerDialogueType.Specific,
                    CharacterID = 2,
                    IntensityState = AwoDialogueIntensityState.Combat
                }
            },
            new()
            {
                Type = EventType.SetTerminalLog,
                Layer = SectorType.Main,
                Zone = 0,
                SetTerminalLog = new AwoSetTerminalLogData
                {
                    TerminalIndex = 1,
                    Type = AwoTerminalLogType.Add,
                    FileName = "LOG_01.TXT",
                    FileContent = "Log file text body",
                    FileContentOriginalLanguage = 2,
                    AttachedAudioFile = 505,
                    AttachedAudioByteSize = 1024,
                    PlayerDialogToTriggerAfterAudio = 606,
                    EventsOnFileRead = new ObservableCollection<WardenEvent>
                    {
                        new() { Type = EventType.ClearWardenIntelQueue, SpecialBool = true }
                    }
                }
            },
            new()
            {
                Type = EventType.SetPocketItem,
                Position = new Vector3Data(5, 5, 5),
                WorldEventObjectFilter = "POCKET_FILTER",
                ObjectiveItems = new AwoPocketItemsData
                {
                    Items = new ObservableCollection<AwoPocketItem>
                    {
                        new()
                        {
                            Index = 0,
                            Count = 2,
                            IsOnTop = true,
                            ItemName = "BULKHEAD KEY",
                            TagType = AwoPocketItemTagType.Closest,
                            PlayerIndex = 1,
                            CustomTag = "KEY_TAG"
                        }
                    }
                }
            },
            new()
            {
                Type = EventType.DoInteractWeakDoorsInZone,
                Layer = SectorType.Secondary,
                Zone = 1,
                Enabled = true
            },
            new()
            {
                Type = EventType.ToggleInteractWeakDoorsInZone,
                Layer = SectorType.Secondary,
                Zone = 2,
                Enabled = false
            },
            new()
            {
                Type = EventType.PickupSentries
            },
            new()
            {
                Type = EventType.SetExpeditionEnvironment,
                Dimension = "Reality",
                EnvironmentData = new AwoEnvironmentData
                {
                    EnvironmentWetness = "Wet",
                    UpdateColor = true,
                    DustColor = "yellow",
                    DustAlphaBoost = "High",
                    DustTurbulence = "Low"
                }
            },
            new()
            {
                Type = EventType.ClearWardenIntelQueue,
                Enabled = true,
                SpecialBool = true
            },
            new()
            {
                Type = EventType.ResetOnDoorApproach,
                Layer = SectorType.Main,
                Zone = 3
            },
            new()
            {
                Type = EventType.FadeScreenInOut,
                FadeScreen = new AwoFadeScreenData
                {
                    FadeColor = "white",
                    CurveTimeStart = 0.2,
                    CurveValueStart = 0.1,
                    CurveTimeEnd = 0.9,
                    CurveValueEnd = 0.8,
                    FadeSpeed = 2.0
                }
            }
        };

        // Export
        var exportedArray = WardenObjectiveDataBlockExporter.SerializeWardenEvents(events, textSynth, isTerminalCommand: false);
        Assert.Equal(events.Count, exportedArray.Count);

        // Populate context text blocks
        foreach (var kvp in textMap)
        {
            importContext.TextBlocks[kvp.Key] = new JsonObject { ["English"] = kvp.Value };
        }

        // Import
        var importedEvents = new ObservableCollection<WardenEvent>();
        LevelLayoutDataBlockImporter.ImportWardenEvents(exportedArray, importedEvents, importContext);

        Assert.Equal(events.Count, importedEvents.Count);

        // NestedEvent assertions
        var impNested = importedEvents[0];
        Assert.Equal(EventType.NestedEvent, impNested.Type);
        Assert.Equal(AwoNestedEventType.RandomWeighted, impNested.NestedEvent.Type);
        Assert.Equal(2, impNested.NestedEvent.MaxRandomEvents);
        Assert.True(impNested.NestedEvent.AllowRepeatsInRandom);
        Assert.True(impNested.NestedEvent.PlayerWoods);
        Assert.False(impNested.NestedEvent.PlayerDauda);
        Assert.True(impNested.NestedEvent.PlayerHackett);
        Assert.False(impNested.NestedEvent.PlayerBishop);
        Assert.Single(impNested.NestedEvent.EventsToActivate);
        Assert.Single(impNested.NestedEvent.WheelOfEvents);
        Assert.Equal("Wheel1", impNested.NestedEvent.WheelOfEvents[0].DebugName);
        Assert.Equal(0.5, impNested.NestedEvent.WheelOfEvents[0].Weight);
        Assert.Single(impNested.NestedEvent.WheelOfEvents[0].Events);

        // EventLoop assertions
        var impLoop = importedEvents[1];
        Assert.Equal(EventType.StartEventLoop, impLoop.Type);
        Assert.Equal(5, impLoop.EventLoop.LoopIndex);
        Assert.Equal(2.5, impLoop.EventLoop.LoopDelay);
        Assert.Equal(10, impLoop.EventLoop.LoopCount);
        Assert.Single(impLoop.EventLoop.EventsToActivate);

        // StopEventLoop assertions
        var impStopLoop = importedEvents[2];
        Assert.Equal(EventType.StopEventLoop, impStopLoop.Type);
        Assert.Equal(5, impStopLoop.Count);

        // TeleportPlayer assertions
        var impTp = importedEvents[3];
        Assert.Equal(EventType.TeleportPlayer, impTp.Type);
        Assert.True(impTp.TeleportPlayer.FlashTeleport);
        Assert.False(impTp.TeleportPlayer.WarpSentries);
        Assert.True(impTp.TeleportPlayer.WarpBigPickups);
        Assert.True(impTp.TeleportPlayer.SendBigPickupsToHost);
        Assert.True(impTp.TeleportPlayer.FullTeamOverflow);
        Assert.True(impTp.TeleportPlayer.FromLocation.Enabled);
        Assert.Equal(1, impTp.TeleportPlayer.FromLocation.DimensionIndex);
        Assert.Equal(SectorType.Secondary, impTp.TeleportPlayer.FromLocation.Layer);
        Assert.Equal(3, impTp.TeleportPlayer.FromLocation.LocalIndex);
        Assert.Equal(2, impTp.TeleportPlayer.TPData.Count);
        Assert.True(impTp.TeleportPlayer.TPData[0].UseLookDirectionVector);
        Assert.Equal(1.0, impTp.TeleportPlayer.TPData[0].LookDirectionV3.Y);
        Assert.False(impTp.TeleportPlayer.TPData[1].UseLookDirectionVector);
        Assert.Equal(AwoTeleportLookDirection.Backward, impTp.TeleportPlayer.TPData[1].LookDirection);

        // InfectPlayer assertions
        var impInf = importedEvents[4];
        Assert.Equal(EventType.InfectPlayer, impInf.Type);
        Assert.True(impInf.InfectPlayer.PlayerWoods);
        Assert.False(impInf.InfectPlayer.PlayerHackett);
        Assert.Equal(0.25, impInf.InfectPlayer.InfectionAmount);
        Assert.True(impInf.InfectPlayer.InfectOverTime);
        Assert.Equal(2.0, impInf.InfectPlayer.Interval);
        Assert.True(impInf.InfectPlayer.UseZone);

        // DamagePlayer assertions
        var impDmg = importedEvents[5];
        Assert.Equal(EventType.DamagePlayer, impDmg.Type);
        Assert.False(impDmg.DamagePlayer.PlayerWoods);
        Assert.True(impDmg.DamagePlayer.PlayerHackett);
        Assert.Equal(50.0, impDmg.DamagePlayer.DamageAmount);
        Assert.True(impDmg.DamagePlayer.DealPercentageDamage);

        // RevivePlayer assertions
        var impRev = importedEvents[6];
        Assert.Equal(EventType.RevivePlayer, impRev.Type);
        Assert.True(impRev.RevivePlayer.PlayerWoods);
        Assert.False(impRev.RevivePlayer.PlayerDauda);

        // AdjustAWOTimer assertions
        var impAdj = importedEvents[7];
        Assert.Equal(EventType.AdjustAWOTimer, impAdj.Type);
        Assert.Equal(30.0, impAdj.AdjustTimer.Duration);
        Assert.Equal("NEW TIMER TITLE", impAdj.AdjustTimer.TitleText);
        Assert.Equal("NEW TIMER BODY", impAdj.AdjustTimer.BodyText);
        Assert.Equal("blue", impAdj.AdjustTimer.TimerColor);
        Assert.True(impAdj.AdjustTimer.UpdateSpecialHud);
        Assert.Equal(2, impAdj.AdjustTimer.SpecialHudIndex);

        // Countup assertions
        var impCnt = importedEvents[8];
        Assert.Equal(EventType.Countup, impCnt.Type);
        Assert.Equal(10.0, impCnt.Countup.StartValue);
        Assert.Equal("COUNTUP TEST", impCnt.Countup.TitleText);
        Assert.Single(impCnt.Countup.EventsOnProgress);
        Assert.Single(impCnt.Countup.EventsOnDone);

        // ForceCompleteChainPuzzle assertions
        var impFccp = importedEvents[9];
        Assert.Equal(EventType.ForceCompleteChainPuzzle, impFccp.Type);
        Assert.Equal(456u, impFccp.ChainPuzzle);
        Assert.Equal(2, impFccp.Count);

        // SpawnNavMarker assertions
        var impNm = importedEvents[10];
        Assert.Equal(EventType.SpawnNavMarker, impNm.Type);
        Assert.Equal(1, impNm.NavMarker.Index);
        Assert.Equal("OBJECTIVE NAV", impNm.NavMarker.Title);
        Assert.Equal("#FF00FF", impNm.NavMarker.Color);

        // ShakeScreen assertions
        var impShake = importedEvents[11];
        Assert.Equal(EventType.ShakeScreen, impShake.Type);
        Assert.Equal(2.5, impShake.Duration);
        Assert.Equal(100.0, impShake.Position.X);
        Assert.Equal(200.0, impShake.Position.Y);
        Assert.Equal(300.0, impShake.Position.Z);
        Assert.Equal("SHAKE_FILTER", impShake.WorldEventObjectFilter);
        Assert.Equal(15.0, impShake.CameraShake.Radius);
        Assert.Equal(0.8, impShake.CameraShake.Amplitude);
        Assert.Equal(45.0, impShake.CameraShake.Frequency);
        Assert.True(impShake.CameraShake.Directional);

        // StartPortalMachine assertions
        var impPort = importedEvents[12];
        Assert.Equal(EventType.StartPortalMachine, impPort.Type);
        Assert.Equal(2, impPort.Portal.TargetDimension);
        Assert.Equal(3.5, impPort.Portal.TeleportDelay);
        Assert.True(impPort.Portal.PreventPortalWarpTeamEvent);

        // SetSuccessScreen assertions
        var impSs = importedEvents[13];
        Assert.Equal(EventType.SetSuccessScreen, impSs.Type);
        Assert.Equal("VICTORY ACHIEVED", impSs.SpecialText);
        Assert.True(impSs.SuccessScreen.HasSuccessScreen);
        Assert.Equal(AwoSuccessScreenType.FlashFakeScreen, impSs.SuccessScreen.Type);
        Assert.Equal(101u, impSs.SuccessScreen.CustomSuccessScreen);

        // PlaySubtitles assertions
        var impSub = importedEvents[14];
        Assert.Equal(EventType.PlaySubtitles, impSub.Type);
        Assert.Equal("Voice line subtitle here", impSub.SoundSubtitle);

        // MultiProgression assertions
        var impMp = importedEvents[15];
        Assert.Equal(EventType.MultiProgression, impMp.Type);
        Assert.Single(impMp.MultiProgression.Items);
        Assert.Equal("MP Header 1", impMp.MultiProgression.Items[0].CustomSubObjectiveHeader);
        Assert.Equal(SectorType.Secondary, impMp.MultiProgression.Items[0].Layer);

        // PlayWaveRoarSound assertions
        var impRoar = importedEvents[16];
        Assert.Equal(EventType.PlayWaveRoarSound, impRoar.Type);
        Assert.Equal(AwoRoarSound.Tank, impRoar.WaveRoarSound.RoarSound);
        Assert.Equal(AwoRoarSize.Big, impRoar.WaveRoarSound.RoarSize);
        Assert.True(impRoar.WaveRoarSound.IsOutside);

        // CustomHudText assertions
        var impCh = importedEvents[17];
        Assert.Equal(EventType.CustomHudText, impCh.Type);
        Assert.Equal("HUD TITLE", impCh.CustomHud.TitleText);
        Assert.Equal("HUD BODY", impCh.CustomHud.BodyText);

        // SpecialHudTimer assertions
        var impSh = importedEvents[18];
        Assert.Equal(EventType.SpecialHudTimer, impSh.Type);
        Assert.Equal(AwoSpecialHudType.StartPersistent, impSh.SpecialHud.Type);
        Assert.Equal("SPECIAL HUD MSG", impSh.SpecialHud.Message);
        Assert.Equal(AwoSpecialHudStyle.BioscanAlarm, impSh.SpecialHud.Style);
        Assert.Single(impSh.SpecialHud.EventsOnProgress);
        Assert.Single(impSh.SpecialHud.EventsOnDone);

        // ForcePlayPlayerDialogue assertions
        var impDlg = importedEvents[19];
        Assert.Equal(EventType.ForcePlayPlayerDialogue, impDlg.Type);
        Assert.Equal(999u, impDlg.DialogueID);
        Assert.Equal(AwoPlayerDialogueType.Specific, impDlg.PlayerDialogue.Type);
        Assert.Equal(2, impDlg.PlayerDialogue.CharacterID);
        Assert.Equal(AwoPlayerCharacter.Hackett, impDlg.PlayerDialogue.Character);
        Assert.Equal(AwoDialogueIntensityState.Combat, impDlg.PlayerDialogue.IntensityState);

        // SetTerminalLog assertions
        var impTl = importedEvents[20];
        Assert.Equal(EventType.SetTerminalLog, impTl.Type);
        Assert.Equal(1, impTl.SetTerminalLog.TerminalIndex);
        Assert.Equal("LOG_01.TXT", impTl.SetTerminalLog.FileName);
        Assert.Equal("Log file text body", impTl.SetTerminalLog.FileContent);
        Assert.Single(impTl.SetTerminalLog.EventsOnFileRead);

        // SetPocketItem assertions
        var impPi = importedEvents[21];
        Assert.Equal(EventType.SetPocketItem, impPi.Type);
        Assert.Single(impPi.ObjectiveItems.Items);
        Assert.Equal("BULKHEAD KEY", impPi.ObjectiveItems.Items[0].ItemName);
        Assert.Equal(AwoPocketItemTagType.Closest, impPi.ObjectiveItems.Items[0].TagType);
        Assert.Equal(AwoPlayerCharacter.Dauda, impPi.ObjectiveItems.Items[0].Player);

        // PickupSentries assertions
        var impPs = importedEvents[24];
        Assert.Equal(EventType.PickupSentries, impPs.Type);

        // SetExpeditionEnvironment assertions
        var impEnv = importedEvents[25];
        Assert.Equal(EventType.SetExpeditionEnvironment, impEnv.Type);
        Assert.Equal("Wet", impEnv.EnvironmentData.EnvironmentWetness);
        Assert.Equal("yellow", impEnv.EnvironmentData.DustColor);

        // ClearWardenIntelQueue assertions
        var impCwi = importedEvents[26];
        Assert.Equal(EventType.ClearWardenIntelQueue, impCwi.Type);
        Assert.True(impCwi.SpecialBool);

        // ResetOnDoorApproach assertions
        var impReset = importedEvents[27];
        Assert.Equal(EventType.ResetOnDoorApproach, impReset.Type);
        Assert.Equal(3, impReset.Zone);

        // FadeScreenInOut assertions
        var impFade = importedEvents[28];
        Assert.Equal(EventType.FadeScreenInOut, impFade.Type);
        Assert.Equal("white", impFade.FadeScreen.FadeColor);
        Assert.Equal(2.0, impFade.FadeScreen.FadeSpeed);
    }

    [Fact]
    public void ForceCompleteChainPuzzle_InTerminalCommand_UsesSpecialNumber()
    {
        var textMap = new Dictionary<uint, string>();
        Func<string?, string, uint> textSynth = (str, ctx) => 1;

        var ev = new WardenEvent
        {
            Type = EventType.ForceCompleteChainPuzzle,
            ChainPuzzle = 789
        };

        var events = new ObservableCollection<WardenEvent> { ev };

        // Export as terminal command event
        var exportedArray = WardenObjectiveDataBlockExporter.SerializeWardenEvents(events, textSynth, isTerminalCommand: true);
        var exportedObj = exportedArray[0] as JsonObject;
        Assert.NotNull(exportedObj);
        Assert.Equal(789, exportedObj!["SpecialNumber"]?.GetValue<int>());
        Assert.Equal(0, exportedObj["ChainPuzzle"]?.GetValue<int>());

        // Import
        var importContext = new ImportContext(new MainWindowViewModel(), AppContext.BaseDirectory);
        var importedEvents = new ObservableCollection<WardenEvent>();
        LevelLayoutDataBlockImporter.ImportWardenEvents(exportedArray, importedEvents, importContext);

        Assert.Single(importedEvents);
        Assert.Equal(789u, importedEvents[0].ChainPuzzle);
    }

    [Fact]
    public void Awo20kEventModels_SubListRemovalMethods_WorkCorrectly()
    {
        // NestedEvent
        var nested = new AwoNestedEventData();
        var evItem = new WardenEvent();
        var wheelItem = new AwoWheelOfEventItem();
        nested.EventsToActivate.Add(evItem);
        nested.WheelOfEvents.Add(wheelItem);
        nested.RemoveEventToActivateCommand.Execute(evItem);
        nested.RemoveWheelItemCommand.Execute(wheelItem);
        Assert.Empty(nested.EventsToActivate);
        Assert.Empty(nested.WheelOfEvents);

        // WheelOfEventItem
        var wheel = new AwoWheelOfEventItem();
        var wheelEv = new WardenEvent();
        wheel.Events.Add(wheelEv);
        wheel.RemoveEventCommand.Execute(wheelEv);
        Assert.Empty(wheel.Events);

        // EventLoop
        var loop = new AwoEventLoopData();
        var loopEv = new WardenEvent();
        loop.EventsToActivate.Add(loopEv);
        loop.RemoveEventToActivateCommand.Execute(loopEv);
        Assert.Empty(loop.EventsToActivate);

        // TeleportPlayer
        var tp = new AwoTeleportPlayerData();
        var tpItem = new AwoTPDataItem();
        tp.TPData.Add(tpItem);
        tp.RemoveTPDataCommand.Execute(tpItem);
        Assert.Empty(tp.TPData);

        // Countup
        var countup = new AwoCountupData();
        var cProg = new AwoCountdownProgressEvent();
        var cDone = new WardenEvent();
        countup.EventsOnProgress.Add(cProg);
        countup.EventsOnDone.Add(cDone);
        countup.RemoveProgressEventCommand.Execute(cProg);
        countup.RemoveDoneEventCommand.Execute(cDone);
        Assert.Empty(countup.EventsOnProgress);
        Assert.Empty(countup.EventsOnDone);

        // SpecialHud
        var sh = new AwoSpecialHudData();
        var shProg = new AwoCountdownProgressEvent();
        var shDone = new WardenEvent();
        sh.EventsOnProgress.Add(shProg);
        sh.EventsOnDone.Add(shDone);
        sh.RemoveProgressEventCommand.Execute(shProg);
        sh.RemoveDoneEventCommand.Execute(shDone);
        Assert.Empty(sh.EventsOnProgress);
        Assert.Empty(sh.EventsOnDone);

        // MultiProgression
        var mp = new AwoMultiProgressionData();
        var mpItem = new AwoMultiProgressionItem();
        mp.Items.Add(mpItem);
        mp.RemoveItemCommand.Execute(mpItem);
        Assert.Empty(mp.Items);

        // SetTerminalLog
        var stl = new AwoSetTerminalLogData();
        var stlEv = new WardenEvent();
        stl.EventsOnFileRead.Add(stlEv);
        stl.RemoveEventOnFileReadCommand.Execute(stlEv);
        Assert.Empty(stl.EventsOnFileRead);

        // PocketItems
        var pi = new AwoPocketItemsData();
        var piItem = new AwoPocketItem();
        pi.Items.Add(piItem);
        pi.RemoveItemCommand.Execute(piItem);
        Assert.Empty(pi.Items);
    }

    [Fact]
    public void LevelLayoutViewModel_RemoveWardenEvent_RemovesNested20kEvents()
    {
        var vm = new LevelLayoutViewModel();
        var door = new PlacedDoorNode();
        vm.EditingDoor = door;

        var nested1 = new WardenEvent { Type = EventType.KillAllPlayers };
        var nested2 = new WardenEvent { Type = EventType.PickupSentries };
        var nested3 = new WardenEvent { Type = EventType.StopEventLoop };

        var nestedEvent = new WardenEvent
        {
            Type = EventType.NestedEvent,
            NestedEvent = new AwoNestedEventData
            {
                EventsToActivate = new ObservableCollection<WardenEvent> { nested1 },
                WheelOfEvents = new ObservableCollection<AwoWheelOfEventItem>
                {
                    new() { Events = new ObservableCollection<WardenEvent> { nested2 } }
                }
            }
        };

        var eventLoop = new WardenEvent
        {
            Type = EventType.StartEventLoop,
            EventLoop = new AwoEventLoopData
            {
                EventsToActivate = new ObservableCollection<WardenEvent> { nested3 }
            }
        };

        door.EventsOnUnlockDoor.Add(nestedEvent);
        door.EventsOnUnlockDoor.Add(eventLoop);

        // Remove nested1
        vm.RemoveWardenEvent(nested1);
        Assert.Empty(nestedEvent.NestedEvent.EventsToActivate);

        // Remove nested2
        vm.RemoveWardenEvent(nested2);
        Assert.Empty(nestedEvent.NestedEvent.WheelOfEvents[0].Events);

        // Remove nested3
        vm.RemoveWardenEvent(nested3);
        Assert.Empty(eventLoop.EventLoop.EventsToActivate);
    }

    [Fact]
    public void AwoPlayerCharacter_And_AwoSuccessScreenType_Properties_SyncCorrectly()
    {
        // Dialogue character
        var dlg = new AwoPlayerDialogueData();
        dlg.Character = AwoPlayerCharacter.Bishop;
        Assert.Equal(3, dlg.CharacterID);
        dlg.CharacterID = 1;
        Assert.Equal(AwoPlayerCharacter.Dauda, dlg.Character);

        // Pocket item player
        var item = new AwoPocketItem();
        item.Player = AwoPlayerCharacter.Hackett;
        Assert.Equal(2, item.PlayerIndex);
        item.PlayerIndex = 0;
        Assert.Equal(AwoPlayerCharacter.Woods, item.Player);

        // TPData player
        var tpData = new AwoTPDataItem();
        tpData.Player = AwoPlayerCharacter.Dauda;
        Assert.Equal(1, tpData.PlayerIndex);
        tpData.PlayerIndex = 3;
        Assert.Equal(AwoPlayerCharacter.Bishop, tpData.Player);

        // SuccessScreen type
        var ss = new AwoSuccessScreenData();
        ss.Type = AwoSuccessScreenType.FlashFakeScreen;
        Assert.Equal(AwoSuccessScreenType.FlashFakeScreen, ss.Type);

        // CameraShake clone and property change
        var cs = new AwoCameraShakeData { Radius = 5.0, Amplitude = 1.0, Frequency = 10.0, Directional = false };
        var csClone = cs.Clone();
        Assert.NotSame(cs, csClone);
        Assert.Equal(5.0, csClone.Radius);
        Assert.Equal(1.0, csClone.Amplitude);
        Assert.Equal(10.0, csClone.Frequency);
        Assert.False(csClone.Directional);
    }

    [Fact]
    public void SetExpeditionEnvironment_HandlesFloatsAndUnchangedStrings_RoundTrip()
    {
        var textMap = new Dictionary<uint, string>();
        Func<string?, string, uint> textSynth = (str, ctx) => 1;

        var ev1 = new WardenEvent
        {
            Type = EventType.SetExpeditionEnvironment,
            EnvironmentData = new AwoEnvironmentData
            {
                EnvironmentWetness = "0.75",
                UpdateColor = true,
                DustColor = "blue",
                DustAlphaBoost = "Unchanged",
                DustTurbulence = "1.5"
            }
        };

        var ev2 = new WardenEvent
        {
            Type = EventType.SetExpeditionEnvironment,
            EnvironmentData = new AwoEnvironmentData
            {
                EnvironmentWetness = "Unchanged",
                UpdateColor = false,
                DustColor = "white",
                DustAlphaBoost = "0.0",
                DustTurbulence = "Unchanged"
            }
        };

        var events = new ObservableCollection<WardenEvent> { ev1, ev2 };

        // Export
        var exportedArray = WardenObjectiveDataBlockExporter.SerializeWardenEvents(events, textSynth, isTerminalCommand: false);
        Assert.Equal(2, exportedArray.Count);

        var obj1 = exportedArray[0] as JsonObject;
        var env1 = obj1?["EnvironmentData"] as JsonObject;
        Assert.NotNull(env1);
        Assert.Equal(0.75, env1!["EnvironmentWetness"]?.GetValue<double>());
        Assert.Equal("Unchanged", env1["DustAlphaBoost"]?.GetValue<string>());
        Assert.Equal(1.5, env1["DustTurbulence"]?.GetValue<double>());

        var obj2 = exportedArray[1] as JsonObject;
        var env2 = obj2?["EnvironmentData"] as JsonObject;
        Assert.NotNull(env2);
        Assert.Equal("Unchanged", env2!["EnvironmentWetness"]?.GetValue<string>());
        Assert.Equal(0.0, env2["DustAlphaBoost"]?.GetValue<double>());
        Assert.Equal("Unchanged", env2["DustTurbulence"]?.GetValue<string>());

        // Import
        var importContext = new ImportContext(new MainWindowViewModel(), AppContext.BaseDirectory);
        var importedEvents = new ObservableCollection<WardenEvent>();
        LevelLayoutDataBlockImporter.ImportWardenEvents(exportedArray, importedEvents, importContext);

        Assert.Equal(2, importedEvents.Count);

        var imp1 = importedEvents[0];
        Assert.Equal("0.75", imp1.EnvironmentData.EnvironmentWetness);
        Assert.True(imp1.EnvironmentData.UpdateColor);
        Assert.Equal("blue", imp1.EnvironmentData.DustColor);
        Assert.Equal("Unchanged", imp1.EnvironmentData.DustAlphaBoost);
        Assert.Equal("1.5", imp1.EnvironmentData.DustTurbulence);

        var imp2 = importedEvents[1];
        Assert.Equal("Unchanged", imp2.EnvironmentData.EnvironmentWetness);
        Assert.False(imp2.EnvironmentData.UpdateColor);
        Assert.Equal("white", imp2.EnvironmentData.DustColor);
        Assert.Equal("0", imp2.EnvironmentData.DustAlphaBoost);
        Assert.Equal("Unchanged", imp2.EnvironmentData.DustTurbulence);
    }

    [Fact]
    public void RoomEnemySpawningItem_DetectsScouts_AndMapsScoutTypes()
    {
        var hib = new RoomEnemySpawningItem { EnemyId = 28 }; // Striker
        Assert.False(hib.IsScout);
        Assert.Equal(ScoutType.Scout, hib.ScoutType);

        var scout = new RoomEnemySpawningItem { EnemyId = 20 }; // Scout
        Assert.True(scout.IsScout);
        Assert.Equal(ScoutType.Scout, scout.ScoutType);

        var chargerScout = new RoomEnemySpawningItem { EnemyId = 41 }; // Charger Scout
        Assert.True(chargerScout.IsScout);
        Assert.Equal(ScoutType.Charger_Scout, chargerScout.ScoutType);

        var shadowScout = new RoomEnemySpawningItem { EnemyId = 40 }; // Shadow Scout
        Assert.True(shadowScout.IsScout);
        Assert.Equal(ScoutType.Shadow_Scout, shadowScout.ScoutType);

        var zoomerScout = new RoomEnemySpawningItem { EnemyId = 54 }; // Zoomer Scout
        Assert.True(zoomerScout.IsScout);
        Assert.Equal(ScoutType.Zoomer_Scout, zoomerScout.ScoutType);

        var nightmareScout = new RoomEnemySpawningItem { EnemyId = 56 }; // Nightmare Scout
        Assert.True(nightmareScout.IsScout);
        Assert.Equal(ScoutType.Nightmare_Scout, nightmareScout.ScoutType);

        // Test clone and serialization data
        var clone = chargerScout.Clone();
        Assert.NotSame(chargerScout, clone);
        Assert.Equal(41u, clone.EnemyId);
        Assert.True(clone.IsScout);

        var data = chargerScout.ToData();
        var fromData = RoomEnemySpawningItem.FromData(data);
        Assert.Equal(41u, fromData.EnemyId);
        Assert.True(fromData.IsScout);
    }

    [Fact]
    public void GridState_RoomEnemies_ExportAndImportStateData_RoundTrip()
    {
        var grid = new LevelLayoutGridState();
        var geo = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_64x64_tech_data_center_HA_01.prefab", 0, 0, initialAreaIndex: 0);
        grid.PlacedGeomorphs.Add(geo);
        geo.GenerateRoom(0);
        geo.SetRoomZone(0, 1);

        var enemies = geo.GetRoomEnemies(0);
        enemies.Add(new RoomEnemySpawningItem { EnemyId = 28, Count = 5, PlacementRandomWeight = 0.5 });
        enemies.Add(new RoomEnemySpawningItem { EnemyId = 41, Count = 2 }); // Charger Scout

        grid.CreateRoomNode(geo, 0);

        var stateData = grid.ExportStateData();
        Assert.NotNull(stateData);

        var restoredGrid = new LevelLayoutGridState();
        restoredGrid.ImportStateData(stateData);

        Assert.Single(restoredGrid.PlacedGeomorphs);
        var restoredGeo = restoredGrid.PlacedGeomorphs[0];
        var restoredEnemies = restoredGeo.GetRoomEnemies(0);
        Assert.Equal(2, restoredEnemies.Count);
        Assert.Equal(28u, restoredEnemies[0].EnemyId);
        Assert.Equal(5, restoredEnemies[0].Count);
        Assert.Equal(0.5, restoredEnemies[0].PlacementRandomWeight);
        Assert.Equal(41u, restoredEnemies[1].EnemyId);
        Assert.Equal(2, restoredEnemies[1].Count);

        Assert.Single(restoredGrid.PlacedRooms);
        Assert.Equal(2, restoredGrid.PlacedRooms[0].Enemies.Count);
    }

    [Fact]
    public void RoomEnemies_GenerateRoomEnemyEvents_CreatesCorrectHibernateAndScoutEvents()
    {
        var grid = new LevelLayoutGridState();
        var geo = grid.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_64x64_tech_data_center_HA_01.prefab", 0, 0);
        grid.PlacedGeomorphs.Add(geo);
        geo.GenerateRoom(0);
        geo.SetRoomSector(0, SectorType.Main);
        geo.SetRoomZone(0, 2);

        grid.CreateRoomNode(geo, 0);
        grid.UpdateRoomNumbers(); // Sets room.RoomNumber

        var room = grid.PlacedRooms.First();
        room.Enemies.Add(new RoomEnemySpawningItem
        {
            EnemyId = 28, // Striker (Hibernate)
            Count = 4,
            PlacementRandomWeight = 0.35,
            PlacementScore = 2.5,
            ResetPlacementInfo = true,
            RandomizeRotation = true
        });

        room.Enemies.Add(new RoomEnemySpawningItem
        {
            EnemyId = 40, // Shadow Scout
            Count = 1
        });

        var generatedEvents = WardenObjectiveDataBlockExporter.GenerateRoomEnemyEvents(grid, "Reality");
        Assert.Equal(2, generatedEvents.Count);

        var hibEvent = generatedEvents[0];
        Assert.Equal(EventType.SpawnHibernateInZone, hibEvent.Type);
        Assert.Equal(SectorType.Main, hibEvent.Layer);
        Assert.Equal(2, hibEvent.Zone);
        Assert.Equal(4, hibEvent.Count);
        Assert.Equal(room.RoomNumber, hibEvent.SpawnHibernates.AreaIndex);
        Assert.Equal(28u, hibEvent.SpawnHibernates.EnemyID);
        Assert.Equal(0.35, hibEvent.SpawnHibernates.PlacementRandomWeight);
        Assert.Equal(2.5, hibEvent.SpawnHibernates.PlacementScore);
        Assert.True(hibEvent.SpawnHibernates.ResetPlacementInfo);
        Assert.True(hibEvent.SpawnHibernates.RandomizeRotation);

        var scoutEvent = generatedEvents[1];
        Assert.Equal(EventType.SpawnScoutInZone, scoutEvent.Type);
        Assert.Equal(SectorType.Main, scoutEvent.Layer);
        Assert.Equal(2, scoutEvent.Zone);
        Assert.Equal(1, scoutEvent.Count);
        Assert.Equal(room.RoomNumber, scoutEvent.SpawnScouts.AreaIndex);
        Assert.Equal(ScoutType.Shadow_Scout, scoutEvent.SpawnScouts.ScoutType);
    }

    [Fact]
    public void ModManager_HasAnyAwoEventsInUse_LocksWhenRoomEnemiesExist()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        vm.LevelLayout.Levels.Clear();

        var level = new LevelLayoutBlock
        {
            PersistentId = 501,
            Name = "AWO_Room_Enemies_Level",
            ComplexType = ComplexType.Tech,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab"
        };
        vm.LevelLayout.Levels.Add(level);
        level.GridState.InitializeStartingTile(level.ElevatorTile, 0, 0);

        Assert.False(vm.LevelLayout.HasAnyAwoEventsInUse());

        // Add a room with enemy
        var geo = level.GridState.CreateGeomorphNode("Assets/AssetPrefabs/Complex/Tech/Geomorphs/geo_tech_room_02.prefab", 1, 0);
        level.GridState.PlacedGeomorphs.Add(geo);
        geo.GenerateRoom(0);
        geo.SetRoomZone(0, 0);
        var room = new PlacedRoomNode
        {
            Geomorph = geo,
            AreaIndex = 0,
            Sector = SectorType.Main,
            Zone = 0,
            Enemies = geo.GetRoomEnemies(0)
        };
        level.GridState.PlacedRooms.Add(room);

        var enemyItem = new RoomEnemySpawningItem { EnemyId = 28, Count = 2 };
        room.Enemies.Add(enemyItem);

        Assert.True(vm.LevelLayout.HasAnyAwoEventsInUse());

        // Enable AWO
        ModManager.Instance.AdvancedWardenObjective = true;
        Assert.True(ModManager.Instance.AdvancedWardenObjective);

        // Attempting to disable AWO should throw InvalidOperationException while room enemy is present
        Assert.Throws<InvalidOperationException>(() => ModManager.Instance.AdvancedWardenObjective = false);
        Assert.True(ModManager.Instance.AdvancedWardenObjective); // Remains true

        // Remove enemy and disable AWO
        room.Enemies.Clear();
        Assert.False(vm.LevelLayout.HasAnyAwoEventsInUse());
        ModManager.Instance.AdvancedWardenObjective = false;
        Assert.False(ModManager.Instance.AdvancedWardenObjective);
    }
}
