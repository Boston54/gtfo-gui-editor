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
    private ObservableCollection<PlacedGeomorphNode> _placedGeomorphs = new();
    private ObservableCollection<PlacedDoorNode> _placedDoors = new();
    private double _zoom = 4.0;
    private double _panX = 400.0;
    private double _panY = 300.0;
    private double _viewportWidth = 800;
    private double _viewportHeight = 600;
    private string _currentStartingPrefab = string.Empty;
    private int _nextZoneIndex = 1;

    public LevelLayoutGridState()
    {
        _placedDoors.CollectionChanged += PlacedDoors_CollectionChanged;
    }

    private void PlacedDoors_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
    }

    private void OnDoorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlacedDoorNode.IsSecondaryDoor) ||
            e.PropertyName == nameof(PlacedDoorNode.IsOverloadDoor) ||
            e.PropertyName == nameof(PlacedDoorNode.DoorType))
        {
            OnPropertyChanged(nameof(HasSecondaryDoor));
            OnPropertyChanged(nameof(HasOverloadDoor));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool HasSecondaryDoor => PlacedDoors.Any(d => d.IsSecondaryDoor);
    public bool HasOverloadDoor => PlacedDoors.Any(d => d.IsOverloadDoor);

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
        PlacedGeomorphs.Clear();
        PlacedDoors.Clear();
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

    public PlacedGeomorphNode? ExpandFromDoor(PlacedDoorNode sourceDoor, string geomorphPrefab, DoorType doorType = DoorType.RoomDoor)
    {
        if (sourceDoor.IsInternal) return null;

        var (targetX, targetY, _) = GetExpansionTargetCell(sourceDoor);
        int minCoord = -(GridSize / 2);
        int maxCoord = GridSize % 2 == 0 ? (GridSize / 2 - 1) : (GridSize / 2);
        if (targetX < minCoord || targetX > maxCoord || targetY < minCoord || targetY > maxCoord)
        {
            return null;
        }

        if (GetGeomorphAt(targetX, targetY) != null)
        {
            return null;
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

        int widthInCells = 1;
        int heightInCells = 1;
        string lowerPath = geomorphPrefab.ToLowerInvariant();
        if (lowerPath.Contains("128x128") || lowerPath.Contains("2x2"))
        {
            widthInCells = 2;
            heightInCells = 2;
        }
        else if (lowerPath.Contains("128x64") || lowerPath.Contains("2x1"))
        {
            widthInCells = 2;
            heightInCells = 1;
        }
        else if (lowerPath.Contains("64x128") || lowerPath.Contains("1x2"))
        {
            widthInCells = 1;
            heightInCells = 2;
        }

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
                        double expWorldX = tempWorldCenterX + exp.ExpanderPosition.X;
                        double expWorldY = tempWorldCenterY - exp.ExpanderPosition.Z;
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

        var newGeo = CreateGeomorphNode(geomorphPrefab, targetX, targetY, initialAreaIndex: connectingArea);
        newGeo.SetRoomZone(connectingArea, targetZone);
        PlacedGeomorphs.Add(newGeo);

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

    public int GenerateRoomFromDoor(PlacedDoorNode roomDoor, DoorType doorType = DoorType.RoomDoor)
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

        if (geoNode.Metadata?.AreaDatas == null || areaIdx < 0 || areaIdx >= geoNode.Metadata.AreaDatas.Count)
        {
            return newDoors;
        }

        var areaData = geoNode.Metadata.AreaDatas[areaIdx];
        if (areaData.ExpanderDatas == null) return newDoors;

        for (int expIdx = 0; expIdx < areaData.ExpanderDatas.Count; expIdx++)
        {
            var exp = areaData.ExpanderDatas[expIdx];
            double localX = exp.ExpanderPosition.X;
            double localY = -exp.ExpanderPosition.Z; // Invert Z to map to 2D canvas Y (+Z is North/-Y)

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
                        LocalX = localX,
                        LocalY = localY,
                        WorldX = geoNode.WorldX + localX,
                        WorldY = geoNode.WorldY + localY,
                        Direction = exp.Direction,
                        RotationAngle = exp.YAngle,
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
                        LocalX = localX,
                        LocalY = localY,
                        WorldX = geoNode.WorldX + localX,
                        WorldY = geoNode.WorldY + localY,
                        Direction = exp.Direction,
                        RotationAngle = exp.YAngle
                    };
                    geoNode.Doors.Add(doorNode);
                    newDoors.Add(doorNode);
                }
            }
        }

        return newDoors;
    }

    public PlacedGeomorphNode CreateGeomorphNode(string prefabPath, int gridX, int gridY, int initialAreaIndex = 0)
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
            Metadata = metadata,
            DisplayName = Path.GetFileNameWithoutExtension(prefabPath)
        };

        string lowerPath = prefabPath.ToLowerInvariant();
        if (lowerPath.Contains("128x128") || lowerPath.Contains("2x2"))
        {
            geoNode.WidthInCells = 2;
            geoNode.HeightInCells = 2;
        }
        else if (lowerPath.Contains("128x64") || lowerPath.Contains("2x1"))
        {
            geoNode.WidthInCells = 2;
            geoNode.HeightInCells = 1;
        }
        else if (lowerPath.Contains("64x128") || lowerPath.Contains("1x2"))
        {
            geoNode.WidthInCells = 1;
            geoNode.HeightInCells = 2;
        }
        else
        {
            geoNode.WidthInCells = 1;
            geoNode.HeightInCells = 1;
        }

        geoNode.UpdateWorldPosition();

        if (initialAreaIndex >= 0)
        {
            GenerateDoorsForRoom(geoNode, initialAreaIndex);
        }

        return geoNode;
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
