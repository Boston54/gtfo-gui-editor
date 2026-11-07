using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Models;

public record AreaOption(int AreaIndex, string DisplayName);

public static class WardenEventVisibilityConfig
{
    public static readonly HashSet<EventType> ChainPuzzleTypes = new()
    {
        EventType.ActivateChainedPuzzle,
        EventType.AddChainPuzzleToSecurityDoor,
        EventType.ForceResetChainPuzzle,
        EventType.ForceCompleteChainPuzzle
    };

    public static readonly HashSet<EventType> UseStaticBioscanPointsTypes = new()
    {
        EventType.ActivateChainedPuzzle
    };

    public static readonly HashSet<EventType> DurationTypes = new()
    {
        EventType.AddToTimer,
        EventType.DimensionFlashTeam,
        EventType.LightsInZone,
        EventType.Countdown,
        EventType.TeleportPlayer,
        EventType.SpawnNavMarker,
        EventType.ShakeScreen,
        EventType.PlaySubtitles,
        EventType.SpecialHudTimer,
        EventType.ForcePlayPlayerDialogue
    };

    public static readonly HashSet<EventType> ClearDimensionTypes = new()
    {
        EventType.DimensionWarpTeam
    };

    public static readonly HashSet<EventType> CustomSubObjectiveTypes = new()
    {
        EventType.UpdateCustomSubObjective
    };

    public static readonly HashSet<EventType> FogSettingTypes = new()
    {
        EventType.SetFogSetting
    };

    public static readonly HashSet<EventType> FogTransitionDurationTypes = new()
    {
        EventType.SetFogSetting
    };

    public static readonly HashSet<EventType> EnemyWaveDataTypes = new()
    {
        EventType.SpawnEnemyWave
    };

    public static readonly HashSet<EventType> EnemyIdTypes = new()
    {
        EventType.SpawnEnemyOnPoint
    };

    public static readonly HashSet<EventType> PositionTypes = new()
    {
        EventType.LightsInZone,
        EventType.SpawnScoutInZone,
        EventType.MoveExtractionWorldPosition,
        EventType.TeleportPlayer,
        EventType.SpawnNavMarker,
        EventType.ShakeScreen,
        EventType.PlayWaveRoarSound,
        EventType.ForcePlayPlayerDialogue,
        EventType.SetPocketItem
    };

    public static readonly HashSet<EventType> CountTypes = new()
    {
        EventType.SpawnEnemyOnPoint,
        EventType.SpawnHibernateInZone,
        EventType.SpawnScoutInZone,
        EventType.StopEventLoop,
        EventType.ForceCompleteChainPuzzle,
        EventType.SpawnNavMarker
    };

    public static readonly HashSet<EventType> WorldEventObjectFilterTypes = new()
    {
        EventType.SpawnEnemyOnPoint,
        EventType.SetNavMarker,
        EventType.SpawnHibernateInZone,
        EventType.SpawnScoutInZone,
        EventType.MoveExtractionWorldPosition,
        EventType.TeleportPlayer,
        EventType.SpawnNavMarker,
        EventType.ShakeScreen,
        EventType.PlayWaveRoarSound,
        EventType.ForcePlayPlayerDialogue,
        EventType.SetPocketItem
    };

    public static readonly HashSet<EventType> CleanUpEnemiesBehindTypes = new()
    {
        EventType.CloseSecurityDoor
    };

    public static readonly HashSet<EventType> SpecialTextTypes = new()
    {
        EventType.LockSecurityDoor,
        EventType.SetDoorInteraction,
        EventType.SetSuccessScreen
    };

    public static readonly HashSet<EventType> ReactorTypes = new()
    {
        EventType.ModifyReactorWaveState
    };

    public static readonly HashSet<EventType> CountdownTypes = new()
    {
        EventType.Countdown
    };

    public static readonly HashSet<EventType> SetZoneLightTypes = new()
    {
        EventType.SetLightDataInZone
    };

    public static readonly HashSet<EventType> SpecialNumberTypes = new()
    {
        EventType.AlertEnemiesInZone
    };

    public static readonly HashSet<EventType> CleanupEnemiesTypes = new()
    {
        EventType.CleanUpEnemiesInZone
    };

    public static readonly HashSet<EventType> SpawnHibernatesTypes = new()
    {
        EventType.SpawnHibernateInZone
    };

    public static readonly HashSet<EventType> SpawnScoutsTypes = new()
    {
        EventType.SpawnScoutInZone
    };

    public static readonly HashSet<EventType> AddTerminalCommandTypes = new()
    {
        EventType.AddTerminalCommand
    };

    public static readonly HashSet<EventType> HideTerminalCommandTypes = new()
    {
        EventType.HideTerminalCommand
    };

    public static readonly HashSet<EventType> UnhideTerminalCommandTypes = new()
    {
        EventType.UnhideTerminalCommand
    };

    public static readonly HashSet<EventType> ActiveEnemyWaveTypes = new()
    {
        EventType.SetActiveEnemyWave
    };

    public static readonly HashSet<EventType> GiveResourceTypes = new()
    {
        EventType.GiveResource
    };

    public static readonly HashSet<EventType> DialogueIdTypes = new()
    {
        EventType.DialogueOnClosest,
        EventType.ForcePlayPlayerDialogue
    };

    public static readonly HashSet<EventType> NestedEventTypes = new()
    {
        EventType.NestedEvent
    };

    public static readonly HashSet<EventType> EventLoopTypes = new()
    {
        EventType.StartEventLoop
    };

    public static readonly HashSet<EventType> TeleportPlayerTypes = new()
    {
        EventType.TeleportPlayer
    };

    public static readonly HashSet<EventType> InfectPlayerTypes = new()
    {
        EventType.InfectPlayer
    };

    public static readonly HashSet<EventType> DamagePlayerTypes = new()
    {
        EventType.DamagePlayer
    };

    public static readonly HashSet<EventType> RevivePlayerTypes = new()
    {
        EventType.RevivePlayer
    };

    public static readonly HashSet<EventType> AdjustTimerTypes = new()
    {
        EventType.AdjustAWOTimer
    };

    public static readonly HashSet<EventType> CountupTypes = new()
    {
        EventType.Countup
    };

    public static readonly HashSet<EventType> NavMarkerTypes = new()
    {
        EventType.SpawnNavMarker
    };

    public static readonly HashSet<EventType> PortalTypes = new()
    {
        EventType.StartPortalMachine
    };

    public static readonly HashSet<EventType> SuccessScreenTypes = new()
    {
        EventType.SetSuccessScreen
    };

    public static readonly HashSet<EventType> MultiProgressionTypes = new()
    {
        EventType.MultiProgression
    };

    public static readonly HashSet<EventType> WaveRoarSoundTypes = new()
    {
        EventType.PlayWaveRoarSound
    };

    public static readonly HashSet<EventType> CustomHudTypes = new()
    {
        EventType.CustomHudText
    };

    public static readonly HashSet<EventType> SpecialHudTypes = new()
    {
        EventType.SpecialHudTimer
    };

    public static readonly HashSet<EventType> PlayerDialogueTypes = new()
    {
        EventType.ForcePlayPlayerDialogue
    };

    public static readonly HashSet<EventType> SetTerminalLogTypes = new()
    {
        EventType.SetTerminalLog
    };

    public static readonly HashSet<EventType> ObjectiveItemsTypes = new()
    {
        EventType.SetPocketItem
    };

    public static readonly HashSet<EventType> EnvironmentDataTypes = new()
    {
        EventType.SetExpeditionEnvironment
    };

    public static readonly HashSet<EventType> SpecialBoolTypes = new()
    {
        EventType.ClearWardenIntelQueue
    };

    public static readonly HashSet<EventType> FadeScreenTypes = new()
    {
        EventType.FadeScreenInOut
    };

    public static readonly HashSet<EventType> CameraShakeTypes = new()
    {
        EventType.ShakeScreen
    };

    public static bool IsChainPuzzleVisible(EventType type) => ChainPuzzleTypes.Contains(type);
    public static bool IsUseStaticBioscanPointsVisible(EventType type) => UseStaticBioscanPointsTypes.Contains(type);
    public static bool IsDurationVisible(EventType type) => DurationTypes.Contains(type);
    public static bool IsClearDimensionVisible(EventType type) => ClearDimensionTypes.Contains(type);
    public static bool IsCustomSubObjectiveVisible(EventType type) => CustomSubObjectiveTypes.Contains(type);
    public static bool IsFogSettingVisible(EventType type) => FogSettingTypes.Contains(type);
    public static bool IsFogTransitionDurationVisible(EventType type) => FogTransitionDurationTypes.Contains(type);
    public static bool IsEnemyWaveDataVisible(EventType type) => EnemyWaveDataTypes.Contains(type);
    public static bool IsEnemyIdVisible(EventType type) => EnemyIdTypes.Contains(type);
    public static bool IsPositionVisible(EventType type) => PositionTypes.Contains(type);
    public static bool IsCountVisible(EventType type) => CountTypes.Contains(type);
    public static bool IsWorldEventObjectFilterVisible(EventType type) => WorldEventObjectFilterTypes.Contains(type);
    public static bool IsCleanUpEnemiesBehindVisible(EventType type) => CleanUpEnemiesBehindTypes.Contains(type);
    public static bool IsSpecialTextVisible(EventType type) => SpecialTextTypes.Contains(type);
    public static bool IsReactorVisible(EventType type) => ReactorTypes.Contains(type);
    public static bool IsCountdownVisible(EventType type) => CountdownTypes.Contains(type);
    public static bool IsSetZoneLightVisible(EventType type) => SetZoneLightTypes.Contains(type);
    public static bool IsSpecialNumberVisible(EventType type) => SpecialNumberTypes.Contains(type);
    public static bool IsCleanupEnemiesVisible(EventType type) => CleanupEnemiesTypes.Contains(type);
    public static bool IsSpawnHibernatesVisible(EventType type) => SpawnHibernatesTypes.Contains(type);
    public static bool IsSpawnScoutsVisible(EventType type) => SpawnScoutsTypes.Contains(type);
    public static bool IsAddTerminalCommandVisible(EventType type) => AddTerminalCommandTypes.Contains(type);
    public static bool IsHideTerminalCommandVisible(EventType type) => HideTerminalCommandTypes.Contains(type);
    public static bool IsUnhideTerminalCommandVisible(EventType type) => UnhideTerminalCommandTypes.Contains(type);
    public static bool IsActiveEnemyWaveVisible(EventType type) => ActiveEnemyWaveTypes.Contains(type);
    public static bool IsGiveResourceVisible(EventType type) => GiveResourceTypes.Contains(type);
    public static bool IsDialogueIdVisible(EventType type) => DialogueIdTypes.Contains(type);
    public static bool IsNestedEventVisible(EventType type) => NestedEventTypes.Contains(type);
    public static bool IsEventLoopVisible(EventType type) => EventLoopTypes.Contains(type);
    public static bool IsTeleportPlayerVisible(EventType type) => TeleportPlayerTypes.Contains(type);
    public static bool IsInfectPlayerVisible(EventType type) => InfectPlayerTypes.Contains(type);
    public static bool IsDamagePlayerVisible(EventType type) => DamagePlayerTypes.Contains(type);
    public static bool IsRevivePlayerVisible(EventType type) => RevivePlayerTypes.Contains(type);
    public static bool IsAdjustTimerVisible(EventType type) => AdjustTimerTypes.Contains(type);
    public static bool IsCountupVisible(EventType type) => CountupTypes.Contains(type);
    public static bool IsNavMarkerVisible(EventType type) => NavMarkerTypes.Contains(type);
    public static bool IsPortalVisible(EventType type) => PortalTypes.Contains(type);
    public static bool IsSuccessScreenVisible(EventType type) => SuccessScreenTypes.Contains(type);
    public static bool IsMultiProgressionVisible(EventType type) => MultiProgressionTypes.Contains(type);
    public static bool IsWaveRoarSoundVisible(EventType type) => WaveRoarSoundTypes.Contains(type);
    public static bool IsCustomHudVisible(EventType type) => CustomHudTypes.Contains(type);
    public static bool IsSpecialHudVisible(EventType type) => SpecialHudTypes.Contains(type);
    public static bool IsPlayerDialogueVisible(EventType type) => PlayerDialogueTypes.Contains(type);
    public static bool IsSetTerminalLogVisible(EventType type) => SetTerminalLogTypes.Contains(type);
    public static bool IsObjectiveItemsVisible(EventType type) => ObjectiveItemsTypes.Contains(type);
    public static bool IsEnvironmentDataVisible(EventType type) => EnvironmentDataTypes.Contains(type);
    public static bool IsSpecialBoolVisible(EventType type) => SpecialBoolTypes.Contains(type);
    public static bool IsFadeScreenVisible(EventType type) => FadeScreenTypes.Contains(type);
    public static bool IsCameraShakeVisible(EventType type) => CameraShakeTypes.Contains(type);
}

public class WardenEvent : INotifyPropertyChanged
{
    private EventType _type = EventType.None;
    private EventTrigger _trigger = EventTrigger.None;
    private uint _chainPuzzle;
    private bool _useStaticBioscanPoints;
    private SectorType _layer = SectorType.Main;
    private string _dimension = "Reality";
    private int _zone;
    private double _delay;
    private double _duration;
    private bool _clearDimension;
    private string _wardenIntel = string.Empty;
    private string _customSubObjectiveHeader = string.Empty;
    private string _customSubObjective = string.Empty;
    private uint _soundId;
    private string _soundSubtitle = string.Empty;
    private uint _dialogueId;
    private uint _fogSetting;
    private int _fogTransitionDuration;
    private GenericWaveSettings _enemyWaveData = new();
    private uint _enemyId;
    private Vector3Data _position = new();
    private int _count = 1;
    private bool _enabled = true;
    private string _worldEventObjectFilter = string.Empty;

    // AWO Fields
    private bool _cleanUpEnemiesBehind;
    private string _specialText = string.Empty;
    private int _specialNumber;
    private bool _specialBool;
    private AwoReactorData _reactor = new();
    private AwoCountdownData _countdown = new();
    private AwoSetZoneLightData _setZoneLight = new();
    private AwoCleanupEnemiesData _cleanupEnemies = new();
    private AwoSpawnHibernatesData _spawnHibernates = new();
    private AwoSpawnScoutsData _spawnScouts = new();
    private AwoAddTerminalCommandData _addTerminalCommand = new();
    private AwoHideTerminalCommandData _hideTerminalCommand = new();
    private AwoUnhideTerminalCommandData _unhideTerminalCommand = new();
    private AwoActiveEnemyWaveData _activeEnemyWave = new();
    private AwoGiveResourceData _giveResource = new();
    private AwoNestedEventData _nestedEvent = new();
    private AwoEventLoopData _eventLoop = new();
    private AwoTeleportPlayerData _teleportPlayer = new();
    private AwoInfectPlayerData _infectPlayer = new();
    private AwoDamagePlayerData _damagePlayer = new();
    private AwoRevivePlayerData _revivePlayer = new();
    private AwoAdjustTimerData _adjustTimer = new();
    private AwoCountupData _countup = new();
    private AwoNavMarkerData _navMarker = new();
    private AwoPortalData _portal = new();
    private AwoSuccessScreenData _successScreen = new();
    private AwoMultiProgressionData _multiProgression = new();
    private AwoWaveRoarSoundData _waveRoarSound = new();
    private AwoCustomHudData _customHud = new();
    private AwoSpecialHudData _specialHud = new();
    private AwoPlayerDialogueData _playerDialogue = new();
    private AwoSetTerminalLogData _setTerminalLog = new();
    private AwoPocketItemsData _objectiveItems = new();
    private AwoEnvironmentData _environmentData = new();
    private AwoFadeScreenData _fadeScreen = new();
    private AwoCameraShakeData _cameraShake = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public WardenEvent()
    {
        _enemyWaveData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EnemyWaveData));
        _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
        _reactor.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Reactor));
        _countdown.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Countdown));
        _setZoneLight.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SetZoneLight));
        _cleanupEnemies.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CleanupEnemies));
        _spawnHibernates.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SpawnHibernates));
        _spawnScouts.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SpawnScouts));
        _addTerminalCommand.PropertyChanged += (s, e) => OnPropertyChanged(nameof(AddTerminalCommand));
        _hideTerminalCommand.PropertyChanged += (s, e) => OnPropertyChanged(nameof(HideTerminalCommand));
        _unhideTerminalCommand.PropertyChanged += (s, e) => OnPropertyChanged(nameof(UnhideTerminalCommand));
        _activeEnemyWave.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ActiveEnemyWave));
        _giveResource.PropertyChanged += (s, e) => OnPropertyChanged(nameof(GiveResource));
        _nestedEvent.PropertyChanged += (s, e) => OnPropertyChanged(nameof(NestedEvent));
        _eventLoop.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EventLoop));
        _teleportPlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TeleportPlayer));
        _infectPlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(InfectPlayer));
        _damagePlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DamagePlayer));
        _revivePlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(RevivePlayer));
        _adjustTimer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(AdjustTimer));
        _countup.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Countup));
        _navMarker.PropertyChanged += (s, e) => OnPropertyChanged(nameof(NavMarker));
        _portal.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Portal));
        _successScreen.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SuccessScreen));
        _multiProgression.PropertyChanged += (s, e) => OnPropertyChanged(nameof(MultiProgression));
        _waveRoarSound.PropertyChanged += (s, e) => OnPropertyChanged(nameof(WaveRoarSound));
        _customHud.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CustomHud));
        _specialHud.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SpecialHud));
        _playerDialogue.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PlayerDialogue));
        _setTerminalLog.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SetTerminalLog));
        _objectiveItems.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ObjectiveItems));
        _environmentData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EnvironmentData));
        _fadeScreen.PropertyChanged += (s, e) => OnPropertyChanged(nameof(FadeScreen));
        _cameraShake.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CameraShake));
    }

    [JsonPropertyName("Type")]
    public EventType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
            {
                NotifyVisibilityChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonPropertyName("Trigger")]
    public EventTrigger Trigger
    {
        get => _trigger;
        set => SetField(ref _trigger, value);
    }

    [JsonPropertyName("ChainPuzzle")]
    public uint ChainPuzzle
    {
        get => _chainPuzzle;
        set => SetField(ref _chainPuzzle, value);
    }

    [JsonPropertyName("UseStaticBioscanPoints")]
    public bool UseStaticBioscanPoints
    {
        get => _useStaticBioscanPoints;
        set => SetField(ref _useStaticBioscanPoints, value);
    }

    [JsonPropertyName("Layer")]
    public SectorType Layer
    {
        get => _layer;
        set
        {
            if (SetField(ref _layer, value))
            {
                OnPropertyChanged(nameof(AvailableZones));
                OnPropertyChanged(nameof(AvailableAreas));
                OnPropertyChanged(nameof(AvailableAreasOnly));
                OnPropertyChanged(nameof(AvailableTerminals));
            }
        }
    }

    [JsonPropertyName("Dimension")]
    public string Dimension
    {
        get => _dimension;
        set
        {
            if (SetField(ref _dimension, value ?? "Reality"))
            {
                OnPropertyChanged(nameof(AvailableZones));
                OnPropertyChanged(nameof(AvailableAreas));
                OnPropertyChanged(nameof(AvailableAreasOnly));
                OnPropertyChanged(nameof(AvailableTerminals));
            }
        }
    }

    [JsonPropertyName("Zone")]
    public int Zone
    {
        get => _zone;
        set
        {
            if (SetField(ref _zone, value))
            {
                OnPropertyChanged(nameof(AvailableAreas));
                OnPropertyChanged(nameof(AvailableAreasOnly));
                OnPropertyChanged(nameof(AvailableTerminals));
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<EventType> AvailableEventTypes
    {
        get
        {
            if (ModManager.Instance.AdvancedWardenObjective)
            {
                return Enum.GetValues<EventType>().Where(t => t != EventType.EventBreak);
            }
            return Enum.GetValues<EventType>().Where(t => (int)t < 10000 && t != EventType.EventBreak);
        }
    }

    [JsonIgnore]
    public IEnumerable<ZoneOption> AvailableZones
    {
        get
        {
            var level = LevelLayoutViewModel.Instance?.SelectedLevel;
            var list = new List<ZoneOption>();
            bool isReality = string.IsNullOrWhiteSpace(Dimension) || string.Equals(Dimension, "Reality", StringComparison.OrdinalIgnoreCase);

            if (level != null && isReality)
            {
                var sectorZones = level.GridState.GetZonesForSector(Layer);
                if (sectorZones.Count == 0)
                {
                    sectorZones.Add(0);
                }

                var sectorSettings = level.GetSectorSettings(Layer);
                foreach (int z in sectorZones)
                {
                    list.Add(new ZoneOption
                    {
                        ZoneIndex = z,
                        DisplayName = sectorSettings.GetZoneDisplayName(z)
                    });
                }
            }
            else if (level != null && !isReality)
            {
                list.Add(new ZoneOption
                {
                    ZoneIndex = 0,
                    DisplayName = "Zone 0"
                });
            }
            else
            {
                list.Add(new ZoneOption
                {
                    ZoneIndex = 0,
                    DisplayName = "Zone 0"
                });
            }

            if (!list.Any(zo => zo.ZoneIndex == Zone))
            {
                string disp = level != null && isReality
                    ? level.GetSectorSettings(Layer).GetZoneDisplayName(Zone)
                    : $"Zone {Zone}";
                list.Add(new ZoneOption
                {
                    ZoneIndex = Zone,
                    DisplayName = disp
                });
                list = list.OrderBy(zo => zo.ZoneIndex).ToList();
            }

            return list;
        }
    }

    [JsonIgnore]
    public IEnumerable<AreaOption> AvailableAreas
    {
        get
        {
            var vm = LevelLayoutViewModel.Instance;
            var list = new List<AreaOption> { new AreaOption(-1, "All Areas (-1)") };
            if (vm?.GridState != null)
            {
                var geosInZone = vm.GridState.PlacedGeomorphs
                    .Where(g => (g.RoomZones != null && g.GeneratedRooms.Any(a => g.GetRoomSector(a) == Layer && g.GetRoomZone(a) == Zone)) ||
                                g.Rooms.Any(r => r.Sector == Layer && r.Zone == Zone))
                    .Concat(vm.GridState.PlacedRooms.Where(r => r.Sector == Layer && r.Zone == Zone && r.Geomorph != null).Select(r => r.Geomorph!))
                    .Distinct()
                    .ToList();

                var areaIndices = new HashSet<int>();
                foreach (var g in geosInZone)
                {
                    foreach (int a in g.GeneratedRooms)
                    {
                        if (g.GetRoomSector(a) == Layer && g.GetRoomZone(a) == Zone)
                        {
                            areaIndices.Add(a);
                        }
                    }
                }
                var placedRoomsInZone = vm.GridState.PlacedRooms
                    .Where(r => r.Sector == Layer && r.Zone == Zone)
                    .ToList();
                for (int i = 0; i < placedRoomsInZone.Count; i++)
                {
                    areaIndices.Add(i);
                }

                if (areaIndices.Count == 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        char letter = (char)('A' + i);
                        list.Add(new AreaOption(i, $"Area {letter} ({i})"));
                    }
                }
                else
                {
                    foreach (int a in areaIndices.OrderBy(x => x))
                    {
                        char letter = a < 26 ? (char)('A' + a) : '?';
                        list.Add(new AreaOption(a, $"Area {letter} ({a})"));
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    char letter = (char)('A' + i);
                    list.Add(new AreaOption(i, $"Area {letter} ({i})"));
                }
            }
            return list;
        }
    }

    [JsonIgnore]
    public IEnumerable<AreaOption> AvailableAreasOnly => AvailableAreas.Where(a => a.AreaIndex >= 0);

    [JsonIgnore]
    public IEnumerable<TerminalOption> AvailableTerminals
    {
        get
        {
            var vm = LevelLayoutViewModel.Instance;
            if (vm?.GridState == null) return new[] { new TerminalOption(0, "Terminal 1") };
            var zoneSettings = vm.GridState.GetOrCreateZoneSettings(Zone);
            int count = zoneSettings.TerminalPlacements.Count;
            var list = new List<TerminalOption>();
            for (int i = 0; i < Math.Max(1, count); i++)
            {
                list.Add(new TerminalOption(i, $"Terminal {i + 1}"));
            }
            return list;
        }
    }

    [JsonPropertyName("Delay")]
    public double Delay
    {
        get => _delay;
        set => SetField(ref _delay, value);
    }

    [JsonPropertyName("Duration")]
    public double Duration
    {
        get => _duration;
        set => SetField(ref _duration, value);
    }

    [JsonPropertyName("ClearDimension")]
    public bool ClearDimension
    {
        get => _clearDimension;
        set => SetField(ref _clearDimension, value);
    }

    [JsonPropertyName("WardenIntel")]
    public string WardenIntel
    {
        get => _wardenIntel;
        set => SetField(ref _wardenIntel, value ?? string.Empty);
    }

    [JsonPropertyName("CustomSubObjectiveHeader")]
    public string CustomSubObjectiveHeader
    {
        get => _customSubObjectiveHeader;
        set => SetField(ref _customSubObjectiveHeader, value ?? string.Empty);
    }

    [JsonPropertyName("CustomSubObjective")]
    public string CustomSubObjective
    {
        get => _customSubObjective;
        set => SetField(ref _customSubObjective, value ?? string.Empty);
    }

    [JsonPropertyName("SoundID")]
    public uint SoundID
    {
        get => _soundId;
        set => SetField(ref _soundId, value);
    }

    [JsonPropertyName("SoundSubtitle")]
    public string SoundSubtitle
    {
        get => _soundSubtitle;
        set => SetField(ref _soundSubtitle, value ?? string.Empty);
    }

    [JsonPropertyName("FogSetting")]
    public uint FogSetting
    {
        get => _fogSetting;
        set => SetField(ref _fogSetting, value);
    }

    [JsonPropertyName("FogTransitionDuration")]
    public int FogTransitionDuration
    {
        get => _fogTransitionDuration;
        set => SetField(ref _fogTransitionDuration, value);
    }

    [JsonPropertyName("EnemyWaveData")]
    public GenericWaveSettings EnemyWaveData
    {
        get => _enemyWaveData;
        set
        {
            var old = _enemyWaveData;
            if (SetField(ref _enemyWaveData, value ?? new GenericWaveSettings()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(EnemyWaveData));
                _enemyWaveData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EnemyWaveData));
            }
        }
    }

    [JsonPropertyName("EnemyID")]
    public uint EnemyID
    {
        get => _enemyId;
        set => SetField(ref _enemyId, value);
    }

    [JsonPropertyName("Position")]
    public Vector3Data Position
    {
        get => _position;
        set
        {
            var old = _position;
            if (SetField(ref _position, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Position));
                _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
            }
        }
    }

    [JsonPropertyName("Count")]
    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    [JsonPropertyName("Enabled")]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [JsonPropertyName("WorldEventObjectFilter")]
    public string WorldEventObjectFilter
    {
        get => _worldEventObjectFilter;
        set => SetField(ref _worldEventObjectFilter, value ?? string.Empty);
    }

    // AWO Property Accessors
    [JsonPropertyName("CleanUpEnemiesBehind")]
    public bool CleanUpEnemiesBehind
    {
        get => _cleanUpEnemiesBehind;
        set => SetField(ref _cleanUpEnemiesBehind, value);
    }

    [JsonPropertyName("SpecialText")]
    public string SpecialText
    {
        get => _specialText;
        set => SetField(ref _specialText, value ?? string.Empty);
    }

    [JsonPropertyName("SpecialNumber")]
    public int SpecialNumber
    {
        get => _specialNumber;
        set => SetField(ref _specialNumber, value);
    }

    [JsonPropertyName("Reactor")]
    public AwoReactorData Reactor
    {
        get => _reactor;
        set
        {
            var old = _reactor;
            if (SetField(ref _reactor, value ?? new AwoReactorData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Reactor));
                _reactor.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Reactor));
            }
        }
    }

    [JsonPropertyName("Countdown")]
    public AwoCountdownData Countdown
    {
        get => _countdown;
        set
        {
            var old = _countdown;
            if (SetField(ref _countdown, value ?? new AwoCountdownData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Countdown));
                _countdown.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Countdown));
            }
        }
    }

    [JsonPropertyName("SetZoneLight")]
    public AwoSetZoneLightData SetZoneLight
    {
        get => _setZoneLight;
        set
        {
            var old = _setZoneLight;
            if (SetField(ref _setZoneLight, value ?? new AwoSetZoneLightData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SetZoneLight));
                _setZoneLight.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SetZoneLight));
            }
        }
    }

    [JsonPropertyName("CleanupEnemies")]
    public AwoCleanupEnemiesData CleanupEnemies
    {
        get => _cleanupEnemies;
        set
        {
            var old = _cleanupEnemies;
            if (SetField(ref _cleanupEnemies, value ?? new AwoCleanupEnemiesData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(CleanupEnemies));
                _cleanupEnemies.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CleanupEnemies));
            }
        }
    }

    [JsonPropertyName("SpawnHibernates")]
    public AwoSpawnHibernatesData SpawnHibernates
    {
        get => _spawnHibernates;
        set
        {
            var old = _spawnHibernates;
            if (SetField(ref _spawnHibernates, value ?? new AwoSpawnHibernatesData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SpawnHibernates));
                _spawnHibernates.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SpawnHibernates));
            }
        }
    }

    [JsonPropertyName("SpawnScouts")]
    public AwoSpawnScoutsData SpawnScouts
    {
        get => _spawnScouts;
        set
        {
            var old = _spawnScouts;
            if (SetField(ref _spawnScouts, value ?? new AwoSpawnScoutsData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SpawnScouts));
                _spawnScouts.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SpawnScouts));
            }
        }
    }

    [JsonPropertyName("AddTerminalCommand")]
    public AwoAddTerminalCommandData AddTerminalCommand
    {
        get => _addTerminalCommand;
        set
        {
            var old = _addTerminalCommand;
            if (SetField(ref _addTerminalCommand, value ?? new AwoAddTerminalCommandData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(AddTerminalCommand));
                _addTerminalCommand.PropertyChanged += (s, e) => OnPropertyChanged(nameof(AddTerminalCommand));
            }
        }
    }

    [JsonPropertyName("HideTerminalCommand")]
    public AwoHideTerminalCommandData HideTerminalCommand
    {
        get => _hideTerminalCommand;
        set
        {
            var old = _hideTerminalCommand;
            if (SetField(ref _hideTerminalCommand, value ?? new AwoHideTerminalCommandData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(HideTerminalCommand));
                _hideTerminalCommand.PropertyChanged += (s, e) => OnPropertyChanged(nameof(HideTerminalCommand));
            }
        }
    }

    [JsonPropertyName("UnhideTerminalCommand")]
    public AwoUnhideTerminalCommandData UnhideTerminalCommand
    {
        get => _unhideTerminalCommand;
        set
        {
            var old = _unhideTerminalCommand;
            if (SetField(ref _unhideTerminalCommand, value ?? new AwoUnhideTerminalCommandData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(UnhideTerminalCommand));
                _unhideTerminalCommand.PropertyChanged += (s, e) => OnPropertyChanged(nameof(UnhideTerminalCommand));
            }
        }
    }

    [JsonPropertyName("ActiveEnemyWave")]
    public AwoActiveEnemyWaveData ActiveEnemyWave
    {
        get => _activeEnemyWave;
        set
        {
            var old = _activeEnemyWave;
            if (SetField(ref _activeEnemyWave, value ?? new AwoActiveEnemyWaveData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(ActiveEnemyWave));
                _activeEnemyWave.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ActiveEnemyWave));
            }
        }
    }

    [JsonPropertyName("GiveResource")]
    public AwoGiveResourceData GiveResource
    {
        get => _giveResource;
        set
        {
            var old = _giveResource;
            if (SetField(ref _giveResource, value ?? new AwoGiveResourceData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(GiveResource));
                _giveResource.PropertyChanged += (s, e) => OnPropertyChanged(nameof(GiveResource));
            }
        }
    }

    [JsonPropertyName("DialogueID")]
    public uint DialogueID
    {
        get => _dialogueId;
        set => SetField(ref _dialogueId, value);
    }

    [JsonPropertyName("SpecialBool")]
    public bool SpecialBool
    {
        get => _specialBool;
        set => SetField(ref _specialBool, value);
    }

    [JsonPropertyName("NestedEvent")]
    public AwoNestedEventData NestedEvent
    {
        get => _nestedEvent;
        set
        {
            var old = _nestedEvent;
            if (SetField(ref _nestedEvent, value ?? new AwoNestedEventData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(NestedEvent));
                _nestedEvent.PropertyChanged += (s, e) => OnPropertyChanged(nameof(NestedEvent));
            }
        }
    }

    [JsonPropertyName("EventLoop")]
    public AwoEventLoopData EventLoop
    {
        get => _eventLoop;
        set
        {
            var old = _eventLoop;
            if (SetField(ref _eventLoop, value ?? new AwoEventLoopData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(EventLoop));
                _eventLoop.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EventLoop));
            }
        }
    }

    [JsonPropertyName("TeleportPlayer")]
    public AwoTeleportPlayerData TeleportPlayer
    {
        get => _teleportPlayer;
        set
        {
            var old = _teleportPlayer;
            if (SetField(ref _teleportPlayer, value ?? new AwoTeleportPlayerData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(TeleportPlayer));
                _teleportPlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TeleportPlayer));
            }
        }
    }

    [JsonPropertyName("InfectPlayer")]
    public AwoInfectPlayerData InfectPlayer
    {
        get => _infectPlayer;
        set
        {
            var old = _infectPlayer;
            if (SetField(ref _infectPlayer, value ?? new AwoInfectPlayerData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(InfectPlayer));
                _infectPlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(InfectPlayer));
            }
        }
    }

    [JsonPropertyName("DamagePlayer")]
    public AwoDamagePlayerData DamagePlayer
    {
        get => _damagePlayer;
        set
        {
            var old = _damagePlayer;
            if (SetField(ref _damagePlayer, value ?? new AwoDamagePlayerData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(DamagePlayer));
                _damagePlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DamagePlayer));
            }
        }
    }

    [JsonPropertyName("RevivePlayer")]
    public AwoRevivePlayerData RevivePlayer
    {
        get => _revivePlayer;
        set
        {
            var old = _revivePlayer;
            if (SetField(ref _revivePlayer, value ?? new AwoRevivePlayerData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(RevivePlayer));
                _revivePlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(RevivePlayer));
            }
        }
    }

    [JsonPropertyName("AdjustTimer")]
    public AwoAdjustTimerData AdjustTimer
    {
        get => _adjustTimer;
        set
        {
            var old = _adjustTimer;
            if (SetField(ref _adjustTimer, value ?? new AwoAdjustTimerData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(AdjustTimer));
                _adjustTimer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(AdjustTimer));
            }
        }
    }

    [JsonPropertyName("Countup")]
    public AwoCountupData Countup
    {
        get => _countup;
        set
        {
            var old = _countup;
            if (SetField(ref _countup, value ?? new AwoCountupData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Countup));
                _countup.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Countup));
            }
        }
    }

    [JsonPropertyName("NavMarker")]
    public AwoNavMarkerData NavMarker
    {
        get => _navMarker;
        set
        {
            var old = _navMarker;
            if (SetField(ref _navMarker, value ?? new AwoNavMarkerData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(NavMarker));
                _navMarker.PropertyChanged += (s, e) => OnPropertyChanged(nameof(NavMarker));
            }
        }
    }

    [JsonPropertyName("Portal")]
    public AwoPortalData Portal
    {
        get => _portal;
        set
        {
            var old = _portal;
            if (SetField(ref _portal, value ?? new AwoPortalData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Portal));
                _portal.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Portal));
            }
        }
    }

    [JsonPropertyName("SuccessScreen")]
    public AwoSuccessScreenData SuccessScreen
    {
        get => _successScreen;
        set
        {
            var old = _successScreen;
            if (SetField(ref _successScreen, value ?? new AwoSuccessScreenData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SuccessScreen));
                _successScreen.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SuccessScreen));
            }
        }
    }

    [JsonPropertyName("MultiProgression")]
    public AwoMultiProgressionData MultiProgression
    {
        get => _multiProgression;
        set
        {
            var old = _multiProgression;
            if (SetField(ref _multiProgression, value ?? new AwoMultiProgressionData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(MultiProgression));
                _multiProgression.PropertyChanged += (s, e) => OnPropertyChanged(nameof(MultiProgression));
            }
        }
    }

    [JsonPropertyName("WaveRoarSound")]
    public AwoWaveRoarSoundData WaveRoarSound
    {
        get => _waveRoarSound;
        set
        {
            var old = _waveRoarSound;
            if (SetField(ref _waveRoarSound, value ?? new AwoWaveRoarSoundData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(WaveRoarSound));
                _waveRoarSound.PropertyChanged += (s, e) => OnPropertyChanged(nameof(WaveRoarSound));
            }
        }
    }

    [JsonPropertyName("CustomHud")]
    public AwoCustomHudData CustomHud
    {
        get => _customHud;
        set
        {
            var old = _customHud;
            if (SetField(ref _customHud, value ?? new AwoCustomHudData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(CustomHud));
                _customHud.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CustomHud));
            }
        }
    }

    [JsonPropertyName("SpecialHud")]
    public AwoSpecialHudData SpecialHud
    {
        get => _specialHud;
        set
        {
            var old = _specialHud;
            if (SetField(ref _specialHud, value ?? new AwoSpecialHudData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SpecialHud));
                _specialHud.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SpecialHud));
            }
        }
    }

    [JsonPropertyName("PlayerDialogue")]
    public AwoPlayerDialogueData PlayerDialogue
    {
        get => _playerDialogue;
        set
        {
            var old = _playerDialogue;
            if (SetField(ref _playerDialogue, value ?? new AwoPlayerDialogueData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(PlayerDialogue));
                _playerDialogue.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PlayerDialogue));
            }
        }
    }

    [JsonPropertyName("SetTerminalLog")]
    public AwoSetTerminalLogData SetTerminalLog
    {
        get => _setTerminalLog;
        set
        {
            var old = _setTerminalLog;
            if (SetField(ref _setTerminalLog, value ?? new AwoSetTerminalLogData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(SetTerminalLog));
                _setTerminalLog.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SetTerminalLog));
            }
        }
    }

    [JsonPropertyName("ObjectiveItems")]
    public AwoPocketItemsData ObjectiveItems
    {
        get => _objectiveItems;
        set
        {
            var old = _objectiveItems;
            if (SetField(ref _objectiveItems, value ?? new AwoPocketItemsData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(ObjectiveItems));
                _objectiveItems.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ObjectiveItems));
            }
        }
    }

    [JsonPropertyName("EnvironmentData")]
    public AwoEnvironmentData EnvironmentData
    {
        get => _environmentData;
        set
        {
            var old = _environmentData;
            if (SetField(ref _environmentData, value ?? new AwoEnvironmentData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(EnvironmentData));
                _environmentData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EnvironmentData));
            }
        }
    }

    [JsonPropertyName("FadeScreen")]
    public AwoFadeScreenData FadeScreen
    {
        get => _fadeScreen;
        set
        {
            var old = _fadeScreen;
            if (SetField(ref _fadeScreen, value ?? new AwoFadeScreenData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(FadeScreen));
                _fadeScreen.PropertyChanged += (s, e) => OnPropertyChanged(nameof(FadeScreen));
            }
        }
    }

    [JsonPropertyName("CameraShake")]
    public AwoCameraShakeData CameraShake
    {
        get => _cameraShake;
        set
        {
            var old = _cameraShake;
            if (SetField(ref _cameraShake, value ?? new AwoCameraShakeData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(CameraShake));
                _cameraShake.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CameraShake));
            }
        }
    }

    // Visibility Flags
    [JsonIgnore]
    public bool IsChainPuzzleVisible => WardenEventVisibilityConfig.IsChainPuzzleVisible(Type);

    [JsonIgnore]
    public bool IsUseStaticBioscanPointsVisible => WardenEventVisibilityConfig.IsUseStaticBioscanPointsVisible(Type);

    [JsonIgnore]
    public bool IsDurationVisible => WardenEventVisibilityConfig.IsDurationVisible(Type);

    [JsonIgnore]
    public bool IsClearDimensionVisible => WardenEventVisibilityConfig.IsClearDimensionVisible(Type);

    [JsonIgnore]
    public bool IsCustomSubObjectiveVisible => WardenEventVisibilityConfig.IsCustomSubObjectiveVisible(Type);

    [JsonIgnore]
    public bool IsCustomSubObjectiveHeaderVisible => WardenEventVisibilityConfig.IsCustomSubObjectiveVisible(Type);

    [JsonIgnore]
    public bool IsFogSettingVisible => WardenEventVisibilityConfig.IsFogSettingVisible(Type);

    [JsonIgnore]
    public bool IsFogTransitionDurationVisible => WardenEventVisibilityConfig.IsFogTransitionDurationVisible(Type);

    [JsonIgnore]
    public bool IsEnemyWaveDataVisible => WardenEventVisibilityConfig.IsEnemyWaveDataVisible(Type);

    [JsonIgnore]
    public bool IsEnemyIdVisible => WardenEventVisibilityConfig.IsEnemyIdVisible(Type);

    [JsonIgnore]
    public bool IsPositionVisible => WardenEventVisibilityConfig.IsPositionVisible(Type);

    [JsonIgnore]
    public bool IsCountVisible => WardenEventVisibilityConfig.IsCountVisible(Type);

    [JsonIgnore]
    public bool IsWorldEventObjectFilterVisible => WardenEventVisibilityConfig.IsWorldEventObjectFilterVisible(Type);

    [JsonIgnore]
    public bool IsCleanUpEnemiesBehindVisible => WardenEventVisibilityConfig.IsCleanUpEnemiesBehindVisible(Type);

    [JsonIgnore]
    public bool IsSpecialTextVisible => WardenEventVisibilityConfig.IsSpecialTextVisible(Type);

    [JsonIgnore]
    public bool IsReactorVisible => WardenEventVisibilityConfig.IsReactorVisible(Type);

    [JsonIgnore]
    public bool IsCountdownVisible => WardenEventVisibilityConfig.IsCountdownVisible(Type);

    [JsonIgnore]
    public bool IsSetZoneLightVisible => WardenEventVisibilityConfig.IsSetZoneLightVisible(Type);

    [JsonIgnore]
    public bool IsSpecialNumberVisible => WardenEventVisibilityConfig.IsSpecialNumberVisible(Type);

    [JsonIgnore]
    public bool IsCleanupEnemiesVisible => WardenEventVisibilityConfig.IsCleanupEnemiesVisible(Type);

    [JsonIgnore]
    public bool IsSpawnHibernatesVisible => WardenEventVisibilityConfig.IsSpawnHibernatesVisible(Type);

    [JsonIgnore]
    public bool IsSpawnScoutsVisible => WardenEventVisibilityConfig.IsSpawnScoutsVisible(Type);

    [JsonIgnore]
    public bool IsAddTerminalCommandVisible => WardenEventVisibilityConfig.IsAddTerminalCommandVisible(Type);

    [JsonIgnore]
    public bool IsHideTerminalCommandVisible => WardenEventVisibilityConfig.IsHideTerminalCommandVisible(Type);

    [JsonIgnore]
    public bool IsUnhideTerminalCommandVisible => WardenEventVisibilityConfig.IsUnhideTerminalCommandVisible(Type);

    [JsonIgnore]
    public bool IsActiveEnemyWaveVisible => WardenEventVisibilityConfig.IsActiveEnemyWaveVisible(Type);

    [JsonIgnore]
    public bool IsGiveResourceVisible => WardenEventVisibilityConfig.IsGiveResourceVisible(Type);

    [JsonIgnore]
    public bool IsDialogueIdVisible => WardenEventVisibilityConfig.IsDialogueIdVisible(Type);

    [JsonIgnore]
    public bool IsNestedEventVisible => WardenEventVisibilityConfig.IsNestedEventVisible(Type);

    [JsonIgnore]
    public bool IsEventLoopVisible => WardenEventVisibilityConfig.IsEventLoopVisible(Type);

    [JsonIgnore]
    public bool IsTeleportPlayerVisible => WardenEventVisibilityConfig.IsTeleportPlayerVisible(Type);

    [JsonIgnore]
    public bool IsInfectPlayerVisible => WardenEventVisibilityConfig.IsInfectPlayerVisible(Type);

    [JsonIgnore]
    public bool IsDamagePlayerVisible => WardenEventVisibilityConfig.IsDamagePlayerVisible(Type);

    [JsonIgnore]
    public bool IsRevivePlayerVisible => WardenEventVisibilityConfig.IsRevivePlayerVisible(Type);

    [JsonIgnore]
    public bool IsAdjustTimerVisible => WardenEventVisibilityConfig.IsAdjustTimerVisible(Type);

    [JsonIgnore]
    public bool IsCountupVisible => WardenEventVisibilityConfig.IsCountupVisible(Type);

    [JsonIgnore]
    public bool IsNavMarkerVisible => WardenEventVisibilityConfig.IsNavMarkerVisible(Type);

    [JsonIgnore]
    public bool IsPortalVisible => WardenEventVisibilityConfig.IsPortalVisible(Type);

    [JsonIgnore]
    public bool IsSuccessScreenVisible => WardenEventVisibilityConfig.IsSuccessScreenVisible(Type);

    [JsonIgnore]
    public bool IsMultiProgressionVisible => WardenEventVisibilityConfig.IsMultiProgressionVisible(Type);

    [JsonIgnore]
    public bool IsWaveRoarSoundVisible => WardenEventVisibilityConfig.IsWaveRoarSoundVisible(Type);

    [JsonIgnore]
    public bool IsCustomHudVisible => WardenEventVisibilityConfig.IsCustomHudVisible(Type);

    [JsonIgnore]
    public bool IsSpecialHudVisible => WardenEventVisibilityConfig.IsSpecialHudVisible(Type);

    [JsonIgnore]
    public bool IsPlayerDialogueVisible => WardenEventVisibilityConfig.IsPlayerDialogueVisible(Type);

    [JsonIgnore]
    public bool IsSetTerminalLogVisible => WardenEventVisibilityConfig.IsSetTerminalLogVisible(Type);

    [JsonIgnore]
    public bool IsObjectiveItemsVisible => WardenEventVisibilityConfig.IsObjectiveItemsVisible(Type);

    [JsonIgnore]
    public bool IsEnvironmentDataVisible => WardenEventVisibilityConfig.IsEnvironmentDataVisible(Type);

    [JsonIgnore]
    public bool IsSpecialBoolVisible => WardenEventVisibilityConfig.IsSpecialBoolVisible(Type);

    [JsonIgnore]
    public bool IsFadeScreenVisible => WardenEventVisibilityConfig.IsFadeScreenVisible(Type);

    [JsonIgnore]
    public bool IsCameraShakeVisible => WardenEventVisibilityConfig.IsCameraShakeVisible(Type);

    [JsonIgnore]
    public string DisplayName => $"Event: {Type}" + (Delay > 0 ? $" (Delay: {Delay}s)" : string.Empty);

    public void NotifyVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsChainPuzzleVisible));
        OnPropertyChanged(nameof(IsUseStaticBioscanPointsVisible));
        OnPropertyChanged(nameof(IsDurationVisible));
        OnPropertyChanged(nameof(IsClearDimensionVisible));
        OnPropertyChanged(nameof(IsCustomSubObjectiveVisible));
        OnPropertyChanged(nameof(IsCustomSubObjectiveHeaderVisible));
        OnPropertyChanged(nameof(IsFogSettingVisible));
        OnPropertyChanged(nameof(IsFogTransitionDurationVisible));
        OnPropertyChanged(nameof(IsEnemyWaveDataVisible));
        OnPropertyChanged(nameof(IsEnemyIdVisible));
        OnPropertyChanged(nameof(IsPositionVisible));
        OnPropertyChanged(nameof(IsCountVisible));
        OnPropertyChanged(nameof(IsWorldEventObjectFilterVisible));
        OnPropertyChanged(nameof(IsCleanUpEnemiesBehindVisible));
        OnPropertyChanged(nameof(IsSpecialTextVisible));
        OnPropertyChanged(nameof(IsReactorVisible));
        OnPropertyChanged(nameof(IsCountdownVisible));
        OnPropertyChanged(nameof(IsSetZoneLightVisible));
        OnPropertyChanged(nameof(IsSpecialNumberVisible));
        OnPropertyChanged(nameof(IsCleanupEnemiesVisible));
        OnPropertyChanged(nameof(IsSpawnHibernatesVisible));
        OnPropertyChanged(nameof(IsSpawnScoutsVisible));
        OnPropertyChanged(nameof(IsAddTerminalCommandVisible));
        OnPropertyChanged(nameof(IsHideTerminalCommandVisible));
        OnPropertyChanged(nameof(IsUnhideTerminalCommandVisible));
        OnPropertyChanged(nameof(IsActiveEnemyWaveVisible));
        OnPropertyChanged(nameof(IsGiveResourceVisible));
        OnPropertyChanged(nameof(IsDialogueIdVisible));
        OnPropertyChanged(nameof(IsNestedEventVisible));
        OnPropertyChanged(nameof(IsEventLoopVisible));
        OnPropertyChanged(nameof(IsTeleportPlayerVisible));
        OnPropertyChanged(nameof(IsInfectPlayerVisible));
        OnPropertyChanged(nameof(IsDamagePlayerVisible));
        OnPropertyChanged(nameof(IsRevivePlayerVisible));
        OnPropertyChanged(nameof(IsAdjustTimerVisible));
        OnPropertyChanged(nameof(IsCountupVisible));
        OnPropertyChanged(nameof(IsNavMarkerVisible));
        OnPropertyChanged(nameof(IsPortalVisible));
        OnPropertyChanged(nameof(IsSuccessScreenVisible));
        OnPropertyChanged(nameof(IsMultiProgressionVisible));
        OnPropertyChanged(nameof(IsWaveRoarSoundVisible));
        OnPropertyChanged(nameof(IsCustomHudVisible));
        OnPropertyChanged(nameof(IsSpecialHudVisible));
        OnPropertyChanged(nameof(IsPlayerDialogueVisible));
        OnPropertyChanged(nameof(IsSetTerminalLogVisible));
        OnPropertyChanged(nameof(IsObjectiveItemsVisible));
        OnPropertyChanged(nameof(IsEnvironmentDataVisible));
        OnPropertyChanged(nameof(IsSpecialBoolVisible));
        OnPropertyChanged(nameof(IsFadeScreenVisible));
        OnPropertyChanged(nameof(IsCameraShakeVisible));
        OnPropertyChanged(nameof(AvailableAreas));
        OnPropertyChanged(nameof(AvailableAreasOnly));
        OnPropertyChanged(nameof(AvailableTerminals));
    }

    public WardenEvent Clone()
    {
        return new WardenEvent
        {
            Type = Type,
            Trigger = Trigger,
            ChainPuzzle = ChainPuzzle,
            UseStaticBioscanPoints = UseStaticBioscanPoints,
            Layer = Layer,
            Dimension = Dimension,
            Zone = Zone,
            Delay = Delay,
            Duration = Duration,
            ClearDimension = ClearDimension,
            WardenIntel = WardenIntel,
            CustomSubObjectiveHeader = CustomSubObjectiveHeader,
            CustomSubObjective = CustomSubObjective,
            SoundID = SoundID,
            SoundSubtitle = SoundSubtitle,
            DialogueID = DialogueID,
            FogSetting = FogSetting,
            FogTransitionDuration = FogTransitionDuration,
            EnemyWaveData = EnemyWaveData?.Clone() ?? new GenericWaveSettings(),
            EnemyID = EnemyID,
            Position = Position?.Clone() ?? new Vector3Data(),
            Count = Count,
            Enabled = Enabled,
            WorldEventObjectFilter = WorldEventObjectFilter,
            CleanUpEnemiesBehind = CleanUpEnemiesBehind,
            SpecialText = SpecialText,
            SpecialNumber = SpecialNumber,
            SpecialBool = SpecialBool,
            Reactor = Reactor?.Clone() ?? new AwoReactorData(),
            Countdown = Countdown?.Clone() ?? new AwoCountdownData(),
            SetZoneLight = SetZoneLight?.Clone() ?? new AwoSetZoneLightData(),
            CleanupEnemies = CleanupEnemies?.Clone() ?? new AwoCleanupEnemiesData(),
            SpawnHibernates = SpawnHibernates?.Clone() ?? new AwoSpawnHibernatesData(),
            SpawnScouts = SpawnScouts?.Clone() ?? new AwoSpawnScoutsData(),
            AddTerminalCommand = AddTerminalCommand?.Clone() ?? new AwoAddTerminalCommandData(),
            HideTerminalCommand = HideTerminalCommand?.Clone() ?? new AwoHideTerminalCommandData(),
            UnhideTerminalCommand = UnhideTerminalCommand?.Clone() ?? new AwoUnhideTerminalCommandData(),
            ActiveEnemyWave = ActiveEnemyWave?.Clone() ?? new AwoActiveEnemyWaveData(),
            GiveResource = GiveResource?.Clone() ?? new AwoGiveResourceData(),
            NestedEvent = NestedEvent?.Clone() ?? new AwoNestedEventData(),
            EventLoop = EventLoop?.Clone() ?? new AwoEventLoopData(),
            TeleportPlayer = TeleportPlayer?.Clone() ?? new AwoTeleportPlayerData(),
            InfectPlayer = InfectPlayer?.Clone() ?? new AwoInfectPlayerData(),
            DamagePlayer = DamagePlayer?.Clone() ?? new AwoDamagePlayerData(),
            RevivePlayer = RevivePlayer?.Clone() ?? new AwoRevivePlayerData(),
            AdjustTimer = AdjustTimer?.Clone() ?? new AwoAdjustTimerData(),
            Countup = Countup?.Clone() ?? new AwoCountupData(),
            NavMarker = NavMarker?.Clone() ?? new AwoNavMarkerData(),
            Portal = Portal?.Clone() ?? new AwoPortalData(),
            SuccessScreen = SuccessScreen?.Clone() ?? new AwoSuccessScreenData(),
            MultiProgression = MultiProgression?.Clone() ?? new AwoMultiProgressionData(),
            WaveRoarSound = WaveRoarSound?.Clone() ?? new AwoWaveRoarSoundData(),
            CustomHud = CustomHud?.Clone() ?? new AwoCustomHudData(),
            SpecialHud = SpecialHud?.Clone() ?? new AwoSpecialHudData(),
            PlayerDialogue = PlayerDialogue?.Clone() ?? new AwoPlayerDialogueData(),
            SetTerminalLog = SetTerminalLog?.Clone() ?? new AwoSetTerminalLogData(),
            ObjectiveItems = ObjectiveItems?.Clone() ?? new AwoPocketItemsData(),
            EnvironmentData = EnvironmentData?.Clone() ?? new AwoEnvironmentData(),
            FadeScreen = FadeScreen?.Clone() ?? new AwoFadeScreenData(),
            CameraShake = CameraShake?.Clone() ?? new AwoCameraShakeData()
        };
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
