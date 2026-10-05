using System;
using System.Collections.Generic;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class RoomSvgTests
{
    [Fact]
    public void LoadMetadata_DeserializesMeshWithVerticesAndIndices()
    {
        var metadata = GeomorphMetadataService.GetMetadata("Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab");
        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.AreaDatas);

        var areaWithMesh = metadata.AreaDatas.FirstOrDefault(a => a.Mesh != null && a.Mesh.Vertices.Count > 0);
        Assert.NotNull(areaWithMesh);
        Assert.NotNull(areaWithMesh.Mesh);
        Assert.True(areaWithMesh.Mesh.Vertices.Count > 0);
        Assert.True(areaWithMesh.Mesh.Indices.Count > 0);
        Assert.True(areaWithMesh.Mesh.Indices.Count % 3 == 0);
    }

    [Fact]
    public void RoomSvgService_GenerateSvgPathData_CreatesCorrectTriangleSvgPaths()
    {
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = 0, Y = 0, Z = 0 },
                new VertexPosition { X = 10, Y = 5, Z = 0 },
                new VertexPosition { X = 0, Y = 5, Z = 10 }
            },
            Indices = new List<int> { 0, 1, 2 }
        };

        string pathData = RoomSvgService.GenerateSvgPathData(mesh);
        Assert.NotEmpty(pathData);
        // Point 0: (0, 0), Point 1: (10, 0), Point 2: (0, -10).
        // Clockwise in 2D is oriented to Counter-Clockwise (CCW): (0,0) -> (0,-10) -> (10,0)
        Assert.Contains("M 0,0 L 0,-10 L 10,0 Z", pathData);
    }

    [Fact]
    public void RoomSvgService_GenerateSvgContent_ProducesValidSvgDocument()
    {
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = -10, Y = 0, Z = -20 },
                new VertexPosition { X = 30, Y = 0, Z = -20 },
                new VertexPosition { X = 30, Y = 0, Z = 40 }
            },
            Indices = new List<int> { 0, 1, 2 }
        };

        string svg = RoomSvgService.GenerateSvgContent(mesh);
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg);
        Assert.Contains("viewBox=\"", svg);
        Assert.Contains("<path d=\"", svg);
        Assert.Contains("fill-rule=\"nonzero\"", svg);
        Assert.Contains("stroke=\"none\"", svg);
        Assert.Contains("stroke-width=\"0\"", svg);
        Assert.EndsWith("</svg>", svg);
    }

    [Fact]
    public void RoomSvgService_OverlappingTrianglesWithOppositeWinding_AreBothOrientedCCW()
    {
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                // Triangle 1 (CW in 2D)
                new VertexPosition { X = 0, Y = 0, Z = 0 },
                new VertexPosition { X = 10, Y = 0, Z = 0 },
                new VertexPosition { X = 0, Y = 0, Z = 10 },
                // Triangle 2 (CCW in 2D) overlapping Triangle 1
                new VertexPosition { X = 0, Y = 0, Z = 0 },
                new VertexPosition { X = 0, Y = 0, Z = 10 },
                new VertexPosition { X = 10, Y = 0, Z = 0 },
                // Degenerate triangle (collinear)
                new VertexPosition { X = 0, Y = 0, Z = 0 },
                new VertexPosition { X = 5, Y = 0, Z = 0 },
                new VertexPosition { X = 10, Y = 0, Z = 0 }
            },
            Indices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        string pathData = RoomSvgService.GenerateSvgPathData(mesh);
        Assert.NotEmpty(pathData);
        // Degenerate triangle (6,7,8) should be skipped (only 2 triangles generated)
        int triangleCount = pathData.Split('Z', StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.Equal(2, triangleCount);

        // Both triangles should be oriented with CCW path: (0,0) -> (0,-10) -> (10,0)
        Assert.Contains("M 0,0 L 0,-10 L 10,0 Z", pathData);

        var geom = RoomSvgService.CreateGeometry(mesh);
        if (geom != null)
        {
            Assert.True(geom.Bounds.Width > 0 && geom.Bounds.Height > 0);
        }
    }

    [Fact]
    public void RoomSvgService_CreateGeometry_ReturnsValidStreamGeometry()
    {
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = 0, Y = 0, Z = 0 },
                new VertexPosition { X = 5, Y = 0, Z = 0 },
                new VertexPosition { X = 0, Y = 0, Z = 5 }
            },
            Indices = new List<int> { 0, 1, 2 }
        };

        string pathData = RoomSvgService.GenerateSvgPathData(mesh);
        Assert.NotEmpty(pathData);
        var geometry = RoomSvgService.CreateGeometry(mesh);
        if (geometry != null)
        {
            Assert.True(geometry.Bounds.Width > 0 && geometry.Bounds.Height > 0);
        }
    }

    [Fact]
    public void RoomSvgService_HandlesNullAndEmptyMeshGracefully()
    {
        Assert.Equal(string.Empty, RoomSvgService.GenerateSvgPathData(null));
        Assert.Equal(string.Empty, RoomSvgService.GenerateSvgContent(null));
        Assert.Null(RoomSvgService.CreateGeometry(null));

        var emptyMesh = new MeshData();
        Assert.Equal(string.Empty, RoomSvgService.GenerateSvgPathData(emptyMesh));
        Assert.Equal(string.Empty, RoomSvgService.GenerateSvgContent(emptyMesh));
        Assert.Null(RoomSvgService.CreateGeometry(emptyMesh));

        var (content, path, geom) = RoomSvgService.LoadRoomSvg(null);
        Assert.Equal(string.Empty, content);
        Assert.Equal(string.Empty, path);
        Assert.Null(geom);
    }

    [Fact]
    public void LevelLayoutGridState_GeneratingStartingTile_LoadsRoomSvgOnMap()
    {
        var gridState = new LevelLayoutGridState();
        string prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";

        gridState.InitializeStartingTile(prefab, 0, 0);

        Assert.Single(gridState.PlacedGeomorphs);
        var geo = gridState.PlacedGeomorphs[0];

        Assert.NotEmpty(geo.Rooms);
        Assert.NotEmpty(gridState.PlacedRooms);

        var room0 = geo.Rooms.FirstOrDefault(r => r.AreaIndex == 0);
        Assert.NotNull(room0);
        Assert.Equal(geo.WorldX, room0.WorldX);
        Assert.Equal(geo.WorldY, room0.WorldY);
        Assert.Equal(geo.WorldX, room0.CanvasLeft);
        Assert.Equal(geo.WorldY, room0.CanvasTop);
        Assert.NotEmpty(room0.SvgPathData);
        Assert.NotEmpty(room0.SvgContent);
        Assert.Equal(0.0, room0.StrokeThickness);
    }

    [Fact]
    public void LevelLayoutGridState_GenerateRoomFromDoor_LoadsRoomSvg()
    {
        var gridState = new LevelLayoutGridState();
        string prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";

        gridState.InitializeStartingTile(prefab, 0, 0);
        var internalDoor = gridState.PlacedDoors.FirstOrDefault(d => d.IsInternal && !d.IsConnected);
        Assert.NotNull(internalDoor);

        int targetArea = internalDoor.AreaFrom == 0 ? internalDoor.AreaTo : internalDoor.AreaFrom;
        int generatedArea = gridState.GenerateRoomFromDoor(internalDoor, DoorType.SecurityDoor);

        Assert.Equal(targetArea, generatedArea);
        Assert.True(gridState.PlacedRooms.Count >= 2);

        var targetRoom = gridState.PlacedRooms.FirstOrDefault(r => r.AreaIndex == generatedArea);
        Assert.NotNull(targetRoom);
        Assert.NotEmpty(targetRoom.SvgPathData);
        Assert.NotEqual(0, targetRoom.Zone); // Security door should assign a new zone
    }

    [Fact]
    public void LevelLayoutGridState_ExpandFromDoor_LoadsNewGeomorphRoomSvg()
    {
        var gridState = new LevelLayoutGridState();
        string prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";

        gridState.InitializeStartingTile(prefab, 0, 0);
        var extDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal && !d.IsConnected);
        Assert.NotNull(extDoor);

        var newGeo = gridState.ExpandFromDoor(extDoor, prefab, DoorType.RoomDoor);
        Assert.NotNull(newGeo);

        Assert.Equal(2, gridState.PlacedGeomorphs.Count);
        Assert.True(gridState.PlacedRooms.Count >= 2);

        var newRoom = newGeo.Rooms.FirstOrDefault();
        Assert.NotNull(newRoom);
        Assert.Equal(newGeo.WorldX, newRoom.WorldX);
        Assert.Equal(newGeo.WorldY, newRoom.WorldY);
        Assert.NotEmpty(newRoom.SvgPathData);
    }

    [Fact]
    public void RoomSvgService_RotatesVerticesProperly()
    {
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = 0, Y = 0, Z = 0 },
                new VertexPosition { X = 10, Y = 0, Z = 0 },
                new VertexPosition { X = 0, Y = 0, Z = 10 }
            },
            Indices = new List<int> { 0, 1, 2 }
        };

        // At 0°: v0=(0, 0), v1=(10, 0), v2=(0, -10). CCW: (0,0) -> (0,-10) -> (10,0)
        string path0 = RoomSvgService.GenerateSvgPathData(mesh, 0.0);
        Assert.Contains("M 0,0 L 0,-10 L 10,0 Z", path0);

        // At 90° clockwise:
        // (0, 0) -> (0, 0)
        // (0, -10) -> (10, 0)
        // (10, 0) -> (0, 10)
        string path90 = RoomSvgService.GenerateSvgPathData(mesh, 90.0);
        Assert.Contains("M 0,0 L 10,0 L 0,10 Z", path90);

        // At 180°:
        // (0, 0) -> (0, 0)
        // (0, -10) -> (0, 10)
        // (10, 0) -> (-10, 0)
        string path180 = RoomSvgService.GenerateSvgPathData(mesh, 180.0);
        Assert.Contains("M 0,0 L 0,10 L -10,0 Z", path180);

        // At 270°:
        // (0, 0) -> (0, 0)
        // (0, -10) -> (-10, 0)
        // (10, 0) -> (0, -10)
        string path270 = RoomSvgService.GenerateSvgPathData(mesh, 270.0);
        Assert.Contains("M 0,0 L -10,0 L 0,-10 Z", path270);
    }
}
