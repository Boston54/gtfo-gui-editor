using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class PlacedDoorNode : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private PlacedGeomorphNode _geomorph = null!;
    private int _expanderIndex;
    private int _areaFrom;
    private int _areaTo = -1;
    private double _localX;
    private double _localY;
    private double _worldX;
    private double _worldY;
    private string _direction = string.Empty;
    private double _rotationAngle;
    private GeomorphAltitude _altitude = GeomorphAltitude.Mid;
    private bool _isSecurityDoor;
    private bool _isMainDoor;
    private bool _isSecondaryDoor;
    private bool _isOverloadDoor;
    private bool _isConnected;
    private bool _isBlocked;
    private PlacedDoorNode? _connectedDoor;
    private uint _chainedPuzzleToSolve;
    private int _aliasOverride;
    private string _aliasPrefixShortOverride = string.Empty;
    private bool _turnOffAlarmOnTerminal;
    private int _terminalZone;
    private int _terminalIndex;
    private ObservableCollection<WardenEvent> _eventsOnPortalWarp = new();
    private ObservableCollection<WardenEvent> _eventsOnApproachDoor = new();
    private ObservableCollection<WardenEvent> _eventsOnUnlockDoor = new();
    private ObservableCollection<WardenEvent> _eventsOnOpenDoor = new();
    private ObservableCollection<WardenEvent> _eventsOnDoorScanStart = new();
    private ObservableCollection<WardenEvent> _eventsOnDoorScanDone = new();
    private ObservableCollection<WardenEvent> _eventsOnBossDeath = new();
    private ObservableCollection<WardenEvent> _eventsOnTrigger = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public PlacedGeomorphNode Geomorph
    {
        get => _geomorph;
        set => SetField(ref _geomorph, value);
    }

    public int ExpanderIndex
    {
        get => _expanderIndex;
        set => SetField(ref _expanderIndex, value);
    }

    public int AreaFrom
    {
        get => _areaFrom;
        set => SetField(ref _areaFrom, value);
    }

    public int AreaTo
    {
        get => _areaTo;
        set
        {
            if (SetField(ref _areaTo, value))
            {
                OnPropertyChanged(nameof(IsInternal));
                OnPropertyChanged(nameof(DoorWidth));
                OnPropertyChanged(nameof(DoorHeight));
                OnPropertyChanged(nameof(CanvasLeft));
                OnPropertyChanged(nameof(CanvasTop));
            }
        }
    }

    public double LocalX
    {
        get => _localX;
        set => SetField(ref _localX, value);
    }

    public double LocalY
    {
        get => _localY;
        set => SetField(ref _localY, value);
    }

    public double WorldX
    {
        get => _worldX;
        set
        {
            if (SetField(ref _worldX, value))
            {
                OnPropertyChanged(nameof(CanvasLeft));
            }
        }
    }

    public double WorldY
    {
        get => _worldY;
        set
        {
            if (SetField(ref _worldY, value))
            {
                OnPropertyChanged(nameof(CanvasTop));
            }
        }
    }

    public string Direction
    {
        get => _direction;
        set
        {
            if (SetField(ref _direction, value))
            {
                OnPropertyChanged(nameof(IsVertical));
                OnPropertyChanged(nameof(DoorWidth));
                OnPropertyChanged(nameof(DoorHeight));
                OnPropertyChanged(nameof(CanvasLeft));
                OnPropertyChanged(nameof(CanvasTop));
            }
        }
    }

    public double RotationAngle
    {
        get => _rotationAngle;
        set
        {
            if (SetField(ref _rotationAngle, value))
            {
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public GeomorphAltitude Altitude
    {
        get => _altitude;
        set => SetField(ref _altitude, value);
    }

    public bool IsSecurityDoor
    {
        get => _isSecurityDoor;
        set
        {
            if (SetField(ref _isSecurityDoor, value))
            {
                if (value)
                {
                    _isMainDoor = false;
                    _isSecondaryDoor = false;
                    _isOverloadDoor = false;
                    OnPropertyChanged(nameof(IsMainDoor));
                    OnPropertyChanged(nameof(IsSecondaryDoor));
                    OnPropertyChanged(nameof(IsOverloadDoor));
                    OnPropertyChanged(nameof(DoorType));
                }
                OnPropertyChanged(nameof(IsZoneBoundary));
                OnPropertyChanged(nameof(DoorBrush));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public bool IsMainDoor
    {
        get => _isMainDoor;
        set
        {
            if (SetField(ref _isMainDoor, value))
            {
                if (value)
                {
                    _isSecurityDoor = false;
                    _isSecondaryDoor = false;
                    _isOverloadDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
                    OnPropertyChanged(nameof(IsSecondaryDoor));
                    OnPropertyChanged(nameof(IsOverloadDoor));
                    OnPropertyChanged(nameof(DoorType));
                }
                OnPropertyChanged(nameof(IsZoneBoundary));
                OnPropertyChanged(nameof(DoorBrush));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public bool IsSecondaryDoor
    {
        get => _isSecondaryDoor;
        set
        {
            if (SetField(ref _isSecondaryDoor, value))
            {
                if (value)
                {
                    _isSecurityDoor = false;
                    _isMainDoor = false;
                    _isOverloadDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
                    OnPropertyChanged(nameof(IsMainDoor));
                    OnPropertyChanged(nameof(IsOverloadDoor));
                    OnPropertyChanged(nameof(DoorType));
                }
                OnPropertyChanged(nameof(IsZoneBoundary));
                OnPropertyChanged(nameof(DoorBrush));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public bool IsOverloadDoor
    {
        get => _isOverloadDoor;
        set
        {
            if (SetField(ref _isOverloadDoor, value))
            {
                if (value)
                {
                    _isSecurityDoor = false;
                    _isMainDoor = false;
                    _isSecondaryDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
                    OnPropertyChanged(nameof(IsMainDoor));
                    OnPropertyChanged(nameof(IsSecondaryDoor));
                    OnPropertyChanged(nameof(DoorType));
                }
                OnPropertyChanged(nameof(IsZoneBoundary));
                OnPropertyChanged(nameof(DoorBrush));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public DoorType DoorType
    {
        get
        {
            if (IsSecondaryDoor) return DoorType.SecondaryDoor;
            if (IsOverloadDoor) return DoorType.OverloadDoor;
            if (IsMainDoor) return DoorType.MainDoor;
            if (IsSecurityDoor) return DoorType.SecurityDoor;
            return DoorType.RoomDoor;
        }
        set
        {
            switch (value)
            {
                case DoorType.SecondaryDoor:
                    IsSecondaryDoor = true;
                    break;
                case DoorType.OverloadDoor:
                    IsOverloadDoor = true;
                    break;
                case DoorType.MainDoor:
                    IsMainDoor = true;
                    break;
                case DoorType.SecurityDoor:
                    IsSecurityDoor = true;
                    break;
                default:
                    _isSecurityDoor = false;
                    _isMainDoor = false;
                    _isSecondaryDoor = false;
                    _isOverloadDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
                    OnPropertyChanged(nameof(IsMainDoor));
                    OnPropertyChanged(nameof(IsSecondaryDoor));
                    OnPropertyChanged(nameof(IsOverloadDoor));
                    OnPropertyChanged(nameof(DoorType));
                    OnPropertyChanged(nameof(IsZoneBoundary));
                    OnPropertyChanged(nameof(DoorBrush));
                    OnPropertyChanged(nameof(TooltipText));
                    break;
            }
        }
    }

    public bool IsZoneBoundary => IsSecurityDoor || IsMainDoor || IsSecondaryDoor || IsOverloadDoor;

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetField(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(DoorBrush));
                OnPropertyChanged(nameof(DoorStatusLabel));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public bool IsBlocked
    {
        get => _isBlocked;
        set
        {
            if (SetField(ref _isBlocked, value))
            {
                OnPropertyChanged(nameof(DoorBrush));
                OnPropertyChanged(nameof(DoorStatusLabel));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public PlacedDoorNode? ConnectedDoor
    {
        get => _connectedDoor;
        set
        {
            if (SetField(ref _connectedDoor, value) && value != null)
            {
                if (value.ChainedPuzzleToSolve == 0 && ChainedPuzzleToSolve != 0)
                    value.ChainedPuzzleToSolve = ChainedPuzzleToSolve;
                else if (ChainedPuzzleToSolve == 0 && value.ChainedPuzzleToSolve != 0)
                    ChainedPuzzleToSolve = value.ChainedPuzzleToSolve;

                if (value.AliasOverride == 0 && AliasOverride != 0)
                    value.AliasOverride = AliasOverride;
                else if (AliasOverride == 0 && value.AliasOverride != 0)
                    AliasOverride = value.AliasOverride;

                if (string.IsNullOrEmpty(value.AliasPrefixShortOverride) && !string.IsNullOrEmpty(AliasPrefixShortOverride))
                    value.AliasPrefixShortOverride = AliasPrefixShortOverride;
                else if (string.IsNullOrEmpty(AliasPrefixShortOverride) && !string.IsNullOrEmpty(value.AliasPrefixShortOverride))
                    AliasPrefixShortOverride = value.AliasPrefixShortOverride;

                if (value.TurnOffAlarmOnTerminal != TurnOffAlarmOnTerminal)
                    value.TurnOffAlarmOnTerminal = TurnOffAlarmOnTerminal;

                if (value.TerminalZone != TerminalZone)
                    value.TerminalZone = TerminalZone;

                if (value.TerminalIndex != TerminalIndex)
                    value.TerminalIndex = TerminalIndex;

                SyncEventCollections(this, value);
            }
        }
    }

    private static void SyncEventCollections(PlacedDoorNode a, PlacedDoorNode b)
    {
        if (a.EventsOnPortalWarp.Count > 0 && b.EventsOnPortalWarp.Count == 0)
            b.EventsOnPortalWarp = a.EventsOnPortalWarp;
        else if (b.EventsOnPortalWarp.Count > 0 && a.EventsOnPortalWarp.Count == 0)
            a.EventsOnPortalWarp = b.EventsOnPortalWarp;
        else if (a.EventsOnPortalWarp != b.EventsOnPortalWarp && a.EventsOnPortalWarp.Count > 0)
            b.EventsOnPortalWarp = a.EventsOnPortalWarp;

        if (a.EventsOnApproachDoor.Count > 0 && b.EventsOnApproachDoor.Count == 0)
            b.EventsOnApproachDoor = a.EventsOnApproachDoor;
        else if (b.EventsOnApproachDoor.Count > 0 && a.EventsOnApproachDoor.Count == 0)
            a.EventsOnApproachDoor = b.EventsOnApproachDoor;
        else if (a.EventsOnApproachDoor != b.EventsOnApproachDoor && a.EventsOnApproachDoor.Count > 0)
            b.EventsOnApproachDoor = a.EventsOnApproachDoor;

        if (a.EventsOnUnlockDoor.Count > 0 && b.EventsOnUnlockDoor.Count == 0)
            b.EventsOnUnlockDoor = a.EventsOnUnlockDoor;
        else if (b.EventsOnUnlockDoor.Count > 0 && a.EventsOnUnlockDoor.Count == 0)
            a.EventsOnUnlockDoor = b.EventsOnUnlockDoor;
        else if (a.EventsOnUnlockDoor != b.EventsOnUnlockDoor && a.EventsOnUnlockDoor.Count > 0)
            b.EventsOnUnlockDoor = a.EventsOnUnlockDoor;

        if (a.EventsOnOpenDoor.Count > 0 && b.EventsOnOpenDoor.Count == 0)
            b.EventsOnOpenDoor = a.EventsOnOpenDoor;
        else if (b.EventsOnOpenDoor.Count > 0 && a.EventsOnOpenDoor.Count == 0)
            a.EventsOnOpenDoor = b.EventsOnOpenDoor;
        else if (a.EventsOnOpenDoor != b.EventsOnOpenDoor && a.EventsOnOpenDoor.Count > 0)
            b.EventsOnOpenDoor = a.EventsOnOpenDoor;

        if (a.EventsOnDoorScanStart.Count > 0 && b.EventsOnDoorScanStart.Count == 0)
            b.EventsOnDoorScanStart = a.EventsOnDoorScanStart;
        else if (b.EventsOnDoorScanStart.Count > 0 && a.EventsOnDoorScanStart.Count == 0)
            a.EventsOnDoorScanStart = b.EventsOnDoorScanStart;
        else if (a.EventsOnDoorScanStart != b.EventsOnDoorScanStart && a.EventsOnDoorScanStart.Count > 0)
            b.EventsOnDoorScanStart = a.EventsOnDoorScanStart;

        if (a.EventsOnDoorScanDone.Count > 0 && b.EventsOnDoorScanDone.Count == 0)
            b.EventsOnDoorScanDone = a.EventsOnDoorScanDone;
        else if (b.EventsOnDoorScanDone.Count > 0 && a.EventsOnDoorScanDone.Count == 0)
            a.EventsOnDoorScanDone = b.EventsOnDoorScanDone;
        else if (a.EventsOnDoorScanDone != b.EventsOnDoorScanDone && a.EventsOnDoorScanDone.Count > 0)
            b.EventsOnDoorScanDone = a.EventsOnDoorScanDone;

        if (a.EventsOnBossDeath.Count > 0 && b.EventsOnBossDeath.Count == 0)
            b.EventsOnBossDeath = a.EventsOnBossDeath;
        else if (b.EventsOnBossDeath.Count > 0 && a.EventsOnBossDeath.Count == 0)
            a.EventsOnBossDeath = b.EventsOnBossDeath;
        else if (a.EventsOnBossDeath != b.EventsOnBossDeath && a.EventsOnBossDeath.Count > 0)
            b.EventsOnBossDeath = a.EventsOnBossDeath;

        if (a.EventsOnTrigger.Count > 0 && b.EventsOnTrigger.Count == 0)
            b.EventsOnTrigger = a.EventsOnTrigger;
        else if (b.EventsOnTrigger.Count > 0 && a.EventsOnTrigger.Count == 0)
            a.EventsOnTrigger = b.EventsOnTrigger;
        else if (a.EventsOnTrigger != b.EventsOnTrigger && a.EventsOnTrigger.Count > 0)
            b.EventsOnTrigger = a.EventsOnTrigger;
    }

    public uint ChainedPuzzleToSolve
    {
        get => _chainedPuzzleToSolve;
        set
        {
            if (SetField(ref _chainedPuzzleToSolve, value))
            {
                if (ConnectedDoor != null && ConnectedDoor.ChainedPuzzleToSolve != value)
                {
                    ConnectedDoor.ChainedPuzzleToSolve = value;
                }
            }
        }
    }

    public int AliasOverride
    {
        get => _aliasOverride;
        set
        {
            if (SetField(ref _aliasOverride, value))
            {
                if (ConnectedDoor != null && ConnectedDoor.AliasOverride != value)
                {
                    ConnectedDoor.AliasOverride = value;
                }
            }
        }
    }

    public string AliasPrefixShortOverride
    {
        get => _aliasPrefixShortOverride;
        set
        {
            string val = value ?? string.Empty;
            if (SetField(ref _aliasPrefixShortOverride, val))
            {
                if (ConnectedDoor != null && ConnectedDoor.AliasPrefixShortOverride != val)
                {
                    ConnectedDoor.AliasPrefixShortOverride = val;
                }
            }
        }
    }

    public bool TurnOffAlarmOnTerminal
    {
        get => _turnOffAlarmOnTerminal;
        set
        {
            if (SetField(ref _turnOffAlarmOnTerminal, value))
            {
                if (ConnectedDoor != null && ConnectedDoor.TurnOffAlarmOnTerminal != value)
                {
                    ConnectedDoor.TurnOffAlarmOnTerminal = value;
                }
            }
        }
    }

    public int TerminalZone
    {
        get => _terminalZone;
        set
        {
            if (SetField(ref _terminalZone, value))
            {
                if (ConnectedDoor != null && ConnectedDoor.TerminalZone != value)
                {
                    ConnectedDoor.TerminalZone = value;
                }
            }
        }
    }

    public int TerminalIndex
    {
        get => _terminalIndex;
        set
        {
            if (SetField(ref _terminalIndex, value))
            {
                if (ConnectedDoor != null && ConnectedDoor.TerminalIndex != value)
                {
                    ConnectedDoor.TerminalIndex = value;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnPortalWarp
    {
        get => _eventsOnPortalWarp;
        set
        {
            if (SetField(ref _eventsOnPortalWarp, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnPortalWarp != _eventsOnPortalWarp)
                {
                    ConnectedDoor.EventsOnPortalWarp = _eventsOnPortalWarp;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnApproachDoor
    {
        get => _eventsOnApproachDoor;
        set
        {
            if (SetField(ref _eventsOnApproachDoor, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnApproachDoor != _eventsOnApproachDoor)
                {
                    ConnectedDoor.EventsOnApproachDoor = _eventsOnApproachDoor;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnUnlockDoor
    {
        get => _eventsOnUnlockDoor;
        set
        {
            if (SetField(ref _eventsOnUnlockDoor, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnUnlockDoor != _eventsOnUnlockDoor)
                {
                    ConnectedDoor.EventsOnUnlockDoor = _eventsOnUnlockDoor;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnOpenDoor
    {
        get => _eventsOnOpenDoor;
        set
        {
            if (SetField(ref _eventsOnOpenDoor, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnOpenDoor != _eventsOnOpenDoor)
                {
                    ConnectedDoor.EventsOnOpenDoor = _eventsOnOpenDoor;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnDoorScanStart
    {
        get => _eventsOnDoorScanStart;
        set
        {
            if (SetField(ref _eventsOnDoorScanStart, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnDoorScanStart != _eventsOnDoorScanStart)
                {
                    ConnectedDoor.EventsOnDoorScanStart = _eventsOnDoorScanStart;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnDoorScanDone
    {
        get => _eventsOnDoorScanDone;
        set
        {
            if (SetField(ref _eventsOnDoorScanDone, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnDoorScanDone != _eventsOnDoorScanDone)
                {
                    ConnectedDoor.EventsOnDoorScanDone = _eventsOnDoorScanDone;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnBossDeath
    {
        get => _eventsOnBossDeath;
        set
        {
            if (SetField(ref _eventsOnBossDeath, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnBossDeath != _eventsOnBossDeath)
                {
                    ConnectedDoor.EventsOnBossDeath = _eventsOnBossDeath;
                }
            }
        }
    }

    public ObservableCollection<WardenEvent> EventsOnTrigger
    {
        get => _eventsOnTrigger;
        set
        {
            if (SetField(ref _eventsOnTrigger, value ?? new()))
            {
                if (ConnectedDoor != null && ConnectedDoor.EventsOnTrigger != _eventsOnTrigger)
                {
                    ConnectedDoor.EventsOnTrigger = _eventsOnTrigger;
                }
            }
        }
    }

    public bool IsInternal => AreaTo >= 0;

    public string DoorBrush
    {
        get
        {
            if (IsBlocked) return "#757575"; // Grey
            if (IsSecondaryDoor) return "#FFA200"; // Secondary Sector
            if (IsOverloadDoor) return "#00F2FF"; // Overload Sector
            if (IsMainDoor) return "#CCCCCC"; // Main Door (Light Grey)
            if (IsSecurityDoor) return "#FF0000"; // Security Door
            if (IsConnected) return "#9D00FF"; // Expanded / Connected
            return "#FFFF00"; // Unexpanded
        }
    }

    public string DoorStatusLabel => IsBlocked ? "Blocked" : (IsConnected ? "Connected" : "Open");

    public bool IsVertical => string.Equals(Direction, "Left", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(Direction, "Right", StringComparison.OrdinalIgnoreCase)
                           || Math.Abs(LocalX) > 28;

    public double DoorWidth => 4.0;

    public double DoorHeight => 1.5;

    public double CanvasLeft => WorldX - (DoorWidth / 2.0);

    public double CanvasTop => WorldY - (DoorHeight / 2.0);

    public string TooltipText
    {
        get
        {
            string secLabel = "";
            if (IsSecondaryDoor) secLabel = " [Secondary Door]";
            else if (IsOverloadDoor) secLabel = " [Overload Door]";
            else if (IsMainDoor) secLabel = " [Main Door]";
            else if (IsSecurityDoor) secLabel = " [Security Door]";

            if (IsBlocked)
            {
                return IsInternal
                    ? $"Room Door ({Direction}, {RotationAngle:0.#}°){secLabel} [Area {AreaFrom} -> Area {AreaTo}] (Blocked by Zone / Security Door Constraint)"
                    : $"Geomorph Door ({Direction}, {RotationAngle:0.#}°){secLabel} (Blocked by Zone / Security Door Constraint)";
            }

            if (IsInternal)
            {
                var roomFrom = Geomorph?.Rooms.FirstOrDefault(r => r.AreaIndex == AreaFrom);
                var roomTo = Geomorph?.Rooms.FirstOrDefault(r => r.AreaIndex == AreaTo);
                string descFrom = roomFrom != null ? roomFrom.UserFacingDisplayName : $"Area {AreaFrom}";
                string descTo = roomTo != null ? roomTo.UserFacingDisplayName : $"Area {AreaTo}";

                if (IsConnected)
                {
                    return $"Room Door ({Direction}, {RotationAngle:0.#}°){secLabel} [{descFrom} <-> {descTo}] (Connected)";
                }
                int ungeneratedArea = (Geomorph != null && Geomorph.IsRoomGenerated(AreaFrom)) ? AreaTo : AreaFrom;
                var sourceRoom = (Geomorph != null && Geomorph.IsRoomGenerated(AreaFrom)) ? roomFrom : roomTo;
                string sourceDesc = sourceRoom != null ? sourceRoom.UserFacingDisplayName : $"Area {(Geomorph != null && Geomorph.IsRoomGenerated(AreaFrom) ? AreaFrom : AreaTo)}";
                return $"Room Door ({Direction}, {RotationAngle:0.#}°){secLabel} [From {sourceDesc}] (Click to expand Area {ungeneratedArea})";
            }

            string status = IsConnected ? "(Connected)" : "(Click to expand geomorph)";
            return $"Geomorph Door ({Direction}, {RotationAngle:0.#}°){secLabel} {status}";
        }
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

public class PlacedGeomorphNode : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private string _prefabPath = string.Empty;
    private string _displayName = string.Empty;
    private int _gridX = 0;
    private int _gridY = 0;
    private double _cellDimension = 64.0;
    private double _rotationAngle;
    private GeomorphAltitude _altitude = GeomorphAltitude.Mid;
    private double _altitudeOffset = 6.0;
    private uint _lighting;
    private double _worldX;
    private double _worldY;
    private double _worldLeft;
    private double _worldTop;
    private int _widthInCells = 1;
    private int _heightInCells = 1;
    private GeomorphMetadata _metadata = new();
    private ObservableCollection<PlacedDoorNode> _doors = new();
    private ObservableCollection<PlacedRoomNode> _rooms = new();
    private readonly HashSet<int> _generatedRooms = new();
    private readonly Dictionary<int, int> _roomZones = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public GeomorphAltitude Altitude
    {
        get => _altitude;
        set
        {
            if (SetField(ref _altitude, value))
            {
                OnPropertyChanged(nameof(AltitudeOffsetValue));
                UpdateRoomsFillBrush();
                foreach (var door in Doors)
                {
                    door.Altitude = value;
                }
            }
        }
    }

    public double AltitudeOffset
    {
        get => _altitudeOffset;
        set
        {
            if (SetField(ref _altitudeOffset, value))
            {
                OnPropertyChanged(nameof(AltitudeOffsetValue));
                UpdateRoomsFillBrush();
            }
        }
    }

    public double AltitudeOffsetValue => Altitude switch
    {
        GeomorphAltitude.High => AltitudeOffset,
        GeomorphAltitude.Low => -AltitudeOffset,
        _ => 0.0
    };

    public uint Lighting
    {
        get => _lighting;
        set => SetField(ref _lighting, value);
    }

    public void UpdateRoomsFillBrush()
    {
        foreach (var room in Rooms)
        {
            room.NotifyAltitudeChanged();
        }
    }

    public IReadOnlyCollection<int> GeneratedRooms => _generatedRooms;
    public IReadOnlyDictionary<int, int> RoomZones => _roomZones;

    public bool IsRoomGenerated(int areaIndex) => _generatedRooms.Contains(areaIndex);

    public bool GenerateRoom(int areaIndex)
    {
        return _generatedRooms.Add(areaIndex);
    }

    public bool UngenerateRoom(int areaIndex)
    {
        _roomZones.Remove(areaIndex);
        var room = _rooms.FirstOrDefault(r => r.AreaIndex == areaIndex);
        if (room != null)
        {
            _rooms.Remove(room);
        }
        return _generatedRooms.Remove(areaIndex);
    }

    public int GetRoomZone(int areaIndex)
    {
        return _roomZones.TryGetValue(areaIndex, out int zone) ? zone : 0;
    }

    public void SetRoomZone(int areaIndex, int zoneIndex)
    {
        _roomZones[areaIndex] = zoneIndex;
        var room = _rooms.FirstOrDefault(r => r.AreaIndex == areaIndex);
        if (room != null)
        {
            room.Zone = zoneIndex;
        }
    }

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string PrefabPath
    {
        get => _prefabPath;
        set => SetField(ref _prefabPath, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public int GridX
    {
        get => _gridX;
        set
        {
            if (SetField(ref _gridX, value))
            {
                UpdateWorldPosition();
            }
        }
    }

    public int GridY
    {
        get => _gridY;
        set
        {
            if (SetField(ref _gridY, value))
            {
                UpdateWorldPosition();
            }
        }
    }

    public double CellDimension
    {
        get => _cellDimension;
        set
        {
            double val = value > 0 ? value : 64.0;
            if (SetField(ref _cellDimension, val))
            {
                OnPropertyChanged(nameof(WidthInPixels));
                OnPropertyChanged(nameof(HeightInPixels));
                UpdateWorldPosition();
            }
        }
    }

    public double RotationAngle
    {
        get => _rotationAngle;
        set
        {
            if (SetField(ref _rotationAngle, value))
            {
                UpdateWorldPosition();
            }
        }
    }

    public double WorldX
    {
        get => _worldX;
        private set => SetField(ref _worldX, value);
    }

    public double WorldY
    {
        get => _worldY;
        private set => SetField(ref _worldY, value);
    }

    public double WorldLeft
    {
        get => _worldLeft;
        private set => SetField(ref _worldLeft, value);
    }

    public double WorldTop
    {
        get => _worldTop;
        private set => SetField(ref _worldTop, value);
    }

    public int WidthInCells
    {
        get => _widthInCells;
        set
        {
            if (SetField(ref _widthInCells, value))
            {
                OnPropertyChanged(nameof(WidthInPixels));
                UpdateWorldPosition();
            }
        }
    }

    public int HeightInCells
    {
        get => _heightInCells;
        set
        {
            if (SetField(ref _heightInCells, value))
            {
                OnPropertyChanged(nameof(HeightInPixels));
                UpdateWorldPosition();
            }
        }
    }

    public double WidthInPixels => WidthInCells * CellDimension;

    public double HeightInPixels => HeightInCells * CellDimension;

    public GeomorphMetadata Metadata
    {
        get => _metadata;
        set => SetField(ref _metadata, value);
    }

    public ObservableCollection<PlacedDoorNode> Doors
    {
        get => _doors;
        set => SetField(ref _doors, value);
    }

    public ObservableCollection<PlacedRoomNode> Rooms
    {
        get => _rooms;
        set => SetField(ref _rooms, value);
    }

    public void UpdateWorldPosition()
    {
        WorldLeft = GridX * CellDimension;
        WorldTop = GridY * CellDimension;
        WorldX = WorldLeft + (WidthInCells * CellDimension) / 2.0;
        WorldY = WorldTop + (HeightInCells * CellDimension) / 2.0;

        foreach (var door in Doors)
        {
            door.WorldX = WorldX + door.LocalX;
            door.WorldY = WorldY + door.LocalY;
        }

        foreach (var room in Rooms)
        {
            room.WorldX = WorldX;
            room.WorldY = WorldY;
        }
    }

    public bool OccupiesCell(int cellX, int cellY)
    {
        return cellX >= GridX && cellX < GridX + WidthInCells &&
               cellY >= GridY && cellY < GridY + HeightInCells;
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

public class GeomorphOption : INotifyPropertyChanged
{
    private Bitmap? _imageBitmap;
    private bool _imageAttempted;
    private string _imageName = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName { get; set; } = string.Empty;
    public string PrefabPath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Complex { get; set; } = string.Empty;

    public string ImageName
    {
        get => _imageName;
        set
        {
            if (_imageName != value)
            {
                _imageName = value;
                _imageAttempted = false;
                _imageBitmap = null;
                OnPropertyChanged(nameof(ImageName));
                OnPropertyChanged(nameof(HasImageBitmap));
            }
        }
    }

    public Bitmap? ImageBitmap
    {
        get
        {
            if (_imageBitmap == null && !_imageAttempted && !string.IsNullOrWhiteSpace(ImageName))
            {
                _imageAttempted = true;
                _imageBitmap = GeomorphService.LoadImageBitmap(ImageName);
                if (_imageBitmap != null)
                {
                    OnPropertyChanged(nameof(HasImageBitmap));
                }
            }
            return _imageBitmap;
        }
        set
        {
            _imageBitmap = value;
            _imageAttempted = true;
            OnPropertyChanged(nameof(ImageBitmap));
            OnPropertyChanged(nameof(HasImageBitmap));
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageName);
    public bool HasImageBitmap => ImageBitmap != null;
    public string? ImagePath => GeomorphService.FindImageFile(ImageName);
    public Geometry? SvgGeometry { get; set; }
    public bool HasSvgGeometry => SvgGeometry != null;

    public string DisplayText => string.IsNullOrWhiteSpace(Category)
        ? DisplayName
        : $"{DisplayName} ({Category})";

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RotationOption
{
    public double RotationAngle { get; set; }
    public string DisplayText { get; set; } = string.Empty;

    public override string ToString() => DisplayText;
}

public class AltitudeOption
{
    public GeomorphAltitude Altitude { get; set; }
    public string DisplayText { get; set; } = string.Empty;

    public override string ToString() => DisplayText;
}

public class PlacedRoomNode : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private PlacedGeomorphNode _geomorph = null!;
    private int _areaIndex;
    private int _zone;
    private int _roomNumber;
    private double _worldX;
    private double _worldY;
    private string _svgContent = string.Empty;
    private string _svgPathData = string.Empty;
    private Geometry? _geometry;
    private IBrush _fillBrush = RoomSvgService.GetZoneFillBrush(0);
    private IBrush _strokeBrush = RoomSvgService.GetZoneStrokeBrush(0);
    private double _strokeThickness = 0.0;
    private bool _isVisible = true;
    private bool _isHovered;
    private bool _isZoneHighlighted;
    private double _area;
    private double _boundsMinX;
    private double _boundsMinY;
    private double _boundsMaxX;
    private double _boundsMaxY;
    private List<Triangle2D> _triangles2D = new();
    private double _meshHeight;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double MeshHeight
    {
        get => _meshHeight;
        set
        {
            if (SetField(ref _meshHeight, value))
            {
                OnPropertyChanged(nameof(TotalHeight));
                UpdateFillBrush();
            }
        }
    }

    public double TotalHeight => (Geomorph?.AltitudeOffsetValue ?? 0.0) + MeshHeight;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public PlacedGeomorphNode Geomorph
    {
        get => _geomorph;
        set => SetField(ref _geomorph, value);
    }

    public int AreaIndex
    {
        get => _areaIndex;
        set
        {
            if (SetField(ref _areaIndex, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public int Zone
    {
        get => _zone;
        set
        {
            if (SetField(ref _zone, value))
            {
                UpdateFillBrush();
                StrokeBrush = RoomSvgService.GetZoneStrokeBrush(value);
                OnPropertyChanged(nameof(UserFacingDisplayName));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public int RoomNumber
    {
        get => _roomNumber;
        set
        {
            if (SetField(ref _roomNumber, value))
            {
                OnPropertyChanged(nameof(RoomLetter));
                OnPropertyChanged(nameof(RoomName));
                OnPropertyChanged(nameof(UserFacingDisplayName));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string RoomLetter => GetRoomLetter(RoomNumber);

    public string RoomName => $"Room {RoomLetter}";

    public string UserFacingDisplayName => $"Zone {Zone}, {RoomName}";

    public static string GetRoomLetter(int roomNumber)
    {
        if (roomNumber < 0) return string.Empty;
        int cycle = (roomNumber / 26) + 1;
        char letter = (char)('A' + (roomNumber % 26));
        return cycle == 1 ? letter.ToString() : $"{letter}{cycle}";
    }

    public double WorldX
    {
        get => _worldX;
        set
        {
            if (SetField(ref _worldX, value))
            {
                OnPropertyChanged(nameof(CanvasLeft));
            }
        }
    }

    public double WorldY
    {
        get => _worldY;
        set
        {
            if (SetField(ref _worldY, value))
            {
                OnPropertyChanged(nameof(CanvasTop));
            }
        }
    }

    public double CanvasLeft => WorldX;
    public double CanvasTop => WorldY;

    public string SvgContent
    {
        get => _svgContent;
        set => SetField(ref _svgContent, value);
    }

    public string SvgPathData
    {
        get => _svgPathData;
        set => SetField(ref _svgPathData, value);
    }

    public Geometry? Geometry
    {
        get => _geometry;
        set
        {
            if (SetField(ref _geometry, value))
            {
                OnPropertyChanged(nameof(HasGeometry));
            }
        }
    }

    public bool HasGeometry => _geometry != null;

    public IBrush FillBrush
    {
        get => _fillBrush;
        set => SetField(ref _fillBrush, value);
    }

    public IBrush StrokeBrush
    {
        get => _strokeBrush;
        set => SetField(ref _strokeBrush, value);
    }

    public double StrokeThickness
    {
        get => _strokeThickness;
        set => SetField(ref _strokeThickness, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (SetField(ref _isHovered, value))
            {
                UpdateFillBrush();
                OnPropertyChanged(nameof(ZIndex));
            }
        }
    }

    public bool IsZoneHighlighted
    {
        get => _isZoneHighlighted;
        set
        {
            if (SetField(ref _isZoneHighlighted, value))
            {
                UpdateFillBrush();
                OnPropertyChanged(nameof(ZIndex));
            }
        }
    }

    public void SetHighlightState(bool isHovered, bool isZoneHighlighted)
    {
        bool changed = false;
        if (_isHovered != isHovered)
        {
            _isHovered = isHovered;
            OnPropertyChanged(nameof(IsHovered));
            changed = true;
        }
        if (_isZoneHighlighted != isZoneHighlighted)
        {
            _isZoneHighlighted = isZoneHighlighted;
            OnPropertyChanged(nameof(IsZoneHighlighted));
            changed = true;
        }
        if (changed)
        {
            UpdateFillBrush();
            OnPropertyChanged(nameof(ZIndex));
        }
    }

    public void NotifyAltitudeChanged()
    {
        OnPropertyChanged(nameof(TotalHeight));
        UpdateFillBrush();
    }

    public void UpdateFillBrush()
    {
        double altOffset = Geomorph?.AltitudeOffset ?? 6.0;
        double height = TotalHeight;

        if (IsHovered)
        {
            FillBrush = RoomSvgService.GetHoveredHeightFillBrush(height, altOffset);
        }
        else if (IsZoneHighlighted)
        {
            FillBrush = RoomSvgService.GetZoneHighlightHeightFillBrush(height, altOffset);
        }
        else
        {
            FillBrush = RoomSvgService.GetHeightFillBrush(height, altOffset);
        }
    }

    public int ZIndex => IsHovered ? 7 : (IsZoneHighlighted ? 6 : 5);

    public double Area
    {
        get => _area;
        set => SetField(ref _area, value);
    }

    public double BoundsMinX
    {
        get => _boundsMinX;
        set => SetField(ref _boundsMinX, value);
    }

    public double BoundsMinY
    {
        get => _boundsMinY;
        set => SetField(ref _boundsMinY, value);
    }

    public double BoundsMaxX
    {
        get => _boundsMaxX;
        set => SetField(ref _boundsMaxX, value);
    }

    public double BoundsMaxY
    {
        get => _boundsMaxY;
        set => SetField(ref _boundsMaxY, value);
    }

    public List<Triangle2D> Triangles2D
    {
        get => _triangles2D;
        set => SetField(ref _triangles2D, value);
    }

    public bool ContainsWorldPoint(double worldX, double worldY)
    {
        if (_triangles2D == null || _triangles2D.Count == 0)
        {
            return false;
        }

        double localX = worldX - WorldX;
        double localY = worldY - WorldY;

        if (localX < BoundsMinX - 0.5 || localX > BoundsMaxX + 0.5 ||
            localY < BoundsMinY - 0.5 || localY > BoundsMaxY + 0.5)
        {
            return false;
        }

        foreach (var tri in _triangles2D)
        {
            if (RoomSvgService.IsPointInTriangle(localX, localY, tri.X0, tri.Y0, tri.X1, tri.Y1, tri.X2, tri.Y2))
            {
                return true;
            }
        }

        return false;
    }

    public string DisplayName => $"{Geomorph?.DisplayName ?? "Room"} - {UserFacingDisplayName} (Area {AreaIndex})";

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

public class ZoneOption
{
    public int ZoneIndex { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}
