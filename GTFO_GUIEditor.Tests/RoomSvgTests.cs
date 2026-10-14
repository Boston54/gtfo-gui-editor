using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
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

    [Fact]
    public void RoomSvgService_Extract2DTriangles_CalculatesAreaAndBounds()
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

        var (triangles, bounds, area) = RoomSvgService.Extract2DTriangles(mesh, 0.0);
        Assert.Single(triangles);
        Assert.Equal(50.0, area, 3);
        Assert.Equal(0.0, bounds.MinX, 3);
        Assert.Equal(10.0, bounds.MaxX, 3);
        Assert.Equal(-10.0, bounds.MinY, 3);
        Assert.Equal(0.0, bounds.MaxY, 3);
    }

    [Fact]
    public void RoomSvgService_IsPointInTriangle_CorrectlyIdentifiesPoints()
    {
        // Triangle with vertices (0,0), (10,0), (0,10)
        Assert.True(RoomSvgService.IsPointInTriangle(2, 2, 0, 0, 10, 0, 0, 10));
        Assert.True(RoomSvgService.IsPointInTriangle(0, 0, 0, 0, 10, 0, 0, 10)); // Vertex
        Assert.True(RoomSvgService.IsPointInTriangle(5, 0, 0, 0, 10, 0, 0, 10)); // Edge
        Assert.False(RoomSvgService.IsPointInTriangle(8, 8, 0, 0, 10, 0, 0, 10)); // Outside
        Assert.False(RoomSvgService.IsPointInTriangle(-1, -1, 0, 0, 10, 0, 0, 10)); // Outside
    }

    [Fact]
    public void PlacedRoomNode_HoverState_UpdatesFillBrushAndZIndexWithoutStrokeWidth()
    {
        var geo = new PlacedGeomorphNode
        {
            Altitude = GeomorphAltitude.Mid,
            AltitudeOffset = 6.0
        };

        var room = new PlacedRoomNode
        {
            Geomorph = geo,
            Zone = 1,
            AreaIndex = 0,
            MeshHeight = 2.0
        };

        double height = room.TotalHeight;
        var baseBrush = (SolidColorBrush)RoomSvgService.GetHeightFillBrush(height, 6.0);
        var hoverBrush = (SolidColorBrush)RoomSvgService.GetHoveredHeightFillBrush(height, 6.0);
        var zoneHighlightBrush = (SolidColorBrush)RoomSvgService.GetZoneHighlightHeightFillBrush(height, 6.0);

        Assert.False(room.IsHovered);
        Assert.False(room.IsZoneHighlighted);
        Assert.Equal(0.0, room.StrokeThickness);
        Assert.Equal(5, room.ZIndex);
        Assert.Equal(baseBrush.Color, ((SolidColorBrush)room.FillBrush).Color);

        // Hover
        room.IsHovered = true;
        Assert.True(room.IsHovered);
        Assert.Equal(0.0, room.StrokeThickness);
        Assert.Equal(7, room.ZIndex);
        Assert.Equal(hoverBrush.Color, ((SolidColorBrush)room.FillBrush).Color);

        // Unhover
        room.IsHovered = false;
        Assert.False(room.IsHovered);
        Assert.Equal(0.0, room.StrokeThickness);
        Assert.Equal(5, room.ZIndex);
        Assert.Equal(baseBrush.Color, ((SolidColorBrush)room.FillBrush).Color);

        // Zone Highlighted (co-zoned room)
        room.IsZoneHighlighted = true;
        Assert.True(room.IsZoneHighlighted);
        Assert.Equal(0.0, room.StrokeThickness);
        Assert.Equal(6, room.ZIndex);
        Assert.Equal(zoneHighlightBrush.Color, ((SolidColorBrush)room.FillBrush).Color);
    }

    [Fact]
    public void LevelLayoutGridState_HoveredRoom_HighlightsHoveredAndCoZonedRooms()
    {
        var gridState = new LevelLayoutGridState { AltitudeOffset = 6.0 };
        var geo = new PlacedGeomorphNode { Altitude = GeomorphAltitude.Mid, AltitudeOffset = 6.0 };

        var roomZone1A = new PlacedRoomNode { Geomorph = geo, AreaIndex = 0, Zone = 1, MeshHeight = 0.0 };
        var roomZone1B = new PlacedRoomNode { Geomorph = geo, AreaIndex = 1, Zone = 1, MeshHeight = 3.0 };
        var roomZone2 = new PlacedRoomNode { Geomorph = geo, AreaIndex = 0, Zone = 2, MeshHeight = -2.0 };

        roomZone1A.UpdateFillBrush();
        roomZone1B.UpdateFillBrush();
        roomZone2.UpdateFillBrush();

        gridState.PlacedRooms.Add(roomZone1A);
        gridState.PlacedRooms.Add(roomZone1B);
        gridState.PlacedRooms.Add(roomZone2);

        var z1ABase = ((SolidColorBrush)RoomSvgService.GetHeightFillBrush(roomZone1A.TotalHeight, 6.0)).Color;
        var z1AHover = ((SolidColorBrush)RoomSvgService.GetHoveredHeightFillBrush(roomZone1A.TotalHeight, 6.0)).Color;
        var z1BBase = ((SolidColorBrush)RoomSvgService.GetHeightFillBrush(roomZone1B.TotalHeight, 6.0)).Color;
        var z1BHighlight = ((SolidColorBrush)RoomSvgService.GetZoneHighlightHeightFillBrush(roomZone1B.TotalHeight, 6.0)).Color;
        var z2Base = ((SolidColorBrush)RoomSvgService.GetHeightFillBrush(roomZone2.TotalHeight, 6.0)).Color;

        // Initially all base colors
        Assert.Equal(z1ABase, ((SolidColorBrush)roomZone1A.FillBrush).Color);
        Assert.Equal(z1BBase, ((SolidColorBrush)roomZone1B.FillBrush).Color);
        Assert.Equal(z2Base, ((SolidColorBrush)roomZone2.FillBrush).Color);

        // Hover roomZone1A
        gridState.HoveredRoom = roomZone1A;
        Assert.True(roomZone1A.IsHovered);
        Assert.False(roomZone1A.IsZoneHighlighted);
        Assert.Equal(z1AHover, ((SolidColorBrush)roomZone1A.FillBrush).Color);
        Assert.Equal(7, roomZone1A.ZIndex);

        // roomZone1B is in same zone (Zone 1)
        Assert.False(roomZone1B.IsHovered);
        Assert.True(roomZone1B.IsZoneHighlighted);
        Assert.Equal(z1BHighlight, ((SolidColorBrush)roomZone1B.FillBrush).Color);
        Assert.Equal(6, roomZone1B.ZIndex);

        // roomZone2 is in different zone (Zone 2) -> unaffected
        Assert.False(roomZone2.IsHovered);
        Assert.False(roomZone2.IsZoneHighlighted);
        Assert.Equal(z2Base, ((SolidColorBrush)roomZone2.FillBrush).Color);
        Assert.Equal(5, roomZone2.ZIndex);

        // Clear hover
        gridState.HoveredRoom = null;
        Assert.Equal(z1ABase, ((SolidColorBrush)roomZone1A.FillBrush).Color);
        Assert.Equal(z1BBase, ((SolidColorBrush)roomZone1B.FillBrush).Color);
        Assert.Equal(z2Base, ((SolidColorBrush)roomZone2.FillBrush).Color);
    }

    [Fact]
    public void LevelLayoutGridState_GetRoomAt_PrioritizesSmallerRoom()
    {
        var gridState = new LevelLayoutGridState();

        // Create large room (Area = 400)
        var largeMesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = -10, Y = 0, Z = -10 },
                new VertexPosition { X = 10, Y = 0, Z = -10 },
                new VertexPosition { X = -10, Y = 0, Z = 10 },
                new VertexPosition { X = 10, Y = 0, Z = 10 }
            },
            Indices = new List<int> { 0, 1, 2, 1, 3, 2 }
        };
        var (largeTriangles, largeBounds, largeArea) = RoomSvgService.Extract2DTriangles(largeMesh, 0.0);

        var largeRoom = new PlacedRoomNode
        {
            WorldX = 100,
            WorldY = 100,
            AreaIndex = 0,
            Area = largeArea,
            Triangles2D = largeTriangles,
            BoundsMinX = largeBounds.MinX,
            BoundsMinY = largeBounds.MinY,
            BoundsMaxX = largeBounds.MaxX,
            BoundsMaxY = largeBounds.MaxY,
            Geometry = RoomSvgService.CreateGeometry(largeMesh)
        };

        // Create small room (Area = 25) inside the bounds of the large room
        var smallMesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = -2.5, Y = 0, Z = -2.5 },
                new VertexPosition { X = 2.5, Y = 0, Z = -2.5 },
                new VertexPosition { X = -2.5, Y = 0, Z = 2.5 },
                new VertexPosition { X = 2.5, Y = 0, Z = 2.5 }
            },
            Indices = new List<int> { 0, 1, 2, 1, 3, 2 }
        };
        var (smallTriangles, smallBounds, smallArea) = RoomSvgService.Extract2DTriangles(smallMesh, 0.0);

        var smallRoom = new PlacedRoomNode
        {
            WorldX = 100,
            WorldY = 100,
            AreaIndex = 1,
            Area = smallArea,
            Triangles2D = smallTriangles,
            BoundsMinX = smallBounds.MinX,
            BoundsMinY = smallBounds.MinY,
            BoundsMaxX = smallBounds.MaxX,
            BoundsMaxY = smallBounds.MaxY,
            Geometry = RoomSvgService.CreateGeometry(smallMesh)
        };

        gridState.PlacedRooms.Add(largeRoom);
        gridState.PlacedRooms.Add(smallRoom);

        // At center (100, 100), point is inside both small and large rooms -> small room must win!
        var hitCenter = gridState.GetRoomAt(100, 100);
        Assert.NotNull(hitCenter);
        Assert.Same(smallRoom, hitCenter);

        // At (108, 108), point is inside large room but outside small room -> large room must be hit
        var hitLargeOnly = gridState.GetRoomAt(108, 108);
        Assert.NotNull(hitLargeOnly);
        Assert.Same(largeRoom, hitLargeOnly);

        // At (200, 200), point is outside all rooms -> null
        var hitOutside = gridState.GetRoomAt(200, 200);
        Assert.Null(hitOutside);
    }

    [Fact]
    public void LevelLayoutViewModel_RoomClick_OpensAndClosesEditRoomDialog()
    {
        var vm = new LevelLayoutViewModel();
        var room = new PlacedRoomNode
        {
            AreaIndex = 2,
            Zone = 3,
            Area = 152.4
        };

        Assert.False(vm.IsEditingRoom);
        Assert.Null(vm.EditingRoom);

        vm.OnRoomClicked(room);
        Assert.True(vm.IsEditingRoom);
        Assert.Same(room, vm.EditingRoom);
        Assert.Equal("2", vm.EditingRoomAreaIndex.ToString());
        Assert.Equal(3, vm.EditingRoomZone);
        Assert.Equal("152.4", vm.EditingRoomAreaSize);

        vm.CloseEditRoomDialog();
        Assert.False(vm.IsEditingRoom);
        Assert.Null(vm.EditingRoom);
    }

    [Fact]
    public void RoomSvgService_HeightColoring_HigherIsLighter_LowerIsDarker()
    {
        double altOffset = 6.0;

        var lowColor = RoomSvgService.GetColorForHeight(-6.0, altOffset);
        var midColor = RoomSvgService.GetColorForHeight(0.0, altOffset);
        var highColor = RoomSvgService.GetColorForHeight(6.0, altOffset);

        // RGB values should strictly increase with height (higher = lighter, lower = darker)
        Assert.True(highColor.R > midColor.R, $"High R {highColor.R} should be > Mid R {midColor.R}");
        Assert.True(highColor.G > midColor.G, $"High G {highColor.G} should be > Mid G {midColor.G}");
        Assert.True(highColor.B > midColor.B, $"High B {highColor.B} should be > Mid B {midColor.B}");

        Assert.True(midColor.R > lowColor.R, $"Mid R {midColor.R} should be > Low R {lowColor.R}");
        Assert.True(midColor.G > lowColor.G, $"Mid G {midColor.G} should be > Low G {lowColor.G}");
        Assert.True(midColor.B > lowColor.B, $"Mid B {midColor.B} should be > Low B {lowColor.B}");
    }

    [Fact]
    public void RoomSvgService_CalculateMeshHeight_AccountsForVertexHeights()
    {
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                new VertexPosition { X = 0, Y = 2.0, Z = 0 },
                new VertexPosition { X = 10, Y = 4.0, Z = 0 },
                new VertexPosition { X = 0, Y = 6.0, Z = 10 }
            },
            Indices = new List<int> { 0, 1, 2 }
        };

        double avgHeight = RoomSvgService.CalculateMeshHeight(mesh);
        Assert.Equal(4.0, avgHeight, 3);
    }

    [Fact]
    public void PlacedGeomorphNode_AltitudeOffset_CalculatesCorrectAltitudeValues()
    {
        var geo = new PlacedGeomorphNode
        {
            AltitudeOffset = 8.0,
            Altitude = GeomorphAltitude.Mid
        };
        Assert.Equal(0.0, geo.AltitudeOffsetValue);

        geo.Altitude = GeomorphAltitude.High;
        Assert.Equal(8.0, geo.AltitudeOffsetValue);

        geo.Altitude = GeomorphAltitude.Low;
        Assert.Equal(-8.0, geo.AltitudeOffsetValue);
    }

    [Fact]
    public void PlacedGeomorphNode_UpdateRoomsFillBrush_UpdatesRoomTotalHeightAndColor()
    {
        var geo = new PlacedGeomorphNode
        {
            AltitudeOffset = 6.0,
            Altitude = GeomorphAltitude.Mid
        };

        var room = new PlacedRoomNode
        {
            Geomorph = geo,
            AreaIndex = 0,
            MeshHeight = 2.0
        };
        geo.Rooms.Add(room);
        room.UpdateFillBrush();

        Assert.Equal(2.0, room.TotalHeight);
        var midColor = ((SolidColorBrush)room.FillBrush).Color;

        // Change altitude to High (+6.0 -> total 8.0)
        geo.Altitude = GeomorphAltitude.High;
        Assert.Equal(8.0, room.TotalHeight);
        var highColor = ((SolidColorBrush)room.FillBrush).Color;

        Assert.True(highColor.R > midColor.R);
        Assert.True(highColor.G > midColor.G);
        Assert.True(highColor.B > midColor.B);

        // Change altitude to Low (-6.0 -> total -4.0)
        geo.Altitude = GeomorphAltitude.Low;
        Assert.Equal(-4.0, room.TotalHeight);
        var lowColor = ((SolidColorBrush)room.FillBrush).Color;

        Assert.True(lowColor.R < midColor.R);
        Assert.True(lowColor.G < midColor.G);
        Assert.True(lowColor.B < midColor.B);
    }

    [Fact]
    public void LevelLayoutGridState_ExpandFromDoor_PlacesWithSelectedAltitude()
    {
        var gridState = new LevelLayoutGridState
        {
            AltitudeOffset = 10.0
        };
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal && (d.Direction == "Forwards" || d.Direction.Contains("Forward")));
        Assert.NotNull(elevatorDoor);

        var placedHighGeo = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0,
            altitude: GeomorphAltitude.High);

        Assert.NotNull(placedHighGeo);
        Assert.Equal(GeomorphAltitude.High, placedHighGeo.Altitude);
        Assert.Equal(10.0, placedHighGeo.AltitudeOffset);
        Assert.Equal(10.0, placedHighGeo.AltitudeOffsetValue);

        var room = placedHighGeo.Rooms.First();
        Assert.True(room.TotalHeight >= 10.0);
    }

    [Fact]
    public void LevelLayoutViewModel_ExpansionAltitudes_PopulatesAndDefaultsToDoorAltitude()
    {
        var vm = new LevelLayoutViewModel();
        var door = vm.GridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(door);
        Assert.Equal(GeomorphAltitude.Mid, door.Altitude);

        // Door at Mid defaults dialog to Mid
        vm.OpenDoorExpansionDialog(door);

        Assert.Equal(3, vm.AvailableExpansionAltitudes.Count);
        Assert.Contains(vm.AvailableExpansionAltitudes, a => a.Altitude == GeomorphAltitude.High && a.DisplayText == "High");
        Assert.Contains(vm.AvailableExpansionAltitudes, a => a.Altitude == GeomorphAltitude.Mid && a.DisplayText == "Mid");
        Assert.Contains(vm.AvailableExpansionAltitudes, a => a.Altitude == GeomorphAltitude.Low && a.DisplayText == "Low");

        Assert.NotNull(vm.SelectedExpansionAltitude);
        Assert.Equal(GeomorphAltitude.Mid, vm.SelectedExpansionAltitude.Altitude);

        vm.CancelDoorExpansion();

        // Change door altitude to High -> Dialog defaults to High
        door.Altitude = GeomorphAltitude.High;
        vm.OpenDoorExpansionDialog(door);
        Assert.NotNull(vm.SelectedExpansionAltitude);
        Assert.Equal(GeomorphAltitude.High, vm.SelectedExpansionAltitude.Altitude);
        vm.CancelDoorExpansion();

        // Change door altitude to Low -> Dialog defaults to Low
        door.Altitude = GeomorphAltitude.Low;
        vm.OpenDoorExpansionDialog(door);
        Assert.NotNull(vm.SelectedExpansionAltitude);
        Assert.Equal(GeomorphAltitude.Low, vm.SelectedExpansionAltitude.Altitude);
        vm.CancelDoorExpansion();
    }

    [Fact]
    public void PlacedGeomorphNode_ChangingAltitude_UpdatesAllDoorsAltitude()
    {
        var geo = new PlacedGeomorphNode
        {
            Altitude = GeomorphAltitude.Mid
        };
        var door1 = new PlacedDoorNode { Geomorph = geo, Altitude = GeomorphAltitude.Mid };
        var door2 = new PlacedDoorNode { Geomorph = geo, Altitude = GeomorphAltitude.Mid };
        geo.Doors.Add(door1);
        geo.Doors.Add(door2);

        geo.Altitude = GeomorphAltitude.High;
        Assert.Equal(GeomorphAltitude.High, door1.Altitude);
        Assert.Equal(GeomorphAltitude.High, door2.Altitude);

        geo.Altitude = GeomorphAltitude.Low;
        Assert.Equal(GeomorphAltitude.Low, door1.Altitude);
        Assert.Equal(GeomorphAltitude.Low, door2.Altitude);
    }

    [Fact]
    public void LevelLayoutGridState_ExpandFromDoor_DefaultsToSourceDoorAltitudeWhenOmitted()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal && (d.Direction == "Forwards" || d.Direction.Contains("Forward")));
        Assert.NotNull(elevatorDoor);
        Assert.Equal(GeomorphAltitude.Mid, elevatorDoor.Altitude);

        // Set door altitude to High
        elevatorDoor.Altitude = GeomorphAltitude.High;

        var placedGeo = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);

        Assert.NotNull(placedGeo);
        Assert.Equal(GeomorphAltitude.High, placedGeo.Altitude);
        Assert.All(placedGeo.Doors, d => Assert.Equal(GeomorphAltitude.High, d.Altitude));
    }

    [Fact]
    public void LevelLayoutViewModel_ExpansionAltitudes_AllowsUserToOverrideDefaultAltitude()
    {
        var vm = new LevelLayoutViewModel();
        var door = vm.GridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal && (d.Direction == "Forwards" || d.Direction.Contains("Forward")));
        Assert.NotNull(door);
        Assert.Equal(GeomorphAltitude.Mid, door.Altitude);

        vm.OpenDoorExpansionDialog(door);
        Assert.Equal(GeomorphAltitude.Mid, vm.SelectedExpansionAltitude?.Altitude);

        // Override default Mid with Low
        vm.SelectedExpansionAltitude = vm.AvailableExpansionAltitudes.First(a => a.Altitude == GeomorphAltitude.Low);
        vm.SelectedExpansionGeomorph = vm.AvailableExpansionGeomorphs.First(g => g.PrefabPath.Contains("mining_dig_site_AS_01"));
        bool success = vm.ConfirmDoorExpansion();

        Assert.True(success);
        var placedGeo = vm.GridState.PlacedGeomorphs.Last();
        Assert.Equal(GeomorphAltitude.Low, placedGeo.Altitude);
        Assert.All(placedGeo.Doors, d => Assert.Equal(GeomorphAltitude.Low, d.Altitude));
    }

    [Fact]
    public void EditRoom_AltitudeAndLighting_UpdatesGeomorphSettings()
    {
        var vm = new LevelLayoutViewModel();
        var room = vm.GridState.PlacedRooms.FirstOrDefault();
        Assert.NotNull(room);
        Assert.NotNull(room.Geomorph);

        vm.OnRoomClicked(room);
        Assert.True(vm.IsEditingRoom);
        Assert.Equal(room, vm.EditingRoom);

        // Change Altitude via ViewModel
        vm.SelectedGeomorphAltitudeOption = vm.AvailableGeomorphAltitudes.First(a => a.Altitude == GeomorphAltitude.High);
        Assert.Equal(GeomorphAltitude.High, room.Geomorph.Altitude);
        Assert.Equal(GeomorphAltitude.High, vm.EditingRoomGeomorphAltitude);
        Assert.All(room.Geomorph.Doors, d => Assert.Equal(GeomorphAltitude.High, d.Altitude));

        // Change Lighting via ViewModel
        vm.EditingRoomGeomorphLighting = 42;
        Assert.Equal(42u, room.Geomorph.Lighting);
        Assert.Equal(42u, vm.EditingRoomGeomorphLighting);

        // Change Lighting via Dropdown Selection
        var customLighting = new LightSettingsBlock { PersistentId = 99901, Name = "Test_Mining_Green" };
        vm.AvailableLightingConfigs.Add(customLighting);
        vm.SelectedGeomorphLightingOption = customLighting;
        Assert.Equal(99901u, room.Geomorph.Lighting);
        Assert.Equal(99901u, vm.EditingRoomGeomorphLighting);
        Assert.Equal(customLighting, vm.SelectedGeomorphLightingOption);

        vm.CloseEditRoomDialog();
        Assert.False(vm.IsEditingRoom);
        Assert.Null(vm.EditingRoom);
    }

    [Fact]
    public void LevelLayoutViewModel_ExpansionLighting_ShownOnlyWhenExpandingToNewZone()
    {
        var lighting1 = new LightSettingsBlock { PersistentId = 10, Name = "Lighting_Default" };
        var lighting2 = new LightSettingsBlock { PersistentId = 20, Name = "Lighting_Emergency_Red" };
        var lightingConfigs = new System.Collections.ObjectModel.ObservableCollection<LightSettingsBlock> { lighting1, lighting2 };

        var vm = new LevelLayoutViewModel(lightingConfigs: lightingConfigs);
        var door = vm.GridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal && (d.Direction == "Forwards" || d.Direction.Contains("Forward")));
        Assert.NotNull(door);

        vm.OpenDoorExpansionDialog(door);
        Assert.Equal(2, vm.AvailableLightingConfigs.Count);
        Assert.Equal(lighting1, vm.SelectedExpansionLighting);

        // Initially DoorType is RoomDoor -> Not expanding to a new zone
        Assert.False(vm.IsExpansionToNewZone);
        Assert.False(vm.ShowExpansionLighting);

        // Switch to Security Door -> Expanding to new zone
        vm.IsExpansionSecurityDoor = true;
        Assert.True(vm.IsExpansionToNewZone);
        Assert.True(vm.ShowExpansionLighting);

        // Switch back to Room Door
        vm.IsExpansionRoomDoor = true;
        Assert.False(vm.IsExpansionToNewZone);
        Assert.False(vm.ShowExpansionLighting);

        // Switch to Main Door and select lighting 2
        vm.IsExpansionMainDoor = true;
        Assert.True(vm.IsExpansionToNewZone);
        vm.SelectedExpansionLighting = lighting2;
        vm.SelectedExpansionGeomorph = vm.AvailableExpansionGeomorphs.First(g => g.PrefabPath.Contains("mining_dig_site_AS_01"));
        bool success = vm.ConfirmDoorExpansion();

        Assert.True(success);
        var placedGeo = vm.GridState.PlacedGeomorphs.Last();
        Assert.Equal(20u, placedGeo.Lighting);
        int targetZone = placedGeo.GetRoomZone(placedGeo.GeneratedRooms.First());
        Assert.Equal(20u, vm.GridState.GetOrCreateZoneSettings(targetZone).Lighting);
    }

    [Fact]
    public void LevelLayoutViewModel_RoomExpansionLighting_ShownOnlyWhenExpandingWithZoneDoor()
    {
        var lighting1 = new LightSettingsBlock { PersistentId = 101, Name = "Lighting_Base" };
        var lighting2 = new LightSettingsBlock { PersistentId = 202, Name = "Lighting_Special" };
        var lightingConfigs = new System.Collections.ObjectModel.ObservableCollection<LightSettingsBlock> { lighting1, lighting2 };

        var vm = new LevelLayoutViewModel(lightingConfigs: lightingConfigs);

        // Expand starting elevator with a room door to place a multi-room geomorph
        var elevatorDoor = vm.GridState.PlacedDoors.First(d => !d.IsInternal && (d.Direction == "Forwards" || d.Direction.Contains("Forward")));
        vm.OpenDoorExpansionDialog(elevatorDoor);
        vm.IsExpansionRoomDoor = true;
        vm.SelectedExpansionGeomorph = vm.AvailableExpansionGeomorphs.First(g => g.PrefabPath.Contains("mining_dig_site_AS_01"));
        Assert.True(vm.ConfirmDoorExpansion());

        var geo = vm.GridState.PlacedGeomorphs.Last();
        var internalDoor = geo.Doors.FirstOrDefault(d => d.IsInternal);
        Assert.NotNull(internalDoor);

        // Open Room Expansion Dialog
        vm.OpenRoomExpansionDialog(internalDoor);
        Assert.True(vm.IsExpandingRoom);
        Assert.Equal(2, vm.AvailableLightingConfigs.Count);
        Assert.Equal(lighting1, vm.SelectedRoomExpansionLighting);

        // Default is Room Door -> Not expanding to a new zone
        Assert.False(vm.IsRoomExpansionToNewZone);
        Assert.False(vm.ShowRoomExpansionLighting);

        // Switch to Security Door -> Expanding to new zone
        vm.IsRoomExpansionSecurityDoor = true;
        Assert.True(vm.IsRoomExpansionToNewZone);
        Assert.True(vm.ShowRoomExpansionLighting);

        // Select lighting 2 and confirm
        vm.SelectedRoomExpansionLighting = lighting2;
        Assert.True(vm.ConfirmRoomExpansion());
        Assert.False(vm.IsExpandingRoom);

        // Verify the generated room has a new zone and the zone settings has lighting 202
        int newArea = vm.RoomExpansionTargetArea;
        int newZone = geo.GetRoomZone(newArea);
        Assert.NotEqual(0, newZone);
        Assert.Equal(202u, vm.GridState.GetOrCreateZoneSettings(newZone).Lighting);
    }

    [Fact]
    public void MainWindowViewModel_SharesLightingConfigsBetweenLightingAndLevelLayout()
    {
        var mainVm = new MainWindowViewModel();
        Assert.Same(mainVm.Lighting.Blocks, mainVm.LevelLayout.AvailableLightingConfigs);

        var customConfig = new LightSettingsBlock { PersistentId = 9999, Name = "Shared_Test_Config" };
        mainVm.Lighting.Blocks.Add(customConfig);

        Assert.Contains(customConfig, mainVm.LevelLayout.AvailableLightingConfigs);
    }

    [Fact]
    public void DeleteGeomorph_LeafGeomorph_DeletesSingleGeomorph()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        var geo1 = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(geo1);
        Assert.Equal(2, gridState.PlacedGeomorphs.Count);

        var disconnected = gridState.GetDisconnectedGeomorphsOnDelete(geo1);
        Assert.Empty(disconnected);

        var deleted = gridState.DeleteGeomorph(geo1);
        Assert.Single(deleted);
        Assert.Contains(geo1, deleted);
        Assert.Single(gridState.PlacedGeomorphs);
        Assert.False(elevatorDoor.IsConnected);
        Assert.Null(elevatorDoor.ConnectedDoor);
    }

    [Fact]
    public void DeleteGeomorph_ChainGeomorph_DeletesAllDisconnectedGeomorphs()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        // Expand Geo 1
        var geo1 = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(geo1);

        // Generate Area 1 on Geo 1 to expose external doors
        var internalDoor = geo1.Doors.FirstOrDefault(d => d.IsInternal);
        if (internalDoor != null)
        {
            gridState.GenerateRoomFromDoor(internalDoor, DoorType.RoomDoor);
        }

        // Find open door on Geo 1 and expand Geo 2
        var geo1Door = geo1.Doors.FirstOrDefault(d => !d.IsInternal && !d.IsConnected);
        Assert.NotNull(geo1Door);

        var geo2 = gridState.ExpandFromDoor(
            geo1Door,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(geo2);
        Assert.Equal(3, gridState.PlacedGeomorphs.Count);

        // Deleting Geo 1 should disconnect Geo 2
        var disconnected = gridState.GetDisconnectedGeomorphsOnDelete(geo1);
        Assert.Single(disconnected);
        Assert.Contains(geo2, disconnected);

        var deleted = gridState.DeleteGeomorph(geo1);
        Assert.Equal(2, deleted.Count);
        Assert.Contains(geo1, deleted);
        Assert.Contains(geo2, deleted);
        Assert.Single(gridState.PlacedGeomorphs);
        Assert.Equal("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", gridState.PlacedGeomorphs[0].PrefabPath);
        Assert.False(elevatorDoor.IsConnected);
    }

    [Fact]
    public void DeleteGeomorph_ViewModelConfirmationAndCancel()
    {
        var vm = new LevelLayoutViewModel();
        var elevatorDoor = vm.GridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        var placedGeo = vm.GridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(placedGeo);

        var room = placedGeo.Rooms.First();
        vm.OnRoomClicked(room);
        Assert.True(vm.IsEditingRoom);

        // Request deletion
        vm.RequestDeleteGeomorph();
        Assert.True(vm.IsConfirmingLayoutDeletion);
        Assert.Equal("Confirm Delete Geomorph", vm.LayoutDeletionTitle);
        Assert.Contains(placedGeo.DisplayName, vm.LayoutDeletionMessage);

        // Cancel deletion
        vm.CancelLayoutDeletion();
        Assert.False(vm.IsConfirmingLayoutDeletion);
        Assert.Equal(2, vm.GridState.PlacedGeomorphs.Count);

        // Request again and Confirm
        vm.RequestDeleteGeomorph();
        Assert.True(vm.IsConfirmingLayoutDeletion);
        vm.ConfirmLayoutDeletion();

        Assert.False(vm.IsConfirmingLayoutDeletion);
        Assert.False(vm.IsEditingRoom);
        Assert.Single(vm.GridState.PlacedGeomorphs);
    }

    [Fact]
    public void DeleteRoom_LeafRoom_OnlyDeletesTargetRoom()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        // Digsite AS 01 has Area 0 and Area 1
        var geo1 = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(geo1);

        // Expand Area 1 internally
        var internalDoor = geo1.Doors.FirstOrDefault(d => d.IsInternal);
        if (internalDoor != null)
        {
            gridState.GenerateRoomFromDoor(internalDoor, DoorType.RoomDoor);
        }

        Assert.Equal(2, geo1.Rooms.Count);
        var room1 = geo1.Rooms.FirstOrDefault(r => r.AreaIndex == 1);
        Assert.NotNull(room1);

        var (disconnectedRooms, disconnectedGeos) = gridState.GetDisconnectedRoomsOnDelete(room1);
        Assert.Empty(disconnectedRooms);
        Assert.Empty(disconnectedGeos);

        var (deletedRooms, deletedGeos) = gridState.DeleteRoom(room1);
        Assert.Single(deletedRooms);
        Assert.Contains(room1, deletedRooms);
        Assert.Empty(deletedGeos);

        // Geo 1 still exists with Area 0
        Assert.Single(geo1.Rooms);
        Assert.Equal(0, geo1.Rooms[0].AreaIndex);
        Assert.Contains(geo1, gridState.PlacedGeomorphs);
        Assert.False(internalDoor!.IsConnected);
    }

    [Fact]
    public void DeleteRoom_RootRoomInMultiRoomGeo_DeletesChildRoomAndRemovesGeomorph()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        var geo1 = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(geo1);

        // Expand Area 1 internally
        var internalDoor = geo1.Doors.FirstOrDefault(d => d.IsInternal);
        if (internalDoor != null)
        {
            gridState.GenerateRoomFromDoor(internalDoor, DoorType.RoomDoor);
        }

        Assert.Equal(2, geo1.Rooms.Count);
        var room0 = geo1.Rooms.FirstOrDefault(r => r.AreaIndex == 0);
        var room1 = geo1.Rooms.FirstOrDefault(r => r.AreaIndex == 1);
        Assert.NotNull(room0);
        Assert.NotNull(room1);

        // Deleting Room 0 (connected to elevator) disconnects Room 1 (which depends on Room 0)
        var (disconnectedRooms, disconnectedGeos) = gridState.GetDisconnectedRoomsOnDelete(room0);
        Assert.Contains(room1, disconnectedRooms);
        Assert.Contains(geo1, disconnectedGeos);

        var (deletedRooms, deletedGeos) = gridState.DeleteRoom(room0);
        Assert.Equal(2, deletedRooms.Count);
        Assert.Contains(room0, deletedRooms);
        Assert.Contains(room1, deletedRooms);
        Assert.Single(deletedGeos);
        Assert.Contains(geo1, deletedGeos);
        Assert.Single(gridState.PlacedGeomorphs);
        Assert.False(elevatorDoor.IsConnected);
    }

    [Fact]
    public void DeleteRoom_ViewModelConfirmationAndCancel()
    {
        var vm = new LevelLayoutViewModel();
        var elevatorDoor = vm.GridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        var placedGeo = vm.GridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(placedGeo);

        var room = placedGeo.Rooms.First();
        vm.OnRoomClicked(room);
        Assert.True(vm.IsEditingRoom);

        // Request Room Deletion
        vm.RequestDeleteRoom();
        Assert.True(vm.IsConfirmingLayoutDeletion);
        Assert.Equal("Confirm Delete Room", vm.LayoutDeletionTitle);
        Assert.Contains(room.DisplayName, vm.LayoutDeletionMessage);

        // Cancel Room Deletion
        vm.CancelLayoutDeletion();
        Assert.False(vm.IsConfirmingLayoutDeletion);
        Assert.Equal(2, vm.GridState.PlacedGeomorphs.Count);

        // Request and Confirm
        vm.RequestDeleteRoom();
        Assert.True(vm.IsConfirmingLayoutDeletion);
        vm.ConfirmLayoutDeletion();

        Assert.False(vm.IsConfirmingLayoutDeletion);
        Assert.False(vm.IsEditingRoom);
        Assert.Single(vm.GridState.PlacedGeomorphs);
    }

    [Fact]
    public void DeleteRoom_RemovesUnexpandedDoorsOfDeletedRoom()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", 0, 0);

        var elevatorDoor = gridState.PlacedDoors.FirstOrDefault(d => !d.IsInternal);
        Assert.NotNull(elevatorDoor);

        // Digsite AS 01 has Area 0 and Area 1
        var geo1 = gridState.ExpandFromDoor(
            elevatorDoor,
            "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
            DoorType.RoomDoor,
            rotationAngle: 0.0);
        Assert.NotNull(geo1);

        // Before expanding Area 1, record door count
        int doorsBeforeArea1 = gridState.PlacedDoors.Count;

        // Expand Area 1 internally (Area 1 has its own external doors which get added to PlacedDoors)
        var internalDoor = geo1.Doors.FirstOrDefault(d => d.IsInternal);
        Assert.NotNull(internalDoor);
        gridState.GenerateRoomFromDoor(internalDoor, DoorType.RoomDoor);

        Assert.True(gridState.PlacedDoors.Count > doorsBeforeArea1);
        var area1ExternalDoors = gridState.PlacedDoors.Where(d => !d.IsInternal && d.Geomorph == geo1 && d.AreaFrom == 1).ToList();
        Assert.NotEmpty(area1ExternalDoors);

        var room1 = geo1.Rooms.FirstOrDefault(r => r.AreaIndex == 1);
        Assert.NotNull(room1);

        // Delete Room 1
        gridState.DeleteRoom(room1);

        // Verify all external doors of Area 1 were removed from PlacedDoors and geo1.Doors
        Assert.DoesNotContain(gridState.PlacedDoors, d => !d.IsInternal && d.Geomorph == geo1 && d.AreaFrom == 1);
        Assert.DoesNotContain(geo1.Doors, d => !d.IsInternal && d.AreaFrom == 1);

        // Verify the internal door connecting Area 0 and Area 1 remains as an unexpanded internal doorway
        Assert.Contains(gridState.PlacedDoors, d => d.IsInternal && d.Geomorph == geo1 && (d.AreaFrom == 0 && d.AreaTo == 1));
        Assert.False(internalDoor.IsConnected);

        // Total door count should match initial state before Area 1 was generated
        Assert.Equal(doorsBeforeArea1, gridState.PlacedDoors.Count);
    }

    [Fact]
    public void EditRoom_ZoneSettings_LightingAndResources()
    {
        var vm = new LevelLayoutViewModel();
        var room = vm.GridState.PlacedRooms.FirstOrDefault();
        Assert.NotNull(room);

        vm.OnRoomClicked(room);
        Assert.True(vm.IsEditingRoom);
        Assert.NotNull(vm.ActiveZoneSettings);
        Assert.Equal(room.Zone, vm.ActiveZoneSettings.ZoneIndex);

        // Configure Resource Kit Uses
        vm.ActiveZoneSettings.MedKitUses = 5;
        vm.ActiveZoneSettings.AmmoKitUses = 10;
        vm.ActiveZoneSettings.ToolKitUses = 4;
        vm.ActiveZoneSettings.DisinfectKitUses = 2;
        Assert.Equal(5, vm.ActiveZoneSettings.MedKitUses);
        Assert.Equal(10, vm.ActiveZoneSettings.AmmoKitUses);
        Assert.Equal(4, vm.ActiveZoneSettings.ToolKitUses);
        Assert.Equal(2, vm.ActiveZoneSettings.DisinfectKitUses);

        // Configure Bulkhead Keys, Power Cells, Fog Turbines, Disinfect Station
        vm.ActiveZoneSettings.BulkheadKeys = 3;
        vm.ActiveZoneSettings.PowerCells = 2;
        vm.ActiveZoneSettings.FogTurbines = 1;
        vm.ActiveZoneSettings.ContainsDisinfectStation = true;
        Assert.Equal(3, vm.ActiveZoneSettings.BulkheadKeys);
        Assert.Equal(2, vm.ActiveZoneSettings.PowerCells);
        Assert.Equal(1, vm.ActiveZoneSettings.FogTurbines);
        Assert.True(vm.ActiveZoneSettings.ContainsDisinfectStation);

        // Change Zone Lighting via Dropdown Selection
        var customLighting = new LightSettingsBlock { PersistentId = 88801, Name = "Test_Zone_Blue" };
        vm.AvailableLightingConfigs.Add(customLighting);
        vm.SelectedZoneLightingOption = customLighting;
        Assert.Equal(88801u, vm.ActiveZoneSettings.Lighting);
        Assert.Equal(88801u, vm.EditingZoneLighting);
        Assert.Equal(customLighting, vm.SelectedZoneLightingOption);

        vm.CloseEditRoomDialog();
        Assert.False(vm.IsEditingRoom);
        Assert.Null(vm.EditingRoom);
    }

    [Fact]
    public void EditRoom_ZoneSettings_DCDoorOptions_MainDoors_Secondary_Overload()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Mining Complex",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab" }
            },
            GeomorphTiles_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab" }
            }
        };

        var level = new LevelLayoutBlock
        {
            Name = "Mining Level",
            PersistentId = 10,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab"
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        // Add 2 Main Doors expanding east and west
        var eastDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right");
        vm.OpenDoorExpansionDialog(eastDoor);
        vm.IsExpansionMainDoor = true;
        Assert.True(vm.ConfirmDoorExpansion());

        var westDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Left");
        vm.OpenDoorExpansionDialog(westDoor);
        vm.IsExpansionMainDoor = true;
        Assert.True(vm.ConfirmDoorExpansion());

        // Open edit room on starting room (Zone 0)
        var startRoom = vm.GridState.PlacedRooms.First(r => r.Zone == 0);
        vm.OnRoomClicked(startRoom);
        Assert.True(vm.IsEditingRoom);
        Assert.NotNull(vm.ActiveZoneSettings);

        // Toggle DC
        vm.ActiveZoneSettings.ContainsDC = true;
        Assert.True(vm.ActiveZoneSettings.ContainsDC);

        // Available DC Main doors should contain 2 options
        Assert.True(vm.HasAvailableDcMainDoors);
        Assert.Equal(2, vm.AvailableDcMainDoors.Count);

        // Select the first Main door
        var opt1 = vm.AvailableDcMainDoors[0];
        var opt2 = vm.AvailableDcMainDoors[1];
        opt1.IsSelected = true;
        Assert.True(opt1.IsSelected);
        Assert.False(opt2.IsSelected);
        Assert.Single(vm.ActiveZoneSettings.DcOpenedMainDoorIds);
        Assert.Contains(opt1.DoorId, vm.ActiveZoneSettings.DcOpenedMainDoorIds);

        // Selecting the second Main door should deselect the first Main door (at most one main door)
        opt2.IsSelected = true;
        Assert.False(opt1.IsSelected);
        Assert.True(opt2.IsSelected);
        Assert.Single(vm.ActiveZoneSettings.DcOpenedMainDoorIds);
        Assert.Contains(opt2.DoorId, vm.ActiveZoneSettings.DcOpenedMainDoorIds);

        // Toggle Secondary and Overload
        vm.ActiveZoneSettings.DcOpensSecondary = true;
        vm.ActiveZoneSettings.DcOpensOverload = true;
        Assert.True(vm.ActiveZoneSettings.DcOpensSecondary);
        Assert.True(vm.ActiveZoneSettings.DcOpensOverload);

        // Deselect the second Main door - now 0 main doors selected
        opt2.IsSelected = false;
        Assert.False(opt1.IsSelected);
        Assert.False(opt2.IsSelected);
        Assert.Empty(vm.ActiveZoneSettings.DcOpenedMainDoorIds);

        vm.CloseEditRoomDialog();
    }
    [Fact]
    public void EditRoom_DCDoorOptions_ShowsSecondaryAndOverloadOnlyWhenSectorExists()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Mining Complex",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab" }
            },
            GeomorphTiles_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab" }
            }
        };

        var level = new LevelLayoutBlock
        {
            Name = "Mining Level",
            PersistentId = 10,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab"
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        // Initially no secondary or overload doors
        Assert.False(vm.HasSecondaryDoor);
        Assert.False(vm.HasOverloadDoor);
        Assert.False(vm.HasSectorDoors);

        // Expand east with Secondary Door
        var eastDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right");
        vm.OpenDoorExpansionDialog(eastDoor);
        vm.IsExpansionSecondaryDoor = true;
        Assert.True(vm.ConfirmDoorExpansion());

        Assert.True(vm.HasSecondaryDoor);
        Assert.False(vm.HasOverloadDoor);
        Assert.True(vm.HasSectorDoors);

        // Expand west with Overload Door
        var westDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Left");
        vm.OpenDoorExpansionDialog(westDoor);
        vm.IsExpansionOverloadDoor = true;
        Assert.True(vm.ConfirmDoorExpansion());

        Assert.True(vm.HasSecondaryDoor);
        Assert.True(vm.HasOverloadDoor);
        Assert.True(vm.HasSectorDoors);

        // Open edit room on starting room
        var startRoom = vm.GridState.PlacedRooms.First(r => r.Zone == 0);
        vm.OnRoomClicked(startRoom);
        Assert.True(vm.IsEditingRoom);
        Assert.True(vm.HasSecondaryDoor);
        Assert.True(vm.HasOverloadDoor);
        Assert.True(vm.HasSectorDoors);

        // Set DC opens secondary and overload
        vm.ActiveZoneSettings!.ContainsDC = true;
        vm.ActiveZoneSettings.DcOpensSecondary = true;
        vm.ActiveZoneSettings.DcOpensOverload = true;
        vm.CloseEditRoomDialog();

        // Delete the geomorph placed to the East (Secondary Door)
        var secondaryGeo = vm.GridState.PlacedGeomorphs.First(g => g.GridX == 1);
        var secRoom = secondaryGeo.Rooms.First();
        vm.OnRoomClicked(secRoom);
        vm.RequestDeleteGeomorph();
        vm.ConfirmLayoutDeletion();

        // Secondary sector no longer exists
        Assert.False(vm.HasSecondaryDoor);
        Assert.True(vm.HasOverloadDoor);
        Assert.True(vm.HasSectorDoors);

        // Open edit room on start room again
        vm.OnRoomClicked(startRoom);
        Assert.False(vm.HasSecondaryDoor);
        Assert.True(vm.HasOverloadDoor);
        Assert.False(vm.ActiveZoneSettings!.DcOpensSecondary);
        Assert.True(vm.ActiveZoneSettings.DcOpensOverload);
        vm.CloseEditRoomDialog();

        // Delete the geomorph placed to the West (Overload Door)
        var overloadGeo = vm.GridState.PlacedGeomorphs.First(g => g.GridX == -1);
        var ovRoom = overloadGeo.Rooms.First();
        vm.OnRoomClicked(ovRoom);
        vm.RequestDeleteGeomorph();
        vm.ConfirmLayoutDeletion();

        // Overload sector no longer exists, neither sector exists
        Assert.False(vm.HasSecondaryDoor);
        Assert.False(vm.HasOverloadDoor);
        Assert.False(vm.HasSectorDoors);

        // Open edit room on start room again
        vm.OnRoomClicked(startRoom);
        Assert.False(vm.HasSecondaryDoor);
        Assert.False(vm.HasOverloadDoor);
        Assert.False(vm.HasSectorDoors);
        Assert.False(vm.ActiveZoneSettings!.DcOpensSecondary);
        Assert.False(vm.ActiveZoneSettings.DcOpensOverload);
        vm.CloseEditRoomDialog();
    }
}
