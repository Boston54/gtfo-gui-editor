using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class LevelLayoutDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var path = context.FindFile(
            "LevelLayoutDataBlock.json",
            "GameData_LevelLayoutDataBlock_bin.json");
        if (!context.TryDeserializeFile<LevelLayoutDataBlockFile>(path, out var file) || file?.Blocks == null)
        {
            return;
        }
        if (file.Blocks.Count == 0)
        {
            return;
        }

        var datablockZoneNumbers = LoadDatablockZoneNumbers(path);
        var target = DatablockManager.Instance.Get<LevelLayoutBlock>();
        target.Clear();
        foreach (var block in file.Blocks)
        {
            datablockZoneNumbers.TryGetValue(block.PersistentId, out var zoneNumbers);
            ImportLevelGenOverride(context, block, zoneNumbers);
            target.Add(block);
        }
    }

    private static Dictionary<uint, List<int>> LoadDatablockZoneNumbers(string? path)
    {
        var result = new Dictionary<uint, List<int>>();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (!root.TryGetProperty("Blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var block in blocks.EnumerateArray())
            {
                if (!block.TryGetProperty("persistentID", out var persistentIdElement) ||
                    !persistentIdElement.TryGetUInt32(out uint persistentId))
                {
                    continue;
                }

                var zoneNumbers = new List<int>();
                if (block.TryGetProperty("Zones", out var zones) && zones.ValueKind == JsonValueKind.Array)
                {
                    foreach (var zone in zones.EnumerateArray())
                    {
                        if (zone.TryGetProperty("LocalIndex", out var localIndex) &&
                            localIndex.ValueKind == JsonValueKind.String)
                        {
                            string value = localIndex.GetString() ?? string.Empty;
                            if (value.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase) &&
                                int.TryParse(value.AsSpan("Zone_".Length), out int zoneNumber))
                            {
                                zoneNumbers.Add(zoneNumber);
                                continue;
                            }
                        }
                        zoneNumbers.Add(zoneNumbers.Count);
                    }
                }
                result[persistentId] = zoneNumbers;
            }
        }
        catch
        {
        }

        return result;
    }

    private static void ImportLevelGenOverride(
        ImportContext context,
        LevelLayoutBlock block,
        IReadOnlyList<int>? zoneNumbers)
    {
        var grid = block.GridState;
        grid.Clear();

        // The elevator tile is the root of the main layout and is not written into
        // GeoOverrides by the exporter. Ensure it exists even when no placements or
        // no LevelGen file exists for this layout.
        if (GetSector(block) == SectorType.Main)
        {
            string elevatorTile = block.ElevatorTile;
            if (string.IsNullOrWhiteSpace(elevatorTile))
            {
                elevatorTile = GeomorphService.GetElevatorOptionsForComplex(block.ComplexType)
                    .FirstOrDefault()?.Prefab ?? string.Empty;
                block.ElevatorTile = elevatorTile;
            }

            if (!string.IsNullOrWhiteSpace(elevatorTile))
            {
                grid.InitializeStartingTile(elevatorTile, 0, 0);
            }
        }

        string tunerDirectory = Path.Combine(context.FolderPath, "Custom", "LGTunerAdvanced");
        string fileName = $"LevelGen_{block.PersistentId}.json";
        string? levelGenPath = null;

        if (Directory.Exists(tunerDirectory))
        {
            levelGenPath = Directory.EnumerateFiles(tunerDirectory, fileName, SearchOption.AllDirectories)
                .FirstOrDefault();
        }

        levelGenPath ??= Directory.EnumerateFiles(context.FolderPath, fileName, SearchOption.AllDirectories)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(levelGenPath) || !File.Exists(levelGenPath))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(levelGenPath));
            var root = document.RootElement;

            var areaConnections = ParseAreaConnections(root);
            if (root.TryGetProperty("GeoOverrides", out var geoOverrides) &&
                geoOverrides.ValueKind == JsonValueKind.Array)
            {
                foreach (var geoOverride in geoOverrides.EnumerateArray())
                {
                    if (!geoOverride.TryGetProperty("Geomorph", out var geomorphElement) ||
                        geomorphElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    string prefabPath = geomorphElement.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(prefabPath))
                    {
                        continue;
                    }

                    int gridX = 0;
                    int gridZ = 0;
                    if (geoOverride.TryGetProperty("Position", out var position))
                    {
                        if (position.TryGetProperty("x", out var x) && x.TryGetInt32(out int xValue))
                        {
                            gridX = xValue;
                        }
                        if (position.TryGetProperty("z", out var z) && z.TryGetInt32(out int zValue))
                        {
                            gridZ = zValue;
                        }
                    }

                    int gridY = -gridZ;
                    if (gridX == 0 && gridY == 0 && grid.PlacedGeomorphs.Any(g => g.GridX == 0 && g.GridY == 0))
                    {
                        continue;
                    }

                    double rotationAngle = 0;
                    if (geoOverride.TryGetProperty("Rotation", out var rotation) && rotation.TryGetInt32(out int rotationValue))
                    {
                        rotationAngle = rotationValue * 90.0;
                    }

                    var altitude = GeomorphAltitude.Mid;
                    if (geoOverride.TryGetProperty("Altitude", out var altitudeElement))
                    {
                        if (altitudeElement.TryGetInt32(out int altitudeValue))
                        {
                            altitude = altitudeValue switch
                            {
                                < 0 => GeomorphAltitude.Low,
                                > 0 => GeomorphAltitude.High,
                                _ => GeomorphAltitude.Mid
                            };
                        }
                        else if (altitudeElement.ValueKind == JsonValueKind.String &&
                                 Enum.TryParse<GeomorphAltitude>(altitudeElement.GetString(), true, out var parsedAltitude))
                        {
                            altitude = parsedAltitude;
                        }
                    }

                    var geomorph = grid.CreateGeomorphNode(prefabPath, gridX, gridY, -1, rotationAngle, altitude);
                    grid.PlacedGeomorphs.Add(geomorph);

                    int connectingArea = FindConnectingArea(grid, geomorph, areaConnections);
                    foreach (var door in grid.GenerateDoorsForRoom(geomorph, connectingArea))
                    {
                        if (!grid.PlacedDoors.Contains(door))
                        {
                            grid.PlacedDoors.Add(door);
                        }
                    }
                    foreach (var room in geomorph.Rooms)
                    {
                        if (!grid.PlacedRooms.Contains(room))
                        {
                            grid.PlacedRooms.Add(room);
                        }
                    }
                    foreach (var door in geomorph.Doors.Where(d => !d.IsInternal))
                    {
                        if (!grid.PlacedDoors.Contains(door))
                        {
                            grid.PlacedDoors.Add(door);
                        }
                    }
                }
            }

            var sector = GetSector(block);
            if (root.TryGetProperty("ZoneOverrides", out var zoneOverrides) &&
                zoneOverrides.ValueKind == JsonValueKind.Array)
            {
                int zoneIndex = 0;
                foreach (var zoneOverride in zoneOverrides.EnumerateArray())
                {
                    int zoneNumber = ParseZoneNumber(zoneNumbers, zoneIndex);
                    if (zoneOverride.TryGetProperty("AreaOverrides", out var areaOverrides) &&
                        areaOverrides.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var areaOverride in areaOverrides.EnumerateArray())
                        {
                            int previousGridX = 0;
                            int previousGridZ = 0;
                            if (areaOverride.TryGetProperty("PreviousCellPosition", out var previousPosition))
                            {
                                if (previousPosition.TryGetProperty("x", out var x) && x.TryGetInt32(out int xValue))
                                {
                                    previousGridX = xValue;
                                }
                                if (previousPosition.TryGetProperty("z", out var z) && z.TryGetInt32(out int zValue))
                                {
                                    previousGridZ = zValue;
                                }
                            }

                            int areaIndex = 0;
                            if (areaOverride.TryGetProperty("PreviousCellInternalAreaID", out var areaElement) &&
                                areaElement.TryGetInt32(out int areaValue))
                            {
                                areaIndex = areaValue;
                            }

                            int previousGridY = -previousGridZ;
                            var previousGeomorph = grid.PlacedGeomorphs.FirstOrDefault(g =>
                                g.GridX == previousGridX && g.GridY == previousGridY);

                            if (previousGeomorph == null)
                            {
                                continue;
                            }

                            int expanderIndex = 0;
                            if (areaOverride.TryGetProperty("LG_ExpanderID", out var expanderElement))
                            {
                                expanderElement.TryGetInt32(out expanderIndex);
                            }

                            var sourceDoor = previousGeomorph.Doors.FirstOrDefault(d =>
                                d.ExpanderIndex == expanderIndex &&
                                d.AreaFrom == areaIndex &&
                                !d.IsInternal) ??
                                previousGeomorph.Doors.FirstOrDefault(d =>
                                    d.ExpanderIndex == expanderIndex &&
                                    d.AreaFrom == areaIndex);

                            PlacedGeomorphNode? targetGeomorph = null;
                            int targetArea = -1;

                            if (sourceDoor?.IsInternal == true)
                            {
                                targetGeomorph = previousGeomorph;
                                targetArea = sourceDoor.AreaFrom == areaIndex
                                    ? sourceDoor.AreaTo
                                    : sourceDoor.AreaFrom;
                            }
                            else if (sourceDoor?.ConnectedDoor?.Geomorph != null)
                            {
                                targetGeomorph = sourceDoor.ConnectedDoor.Geomorph;
                                targetArea = sourceDoor.ConnectedDoor.AreaFrom;
                            }

                            if (targetGeomorph == null || targetArea < 0)
                            {
                                continue;
                            }

                            if (!targetGeomorph.IsRoomGenerated(targetArea))
                            {
                                grid.GenerateDoorsForRoom(targetGeomorph, targetArea);
                            }
                            targetGeomorph.SetRoomSector(targetArea, sector);
                            targetGeomorph.SetRoomZone(targetArea, zoneNumber);

                            var targetRoom = targetGeomorph.Rooms.FirstOrDefault(r => r.AreaIndex == targetArea);
                            if (targetRoom != null)
                            {
                                targetRoom.Sector = sector;
                                targetRoom.Zone = zoneNumber;
                            }
                        }
                    }
                    zoneIndex++;
                }
            }

            ReconcileImportedDoorsAndRooms(grid);
            grid.RecalculateSecurityDoorConstraints();
        }
        catch
        {
        }
    }

    private static List<AreaConnection> ParseAreaConnections(JsonElement root)
    {
        var connections = new List<AreaConnection>();
        if (!root.TryGetProperty("ZoneOverrides", out var zoneOverrides) ||
            zoneOverrides.ValueKind != JsonValueKind.Array)
        {
            return connections;
        }

        foreach (var zoneOverride in zoneOverrides.EnumerateArray())
        {
            if (!zoneOverride.TryGetProperty("AreaOverrides", out var areaOverrides) ||
                areaOverrides.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var areaOverride in areaOverrides.EnumerateArray())
            {
                int previousGridX = 0;
                int previousGridZ = 0;
                int previousArea = 0;
                int expanderIndex = 0;

                if (areaOverride.TryGetProperty("PreviousCellPosition", out var previousPosition))
                {
                    previousPosition.TryGetProperty("x", out var xElement);
                    previousPosition.TryGetProperty("z", out var zElement);
                    xElement.TryGetInt32(out previousGridX);
                    zElement.TryGetInt32(out previousGridZ);
                }
                areaOverride.TryGetProperty("PreviousCellInternalAreaID", out var previousAreaElement);
                previousAreaElement.TryGetInt32(out previousArea);
                areaOverride.TryGetProperty("LG_ExpanderID", out var expanderElement);
                expanderElement.TryGetInt32(out expanderIndex);

                connections.Add(new AreaConnection(
                    previousGridX,
                    previousGridZ,
                    previousArea,
                    expanderIndex));
            }
        }

        return connections;
    }

    private static int FindConnectingArea(
        LevelLayoutGridState grid,
        PlacedGeomorphNode geomorph,
        IReadOnlyList<AreaConnection> areaConnections)
    {
        var metadata = geomorph.Metadata;
        if (metadata?.AreaDatas == null || metadata.AreaDatas.Count == 0)
        {
            return 0;
        }

        double bestDistance = double.MaxValue;
        int bestArea = 0;
        bool foundMatch = false;

        foreach (var connection in areaConnections)
        {
            var parent = grid.PlacedGeomorphs.FirstOrDefault(g =>
                g != geomorph &&
                g.GridX == connection.PreviousGridX &&
                g.GridY == -connection.PreviousGridZ);
            if (parent == null)
            {
                continue;
            }

            var parentDoor = parent.Doors.FirstOrDefault(d =>
                d.ExpanderIndex == connection.ExpanderIndex &&
                d.AreaFrom == connection.PreviousArea);
            if (parentDoor == null)
            {
                parentDoor = parent.Doors.FirstOrDefault(d =>
                    d.ExpanderIndex == connection.ExpanderIndex &&
                    !d.IsInternal);
            }
            if (parentDoor == null)
            {
                continue;
            }

            for (int areaIndex = 0; areaIndex < metadata.AreaDatas.Count; areaIndex++)
            {
                foreach (var expander in metadata.AreaDatas[areaIndex]?.ExpanderDatas ?? new())
                {
                    if (expander.AreaTo >= 0)
                    {
                        continue;
                    }

                    var (localX, localY) = LevelLayoutGridState.RotateLocalOffset(
                        expander.ExpanderPosition.X,
                        -expander.ExpanderPosition.Z,
                        geomorph.RotationAngle);
                    double worldX = geomorph.WorldX + localX;
                    double worldY = geomorph.WorldY + localY;
                    double distance = Math.Sqrt(
                        Math.Pow(worldX - parentDoor.WorldX, 2) +
                        Math.Pow(worldY - parentDoor.WorldY, 2));

                    if (distance < bestDistance && distance <= 10.0)
                    {
                        bestDistance = distance;
                        bestArea = areaIndex;
                        foundMatch = true;
                    }
                }
            }
        }

        if (foundMatch)
        {
            return bestArea;
        }

        for (int areaIndex = 0; areaIndex < metadata.AreaDatas.Count; areaIndex++)
        {
            var expanders = metadata.AreaDatas[areaIndex]?.ExpanderDatas;
            if (expanders == null)
            {
                continue;
            }

            foreach (var expander in expanders.Where(e => e.AreaTo < 0))
            {
                var (localX, localY) = LevelLayoutGridState.RotateLocalOffset(
                    expander.ExpanderPosition.X,
                    -expander.ExpanderPosition.Z,
                    geomorph.RotationAngle);

                double worldX = geomorph.WorldX + localX;
                double worldY = geomorph.WorldY + localY;

                foreach (var existingDoor in grid.PlacedDoors.Where(d => !d.IsInternal &&
                                                                      d.ConnectedDoor == null &&
                                                                      d.Geomorph != geomorph))
                {
                    double distance = Math.Sqrt(
                        Math.Pow(worldX - existingDoor.WorldX, 2) +
                        Math.Pow(worldY - existingDoor.WorldY, 2));

                    if (distance < bestDistance && distance <= 10.0)
                    {
                        bestDistance = distance;
                        bestArea = areaIndex;
                        foundMatch = true;
                    }
                }
            }
        }

        return foundMatch ? bestArea : 0;
    }

    private static void ReconcileImportedDoorsAndRooms(LevelLayoutGridState grid)
    {
        foreach (var geomorph in grid.PlacedGeomorphs)
        {
            foreach (var room in geomorph.Rooms)
            {
                if (!grid.PlacedRooms.Contains(room))
                {
                    grid.PlacedRooms.Add(room);
                }
            }

            foreach (var door in geomorph.Doors)
            {
                if (!grid.PlacedDoors.Contains(door))
                {
                    grid.PlacedDoors.Add(door);
                }
            }
        }

        foreach (var door in grid.PlacedDoors.Where(d => !d.IsInternal && d.ConnectedDoor == null).ToList())
        {
            var matchingDoor = grid.PlacedDoors.FirstOrDefault(other =>
                !other.IsInternal &&
                other != door &&
                other.Geomorph != door.Geomorph &&
                other.ConnectedDoor == null &&
                Math.Abs(other.WorldX - door.WorldX) < 4.0 &&
                Math.Abs(other.WorldY - door.WorldY) < 4.0 &&
                (other.Direction != door.Direction ||
                 Math.Abs(other.RotationAngle - door.RotationAngle) % 180.0 > 1.0));

            if (matchingDoor == null)
            {
                continue;
            }

            door.ConnectedDoor = matchingDoor;
            matchingDoor.ConnectedDoor = door;
            door.IsConnected = true;
            matchingDoor.IsConnected = true;
        }

        grid.UpdateRoomNumbers();
    }

    private static SectorType GetSector(LevelLayoutBlock block)
    {
        string name = block.Name ?? string.Empty;
        if (name.Contains("_Third", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("_Overload", StringComparison.OrdinalIgnoreCase))
        {
            return SectorType.Overload;
        }
        if (name.Contains("_Secondary", StringComparison.OrdinalIgnoreCase))
        {
            return SectorType.Secondary;
        }
        return SectorType.Main;
    }

    private sealed class AreaConnection
    {
        public AreaConnection(int previousGridX, int previousGridZ, int previousArea, int expanderIndex)
        {
            PreviousGridX = previousGridX;
            PreviousGridZ = previousGridZ;
            PreviousArea = previousArea;
            ExpanderIndex = expanderIndex;
        }

        public int PreviousGridX { get; }
        public int PreviousGridZ { get; }
        public int PreviousArea { get; }
        public int ExpanderIndex { get; }
    }

    private static int ParseZoneNumber(IReadOnlyList<int>? zoneNumbers, int zoneOverrideIndex)
    {
        if (zoneNumbers != null && zoneOverrideIndex >= 0 && zoneOverrideIndex < zoneNumbers.Count)
        {
            return zoneNumbers[zoneOverrideIndex];
        }
        return zoneOverrideIndex;
    }

    private sealed class LevelLayoutDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<LevelLayoutBlock>? Blocks { get; set; }
    }
}
