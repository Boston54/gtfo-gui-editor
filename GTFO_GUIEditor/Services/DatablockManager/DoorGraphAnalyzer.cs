using System;
using System.Collections.Generic;
using System.Linq;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Provides graph traversal and topology analysis for door connections, bulkhead placements, and zone layouts.
/// </summary>
public static class DoorGraphAnalyzer
{
    public static string NormalizeZoneString(string? zone)
    {
        if (string.IsNullOrWhiteSpace(zone)) return "Zone_0";
        if (int.TryParse(zone, out int parsed)) return $"Zone_{parsed}";
        if (!zone.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase)) return $"Zone_{zone}";
        return zone;
    }

    public static string NormalizeLayerTypeString(string? layerType)
    {
        if (string.IsNullOrWhiteSpace(layerType)) return "MainLayer";
        if (layerType.Equals("Main", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("MainLayer", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("MainSector", StringComparison.OrdinalIgnoreCase))
            return "MainLayer";
        if (layerType.Equals("Secondary", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("SecondaryLayer", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("SecondarySector", StringComparison.OrdinalIgnoreCase))
            return "SecondaryLayer";
        if (layerType.Equals("Third", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("ThirdLayer", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("ThirdSector", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("Overload", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("OverloadSector", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("OverloadLayer", StringComparison.OrdinalIgnoreCase))
            return "ThirdLayer";
        return layerType;
    }

    public static (string LayerType, string Zone)? ResolveBuildFrom(
        LevelLayoutGridState? gridState,
        DoorType targetDoorType)
    {
        if (gridState == null || gridState.PlacedGeomorphs.Count == 0) return null;

        var rootGeo = gridState.PlacedGeomorphs[0];
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

            // 1. Internal doors
            foreach (var d in gridState.PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
            {
                int neighborArea = -1;
                if (d.AreaFrom == currArea) neighborArea = d.AreaTo;
                else if (d.AreaTo == currArea) neighborArea = d.AreaFrom;

                if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                {
                    bool isTarget = (targetDoorType == DoorType.SecondaryDoor &&
                                     (d.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor)) ||
                                    (targetDoorType == DoorType.OverloadDoor &&
                                     (d.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor));

                    if (isTarget)
                    {
                        string layerType = currSector switch
                        {
                            SectorType.Main => "MainLayer",
                            SectorType.Secondary => "SecondaryLayer",
                            SectorType.Overload => "ThirdLayer",
                            _ => "MainLayer"
                        };
                        return (layerType, $"Zone_{currZone}");
                    }

                    var neighbor = (currGeo, neighborArea);
                    if (visited.Add(neighbor))
                    {
                        SectorType nextSector = currSector;
                        if (d.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor)
                            nextSector = SectorType.Secondary;
                        else if (d.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor)
                            nextSector = SectorType.Overload;
                        queue.Enqueue((currGeo, neighborArea, nextSector));
                    }
                }
            }

            // 2. External doors
            foreach (var d in gridState.PlacedDoors.Where(d =>
                         !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected &&
                         d.ConnectedDoor != null))
            {
                var otherDoor = d.ConnectedDoor!;
                var otherGeo = otherDoor.Geomorph;
                if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                {
                    bool isTarget = (targetDoorType == DoorType.SecondaryDoor && (d.IsSecondaryDoor ||
                                        otherDoor.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor ||
                                        otherDoor.DoorType == DoorType.SecondaryDoor)) ||
                                    (targetDoorType == DoorType.OverloadDoor && (d.IsOverloadDoor ||
                                        otherDoor.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor ||
                                        otherDoor.DoorType == DoorType.OverloadDoor));

                    if (isTarget)
                    {
                        string layerType = currSector switch
                        {
                            SectorType.Main => "MainLayer",
                            SectorType.Secondary => "SecondaryLayer",
                            SectorType.Overload => "ThirdLayer",
                            _ => "MainLayer"
                        };
                        return (layerType, $"Zone_{currZone}");
                    }

                    var neighbor = (otherGeo, otherDoor.AreaFrom);
                    if (visited.Add(neighbor))
                    {
                        SectorType nextSector = currSector;
                        if (d.IsSecondaryDoor || otherDoor.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor ||
                            otherDoor.DoorType == DoorType.SecondaryDoor) nextSector = SectorType.Secondary;
                        else if (d.IsOverloadDoor || otherDoor.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor ||
                                 otherDoor.DoorType == DoorType.OverloadDoor) nextSector = SectorType.Overload;
                        queue.Enqueue((otherGeo, otherDoor.AreaFrom, nextSector));
                    }
                }
            }
        }

        // Direct fallback if BFS didn't reach the door
        var directDoor = gridState.PlacedDoors.FirstOrDefault(d =>
            (targetDoorType == DoorType.SecondaryDoor && (d.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor)) ||
            (targetDoorType == DoorType.OverloadDoor && (d.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor)));

        if (directDoor != null && directDoor.Geomorph != null)
        {
            int z = directDoor.Geomorph.GetRoomZone(directDoor.AreaFrom);
            var sec = gridState.GetSectorForZone(z);
            string layer = sec switch
            {
                SectorType.Main => "MainLayer",
                SectorType.Secondary => "SecondaryLayer",
                SectorType.Overload => "ThirdLayer",
                _ => "MainLayer"
            };
            return (layer, $"Zone_{z}");
        }

        return null;
    }

    public static List<string> GetBulkheadZones(LevelLayoutGridState? gridState, SectorType sectorType)
    {
        var list = new List<string>();
        if (gridState == null || gridState.PlacedGeomorphs.Count == 0) return list;

        var queue = new Queue<(PlacedGeomorphNode geo, int area, SectorType sector, int zone)>();
        var visited = new HashSet<(string geoId, int area)>();

        var rootGeo = gridState.PlacedGeomorphs[0];
        if (rootGeo.IsRoomGenerated(0))
        {
            var sec = rootGeo.GetRoomSector(0);
            var z = rootGeo.GetRoomZone(0);
            visited.Add((rootGeo.Id, 0));
            queue.Enqueue((rootGeo, 0, sec, z));
        }

        void ProcessQueue()
        {
            while (queue.Count > 0)
            {
                var (currGeo, currArea, currSector, currZone) = queue.Dequeue();

                // 1. Internal doors
                foreach (var d in gridState.PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
                {
                    int neighborArea = (d.AreaFrom == currArea) ? d.AreaTo : (d.AreaTo == currArea ? d.AreaFrom : -1);
                    if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                    {
                        var neighborKey = (currGeo.Id, neighborArea);
                        bool isUnvisited = !visited.Contains(neighborKey);
                        var neighborSector = currGeo.GetRoomSector(neighborArea);
                        var neighborZone = currGeo.GetRoomZone(neighborArea);

                        bool isMainDoor = d.IsMainDoor || d.DoorType == DoorType.MainDoor;
                        if (isMainDoor && isUnvisited)
                        {
                            if (neighborSector == sectorType)
                            {
                                list.Add($"Zone_{neighborZone}");
                            }
                        }

                        if (visited.Add(neighborKey))
                        {
                            queue.Enqueue((currGeo, neighborArea, neighborSector, neighborZone));
                        }
                    }
                }

                // 2. External doors
                foreach (var d in gridState.PlacedDoors.Where(d =>
                             !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected &&
                             d.ConnectedDoor != null))
                {
                    var otherDoor = d.ConnectedDoor!;
                    var otherGeo = otherDoor.Geomorph;
                    if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                    {
                        var neighborKey = (otherGeo.Id, otherDoor.AreaFrom);
                        bool isUnvisited = !visited.Contains(neighborKey);
                        var neighborSector = otherGeo.GetRoomSector(otherDoor.AreaFrom);
                        var neighborZone = otherGeo.GetRoomZone(otherDoor.AreaFrom);

                        bool isMainDoor = d.IsMainDoor || otherDoor.IsMainDoor ||
                                          d.DoorType == DoorType.MainDoor ||
                                          otherDoor.DoorType == DoorType.MainDoor;
                        if (isMainDoor && isUnvisited)
                        {
                            if (neighborSector == sectorType)
                            {
                                list.Add($"Zone_{neighborZone}");
                            }
                        }

                        if (visited.Add(neighborKey))
                        {
                            queue.Enqueue((otherGeo, otherDoor.AreaFrom, neighborSector, neighborZone));
                        }
                    }
                }
            }
        }

        ProcessQueue();

        // Check any unvisited disconnected rooms
        foreach (var geo in gridState.PlacedGeomorphs)
        {
            foreach (var area in geo.GeneratedRooms)
            {
                var key = (geo.Id, area);
                if (!visited.Contains(key))
                {
                    var sec = geo.GetRoomSector(area);
                    var z = geo.GetRoomZone(area);
                    visited.Add(key);
                    queue.Enqueue((geo, area, sec, z));
                    ProcessQueue();
                }
            }
        }

        return list.Distinct().ToList();
    }

    public static List<string> GetMainBulkheadZones(LevelLayoutGridState? gridState)
    {
        return GetBulkheadZones(gridState, SectorType.Main);
    }

    public static List<string> GetSecondaryBulkheadZones(LevelLayoutGridState? gridState)
    {
        return GetBulkheadZones(gridState, SectorType.Secondary);
    }
}
