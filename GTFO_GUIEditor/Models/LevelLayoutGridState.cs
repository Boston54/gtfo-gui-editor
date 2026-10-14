using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class LevelLayoutGridState : INotifyPropertyChanged
{
    private int _gridSize = 40;
    private double _cellDimension = 64.0;
    private double _altitudeOffset = 6.0;
    private ObservableCollection<PlacedGeomorphNode> _placedGeomorphs = new();
    private ObservableCollection<PlacedDoorNode> _placedDoors = new();
    private ObservableCollection<PlacedRoomNode> _placedRooms = new();
    private PlacedRoomNode? _hoveredRoom;
    private double _zoom = 4.0;
    private double _panX = 400.0;
    private double _panY = 300.0;
    private double _viewportWidth = 800;
    private double _viewportHeight = 600;
    private string _currentStartingPrefab = string.Empty;
    private int _nextZoneIndex = 1;
    private readonly Dictionary<int, ZoneSettings> _zoneSettings = new();

    public LevelLayoutGridState()
    {
        _placedDoors.CollectionChanged += PlacedDoors_CollectionChanged;
    }

    public ZoneSettings GetOrCreateZoneSettings(int zoneIndex)
    {
        if (!_zoneSettings.TryGetValue(zoneIndex, out var settings))
        {
            settings = new ZoneSettings { ZoneIndex = zoneIndex };
            _zoneSettings[zoneIndex] = settings;
        }
        return settings;
    }

    public IReadOnlyDictionary<int, ZoneSettings> ZoneSettingsMap => _zoneSettings;

    private void PlacedDoors_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            // Reset occurred
        }
        else
        {
            if (e.NewItems != null)
            {
                foreach (PlacedDoorNode door in e.NewItems)
                {
                    door.PropertyChanged += OnDoorPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (PlacedDoorNode door in e.OldItems)
                {
                    door.PropertyChanged -= OnDoorPropertyChanged;
                }
            }
        }
        NotifyLegendProperties();
    }

    private void OnDoorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlacedDoorNode.IsSecurityDoor) ||
            e.PropertyName == nameof(PlacedDoorNode.IsMainDoor) ||
            e.PropertyName == nameof(PlacedDoorNode.IsSecondaryDoor) ||
            e.PropertyName == nameof(PlacedDoorNode.IsOverloadDoor) ||
            e.PropertyName == nameof(PlacedDoorNode.IsConnected) ||
            e.PropertyName == nameof(PlacedDoorNode.IsBlocked) ||
            e.PropertyName == nameof(PlacedDoorNode.DoorBrush) ||
            e.PropertyName == nameof(PlacedDoorNode.DoorType))
        {
            NotifyLegendProperties();
        }
    }

    private void NotifyLegendProperties()
    {
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
        OnPropertyChanged(nameof(HasUnexpandedDoors));
        OnPropertyChanged(nameof(HasConnectedDoors));
        OnPropertyChanged(nameof(HasSecurityDoors));
        OnPropertyChanged(nameof(HasMainDoors));
        OnPropertyChanged(nameof(HasSecondaryDoors));
        OnPropertyChanged(nameof(HasOverloadDoors));
        OnPropertyChanged(nameof(HasBlockedDoors));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool HasSecondaryDoor => PlacedDoors.Any(d => d.IsSecondaryDoor);
    public bool HasOverloadDoor => PlacedDoors.Any(d => d.IsOverloadDoor);

    public bool HasUnexpandedDoors => PlacedDoors.Any(d => d.DoorBrush == "#FFFF00");
    public bool HasConnectedDoors => PlacedDoors.Any(d => d.DoorBrush == "#9D00FF");
    public bool HasSecurityDoors => PlacedDoors.Any(d => d.DoorBrush == "#FF0000");
    public bool HasMainDoors => PlacedDoors.Any(d => d.DoorBrush == "#CCCCCC");
    public bool HasSecondaryDoors => PlacedDoors.Any(d => d.DoorBrush == "#FFA200");
    public bool HasOverloadDoors => PlacedDoors.Any(d => d.DoorBrush == "#00F2FF");
    public bool HasBlockedDoors => PlacedDoors.Any(d => d.DoorBrush == "#757575");

    public int GetNextZoneId() => _nextZoneIndex++;
    public void ResetZoneIdCounter() => _nextZoneIndex = 1;

    public int GridSize
    {
        get => _gridSize;
        set
        {
            int val = Math.Max(1, value);
            if (SetField(ref _gridSize, val))
            {
                OnPropertyChanged(nameof(TotalWorldSize));
                OnPropertyChanged(nameof(WorldGridWidth));
                OnPropertyChanged(nameof(WorldGridHeight));
                OnPropertyChanged(nameof(WorldGridLeft));
                OnPropertyChanged(nameof(WorldGridTop));
                OnPropertyChanged(nameof(HasGridSizeWarning));
                OnPropertyChanged(nameof(GridSizeWarningMessage));
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
                OnPropertyChanged(nameof(TotalWorldSize));
                OnPropertyChanged(nameof(WorldGridWidth));
                OnPropertyChanged(nameof(WorldGridHeight));
                OnPropertyChanged(nameof(WorldGridLeft));
                OnPropertyChanged(nameof(WorldGridTop));
                foreach (var geo in PlacedGeomorphs)
                {
                    geo.CellDimension = val;
                }
            }
        }
    }

    public double AltitudeOffset
    {
        get => _altitudeOffset;
        set
        {
            double val = value > 0 ? value : 6.0;
            if (SetField(ref _altitudeOffset, val))
            {
                foreach (var geo in PlacedGeomorphs)
                {
                    geo.AltitudeOffset = val;
                }
            }
        }
    }

    public double TotalWorldSize => GridSize * CellDimension;
    public double WorldGridWidth => GridSize * CellDimension;
    public double WorldGridHeight => GridSize * CellDimension;
    public double WorldGridLeft => MinGridCoord * CellDimension;
    public double WorldGridTop => MinGridCoord * CellDimension;

    public int MinGridCoord => -(GridSize / 2);
    public int MaxGridCoord => GridSize % 2 == 0 ? (GridSize / 2 - 1) : (GridSize / 2);

    public (int width, int height, int minX, int maxX, int minY, int maxY) GetLayoutBounds()
    {
        if (PlacedGeomorphs.Count == 0)
        {
            return (0, 0, 0, 0, 0, 0);
        }

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var geo in PlacedGeomorphs)
        {
            int geoMinX = geo.GridX;
            int geoMaxX = geo.GridX + geo.WidthInCells - 1;
            int geoMinY = geo.GridY;
            int geoMaxY = geo.GridY + geo.HeightInCells - 1;

            if (geoMinX < minX) minX = geoMinX;
            if (geoMaxX > maxX) maxX = geoMaxX;
            if (geoMinY < minY) minY = geoMinY;
            if (geoMaxY > maxY) maxY = geoMaxY;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        return (width, height, minX, maxX, minY, maxY);
    }

    public bool CheckIfLayoutExceedsGrid(int gridSize)
    {
        if (PlacedGeomorphs.Count == 0) return false;
        var (width, height, minX, maxX, minY, maxY) = GetLayoutBounds();

        int minCoord = -(gridSize / 2);
        int maxCoord = gridSize % 2 == 0 ? (gridSize / 2 - 1) : (gridSize / 2);

        return width > gridSize || height > gridSize ||
               minX < minCoord || maxX > maxCoord ||
               minY < minCoord || maxY > maxCoord;
    }

    public bool HasGridSizeWarning => CheckIfLayoutExceedsGrid(GridSize);

    public string GridSizeWarningMessage
    {
        get
        {
            var (width, height, _, _, _, _) = GetLayoutBounds();
            return $"Warning: Current level layout dimensions ({width}x{height} cells) exceed the grid size ({GridSize}x{GridSize}). Increase the grid size in the Complex Resource Set.";
        }
    }

    public ObservableCollection<PlacedGeomorphNode> PlacedGeomorphs
    {
        get => _placedGeomorphs;
        set => SetField(ref _placedGeomorphs, value);
    }

    public ObservableCollection<PlacedDoorNode> PlacedDoors
    {
        get => _placedDoors;
        set => SetField(ref _placedDoors, value);
    }

    public ObservableCollection<PlacedRoomNode> PlacedRooms
    {
        get => _placedRooms;
        set => SetField(ref _placedRooms, value);
    }

    public PlacedRoomNode? HoveredRoom
    {
        get => _hoveredRoom;
        set
        {
            if (_hoveredRoom == value) return;
            _hoveredRoom = value;
            UpdateRoomHighlightStates();
            OnPropertyChanged(nameof(HoveredRoom));
        }
    }

    public void UpdateRoomHighlightStates()
    {
        int? activeZone = _hoveredRoom?.Zone;
        foreach (var room in _placedRooms)
        {
            bool isHovered = room == _hoveredRoom;
            bool isZoneHighlighted = !isHovered && activeZone.HasValue && room.Zone == activeZone.Value;
            room.SetHighlightState(isHovered, isZoneHighlighted);
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            double clamped = Math.Clamp(value, 0.1, 20.0);
            if (SetField(ref _zoom, clamped))
            {
                OnPropertyChanged(nameof(ZoomPercentage));
            }
        }
    }

    public string ZoomPercentage => $"{Math.Round(Zoom * 100)}%";

    public double PanX
    {
        get => _panX;
        set => SetField(ref _panX, value);
    }

    public double PanY
    {
        get => _panY;
        set => SetField(ref _panY, value);
    }

    public string CurrentStartingPrefab
    {
        get => _currentStartingPrefab;
        set => SetField(ref _currentStartingPrefab, value);
    }

    public double ViewportWidth
    {
        get => _viewportWidth;
        set => SetField(ref _viewportWidth, value);
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set => SetField(ref _viewportHeight, value);
    }

    public void Pan(double deltaX, double deltaY)
    {
        PanX += deltaX;
        PanY += deltaY;
    }

    public void ZoomIn(double? centerX = null, double? centerY = null)
    {
        ZoomAt(1.0, centerX ?? (ViewportWidth / 2.0), centerY ?? (ViewportHeight / 2.0));
    }

    public void ZoomOut(double? centerX = null, double? centerY = null)
    {
        ZoomAt(-1.0, centerX ?? (ViewportWidth / 2.0), centerY ?? (ViewportHeight / 2.0));
    }

    public void ZoomAt(double deltaWheel, double mouseX, double mouseY)
    {
        double oldZoom = Zoom;
        double zoomFactor = deltaWheel > 0 ? 1.15 : (1.0 / 1.15);
        double newZoom = Math.Clamp(oldZoom * zoomFactor, 0.1, 20.0);

        if (Math.Abs(newZoom - oldZoom) < 0.0001) return;

        // Zoom relative to mouse position:
        // mouseX = PanX + worldX * oldZoom => worldX = (mouseX - PanX) / oldZoom
        // newPanX = mouseX - worldX * newZoom = mouseX - (mouseX - PanX) * (newZoom / oldZoom)
        PanX = mouseX - (mouseX - PanX) * (newZoom / oldZoom);
        PanY = mouseY - (mouseY - PanY) * (newZoom / oldZoom);
        Zoom = newZoom;
    }

    public void ResetView(double viewportWidth = 800, double viewportHeight = 600)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Zoom = 4.0;

        // Center on starting elevator tile (or cell 0,0 center if no geomorph placed yet)
        double targetWorldX = 0.5 * CellDimension;
        double targetWorldY = 0.5 * CellDimension;

        if (PlacedGeomorphs.Count > 0)
        {
            targetWorldX = PlacedGeomorphs[0].WorldX;
            targetWorldY = PlacedGeomorphs[0].WorldY;
        }

        PanX = (viewportWidth / 2.0) - (targetWorldX * Zoom);
        PanY = (viewportHeight / 2.0) - (targetWorldY * Zoom);
    }

    public void Clear()
    {
        foreach (var door in PlacedDoors)
        {
            door.PropertyChanged -= OnDoorPropertyChanged;
        }
        HoveredRoom = null;
        PlacedGeomorphs.Clear();
        PlacedDoors.Clear();
        PlacedRooms.Clear();
        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
    }

    public void InitializeStartingTile(string? startingPrefab, int gridX = 0, int gridY = 0)
    {
        Clear();
        ResetZoneIdCounter();
        CurrentStartingPrefab = startingPrefab ?? string.Empty;

        if (string.IsNullOrWhiteSpace(startingPrefab))
        {
            OnPropertyChanged(nameof(HasGridSizeWarning));
            OnPropertyChanged(nameof(GridSizeWarningMessage));
            return;
        }

        var geoNode = CreateGeomorphNode(startingPrefab, gridX, gridY);
        geoNode.SetRoomZone(0, 0);
        PlacedGeomorphs.Add(geoNode);

        foreach (var room in geoNode.Rooms)
        {
            if (!PlacedRooms.Contains(room))
            {
                PlacedRooms.Add(room);
            }
        }

        foreach (var door in geoNode.Doors)
        {
            PlacedDoors.Add(door);
        }

        RecalculateSecurityDoorConstraints();

        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
    }

    public PlacedGeomorphNode? GetGeomorphAt(int cellX, int cellY)
    {
        return PlacedGeomorphs.FirstOrDefault(g => g.OccupiesCell(cellX, cellY));
    }

    public PlacedRoomNode? GetRoomAt(double worldX, double worldY)
    {
        PlacedRoomNode? bestRoom = null;
        double minArea = double.MaxValue;

        foreach (var room in PlacedRooms)
        {
            if (room.IsVisible && room.ContainsWorldPoint(worldX, worldY))
            {
                if (room.Area < minArea)
                {
                    minArea = room.Area;
                    bestRoom = room;
                }
            }
        }

        return bestRoom;
    }

    public static (double RotatedX, double RotatedY) RotateLocalOffset(double x, double y, double angleDegrees)
    {
        double normAngle = ((angleDegrees % 360) + 360) % 360;
        if (Math.Abs(normAngle - 0) < 0.1) return (x == 0 ? 0 : x, y == 0 ? 0 : y);
        if (Math.Abs(normAngle - 90) < 0.1) return (-y == 0 ? 0 : -y, x == 0 ? 0 : x);
        if (Math.Abs(normAngle - 180) < 0.1) return (-x == 0 ? 0 : -x, -y == 0 ? 0 : -y);
        if (Math.Abs(normAngle - 270) < 0.1) return (y == 0 ? 0 : y, -x == 0 ? 0 : -x);

        double rad = normAngle * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double rx = x * cos - y * sin;
        double ry = x * sin + y * cos;
        return (rx == 0 ? 0 : rx, ry == 0 ? 0 : ry);
    }

    public static string RotateDirection(string direction, double angleDegrees)
    {
        if (string.IsNullOrWhiteSpace(direction) || direction == "InZoneArea")
        {
            return direction;
        }

        double normAngle = ((angleDegrees % 360) + 360) % 360;
        int steps = (int)Math.Round(normAngle / 90.0) % 4;
        if (steps == 0) return direction;

        string[] order = { "Forwards", "Right", "Backwards", "Left" };
        int idx = Array.IndexOf(order, direction);
        if (idx >= 0)
        {
            return order[(idx + steps) % 4];
        }

        return direction;
    }

    public List<double> GetValidRotationsForGeomorph(PlacedDoorNode? sourceDoor, string? geomorphPrefab)
    {
        var validRotations = new List<double>();
        if (sourceDoor == null || string.IsNullOrWhiteSpace(geomorphPrefab))
        {
            return validRotations;
        }

        var metadata = GeomorphMetadataService.GetMetadata(geomorphPrefab);
        if (metadata?.AreaDatas == null || metadata.AreaDatas.Count == 0)
        {
            validRotations.Add(0.0);
            return validRotations;
        }

        var (targetX, targetY, _) = GetExpansionTargetCell(sourceDoor);

        int baseWidth = 1;
        int baseHeight = 1;
        string lowerPath = geomorphPrefab.ToLowerInvariant();
        if (lowerPath.Contains("128x128") || lowerPath.Contains("2x2"))
        {
            baseWidth = 2;
            baseHeight = 2;
        }
        else if (lowerPath.Contains("128x64") || lowerPath.Contains("2x1"))
        {
            baseWidth = 2;
            baseHeight = 1;
        }
        else if (lowerPath.Contains("64x128") || lowerPath.Contains("1x2"))
        {
            baseWidth = 1;
            baseHeight = 2;
        }

        double[] candidateAngles = { 0.0, 90.0, 180.0, 270.0 };
        foreach (double angle in candidateAngles)
        {
            double normAngle = ((angle % 360) + 360) % 360;
            int widthInCells = (Math.Abs(normAngle - 90) < 0.1 || Math.Abs(normAngle - 270) < 0.1) ? baseHeight : baseWidth;
            int heightInCells = (Math.Abs(normAngle - 90) < 0.1 || Math.Abs(normAngle - 270) < 0.1) ? baseWidth : baseHeight;

            double tempWorldLeft = targetX * CellDimension;
            double tempWorldTop = targetY * CellDimension;
            double tempWorldCenterX = tempWorldLeft + (widthInCells * CellDimension) / 2.0;
            double tempWorldCenterY = tempWorldTop + (heightInCells * CellDimension) / 2.0;

            bool hasMatchingDoor = false;
            foreach (var ad in metadata.AreaDatas)
            {
                if (ad.ExpanderDatas == null) continue;
                foreach (var exp in ad.ExpanderDatas)
                {
                    if (exp.AreaTo < 0) // External expander
                    {
                        var (localX, localY) = RotateLocalOffset(exp.ExpanderPosition.X, -exp.ExpanderPosition.Z, angle);
                        double expWorldX = tempWorldCenterX + localX;
                        double expWorldY = tempWorldCenterY + localY;
                        double dist = Math.Sqrt(Math.Pow(expWorldX - sourceDoor.WorldX, 2) + Math.Pow(expWorldY - sourceDoor.WorldY, 2));
                        if (dist < 10.0)
                        {
                            hasMatchingDoor = true;
                            break;
                        }
                    }
                }
                if (hasMatchingDoor) break;
            }

            if (hasMatchingDoor)
            {
                validRotations.Add(angle);
            }
        }

        return validRotations;
    }

    public (int targetX, int targetY, string directionName) GetExpansionTargetCell(PlacedDoorNode door)
    {
        var geo = door.Geomorph;
        string dir = door.Direction?.ToLowerInvariant() ?? string.Empty;

        if (dir.Contains("forward") || dir.Contains("north"))
        {
            return (geo.GridX, geo.GridY - 1, "North (Forwards)");
        }
        if (dir.Contains("backward") || dir.Contains("south"))
        {
            return (geo.GridX, geo.GridY + geo.HeightInCells, "South (Backwards)");
        }
        if (dir.Contains("left") || dir.Contains("west"))
        {
            return (geo.GridX - 1, geo.GridY, "West (Left)");
        }
        if (dir.Contains("right") || dir.Contains("east"))
        {
            return (geo.GridX + geo.WidthInCells, geo.GridY, "East (Right)");
        }

        // Coordinate-based fallback relative to geomorph center
        if (door.LocalY <= -20)
        {
            return (geo.GridX, geo.GridY - 1, "North (Forwards)");
        }
        if (door.LocalY >= 20)
        {
            return (geo.GridX, geo.GridY + geo.HeightInCells, "South (Backwards)");
        }
        if (door.LocalX <= -20)
        {
            return (geo.GridX - 1, geo.GridY, "West (Left)");
        }
        if (door.LocalX >= 20)
        {
            return (geo.GridX + geo.WidthInCells, geo.GridY, "East (Right)");
        }

        return (geo.GridX, geo.GridY - 1, "North (Forwards)");
    }

    public PlacedGeomorphNode? ExpandFromDoor(PlacedDoorNode sourceDoor, string geomorphPrefab, bool isSecurityDoor)
    {
        return ExpandFromDoor(sourceDoor, geomorphPrefab, isSecurityDoor ? DoorType.SecurityDoor : DoorType.RoomDoor);
    }

    public PlacedGeomorphNode? ExpandFromDoor(PlacedDoorNode sourceDoor, string geomorphPrefab, DoorType doorType = DoorType.RoomDoor, double? rotationAngle = null, GeomorphAltitude? altitude = null, uint? lighting = null)
    {
        if (sourceDoor.IsInternal) return null;

        var (targetX, targetY, _) = GetExpansionTargetCell(sourceDoor);
        int minCoord = -(GridSize / 2);
        int maxCoord = GridSize % 2 == 0 ? (GridSize / 2 - 1) : (GridSize / 2);
        if (targetX < minCoord || targetX > maxCoord || targetY < minCoord || targetY > maxCoord)
        {
            return null;
        }

        // Determine actual rotation angle to apply
        double actualRotation = 0.0;
        if (rotationAngle.HasValue)
        {
            actualRotation = rotationAngle.Value;
        }
        else
        {
            var validRotations = GetValidRotationsForGeomorph(sourceDoor, geomorphPrefab);
            actualRotation = validRotations.Count > 0 ? validRotations[0] : 0.0;
        }

        int baseWidth = 1;
        int baseHeight = 1;
        string lowerPath = geomorphPrefab.ToLowerInvariant();
        if (lowerPath.Contains("128x128") || lowerPath.Contains("2x2"))
        {
            baseWidth = 2;
            baseHeight = 2;
        }
        else if (lowerPath.Contains("128x64") || lowerPath.Contains("2x1"))
        {
            baseWidth = 2;
            baseHeight = 1;
        }
        else if (lowerPath.Contains("64x128") || lowerPath.Contains("1x2"))
        {
            baseWidth = 1;
            baseHeight = 2;
        }

        double normAngle = ((actualRotation % 360) + 360) % 360;
        int widthInCells = (Math.Abs(normAngle - 90) < 0.1 || Math.Abs(normAngle - 270) < 0.1) ? baseHeight : baseWidth;
        int heightInCells = (Math.Abs(normAngle - 90) < 0.1 || Math.Abs(normAngle - 270) < 0.1) ? baseWidth : baseHeight;

        for (int cx = targetX; cx < targetX + widthInCells; cx++)
        {
            for (int cy = targetY; cy < targetY + heightInCells; cy++)
            {
                if (cx < minCoord || cx > maxCoord || cy < minCoord || cy > maxCoord) return null;
                if (GetGeomorphAt(cx, cy) != null) return null;
            }
        }

        int sourceZone = sourceDoor.Geomorph?.GetRoomZone(sourceDoor.AreaFrom) ?? 0;
        bool isZoneBoundary = doorType != DoorType.RoomDoor;
        int targetZone = isZoneBoundary ? GetNextZoneId() : sourceZone;

        // Find which area in the new geomorph connects to sourceDoor
        var metadata = GeomorphMetadataService.GetMetadata(geomorphPrefab) ?? new GeomorphMetadata
        {
            GeoAssetLink = geomorphPrefab
        };

        int connectingArea = 0;
        double bestDist = double.MaxValue;

        double tempWorldLeft = targetX * CellDimension;
        double tempWorldTop = targetY * CellDimension;
        double tempWorldCenterX = tempWorldLeft + (widthInCells * CellDimension) / 2.0;
        double tempWorldCenterY = tempWorldTop + (heightInCells * CellDimension) / 2.0;

        if (metadata.AreaDatas != null)
        {
            for (int a = 0; a < metadata.AreaDatas.Count; a++)
            {
                var ad = metadata.AreaDatas[a];
                if (ad.ExpanderDatas == null) continue;

                foreach (var exp in ad.ExpanderDatas)
                {
                    if (exp.AreaTo < 0) // External expander
                    {
                        var (localX, localY) = RotateLocalOffset(exp.ExpanderPosition.X, -exp.ExpanderPosition.Z, actualRotation);
                        double expWorldX = tempWorldCenterX + localX;
                        double expWorldY = tempWorldCenterY + localY;
                        double dist = Math.Sqrt(Math.Pow(expWorldX - sourceDoor.WorldX, 2) + Math.Pow(expWorldY - sourceDoor.WorldY, 2));
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            connectingArea = a;
                        }
                    }
                }
            }
        }

        GeomorphAltitude actualAltitude = altitude ?? sourceDoor.Altitude;
        uint actualLighting = lighting ?? 0;
        var newGeo = CreateGeomorphNode(geomorphPrefab, targetX, targetY, initialAreaIndex: connectingArea, rotationAngle: actualRotation, altitude: actualAltitude, lighting: actualLighting);
        newGeo.SetRoomZone(connectingArea, targetZone);
        if (isZoneBoundary && actualLighting > 0)
        {
            GetOrCreateZoneSettings(targetZone).Lighting = actualLighting;
        }
        PlacedGeomorphs.Add(newGeo);

        foreach (var room in newGeo.Rooms)
        {
            if (!PlacedRooms.Contains(room))
            {
                PlacedRooms.Add(room);
            }
        }

        foreach (var door in newGeo.Doors)
        {
            PlacedDoors.Add(door);
        }

        ConnectAdjacentDoors(newGeo, sourceDoor, doorType);
        RecalculateSecurityDoorConstraints();

        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));

        return newGeo;
    }

    public int GenerateRoomFromDoor(PlacedDoorNode roomDoor, bool isSecurityDoor)
    {
        return GenerateRoomFromDoor(roomDoor, isSecurityDoor ? DoorType.SecurityDoor : DoorType.RoomDoor);
    }

    public int GenerateRoomFromDoor(PlacedDoorNode roomDoor, DoorType doorType = DoorType.RoomDoor, uint? lighting = null)
    {
        if (!roomDoor.IsInternal || roomDoor.Geomorph == null) return -1;

        var geo = roomDoor.Geomorph;
        int targetArea = -1;
        int sourceArea = -1;

        if (!geo.IsRoomGenerated(roomDoor.AreaFrom) && geo.IsRoomGenerated(roomDoor.AreaTo))
        {
            targetArea = roomDoor.AreaFrom;
            sourceArea = roomDoor.AreaTo;
        }
        else if (!geo.IsRoomGenerated(roomDoor.AreaTo) && geo.IsRoomGenerated(roomDoor.AreaFrom))
        {
            targetArea = roomDoor.AreaTo;
            sourceArea = roomDoor.AreaFrom;
        }

        if (targetArea >= 0 && sourceArea >= 0)
        {
            int sourceZone = geo.GetRoomZone(sourceArea);
            bool isZoneBoundary = (doorType != DoorType.RoomDoor || roomDoor.IsZoneBoundary);
            int targetZone = isZoneBoundary ? GetNextZoneId() : sourceZone;
            geo.SetRoomZone(targetArea, targetZone);
            if (isZoneBoundary && lighting.HasValue && lighting.Value > 0)
            {
                GetOrCreateZoneSettings(targetZone).Lighting = lighting.Value;
            }

            var newDoors = GenerateDoorsForRoom(geo, targetArea);
            foreach (var door in newDoors)
            {
                PlacedDoors.Add(door);
            }
        }

        if (geo.IsRoomGenerated(roomDoor.AreaFrom) && geo.IsRoomGenerated(roomDoor.AreaTo))
        {
            roomDoor.IsConnected = true;
        }

        if (doorType != DoorType.RoomDoor)
        {
            roomDoor.DoorType = doorType;
        }

        RecalculateSecurityDoorConstraints();
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
        return targetArea;
    }

    public void ToggleSecurityDoor(PlacedDoorNode door)
    {
        door.IsSecurityDoor = !door.IsSecurityDoor;
        if (door.ConnectedDoor != null)
        {
            door.ConnectedDoor.IsSecurityDoor = door.IsSecurityDoor;
        }

        RecalculateSecurityDoorConstraints();
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
    }

    public void ConnectAdjacentDoors(PlacedGeomorphNode newGeo, PlacedDoorNode? sourceDoor = null, bool isSecurityDoor = false)
    {
        ConnectAdjacentDoors(newGeo, sourceDoor, isSecurityDoor ? DoorType.SecurityDoor : DoorType.RoomDoor);
    }

    public void ConnectAdjacentDoors(PlacedGeomorphNode newGeo, PlacedDoorNode? sourceDoor = null, DoorType doorType = DoorType.RoomDoor)
    {
        if (sourceDoor != null)
        {
            sourceDoor.IsConnected = true;
            if (doorType != DoorType.RoomDoor)
            {
                sourceDoor.DoorType = doorType;
            }

            // Find matching facing door on newGeo
            PlacedDoorNode? matchingDoor = null;
            double bestDist = double.MaxValue;

            foreach (var door in newGeo.Doors.Where(d => !d.IsInternal))
            {
                double dist = Math.Sqrt(Math.Pow(door.WorldX - sourceDoor.WorldX, 2) + Math.Pow(door.WorldY - sourceDoor.WorldY, 2));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    matchingDoor = door;
                }
            }

            if (matchingDoor != null && bestDist < 10.0)
            {
                matchingDoor.IsConnected = true;
                matchingDoor.ConnectedDoor = sourceDoor;
                sourceDoor.ConnectedDoor = matchingDoor;
                if (doorType != DoorType.RoomDoor)
                {
                    matchingDoor.DoorType = doorType;
                }
            }
        }

        // Also connect any other facing external doors between newGeo and other placed geomorphs
        foreach (var door in newGeo.Doors.Where(d => !d.IsInternal && d.ConnectedDoor == null))
        {
            foreach (var otherDoor in PlacedDoors.Where(d => !d.IsInternal && d.Geomorph != newGeo && d.ConnectedDoor == null))
            {
                double dist = Math.Sqrt(Math.Pow(door.WorldX - otherDoor.WorldX, 2) + Math.Pow(door.WorldY - otherDoor.WorldY, 2));
                if (dist < 4.0)
                {
                    door.ConnectedDoor = otherDoor;
                    otherDoor.ConnectedDoor = door;
                    break;
                }
            }
        }
    }

    public void RecalculateSecurityDoorConstraints()
    {
        if (PlacedGeomorphs.Count == 0) return;

        var rootGeo = PlacedGeomorphs[0];

        // Pre-pair any facing external doors that are close to each other
        foreach (var d in PlacedDoors.Where(d => !d.IsInternal))
        {
            if (d.ConnectedDoor != null) continue;
            var other = PlacedDoors.FirstOrDefault(od =>
                !od.IsInternal &&
                od.Geomorph != d.Geomorph &&
                od.ConnectedDoor == null &&
                Math.Sqrt(Math.Pow(d.WorldX - od.WorldX, 2) + Math.Pow(d.WorldY - od.WorldY, 2)) < 4.0);

            if (other != null)
            {
                d.ConnectedDoor = other;
                other.ConnectedDoor = d;
            }
        }

        // Traverse layout from root (Starting elevator room 0) using BFS to assign Zone IDs to all generated rooms
        var roomZones = new Dictionary<(string geoId, int areaIdx), int>();
        var visited = new HashSet<(string geoId, int areaIdx)>();
        var primaryZoneConnections = new Dictionary<string, (PlacedDoorNode d1, PlacedDoorNode? d2)>();

        int nextZone = 1;
        var queue = new Queue<((PlacedGeomorphNode geo, int areaIdx) room, int zone)>();

        var rootRoom = (rootGeo, 0);
        visited.Add((rootGeo.Id, 0));
        roomZones[(rootGeo.Id, 0)] = 0;
        queue.Enqueue((rootRoom, 0));

        while (queue.Count > 0)
        {
            var (curr, currZone) = queue.Dequeue();

            // Internal door connections from PlacedDoors (only follow connected doors or zone boundaries)
            foreach (var d in PlacedDoors.Where(d => d.IsInternal && d.Geomorph == curr.geo && (d.IsConnected || d.IsZoneBoundary)))
            {
                int neighborArea = -1;
                if (d.AreaFrom == curr.areaIdx)
                {
                    neighborArea = d.AreaTo;
                }
                else if (d.AreaTo == curr.areaIdx)
                {
                    neighborArea = d.AreaFrom;
                }

                if (neighborArea >= 0 && curr.geo.IsRoomGenerated(neighborArea))
                {
                    var neighborKey = (curr.geo.Id, neighborArea);
                    if (!visited.Contains(neighborKey))
                    {
                        visited.Add(neighborKey);
                        int targetZone;
                        if (d.IsZoneBoundary)
                        {
                            targetZone = nextZone++;
                            string pairKey = GetZonePairKey(currZone, targetZone);
                            if (!primaryZoneConnections.ContainsKey(pairKey))
                            {
                                primaryZoneConnections[pairKey] = (d, null);
                            }
                        }
                        else
                        {
                            targetZone = currZone;
                        }
                        roomZones[neighborKey] = targetZone;
                        queue.Enqueue(((curr.geo, neighborArea), targetZone));
                    }
                }
            }

            // External connected doors to other geomorphs from PlacedDoors (only follow connected doors or zone boundaries)
            foreach (var d in PlacedDoors.Where(d => !d.IsInternal && d.Geomorph == curr.geo && d.AreaFrom == curr.areaIdx && d.ConnectedDoor != null && (d.IsConnected || d.IsZoneBoundary || d.ConnectedDoor.IsZoneBoundary)))
            {
                var otherDoor = d.ConnectedDoor!;
                var otherGeo = otherDoor.Geomorph;
                if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                {
                    var neighborKey = (otherGeo.Id, otherDoor.AreaFrom);
                    if (!visited.Contains(neighborKey))
                    {
                        visited.Add(neighborKey);
                        int targetZone;
                        if (d.IsZoneBoundary || otherDoor.IsZoneBoundary)
                        {
                            targetZone = nextZone++;
                            string pairKey = GetZonePairKey(currZone, targetZone);
                            if (!primaryZoneConnections.ContainsKey(pairKey))
                            {
                                primaryZoneConnections[pairKey] = (d, otherDoor);
                            }
                        }
                        else
                        {
                            targetZone = currZone;
                        }
                        roomZones[neighborKey] = targetZone;
                        queue.Enqueue(((otherGeo, otherDoor.AreaFrom), targetZone));
                    }
                }
            }
        }

        // Handle any disconnected rooms (if any)
        foreach (var geo in PlacedGeomorphs)
        {
            foreach (var area in geo.GeneratedRooms)
            {
                var key = (geo.Id, area);
                if (!visited.Contains(key))
                {
                    int startZone = geo.GetRoomZone(area);
                    visited.Add(key);
                    roomZones[key] = startZone;
                    queue.Enqueue(((geo, area), startZone));

                    while (queue.Count > 0)
                    {
                        var (curr, currZone) = queue.Dequeue();

                        // Internal door connections
                        foreach (var d in PlacedDoors.Where(d => d.IsInternal && d.Geomorph == curr.geo && (d.IsConnected || d.IsZoneBoundary)))
                        {
                            int neighborArea = -1;
                            if (d.AreaFrom == curr.areaIdx) neighborArea = d.AreaTo;
                            else if (d.AreaTo == curr.areaIdx) neighborArea = d.AreaFrom;

                            if (neighborArea >= 0 && curr.geo.IsRoomGenerated(neighborArea))
                            {
                                var neighborKey = (curr.geo.Id, neighborArea);
                                if (!visited.Contains(neighborKey))
                                {
                                    visited.Add(neighborKey);
                                    int targetZone = d.IsZoneBoundary ? nextZone++ : currZone;
                                    roomZones[neighborKey] = targetZone;
                                    queue.Enqueue(((curr.geo, neighborArea), targetZone));
                                }
                            }
                        }

                        // External door connections
                        foreach (var d in PlacedDoors.Where(d => !d.IsInternal && d.Geomorph == curr.geo && d.AreaFrom == curr.areaIdx && d.ConnectedDoor != null && (d.IsConnected || d.IsZoneBoundary || d.ConnectedDoor.IsZoneBoundary)))
                        {
                            var otherDoor = d.ConnectedDoor!;
                            var otherGeo = otherDoor.Geomorph;
                            if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                            {
                                var neighborKey = (otherGeo.Id, otherDoor.AreaFrom);
                                if (!visited.Contains(neighborKey))
                                {
                                    visited.Add(neighborKey);
                                    int targetZone = (d.IsZoneBoundary || otherDoor.IsZoneBoundary) ? nextZone++ : currZone;
                                    roomZones[neighborKey] = targetZone;
                                    queue.Enqueue(((otherGeo, otherDoor.AreaFrom), targetZone));
                                }
                            }
                        }
                    }
                }
            }
        }

        foreach (var geo in PlacedGeomorphs)
        {
            foreach (var area in geo.GeneratedRooms)
            {
                var key = (geo.Id, area);
                if (roomZones.TryGetValue(key, out int z))
                {
                    geo.SetRoomZone(area, z);
                }
            }
        }
        _nextZoneIndex = Math.Max(_nextZoneIndex, nextZone);

        // Validate all internal doors
        var internalGroups = PlacedDoors
            .Where(d => d.IsInternal && d.Geomorph != null)
            .GroupBy(d => $"{d.Geomorph.Id}_{Math.Min(d.AreaFrom, d.AreaTo)}_{Math.Max(d.AreaFrom, d.AreaTo)}");

        foreach (var group in internalGroups)
        {
            var first = group.First();
            var geo = first.Geomorph;
            bool isBothGenerated = geo.IsRoomGenerated(first.AreaFrom) && geo.IsRoomGenerated(first.AreaTo);

            var secDoor = group.FirstOrDefault(d => d.IsZoneBoundary);

            if (secDoor != null)
            {
                // If a security door exists between these areas, alternate parallel doors are always blocked
                foreach (var d in group)
                {
                    if (d == secDoor)
                    {
                        d.IsBlocked = false;
                        d.IsConnected = isBothGenerated;
                    }
                    else
                    {
                        d.IsBlocked = true;
                        d.IsConnected = false;
                    }
                }
            }
            else if (!isBothGenerated)
            {
                // If one of the rooms is not yet generated, internal doors remain open as expansion opportunities
                foreach (var d in group)
                {
                    d.IsBlocked = false;
                    d.IsConnected = false;
                }
            }
            else
            {
                int z1 = geo.GetRoomZone(first.AreaFrom);
                int z2 = geo.GetRoomZone(first.AreaTo);

                if (z1 == z2)
                {
                    foreach (var d in group)
                    {
                        d.IsBlocked = false;
                        d.IsConnected = true;
                    }
                }
                else
                {
                    // Different zones: check if one of the doors is the primary security / zone boundary door
                    string pairKey = GetZonePairKey(z1, z2);
                    primaryZoneConnections.TryGetValue(pairKey, out var primaryPair);

                    var primarySecDoor = group.FirstOrDefault(d => d == primaryPair.d1 || d == primaryPair.d2);
                    if (primarySecDoor != null)
                    {
                        var primaryDoorType = primarySecDoor.DoorType != DoorType.RoomDoor ? primarySecDoor.DoorType : DoorType.SecurityDoor;
                        foreach (var d in group)
                        {
                            if (d == primarySecDoor)
                            {
                                d.IsBlocked = false;
                                d.IsConnected = true;
                                d.DoorType = primaryDoorType;
                            }
                            else
                            {
                                d.IsBlocked = true;
                                d.IsConnected = false;
                            }
                        }
                    }
                    else
                    {
                        // Both generated, different zones without being the primary zone connection -> blocked
                        foreach (var d in group)
                        {
                            d.IsBlocked = true;
                            d.IsConnected = false;
                        }
                    }
                }
            }
        }

        // Validate all external doors
        var externalGroups = PlacedDoors
            .Where(d => !d.IsInternal && d.Geomorph != null)
            .GroupBy(d =>
            {
                if (d.ConnectedDoor?.Geomorph != null)
                {
                    string k1 = $"{d.Geomorph.Id}:{d.AreaFrom}";
                    string k2 = $"{d.ConnectedDoor.Geomorph.Id}:{d.ConnectedDoor.AreaFrom}";
                    return string.CompareOrdinal(k1, k2) < 0 ? $"{k1}__{k2}" : $"{k2}__{k1}";
                }
                return d.Id;
            });

        foreach (var group in externalGroups)
        {
            var first = group.First();
            if (first.ConnectedDoor?.Geomorph == null)
            {
                foreach (var d in group)
                {
                    d.IsConnected = false;
                    d.IsBlocked = false;
                }
                continue;
            }

            var otherDoor = first.ConnectedDoor;
            var geo1 = first.Geomorph;
            var geo2 = otherDoor.Geomorph!;

            bool isBothGenerated = geo1.IsRoomGenerated(first.AreaFrom) && geo2.IsRoomGenerated(otherDoor.AreaFrom);

            var secDoor = group.FirstOrDefault(d => d.IsZoneBoundary || (d.ConnectedDoor?.IsZoneBoundary ?? false));
            if (secDoor != null)
            {
                var partner = secDoor.ConnectedDoor;
                var primaryDoorType = secDoor.DoorType != DoorType.RoomDoor ? secDoor.DoorType : (partner?.DoorType ?? DoorType.SecurityDoor);
                if (primaryDoorType == DoorType.RoomDoor) primaryDoorType = DoorType.SecurityDoor;

                foreach (var d in group)
                {
                    if (d == secDoor || d == partner)
                    {
                        d.IsBlocked = false;
                        d.IsConnected = isBothGenerated;
                        d.DoorType = primaryDoorType;
                    }
                    else
                    {
                        d.IsBlocked = true;
                        d.IsConnected = false;
                    }
                }
            }
            else if (!isBothGenerated)
            {
                foreach (var d in group)
                {
                    d.IsConnected = false;
                    d.IsBlocked = false;
                }
            }
            else
            {
                int z1 = geo1.GetRoomZone(first.AreaFrom);
                int z2 = geo2.GetRoomZone(otherDoor.AreaFrom);

                if (z1 == z2)
                {
                    foreach (var d in group)
                    {
                        d.IsBlocked = false;
                        d.IsConnected = true;
                    }
                }
                else
                {
                    // Different zones: only the single primary security door can connect them
                    string pairKey = GetZonePairKey(z1, z2);
                    primaryZoneConnections.TryGetValue(pairKey, out var primaryPair);

                    var primarySecDoor = group.FirstOrDefault(d => d == primaryPair.d1 || d == primaryPair.d2 || (d.ConnectedDoor != null && (d.ConnectedDoor == primaryPair.d1 || d.ConnectedDoor == primaryPair.d2)));

                    if (primarySecDoor != null)
                    {
                        var partner = primarySecDoor.ConnectedDoor;
                        var primaryDoorType = primarySecDoor.DoorType != DoorType.RoomDoor ? primarySecDoor.DoorType : (partner?.DoorType ?? DoorType.SecurityDoor);
                        if (primaryDoorType == DoorType.RoomDoor) primaryDoorType = DoorType.SecurityDoor;

                        foreach (var d in group)
                        {
                            if (d == primarySecDoor || d == partner)
                            {
                                d.IsBlocked = false;
                                d.IsConnected = true;
                                d.DoorType = primaryDoorType;
                            }
                            else
                            {
                                d.IsBlocked = true;
                                d.IsConnected = false;
                            }
                        }
                    }
                    else
                    {
                        // Not the primary security door between these zones -> block all
                        foreach (var d in group)
                        {
                            d.IsBlocked = true;
                            d.IsConnected = false;
                        }
                    }
                }
            }
        }
    }

    private static string GetZonePairKey(int z1, int z2)
    {
        return z1 < z2 ? $"{z1}_{z2}" : $"{z2}_{z1}";
    }

    public List<PlacedDoorNode> GenerateDoorsForRoom(PlacedGeomorphNode geoNode, int areaIdx)
    {
        var newDoors = new List<PlacedDoorNode>();
        geoNode.GenerateRoom(areaIdx);
        CreateRoomNode(geoNode, areaIdx);

        if (geoNode.Metadata?.AreaDatas == null || areaIdx < 0 || areaIdx >= geoNode.Metadata.AreaDatas.Count)
        {
            return newDoors;
        }

        var areaData = geoNode.Metadata.AreaDatas[areaIdx];
        if (areaData.ExpanderDatas == null) return newDoors;

        for (int expIdx = 0; expIdx < areaData.ExpanderDatas.Count; expIdx++)
        {
            var exp = areaData.ExpanderDatas[expIdx];
            var (localX, localY) = RotateLocalOffset(exp.ExpanderPosition.X, -exp.ExpanderPosition.Z, geoNode.RotationAngle);
            double rotatedAngle = Math.Round(((exp.YAngle + geoNode.RotationAngle) % 360 + 360) % 360, 4);
            if (Math.Abs(rotatedAngle - 360.0) < 0.0001) rotatedAngle = 0.0;
            string rotatedDirection = RotateDirection(exp.Direction, geoNode.RotationAngle);

            if (exp.AreaTo >= 0)
            {
                // Internal doorway connecting areaIdx and exp.AreaTo
                int minArea = Math.Min(areaIdx, exp.AreaTo);
                int maxArea = Math.Max(areaIdx, exp.AreaTo);

                // Check if door already exists on geoNode.Doors
                var existingDoor = geoNode.Doors.FirstOrDefault(d =>
                    d.IsInternal &&
                    d.AreaFrom == minArea &&
                    d.AreaTo == maxArea &&
                    Math.Abs(d.LocalX - localX) < 1.0 &&
                    Math.Abs(d.LocalY - localY) < 1.0);

                if (existingDoor != null)
                {
                    // If both areas are now generated, mark as connected (if same zone or valid security door)
                    if (geoNode.IsRoomGenerated(minArea) && geoNode.IsRoomGenerated(maxArea))
                    {
                        int z1 = geoNode.GetRoomZone(minArea);
                        int z2 = geoNode.GetRoomZone(maxArea);
                        if (z1 == z2 || existingDoor.IsZoneBoundary)
                        {
                            existingDoor.IsConnected = true;
                            existingDoor.IsBlocked = false;
                        }
                        else
                        {
                            existingDoor.IsConnected = false;
                            existingDoor.IsBlocked = true;
                        }
                    }
                }
                else
                {
                    bool isBothGenerated = geoNode.IsRoomGenerated(minArea) && geoNode.IsRoomGenerated(maxArea);
                    bool isConnected = false;
                    bool isBlocked = false;

                    if (isBothGenerated)
                    {
                        int z1 = geoNode.GetRoomZone(minArea);
                        int z2 = geoNode.GetRoomZone(maxArea);
                        if (z1 == z2)
                        {
                            isConnected = true;
                        }
                        else
                        {
                            isBlocked = true;
                        }
                    }

                    var doorNode = new PlacedDoorNode
                    {
                        Geomorph = geoNode,
                        ExpanderIndex = expIdx,
                        AreaFrom = minArea,
                        AreaTo = maxArea,
                        Altitude = geoNode.Altitude,
                        LocalX = localX,
                        LocalY = localY,
                        WorldX = geoNode.WorldX + localX,
                        WorldY = geoNode.WorldY + localY,
                        Direction = rotatedDirection,
                        RotationAngle = rotatedAngle,
                        IsConnected = isConnected,
                        IsBlocked = isBlocked
                    };
                    geoNode.Doors.Add(doorNode);
                    newDoors.Add(doorNode);
                }
            }
            else
            {
                // External doorway / connector
                var existingDoor = geoNode.Doors.FirstOrDefault(d =>
                    !d.IsInternal &&
                    d.AreaFrom == areaIdx &&
                    Math.Abs(d.LocalX - localX) < 1.0 &&
                    Math.Abs(d.LocalY - localY) < 1.0);

                if (existingDoor == null)
                {
                    var doorNode = new PlacedDoorNode
                    {
                        Geomorph = geoNode,
                        ExpanderIndex = expIdx,
                        AreaFrom = areaIdx,
                        AreaTo = -1,
                        Altitude = geoNode.Altitude,
                        LocalX = localX,
                        LocalY = localY,
                        WorldX = geoNode.WorldX + localX,
                        WorldY = geoNode.WorldY + localY,
                        Direction = rotatedDirection,
                        RotationAngle = rotatedAngle
                    };
                    geoNode.Doors.Add(doorNode);
                    newDoors.Add(doorNode);
                }
            }
        }

        return newDoors;
    }

    public PlacedRoomNode? CreateRoomNode(PlacedGeomorphNode geoNode, int areaIdx)
    {
        var existingRoom = geoNode.Rooms.FirstOrDefault(r => r.AreaIndex == areaIdx);
        if (existingRoom != null)
        {
            if (!PlacedRooms.Contains(existingRoom))
            {
                PlacedRooms.Add(existingRoom);
            }
            return existingRoom;
        }

        var areaData = geoNode.Metadata?.AreaDatas != null && areaIdx >= 0 && areaIdx < geoNode.Metadata.AreaDatas.Count
            ? geoNode.Metadata.AreaDatas[areaIdx]
            : null;

        int zone = geoNode.GetRoomZone(areaIdx);
        var mesh = areaData?.Mesh;
        double meshHeight = RoomSvgService.CalculateMeshHeight(mesh);
        double totalHeight = geoNode.AltitudeOffsetValue + meshHeight;
        var (svgContent, svgPathData, geometry) = RoomSvgService.LoadRoomSvg(mesh, totalHeight, geoNode.AltitudeOffset, geoNode.RotationAngle);
        var (triangles, bounds, area) = RoomSvgService.Extract2DTriangles(mesh, geoNode.RotationAngle);

        var roomNode = new PlacedRoomNode
        {
            Geomorph = geoNode,
            AreaIndex = areaIdx,
            Zone = zone,
            MeshHeight = meshHeight,
            WorldX = geoNode.WorldX,
            WorldY = geoNode.WorldY,
            SvgContent = svgContent,
            SvgPathData = svgPathData,
            Geometry = geometry,
            Triangles2D = triangles,
            BoundsMinX = bounds.MinX,
            BoundsMinY = bounds.MinY,
            BoundsMaxX = bounds.MaxX,
            BoundsMaxY = bounds.MaxY,
            Area = area
        };
        roomNode.UpdateFillBrush();

        geoNode.Rooms.Add(roomNode);
        if (!PlacedRooms.Contains(roomNode))
        {
            PlacedRooms.Add(roomNode);
        }

        return roomNode;
    }

    public PlacedGeomorphNode CreateGeomorphNode(string prefabPath, int gridX, int gridY, int initialAreaIndex = 0, double rotationAngle = 0.0, GeomorphAltitude altitude = GeomorphAltitude.Mid, uint lighting = 0)
    {
        var metadata = GeomorphMetadataService.GetMetadata(prefabPath) ?? new GeomorphMetadata
        {
            GeoAssetLink = prefabPath
        };

        var geoNode = new PlacedGeomorphNode
        {
            PrefabPath = prefabPath,
            GridX = gridX,
            GridY = gridY,
            CellDimension = CellDimension,
            Altitude = altitude,
            AltitudeOffset = AltitudeOffset,
            Lighting = lighting,
            Metadata = metadata,
            DisplayName = Path.GetFileNameWithoutExtension(prefabPath),
            RotationAngle = rotationAngle
        };

        int baseWidth = 1;
        int baseHeight = 1;
        string lowerPath = prefabPath.ToLowerInvariant();
        if (lowerPath.Contains("128x128") || lowerPath.Contains("2x2"))
        {
            baseWidth = 2;
            baseHeight = 2;
        }
        else if (lowerPath.Contains("128x64") || lowerPath.Contains("2x1"))
        {
            baseWidth = 2;
            baseHeight = 1;
        }
        else if (lowerPath.Contains("64x128") || lowerPath.Contains("1x2"))
        {
            baseWidth = 1;
            baseHeight = 2;
        }
        else
        {
            baseWidth = 1;
            baseHeight = 1;
        }

        double normAngle = ((rotationAngle % 360) + 360) % 360;
        if (Math.Abs(normAngle - 90) < 0.1 || Math.Abs(normAngle - 270) < 0.1)
        {
            geoNode.WidthInCells = baseHeight;
            geoNode.HeightInCells = baseWidth;
        }
        else
        {
            geoNode.WidthInCells = baseWidth;
            geoNode.HeightInCells = baseHeight;
        }

        geoNode.UpdateWorldPosition();

        if (initialAreaIndex >= 0)
        {
            GenerateDoorsForRoom(geoNode, initialAreaIndex);
        }

        return geoNode;
    }

    public List<PlacedGeomorphNode> GetDisconnectedGeomorphsOnDelete(PlacedGeomorphNode targetGeo)
    {
        if (PlacedGeomorphs.Count == 0) return new List<PlacedGeomorphNode>();
        var rootGeo = PlacedGeomorphs[0];
        if (targetGeo == rootGeo)
        {
            return PlacedGeomorphs.Where(g => g != targetGeo).ToList();
        }

        var reachableRooms = new HashSet<(PlacedGeomorphNode geo, int area)>();
        var queue = new Queue<(PlacedGeomorphNode geo, int area)>();

        if (rootGeo.IsRoomGenerated(0))
        {
            reachableRooms.Add((rootGeo, 0));
            queue.Enqueue((rootGeo, 0));
        }

        while (queue.Count > 0)
        {
            var (currGeo, currArea) = queue.Dequeue();

            // 1. Internal doors within currGeo
            foreach (var d in PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
            {
                int neighborArea = -1;
                if (d.AreaFrom == currArea) neighborArea = d.AreaTo;
                else if (d.AreaTo == currArea) neighborArea = d.AreaFrom;

                if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                {
                    var neighbor = (currGeo, neighborArea);
                    if (reachableRooms.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // 2. External doors to other geomorphs
            foreach (var d in PlacedDoors.Where(d => !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected && d.ConnectedDoor != null))
            {
                var otherDoor = d.ConnectedDoor!;
                var otherGeo = otherDoor.Geomorph;
                if (otherGeo != null && otherGeo != targetGeo && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                {
                    var neighbor = (otherGeo, otherDoor.AreaFrom);
                    if (reachableRooms.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        var reachableGeomorphs = reachableRooms.Select(r => r.geo).ToHashSet();
        return PlacedGeomorphs.Where(g => g != targetGeo && !reachableGeomorphs.Contains(g)).ToList();
    }

    public Dictionary<int, SectorType> GetZoneSectorMap()
    {
        var map = new Dictionary<int, SectorType>();
        if (PlacedGeomorphs.Count == 0)
        {
            map[0] = SectorType.Main;
            return map;
        }

        var rootGeo = PlacedGeomorphs[0];
        int rootZone = rootGeo.GetRoomZone(0);
        map[rootZone] = SectorType.Main;

        var queue = new Queue<(PlacedGeomorphNode geo, int area, SectorType sector)>();
        var visited = new HashSet<(PlacedGeomorphNode geo, int area)>();

        if (rootGeo.IsRoomGenerated(0))
        {
            visited.Add((rootGeo, 0));
            queue.Enqueue((rootGeo, 0, SectorType.Main));
        }

        while (queue.Count > 0)
        {
            var (currGeo, currArea, currSector) = queue.Dequeue();
            int currZone = currGeo.GetRoomZone(currArea);
            map[currZone] = currSector;

            // 1. Internal doors
            foreach (var d in PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
            {
                int neighborArea = -1;
                if (d.AreaFrom == currArea) neighborArea = d.AreaTo;
                else if (d.AreaTo == currArea) neighborArea = d.AreaFrom;

                if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                {
                    var neighbor = (currGeo, neighborArea);
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        SectorType nextSector = currSector;
                        if (d.IsSecondaryDoor) nextSector = SectorType.Secondary;
                        else if (d.IsOverloadDoor) nextSector = SectorType.Overload;
                        queue.Enqueue((currGeo, neighborArea, nextSector));
                    }
                }
            }

            // 2. External doors
            foreach (var d in PlacedDoors.Where(d => !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected && d.ConnectedDoor != null))
            {
                var otherDoor = d.ConnectedDoor!;
                var otherGeo = otherDoor.Geomorph;
                if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                {
                    var neighbor = (otherGeo, otherDoor.AreaFrom);
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        SectorType nextSector = currSector;
                        if (d.IsSecondaryDoor || otherDoor.IsSecondaryDoor) nextSector = SectorType.Secondary;
                        else if (d.IsOverloadDoor || otherDoor.IsOverloadDoor) nextSector = SectorType.Overload;
                        queue.Enqueue((otherGeo, otherDoor.AreaFrom, nextSector));
                    }
                }
            }
        }

        foreach (var room in PlacedRooms)
        {
            if (!map.ContainsKey(room.Zone))
            {
                map[room.Zone] = SectorType.Main;
            }
        }

        return map;
    }

    public SectorType GetSectorForZone(int zoneIndex)
    {
        var map = GetZoneSectorMap();
        return map.TryGetValue(zoneIndex, out var sector) ? sector : SectorType.Main;
    }

    public List<int> GetZonesForSector(SectorType sector)
    {
        var map = GetZoneSectorMap();
        var list = map.Where(kvp => kvp.Value == sector).Select(kvp => kvp.Key).Distinct().OrderBy(z => z).ToList();
        if (list.Count == 0 && sector == SectorType.Main)
        {
            list.Add(0);
        }
        return list;
    }

    public List<PlacedGeomorphNode> DeleteGeomorph(PlacedGeomorphNode targetGeo)
    {
        if (!PlacedGeomorphs.Contains(targetGeo)) return new List<PlacedGeomorphNode>();

        var disconnected = GetDisconnectedGeomorphsOnDelete(targetGeo);
        var toDelete = new List<PlacedGeomorphNode> { targetGeo };
        toDelete.AddRange(disconnected);
        var toDeleteSet = toDelete.ToHashSet();

        foreach (var geo in toDelete)
        {
            foreach (var door in geo.Doors.ToList())
            {
                if (door.ConnectedDoor != null)
                {
                    var other = door.ConnectedDoor;
                    door.ConnectedDoor = null;
                    door.IsConnected = false;
                    if (other.Geomorph != null && !toDeleteSet.Contains(other.Geomorph))
                    {
                        other.ConnectedDoor = null;
                        other.IsConnected = false;
                        other.IsSecurityDoor = false;
                        other.IsMainDoor = false;
                        other.IsSecondaryDoor = false;
                        other.IsOverloadDoor = false;
                    }
                }
                PlacedDoors.Remove(door);
            }

            foreach (var room in geo.Rooms.ToList())
            {
                PlacedRooms.Remove(room);
            }

            PlacedGeomorphs.Remove(geo);
        }

        if (HoveredRoom != null && !PlacedRooms.Contains(HoveredRoom))
        {
            HoveredRoom = null;
        }

        if (PlacedGeomorphs.Count > 0)
        {
            RecalculateSecurityDoorConstraints();
            UpdateRoomHighlightStates();
        }

        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));

        return toDelete;
    }

    public (List<PlacedRoomNode> DisconnectedRooms, List<PlacedGeomorphNode> DisconnectedGeomorphs) GetDisconnectedRoomsOnDelete(PlacedRoomNode targetRoom)
    {
        if (PlacedGeomorphs.Count == 0 || targetRoom.Geomorph == null)
            return (new List<PlacedRoomNode>(), new List<PlacedGeomorphNode>());

        var rootGeo = PlacedGeomorphs[0];
        if (targetRoom.Geomorph == rootGeo && targetRoom.AreaIndex == 0)
        {
            var otherRooms = PlacedRooms.Where(r => r != targetRoom).ToList();
            var allGeos = PlacedGeomorphs.ToList();
            return (otherRooms, allGeos);
        }

        var reachableRooms = new HashSet<(PlacedGeomorphNode geo, int area)>();
        var queue = new Queue<(PlacedGeomorphNode geo, int area)>();

        if (rootGeo.IsRoomGenerated(0))
        {
            reachableRooms.Add((rootGeo, 0));
            queue.Enqueue((rootGeo, 0));
        }

        while (queue.Count > 0)
        {
            var (currGeo, currArea) = queue.Dequeue();

            // 1. Internal doors
            foreach (var d in PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
            {
                int neighborArea = -1;
                if (d.AreaFrom == currArea) neighborArea = d.AreaTo;
                else if (d.AreaTo == currArea) neighborArea = d.AreaFrom;

                if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                {
                    var neighbor = (currGeo, neighborArea);
                    if (neighbor != (targetRoom.Geomorph, targetRoom.AreaIndex))
                    {
                        if (reachableRooms.Add(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // 2. External doors
            foreach (var d in PlacedDoors.Where(d => !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected && d.ConnectedDoor != null))
            {
                var otherDoor = d.ConnectedDoor!;
                var otherGeo = otherDoor.Geomorph;
                if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                {
                    var neighbor = (otherGeo, otherDoor.AreaFrom);
                    if (neighbor != (targetRoom.Geomorph, targetRoom.AreaIndex))
                    {
                        if (reachableRooms.Add(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }

        var disconnectedRooms = PlacedRooms.Where(r => r != targetRoom && !reachableRooms.Contains((r.Geomorph!, r.AreaIndex))).ToList();
        var reachableGeomorphs = reachableRooms.Select(r => r.geo).ToHashSet();
        var disconnectedGeomorphs = PlacedGeomorphs.Where(g => !reachableGeomorphs.Contains(g)).ToList();

        return (disconnectedRooms, disconnectedGeomorphs);
    }

    public (List<PlacedRoomNode> DeletedRooms, List<PlacedGeomorphNode> DeletedGeomorphs) DeleteRoom(PlacedRoomNode targetRoom)
    {
        if (!PlacedRooms.Contains(targetRoom) || targetRoom.Geomorph == null)
            return (new List<PlacedRoomNode>(), new List<PlacedGeomorphNode>());

        var (disconnectedRooms, disconnectedGeos) = GetDisconnectedRoomsOnDelete(targetRoom);
        var allRoomsToDelete = new List<PlacedRoomNode> { targetRoom };
        allRoomsToDelete.AddRange(disconnectedRooms);

        foreach (var room in allRoomsToDelete)
        {
            var geo = room.Geomorph;
            if (geo != null)
            {
                geo.UngenerateRoom(room.AreaIndex);

                // Handle internal doors involving this room
                foreach (var d in geo.Doors.Where(d => d.IsInternal && (d.AreaFrom == room.AreaIndex || d.AreaTo == room.AreaIndex)).ToList())
                {
                    int otherArea = (d.AreaFrom == room.AreaIndex) ? d.AreaTo : d.AreaFrom;
                    if (geo.IsRoomGenerated(otherArea))
                    {
                        // The other area is still generated; keep the door as an unexpanded internal doorway
                        d.IsConnected = false;
                        d.IsSecurityDoor = false;
                        d.IsMainDoor = false;
                        d.IsSecondaryDoor = false;
                        d.IsOverloadDoor = false;
                        d.IsBlocked = false;
                    }
                    else
                    {
                        // Neither area is generated anymore; remove the orphaned internal door
                        if (d.ConnectedDoor != null)
                        {
                            var other = d.ConnectedDoor;
                            other.ConnectedDoor = null;
                            other.IsConnected = false;
                            other.IsSecurityDoor = false;
                            other.IsMainDoor = false;
                            other.IsSecondaryDoor = false;
                            other.IsOverloadDoor = false;
                            d.ConnectedDoor = null;
                        }
                        d.IsConnected = false;
                        geo.Doors.Remove(d);
                        PlacedDoors.Remove(d);
                    }
                }

                // Handle external doors belonging to this room
                foreach (var d in geo.Doors.Where(d => !d.IsInternal && d.AreaFrom == room.AreaIndex).ToList())
                {
                    if (d.ConnectedDoor != null)
                    {
                        var other = d.ConnectedDoor;
                        other.ConnectedDoor = null;
                        other.IsConnected = false;
                        other.IsSecurityDoor = false;
                        other.IsMainDoor = false;
                        other.IsSecondaryDoor = false;
                        other.IsOverloadDoor = false;
                        d.ConnectedDoor = null;
                    }
                    d.IsConnected = false;
                    geo.Doors.Remove(d);
                    PlacedDoors.Remove(d);
                }
            }
            PlacedRooms.Remove(room);
        }

        var deletedGeomorphs = new List<PlacedGeomorphNode>();
        foreach (var geo in PlacedGeomorphs.ToList())
        {
            if (geo.GeneratedRooms.Count == 0)
            {
                deletedGeomorphs.Add(geo);
                foreach (var door in geo.Doors.ToList())
                {
                    if (door.ConnectedDoor != null)
                    {
                        var other = door.ConnectedDoor;
                        other.ConnectedDoor = null;
                        other.IsConnected = false;
                        other.IsSecurityDoor = false;
                        other.IsMainDoor = false;
                        other.IsSecondaryDoor = false;
                        other.IsOverloadDoor = false;
                        door.ConnectedDoor = null;
                    }
                    door.IsConnected = false;
                    geo.Doors.Remove(door);
                    PlacedDoors.Remove(door);
                }
                PlacedGeomorphs.Remove(geo);
            }
        }

        if (HoveredRoom != null && !PlacedRooms.Contains(HoveredRoom))
        {
            HoveredRoom = null;
        }

        if (PlacedGeomorphs.Count > 0)
        {
            RecalculateSecurityDoorConstraints();
            UpdateRoomHighlightStates();
        }

        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));

        return (allRoomsToDelete, deletedGeomorphs);
    }

    public LevelLayoutGridStateData ExportStateData()
    {
        var data = new LevelLayoutGridStateData
        {
            GridSize = GridSize,
            CellDimension = CellDimension,
            AltitudeOffset = AltitudeOffset,
            Zoom = Zoom,
            PanX = PanX,
            PanY = PanY,
            CurrentStartingPrefab = CurrentStartingPrefab,
            NextZoneIndex = _nextZoneIndex
        };

        foreach (var geo in PlacedGeomorphs)
        {
            data.PlacedGeomorphs.Add(new PlacedGeomorphNodeData
            {
                Id = geo.Id,
                PrefabPath = geo.PrefabPath,
                DisplayName = geo.DisplayName,
                GridX = geo.GridX,
                GridY = geo.GridY,
                RotationAngle = geo.RotationAngle,
                Altitude = geo.Altitude,
                AltitudeOffset = geo.AltitudeOffset,
                Lighting = geo.Lighting,
                GeneratedRooms = geo.GeneratedRooms.ToList(),
                RoomZones = new Dictionary<int, int>(geo.RoomZones)
            });
        }

        foreach (var door in PlacedDoors)
        {
            data.PlacedDoors.Add(new PlacedDoorNodeData
            {
                Id = door.Id,
                GeomorphId = door.Geomorph?.Id ?? string.Empty,
                ExpanderIndex = door.ExpanderIndex,
                AreaFrom = door.AreaFrom,
                AreaTo = door.AreaTo,
                LocalX = door.LocalX,
                LocalY = door.LocalY,
                WorldX = door.WorldX,
                WorldY = door.WorldY,
                Direction = door.Direction,
                RotationAngle = door.RotationAngle,
                Altitude = door.Altitude,
                IsSecurityDoor = door.IsSecurityDoor,
                IsMainDoor = door.IsMainDoor,
                IsSecondaryDoor = door.IsSecondaryDoor,
                IsOverloadDoor = door.IsOverloadDoor,
                IsConnected = door.IsConnected,
                IsBlocked = door.IsBlocked,
                ConnectedDoorId = door.ConnectedDoor?.Id
            });
        }

        foreach (var kvp in _zoneSettings)
        {
            var zs = kvp.Value;
            data.ZoneSettings.Add(new ZoneSettingsData
            {
                ZoneIndex = zs.ZoneIndex,
                Lighting = zs.Lighting,
                MedKitUses = zs.MedKitUses,
                AmmoKitUses = zs.AmmoKitUses,
                ToolKitUses = zs.ToolKitUses,
                DisinfectKitUses = zs.DisinfectKitUses,
                BulkheadKeys = zs.BulkheadKeys,
                ContainsDC = zs.ContainsDC,
                ContainsDisinfectStation = zs.ContainsDisinfectStation,
                PowerCells = zs.PowerCells,
                FogTurbines = zs.FogTurbines,
                DcOpensSecondary = zs.DcOpensSecondary,
                DcOpensOverload = zs.DcOpensOverload,
                DcOpenedMainDoorIds = zs.DcOpenedMainDoorIds.ToList()
            });
        }

        return data;
    }

    public void ImportStateData(LevelLayoutGridStateData? data)
    {
        Clear();
        _zoneSettings.Clear();

        if (data == null)
        {
            ResetZoneIdCounter();
            return;
        }

        GridSize = data.GridSize > 0 ? data.GridSize : 40;
        CellDimension = data.CellDimension > 0 ? data.CellDimension : 64.0;
        AltitudeOffset = data.AltitudeOffset > 0 ? data.AltitudeOffset : 6.0;
        Zoom = data.Zoom > 0 ? data.Zoom : 4.0;
        PanX = data.PanX;
        PanY = data.PanY;
        CurrentStartingPrefab = data.CurrentStartingPrefab ?? string.Empty;
        _nextZoneIndex = data.NextZoneIndex > 0 ? data.NextZoneIndex : 1;

        if (data.ZoneSettings != null)
        {
            foreach (var zsData in data.ZoneSettings)
            {
                var zs = GetOrCreateZoneSettings(zsData.ZoneIndex);
                zs.Lighting = zsData.Lighting;
                zs.MedKitUses = zsData.MedKitUses;
                zs.AmmoKitUses = zsData.AmmoKitUses;
                zs.ToolKitUses = zsData.ToolKitUses;
                zs.DisinfectKitUses = zsData.DisinfectKitUses;
                zs.BulkheadKeys = zsData.BulkheadKeys;
                zs.ContainsDC = zsData.ContainsDC;
                zs.ContainsDisinfectStation = zsData.ContainsDisinfectStation;
                zs.PowerCells = zsData.PowerCells;
                zs.FogTurbines = zsData.FogTurbines;
                zs.DcOpensSecondary = zsData.DcOpensSecondary;
                zs.DcOpensOverload = zsData.DcOpensOverload;
                zs.DcOpenedMainDoorIds.Clear();
                if (zsData.DcOpenedMainDoorIds != null)
                {
                    foreach (var id in zsData.DcOpenedMainDoorIds)
                    {
                        zs.DcOpenedMainDoorIds.Add(id);
                    }
                }
            }
        }

        var geoMap = new Dictionary<string, PlacedGeomorphNode>();
        if (data.PlacedGeomorphs != null)
        {
            foreach (var gData in data.PlacedGeomorphs)
            {
                var metadata = GeomorphMetadataService.GetMetadata(gData.PrefabPath) ?? new GeomorphMetadata
                {
                    GeoAssetLink = gData.PrefabPath ?? string.Empty
                };

                var geoNode = new PlacedGeomorphNode
                {
                    Id = string.IsNullOrEmpty(gData.Id) ? Guid.NewGuid().ToString() : gData.Id,
                    PrefabPath = gData.PrefabPath ?? string.Empty,
                    GridX = gData.GridX,
                    GridY = gData.GridY,
                    CellDimension = CellDimension,
                    Altitude = gData.Altitude,
                    AltitudeOffset = gData.AltitudeOffset > 0 ? gData.AltitudeOffset : AltitudeOffset,
                    Lighting = gData.Lighting,
                    Metadata = metadata,
                    DisplayName = string.IsNullOrEmpty(gData.DisplayName) ? Path.GetFileNameWithoutExtension(gData.PrefabPath ?? string.Empty) : gData.DisplayName,
                    RotationAngle = gData.RotationAngle
                };

                int baseWidth = 1;
                int baseHeight = 1;
                string lowerPath = geoNode.PrefabPath.ToLowerInvariant();
                if (lowerPath.Contains("128x128") || lowerPath.Contains("2x2"))
                {
                    baseWidth = 2;
                    baseHeight = 2;
                }
                else if (lowerPath.Contains("128x64") || lowerPath.Contains("2x1"))
                {
                    baseWidth = 2;
                    baseHeight = 1;
                }
                else if (lowerPath.Contains("64x128") || lowerPath.Contains("1x2"))
                {
                    baseWidth = 1;
                    baseHeight = 2;
                }

                double normAngle = ((geoNode.RotationAngle % 360) + 360) % 360;
                if (Math.Abs(normAngle - 90) < 0.1 || Math.Abs(normAngle - 270) < 0.1)
                {
                    geoNode.WidthInCells = baseHeight;
                    geoNode.HeightInCells = baseWidth;
                }
                else
                {
                    geoNode.WidthInCells = baseWidth;
                    geoNode.HeightInCells = baseHeight;
                }

                geoNode.UpdateWorldPosition();

                if (gData.GeneratedRooms != null)
                {
                    foreach (int areaIndex in gData.GeneratedRooms)
                    {
                        geoNode.GenerateRoom(areaIndex);
                        int zone = 0;
                        if (gData.RoomZones != null && gData.RoomZones.TryGetValue(areaIndex, out int z))
                        {
                            zone = z;
                        }
                        geoNode.SetRoomZone(areaIndex, zone);
                        CreateRoomNode(geoNode, areaIndex);
                    }
                }

                PlacedGeomorphs.Add(geoNode);
                geoMap[geoNode.Id] = geoNode;
            }
        }

        var doorMap = new Dictionary<string, PlacedDoorNode>();
        if (data.PlacedDoors != null)
        {
            foreach (var dData in data.PlacedDoors)
            {
                if (!geoMap.TryGetValue(dData.GeomorphId, out var geoNode))
                {
                    continue;
                }

                var doorNode = new PlacedDoorNode
                {
                    Id = string.IsNullOrEmpty(dData.Id) ? Guid.NewGuid().ToString() : dData.Id,
                    Geomorph = geoNode,
                    ExpanderIndex = dData.ExpanderIndex,
                    AreaFrom = dData.AreaFrom,
                    AreaTo = dData.AreaTo,
                    LocalX = dData.LocalX,
                    LocalY = dData.LocalY,
                    WorldX = dData.WorldX,
                    WorldY = dData.WorldY,
                    Direction = dData.Direction ?? string.Empty,
                    RotationAngle = dData.RotationAngle,
                    Altitude = dData.Altitude,
                    IsSecurityDoor = dData.IsSecurityDoor,
                    IsMainDoor = dData.IsMainDoor,
                    IsSecondaryDoor = dData.IsSecondaryDoor,
                    IsOverloadDoor = dData.IsOverloadDoor,
                    IsConnected = dData.IsConnected,
                    IsBlocked = dData.IsBlocked
                };

                geoNode.Doors.Add(doorNode);
                PlacedDoors.Add(doorNode);
                doorMap[doorNode.Id] = doorNode;
            }

            foreach (var dData in data.PlacedDoors)
            {
                if (!string.IsNullOrEmpty(dData.ConnectedDoorId) &&
                    doorMap.TryGetValue(dData.Id, out var door) &&
                    doorMap.TryGetValue(dData.ConnectedDoorId, out var connectedDoor))
                {
                    door.ConnectedDoor = connectedDoor;
                }
            }
        }

        if (PlacedGeomorphs.Count > 0)
        {
            RecalculateSecurityDoorConstraints();
            UpdateRoomHighlightStates();
        }

        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
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
