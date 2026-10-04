using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
    private bool _isSecurityDoor;
    private bool _isSecondaryDoor;
    private bool _isOverloadDoor;
    private bool _isConnected;
    private bool _isBlocked;
    private PlacedDoorNode? _connectedDoor;

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

    public bool IsSecurityDoor
    {
        get => _isSecurityDoor;
        set
        {
            if (SetField(ref _isSecurityDoor, value))
            {
                if (value)
                {
                    _isSecondaryDoor = false;
                    _isOverloadDoor = false;
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
                    _isOverloadDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
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
                    _isSecondaryDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
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
                case DoorType.SecurityDoor:
                    IsSecurityDoor = true;
                    break;
                default:
                    _isSecurityDoor = false;
                    _isSecondaryDoor = false;
                    _isOverloadDoor = false;
                    OnPropertyChanged(nameof(IsSecurityDoor));
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

    public bool IsZoneBoundary => IsSecurityDoor || IsSecondaryDoor || IsOverloadDoor;

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
        set => SetField(ref _connectedDoor, value);
    }

    public bool IsInternal => AreaTo >= 0;

    public string DoorBrush
    {
        get
        {
            if (IsBlocked) return "#757575"; // Grey
            if (IsSecondaryDoor) return "#FFA200"; // Secondary Sector
            if (IsOverloadDoor) return "#00F2FF"; // Overload Sector
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
            else if (IsSecurityDoor) secLabel = " [Security Door]";

            if (IsBlocked)
            {
                return IsInternal
                    ? $"Room Door ({Direction}, {RotationAngle:0.#}°){secLabel} [Area {AreaFrom} -> Area {AreaTo}] (Blocked by Zone / Security Door Constraint)"
                    : $"Geomorph Door ({Direction}, {RotationAngle:0.#}°){secLabel} (Blocked by Zone / Security Door Constraint)";
            }

            if (IsInternal)
            {
                if (IsConnected)
                {
                    return $"Room Door ({Direction}, {RotationAngle:0.#}°){secLabel} [Area {AreaFrom} <-> Area {AreaTo}] (Connected)";
                }
                int ungeneratedArea = (Geomorph != null && Geomorph.IsRoomGenerated(AreaFrom)) ? AreaTo : AreaFrom;
                return $"Room Door ({Direction}, {RotationAngle:0.#}°){secLabel} [Area {AreaFrom} -> Area {AreaTo}] (Click to expand Area {ungeneratedArea})";
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
    private double _worldX;
    private double _worldY;
    private double _worldLeft;
    private double _worldTop;
    private int _widthInCells = 1;
    private int _heightInCells = 1;
    private GeomorphMetadata _metadata = new();
    private ObservableCollection<PlacedDoorNode> _doors = new();
    private readonly HashSet<int> _generatedRooms = new();
    private readonly Dictionary<int, int> _roomZones = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyCollection<int> GeneratedRooms => _generatedRooms;
    public IReadOnlyDictionary<int, int> RoomZones => _roomZones;

    public bool IsRoomGenerated(int areaIndex) => _generatedRooms.Contains(areaIndex);

    public bool GenerateRoom(int areaIndex)
    {
        return _generatedRooms.Add(areaIndex);
    }

    public int GetRoomZone(int areaIndex)
    {
        return _roomZones.TryGetValue(areaIndex, out int zone) ? zone : 0;
    }

    public void SetRoomZone(int areaIndex, int zoneIndex)
    {
        _roomZones[areaIndex] = zoneIndex;
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

public class GeomorphOption
{
    public string DisplayName { get; set; } = string.Empty;
    public string PrefabPath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(Category)
        ? DisplayName
        : $"{DisplayName} ({Category})";
}
