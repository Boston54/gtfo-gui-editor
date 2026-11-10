using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using GTFO_GUIEditor.Views;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class TriggerTests
{
    [Fact]
    public void CoordinateSystem_ElevatorCentreAndTopRight_MapCorrectly()
    {
        // Elevator center is (0,0) in trigger coordinate space -> (32, 32) in canvas space
        var centerTrigger = new PlacedTriggerNode
        {
            Center = new Vector3Data(0.0, 0.0, 0.0)
        };

        Assert.Equal(32.0, centerTrigger.WorldCenterX);
        Assert.Equal(32.0, centerTrigger.WorldCenterY);

        // Top right of elevator tile is (32, 32) in trigger coordinate space -> (64, 0) in canvas space
        var topRightTrigger = new PlacedTriggerNode
        {
            Center = new Vector3Data(32.0, 32.0, 0.0)
        };

        Assert.Equal(64.0, topRightTrigger.WorldCenterX);
        Assert.Equal(0.0, topRightTrigger.WorldCenterY);
    }

    [Fact]
    public void TriggerShapes_HaveCorrectPropertiesAndDimensions()
    {
        // 1. Box Collider: Center Vector3, Size Vector3
        var box = new PlacedTriggerNode
        {
            Shape = TriggerShape.Box,
            Center = new Vector3Data(10.0, 20.0, 5.0),
            Size = new Vector3Data(15.0, 25.0, 30.0)
        };

        Assert.True(box.IsBox);
        Assert.False(box.IsSphere);
        Assert.False(box.IsCapsule);
        Assert.Equal(15.0, box.CanvasWidth);
        Assert.Equal(25.0, box.CanvasHeight);
        Assert.Equal(42.0, box.WorldCenterX); // 10 + 32
        Assert.Equal(12.0, box.WorldCenterY); // 32 - 20

        // 2. Sphere Collider: Center Vector3, Radius float
        var sphere = new PlacedTriggerNode
        {
            Shape = TriggerShape.Sphere,
            Center = new Vector3Data(0.0, 0.0, 0.0),
            Radius = 8.0
        };

        Assert.False(sphere.IsBox);
        Assert.True(sphere.IsSphere);
        Assert.False(sphere.IsCapsule);
        Assert.Equal(16.0, sphere.CanvasWidth); // Radius * 2
        Assert.Equal(16.0, sphere.CanvasHeight); // Radius * 2

        // 3. Capsule Collider: Center Vector3, Radius float, Height float (rendered as vertical orientation circle on map)
        var capsule = new PlacedTriggerNode
        {
            Shape = TriggerShape.Capsule,
            Center = new Vector3Data(0.0, 0.0, 0.0),
            Radius = 4.0,
            Height = 12.0
        };

        Assert.False(capsule.IsBox);
        Assert.False(capsule.IsSphere);
        Assert.True(capsule.IsCapsule);
        Assert.Equal(8.0, capsule.CanvasWidth); // Radius * 2
        Assert.Equal(8.0, capsule.CanvasHeight); // Radius * 2 (oriented vertically)
    }

    [Fact]
    public void ShapeChanging_UpdatesShapeAndBoundsDynamically()
    {
        var trigger = new PlacedTriggerNode
        {
            Shape = TriggerShape.Box,
            Center = new Vector3Data(0.0, 0.0, 0.0),
            Size = new Vector3Data(20.0, 30.0, 10.0),
            Radius = 6.0,
            Height = 14.0
        };

        Assert.Equal(TriggerShape.Box, trigger.Shape);
        Assert.Equal(20.0, trigger.CanvasWidth);
        Assert.Equal(30.0, trigger.CanvasHeight);

        // Switch to Sphere
        trigger.Shape = TriggerShape.Sphere;
        Assert.True(trigger.IsSphere);
        Assert.Equal(12.0, trigger.CanvasWidth); // 6.0 * 2
        Assert.Equal(12.0, trigger.CanvasHeight); // 6.0 * 2

        // Switch to Capsule (Circle on 2D layout)
        trigger.Shape = TriggerShape.Capsule;
        Assert.True(trigger.IsCapsule);
        Assert.Equal(12.0, trigger.CanvasWidth); // 6.0 * 2
        Assert.Equal(12.0, trigger.CanvasHeight); // 6.0 * 2
    }

    [Fact]
    public void PaletteCategories_ContainTriggersAndAreExtendable()
    {
        var vm = new LevelLayoutViewModel();
        Assert.NotEmpty(vm.PaletteCategories);

        var triggerCategory = vm.PaletteCategories.FirstOrDefault(c => c.Name == "Triggers");
        Assert.NotNull(triggerCategory);
        Assert.Equal(3, triggerCategory.Items.Count);

        var boxItem = triggerCategory.Items.FirstOrDefault(i => i.TriggerShape == TriggerShape.Box);
        var sphereItem = triggerCategory.Items.FirstOrDefault(i => i.TriggerShape == TriggerShape.Sphere);
        var capsuleItem = triggerCategory.Items.FirstOrDefault(i => i.TriggerShape == TriggerShape.Capsule);

        Assert.NotNull(boxItem);
        Assert.NotNull(sphereItem);
        Assert.NotNull(capsuleItem);

        // Test toggle palette
        bool initial = vm.IsPaletteExpanded;
        vm.TogglePalette();
        Assert.NotEqual(initial, vm.IsPaletteExpanded);
        vm.TogglePalette();
        Assert.Equal(initial, vm.IsPaletteExpanded);
    }

    [Fact]
    public void GridState_GetHeightAt_ReturnsAltitudeOffsetWhenTilePresent()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Elevator_Alleyway", 0, 0);

        // Elevator at (0,0) with cell size 64x64 spans world (0,0) to (64,64)
        double height = gridState.GetHeightAt(32.0, 32.0);
        Assert.Equal(0.0, height);

        // Empty area with no tile returns 0.0
        double emptyHeight = gridState.GetHeightAt(500.0, 500.0);
        Assert.Equal(0.0, emptyHeight);
    }

    [Fact]
    public void AddTrigger_AndDialogOperations_WorkCorrectly()
    {
        var vm = new LevelLayoutViewModel();
        var trigger = vm.AddTrigger(TriggerShape.Box, 10.0, -15.0, 6.0);

        Assert.Single(vm.GridState.PlacedTriggers);
        Assert.Equal(10.0, trigger.Center.X);
        Assert.Equal(-15.0, trigger.Center.Y);
        Assert.Equal(6.0, trigger.Center.Z);

        // Open dialog
        vm.OpenEditTriggerDialog(trigger);
        Assert.True(vm.IsEditingTrigger);
        Assert.Equal(trigger, vm.EditingTrigger);

        // Edit shape via dialog
        vm.EditingTrigger!.Shape = TriggerShape.Capsule;
        Assert.Equal(TriggerShape.Capsule, trigger.Shape);

        // Close dialog
        vm.CloseTriggerConfigDialog();
        Assert.False(vm.IsEditingTrigger);
        Assert.Null(vm.EditingTrigger);

        // Delete trigger via dialog
        vm.OpenEditTriggerDialog(trigger);
        vm.RequestDeleteEditingTrigger();
        Assert.Empty(vm.GridState.PlacedTriggers);
        Assert.False(vm.IsEditingTrigger);
    }

    [Fact]
    public void StateExportAndImport_PreservesPlacedTriggers()
    {
        var gridState = new LevelLayoutGridState();
        var trigger1 = new PlacedTriggerNode
        {
            Name = "Trigger Alpha",
            Shape = TriggerShape.Box,
            Center = new Vector3Data(12.5, 34.2, 8.0),
            Size = new Vector3Data(20.0, 15.0, 10.0)
        };
        var trigger2 = new PlacedTriggerNode
        {
            Name = "Trigger Beta",
            Shape = TriggerShape.Sphere,
            Center = new Vector3Data(-5.0, 10.0, 3.0),
            Radius = 7.5
        };
        var trigger3 = new PlacedTriggerNode
        {
            Name = "Trigger Gamma",
            Shape = TriggerShape.Capsule,
            Center = new Vector3Data(0.0, 0.0, 0.0),
            Radius = 4.0,
            Height = 16.0
        };

        gridState.PlacedTriggers.Add(trigger1);
        gridState.PlacedTriggers.Add(trigger2);
        gridState.PlacedTriggers.Add(trigger3);

        var exported = gridState.ExportStateData();
        Assert.Equal(3, exported.PlacedTriggers.Count);

        var newGridState = new LevelLayoutGridState();
        newGridState.ImportStateData(exported);

        Assert.Equal(3, newGridState.PlacedTriggers.Count);

        var imported1 = newGridState.PlacedTriggers[0];
        Assert.Equal("Trigger Alpha", imported1.Name);
        Assert.Equal(TriggerShape.Box, imported1.Shape);
        Assert.Equal(12.5, imported1.Center.X);
        Assert.Equal(34.2, imported1.Center.Y);
        Assert.Equal(8.0, imported1.Center.Z);
        Assert.Equal(20.0, imported1.Size.X);

        var imported2 = newGridState.PlacedTriggers[1];
        Assert.Equal("Trigger Beta", imported2.Name);
        Assert.Equal(TriggerShape.Sphere, imported2.Shape);
        Assert.Equal(7.5, imported2.Radius);

        var imported3 = newGridState.PlacedTriggers[2];
        Assert.Equal("Trigger Gamma", imported3.Name);
        Assert.Equal(TriggerShape.Capsule, imported3.Shape);
        Assert.Equal(4.0, imported3.Radius);
        Assert.Equal(16.0, imported3.Height);
    }

    [Fact]
    public void TriggerBrushesAndTooltips_DisplayCoordinatesAndSizeCorrectly()
    {
        var box = new PlacedTriggerNode
        {
            Shape = TriggerShape.Box,
            Center = new Vector3Data(10.0, -20.0, 5.0),
            Size = new Vector3Data(15.0, 25.0, 30.0)
        };

        Assert.Contains("#40FFA726", box.FillBrush);
        Assert.Contains("#FFFFA726", box.StrokeBrush);
        Assert.Contains("Position: (10.00, -20.00, 5.00)", box.TooltipText);
        Assert.Contains("Size: X=15.0, Y=25.0, Z=30.0", box.TooltipText);

        var sphere = new PlacedTriggerNode
        {
            Shape = TriggerShape.Sphere,
            Center = new Vector3Data(0.0, 12.5, 3.0),
            Radius = 6.5
        };

        Assert.Contains("#4081C784", sphere.FillBrush);
        Assert.Contains("#FF81C784", sphere.StrokeBrush);
        Assert.Contains("Position: (0.00, 12.50, 3.00)", sphere.TooltipText);
        Assert.Contains("Radius: 6.5", sphere.TooltipText);

        var capsule = new PlacedTriggerNode
        {
            Shape = TriggerShape.Capsule,
            Center = new Vector3Data(5.0, 5.0, 1.0),
            Radius = 4.0,
            Height = 12.0
        };

        Assert.Contains("#40BA68C8", capsule.FillBrush);
        Assert.Contains("#FFBA68C8", capsule.StrokeBrush);
        Assert.Contains("Position: (5.00, 5.00, 1.00)", capsule.TooltipText);
        Assert.Contains("Radius: 4.0, Height: 12.0", capsule.TooltipText);
    }

    [Fact]
    public void DragPreviewTrigger_UpdatesCorrectlyInGridState()
    {
        var gridState = new LevelLayoutGridState();
        Assert.False(gridState.IsDraggingTriggerPreview);
        Assert.NotNull(gridState.DragPreviewTrigger);

        gridState.DragPreviewTrigger.Shape = TriggerShape.Capsule;
        gridState.DragPreviewTrigger.Center.X = 15.0;
        gridState.DragPreviewTrigger.Center.Y = 25.0;
        gridState.DragPreviewTrigger.Center.Z = 6.0;
        gridState.DragPreviewTrigger.NotifyTransformProperties();
        gridState.IsDraggingTriggerPreview = true;

        Assert.True(gridState.IsDraggingTriggerPreview);
        Assert.Equal(47.0, gridState.DragPreviewTrigger.WorldCenterX); // 15 + 32
        Assert.Equal(7.0, gridState.DragPreviewTrigger.WorldCenterY); // 32 - 25
    }

    [Fact]
    public void HoverCoordinates_UpdateAndClearCorrectly()
    {
        var gridState = new LevelLayoutGridState();
        Assert.False(gridState.HasHoverCoordinates);

        gridState.UpdateHoverCoordinates(12.34, -5.67, 6.0);
        Assert.True(gridState.HasHoverCoordinates);
        Assert.Contains("X: 12.34", gridState.HoverCoordinatesText);
        Assert.Contains("Y: -5.67", gridState.HoverCoordinatesText);
        Assert.Contains("Z: 6.00", gridState.HoverCoordinatesText);

        gridState.ClearHoverCoordinates();
        Assert.False(gridState.HasHoverCoordinates);
    }

    [Fact]
    public void Triangle2D_BarycentricInterpolation_CalculatesExactHeights()
    {
        // Sloped triangle from (0,0, 0m) to (10,0, 0m) to (0,10, 10m)
        var triangle = new Triangle2D(0.0, 0.0, 10.0, 0.0, 0.0, 10.0, 0.0, 0.0, 10.0);

        // At vertex 0 (0,0) -> height 0.0
        Assert.Equal(0.0, triangle.GetHeightAt(0.0, 0.0), 3);

        // At vertex 2 (0,10) -> height 10.0
        Assert.Equal(10.0, triangle.GetHeightAt(0.0, 10.0), 3);

        // At midpoint along slope (0,5) -> height 5.0
        Assert.Equal(5.0, triangle.GetHeightAt(0.0, 5.0), 3);

        // At centroid (3.333, 3.333) -> height 3.333
        Assert.Equal(10.0 / 3.0, triangle.GetHeightAt(10.0 / 3.0, 10.0 / 3.0), 3);
    }

    [Fact]
    public void Extract2DTriangles_And_BuildHeightLayers_CreatesMultiHeightLayers()
    {
        // Mesh with two flat platforms at different heights: floor at 0m, catwalk at 8m
        var mesh = new MeshData
        {
            Vertices = new List<VertexPosition>
            {
                // Floor triangle at 0m
                new(0.0, 0.0, 0.0),
                new(10.0, 0.0, 0.0),
                new(0.0, 0.0, 10.0),
                // Catwalk triangle at 8m (Y=8.0 in Unity coordinates)
                new(20.0, 8.0, 0.0),
                new(30.0, 8.0, 0.0),
                new(20.0, 8.0, 10.0)
            },
            Indices = new List<int> { 0, 1, 2, 3, 4, 5 }
        };

        var (triangles, bounds, area) = GTFO_GUIEditor.Services.RoomSvgService.Extract2DTriangles(mesh, 0.0);
        Assert.Equal(2, triangles.Count);
        Assert.Equal(0.0, triangles[0].AvgZ);
        Assert.Equal(8.0, triangles[1].AvgZ);

        // Build height layers
        var layers = GTFO_GUIEditor.Services.RoomSvgService.BuildHeightLayers(triangles, altitudeOffset: 6.0);
        Assert.Equal(2, layers.Count);
        Assert.Equal(0.0, layers[0].LocalHeight);
        Assert.Equal(8.0, layers[1].LocalHeight);
        Assert.NotNull(layers[0].FillBrush);
        Assert.NotNull(layers[1].FillBrush);
    }

    [Fact]
    public void PaletteItems_DoNotContainEmojis()
    {
        var vm = new LevelLayoutViewModel();
        var triggerCategory = vm.PaletteCategories.FirstOrDefault(c => c.Name == "Triggers");
        Assert.NotNull(triggerCategory);

        foreach (var item in triggerCategory.Items)
        {
            Assert.True(string.IsNullOrEmpty(item.IconText));
        }
    }

    [Fact]
    public void GridState_GetHeightAt_ReturnsPreciseTriangleHeightInsideRoom()
    {
        var gridState = new LevelLayoutGridState();
        var room = new PlacedRoomNode
        {
            WorldX = 0,
            WorldY = 0,
            BoundsMinX = 0,
            BoundsMinY = 0,
            BoundsMaxX = 50,
            BoundsMaxY = 50,
            Area = 2500,
            Triangles2D = new List<Triangle2D>
            {
                // Triangle A: flat floor at local height 2.0m spanning (0,0)-(20,0)-(0,20)
                new(0.0, 0.0, 20.0, 0.0, 0.0, 20.0, 2.0, 2.0, 2.0),
                // Triangle B: sloped ramp from 4.0m to 10.0m spanning (20,20)-(40,20)-(20,40)
                new(20.0, 20.0, 40.0, 20.0, 20.0, 40.0, 4.0, 4.0, 10.0)
            }
        };

        gridState.PlacedRooms.Add(room);

        // Point inside Triangle A -> height 2.0
        double heightA = gridState.GetHeightAt(5.0, 5.0);
        Assert.Equal(2.0, heightA, 2);

        // Point inside Triangle B (at base) -> height 4.0
        double heightBBase = gridState.GetHeightAt(25.0, 21.0);
        Assert.Equal(4.0 + (1.0 / 20.0) * 6.0, heightBBase, 2);

        // Point inside Triangle B (near top) -> height ~10.0
        double heightBTop = gridState.GetHeightAt(20.5, 38.0);
        Assert.True(heightBTop > 8.5);
    }

    [Fact]
    public void IsPointerInTriggerMiddle_CorrectlyDistinguishesMiddleAndEdgeRegions()
    {
        // Box Trigger (20 x 20)
        double boxW = 20.0, boxH = 20.0;
        // Center at (10, 10)
        Assert.True(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(10.0, 10.0), boxW,
            boxH, TriggerShape.Box));
        Assert.True(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(12.0, 12.0), boxW,
            boxH, TriggerShape.Box));
        // Outer edge at (1.0, 10.0) or (19.0, 19.0)
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(1.0, 10.0), boxW,
            boxH, TriggerShape.Box));
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(19.0, 10.0), boxW,
            boxH, TriggerShape.Box));
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(10.0, 1.0), boxW,
            boxH, TriggerShape.Box));
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(10.0, 19.0), boxW,
            boxH, TriggerShape.Box));

        // Sphere / Capsule Trigger (Circle radius 10, width 20, height 20)
        double circW = 20.0, circH = 20.0;
        Assert.True(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(10.0, 10.0), circW,
            circH, TriggerShape.Sphere));
        Assert.True(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(12.0, 12.0), circW,
            circH, TriggerShape.Sphere));
        Assert.True(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(10.0, 10.0), circW,
            circH, TriggerShape.Capsule));
        // Outer radius edge
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(2.0, 10.0), circW,
            circH, TriggerShape.Sphere));
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(18.0, 10.0),
            circW, circH, TriggerShape.Sphere));
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(2.0, 10.0), circW,
            circH, TriggerShape.Capsule));
        Assert.False(GTFO_GUIEditor.Views.LevelLayoutView.IsPointerInTriggerMiddle(new Avalonia.Point(18.0, 10.0),
            circW, circH, TriggerShape.Capsule));
    }

    [Fact]
    public void AllTriggerTypes_SupportIsToggle()
    {
        var box = new PlacedTriggerNode { Shape = TriggerShape.Box, Type = TriggerType.Collision, IsToggle = true };
        var sphere = new PlacedTriggerNode
            { Shape = TriggerShape.Sphere, Type = TriggerType.Interaction, IsToggle = true };
        var capsule = new PlacedTriggerNode
            { Shape = TriggerShape.Capsule, Type = TriggerType.LookAt, IsToggle = false };

        Assert.True(box.IsToggle);
        Assert.True(sphere.IsToggle);
        Assert.False(capsule.IsToggle);

        capsule.IsToggle = true;
        Assert.True(capsule.IsToggle);
    }

    [Fact]
    public void InteractionTriggers_PropertiesAndCarryItemTransform_WorkCorrectly()
    {
        var trigger = new PlacedTriggerNode
        {
            Type = TriggerType.Interaction,
            IsToggle = true,
            InteractionText = "Insert Power Cell",
            CarryItemInsertType = CarryItemInsertType.PowerCell,
            RemoveItemOnInsert = true,
            ItemStateAfterInsert = ItemStateAfterInsert.Inserted_Visible_NotInteractable
        };

        trigger.CarryItemTransform.Position = new Vector3Data(1.5, 2.5, 3.5);
        trigger.CarryItemTransform.Rotation = new Vector3Data(0.0, 90.0, 0.0);
        trigger.CarryItemTransform.Scale = new Vector3Data(1.2, 1.2, 1.2);

        Assert.True(trigger.IsInteraction);
        Assert.False(trigger.IsCollision);
        Assert.False(trigger.IsLookAt);
        Assert.True(trigger.IsToggle);
        Assert.Equal("Insert Power Cell", trigger.InteractionText);
        Assert.Equal(CarryItemInsertType.PowerCell, trigger.CarryItemInsertType);
        Assert.True(trigger.HasCarryItem);
        Assert.True(trigger.RemoveItemOnInsert);
        Assert.Equal(ItemStateAfterInsert.Inserted_Visible_NotInteractable, trigger.ItemStateAfterInsert);

        Assert.Equal(1.5, trigger.CarryItemTransform.Position.X);
        Assert.Equal(2.5, trigger.CarryItemTransform.Position.Y);
        Assert.Equal(3.5, trigger.CarryItemTransform.Position.Z);
        Assert.Equal(90.0, trigger.CarryItemTransform.Rotation.Y);
        Assert.Equal(1.2, trigger.CarryItemTransform.Scale.X);

        // Test None carry item insert type
        trigger.CarryItemInsertType = CarryItemInsertType.None;
        Assert.False(trigger.HasCarryItem);

        // Test other insert types and states
        trigger.CarryItemInsertType = CarryItemInsertType.SmallHSU;
        Assert.True(trigger.HasCarryItem);
        trigger.ItemStateAfterInsert = ItemStateAfterInsert.Inserted_Disabled;
        Assert.Equal(ItemStateAfterInsert.Inserted_Disabled, trigger.ItemStateAfterInsert);

        trigger.CarryItemInsertType = CarryItemInsertType.Crate;
        Assert.True(trigger.HasCarryItem);
        trigger.ItemStateAfterInsert = ItemStateAfterInsert.Default;
        Assert.Equal(ItemStateAfterInsert.Default, trigger.ItemStateAfterInsert);
    }

    [Fact]
    public void StateExportAndImport_PreservesInteractionTriggersAndCarryItemTransform()
    {
        var gridState = new LevelLayoutGridState();
        var trigger = new PlacedTriggerNode
        {
            Name = "Cell Station Trigger",
            Shape = TriggerShape.Box,
            Type = TriggerType.Interaction,
            IsToggle = true,
            Center = new Vector3Data(10.0, 20.0, 5.0),
            Size = new Vector3Data(8.0, 8.0, 8.0),
            InteractionText = "Insert Generator Cell",
            CarryItemInsertType = CarryItemInsertType.PowerCell,
            RemoveItemOnInsert = true,
            ItemStateAfterInsert = ItemStateAfterInsert.Inserted_Visible_NotInteractable,
            CarryItemTransform = new CarryItemTransformData
            {
                Position = new Vector3Data(0.5, 1.0, 0.2),
                Rotation = new Vector3Data(0.0, 45.0, 0.0),
                Scale = new Vector3Data(1.0, 1.0, 1.0)
            }
        };

        gridState.PlacedTriggers.Add(trigger);

        var exported = gridState.ExportStateData();
        Assert.Single(exported.PlacedTriggers);

        var newGridState = new LevelLayoutGridState();
        newGridState.ImportStateData(exported);

        Assert.Single(newGridState.PlacedTriggers);
        var imported = newGridState.PlacedTriggers[0];

        Assert.Equal("Cell Station Trigger", imported.Name);
        Assert.Equal(TriggerShape.Box, imported.Shape);
        Assert.Equal(TriggerType.Interaction, imported.Type);
        Assert.True(imported.IsToggle);
        Assert.True(imported.IsInteraction);
        Assert.Equal("Insert Generator Cell", imported.InteractionText);
        Assert.Equal(CarryItemInsertType.PowerCell, imported.CarryItemInsertType);
        Assert.True(imported.HasCarryItem);
        Assert.True(imported.RemoveItemOnInsert);
        Assert.Equal(ItemStateAfterInsert.Inserted_Visible_NotInteractable, imported.ItemStateAfterInsert);
        Assert.NotNull(imported.CarryItemTransform);
        Assert.Equal(0.5, imported.CarryItemTransform.Position.X);
        Assert.Equal(1.0, imported.CarryItemTransform.Position.Y);
        Assert.Equal(0.2, imported.CarryItemTransform.Position.Z);
        Assert.Equal(45.0, imported.CarryItemTransform.Rotation.Y);
        Assert.Equal(1.0, imported.CarryItemTransform.Scale.X);
    }

    [Fact]
    public void ViewModel_TriggerEnumsAndCollections_ArePopulated()
    {
        var vm = new LevelLayoutViewModel();
        Assert.NotEmpty(vm.AvailableTriggerShapes);
        Assert.NotEmpty(vm.AvailableTriggerTypes);
        Assert.NotEmpty(vm.AvailableCarryItemInsertTypes);
        Assert.NotEmpty(vm.AvailableItemStatesAfterInsert);

        Assert.Contains(TriggerType.Collision, vm.AvailableTriggerTypes);
        Assert.Contains(TriggerType.Interaction, vm.AvailableTriggerTypes);
        Assert.Contains(TriggerType.LookAt, vm.AvailableTriggerTypes);

        Assert.Contains(CarryItemInsertType.None, vm.AvailableCarryItemInsertTypes);
        Assert.Contains(CarryItemInsertType.PowerCell, vm.AvailableCarryItemInsertTypes);
        Assert.Contains(CarryItemInsertType.SmallHSU, vm.AvailableCarryItemInsertTypes);
        Assert.Contains(CarryItemInsertType.Crate, vm.AvailableCarryItemInsertTypes);

        Assert.Contains(ItemStateAfterInsert.Default, vm.AvailableItemStatesAfterInsert);
        Assert.Contains(ItemStateAfterInsert.Inserted_Visible_NotInteractable, vm.AvailableItemStatesAfterInsert);
        Assert.Contains(ItemStateAfterInsert.Inserted_Disabled, vm.AvailableItemStatesAfterInsert);
    }

    [Fact]
    public void AllTriggerTypes_SupportWardenEvents_AndStatePreservation()
    {
        var trigger = new PlacedTriggerNode
        {
            Name = "Event Trigger",
            Shape = TriggerShape.Box,
            Type = TriggerType.Collision,
            IsToggle = false,
            Center = new Vector3Data(0.0, 0.0, 0.0)
        };

        Assert.NotNull(trigger.Events);
        Assert.Empty(trigger.Events);

        // Add events
        var event1 = new WardenEvent
        {
            Type = EventType.SpawnEnemyWave,
            Delay = 2.5f,
            Enabled = true
        };
        var event2 = new WardenEvent
        {
            Type = EventType.PlaySound,
            Delay = 0.0f,
            SoundID = 12345
        };

        trigger.Events.Add(event1);
        trigger.Events.Add(event2);

        Assert.Equal(2, trigger.Events.Count);

        // Test Clone
        var clone = trigger.Clone();
        Assert.Equal(2, clone.Events.Count);
        Assert.Equal(EventType.SpawnEnemyWave, clone.Events[0].Type);
        Assert.Equal(2.5f, clone.Events[0].Delay);
        Assert.Equal(EventType.PlaySound, clone.Events[1].Type);
        Assert.Equal(12345u, clone.Events[1].SoundID);

        // Ensure deep copy
        clone.Events[0].Delay = 5.0f;
        Assert.Equal(2.5f, trigger.Events[0].Delay);

        // Test GridState Export and Import with Events
        var gridState = new LevelLayoutGridState();
        gridState.PlacedTriggers.Add(trigger);

        var exported = gridState.ExportStateData();
        Assert.Single(exported.PlacedTriggers);
        Assert.NotNull(exported.PlacedTriggers[0].Events);
        Assert.Equal(2, exported.PlacedTriggers[0].Events!.Count);

        var newGridState = new LevelLayoutGridState();
        newGridState.ImportStateData(exported);

        Assert.Single(newGridState.PlacedTriggers);
        var importedTrigger = newGridState.PlacedTriggers[0];
        Assert.Equal(2, importedTrigger.Events.Count);
        Assert.Equal(EventType.SpawnEnemyWave, importedTrigger.Events[0].Type);
        Assert.Equal(2.5f, importedTrigger.Events[0].Delay);
        Assert.Equal(EventType.PlaySound, importedTrigger.Events[1].Type);
    }

    [Fact]
    public void ViewModel_TriggerEvents_AddAndRemoveCommandsWork()
    {
        var vm = new LevelLayoutViewModel();
        var trigger = vm.AddTrigger(TriggerShape.Sphere, 0, 0, 0);

        vm.OpenEditTriggerDialog(trigger);
        Assert.True(vm.IsEditingTrigger);
        Assert.Same(trigger, vm.EditingTrigger);

        Assert.Empty(trigger.Events);

        // Add event via ViewModel command
        vm.AddEditingTriggerEvent();
        Assert.Single(trigger.Events);

        var addedEvent = trigger.Events[0];
        Assert.NotNull(addedEvent);

        // Remove event via global RemoveWardenEventCommand handler
        vm.RemoveWardenEvent(addedEvent);
        Assert.Empty(trigger.Events);
    }

    [Fact]
    public void AraLevelLayoutService_ExportAndImport_PreservesAllTriggerTypesAndEvents()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "AraTriggerTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var level = mainVm.LevelLayout.Levels[0];
            level.PersistentId = 12345;
            level.Name = "TestExpedition";

            var exp = mainVm.Rundown.SelectedRundown?.Expeditions.FirstOrDefault();
            if (exp != null)
            {
                exp.LevelLayoutData = level.PersistentId;
                exp.Descriptive.Prefix = "A1";
                exp.Descriptive.PublicName = "Test Expedition";
            }

            var gridState = level.GridState;
            gridState.PlacedTriggers.Clear();

            // 1. Collision Box Trigger with event
            var boxTrigger = new PlacedTriggerNode
            {
                Name = "Zone0_BoxTrigger",
                Shape = TriggerShape.Box,
                Type = TriggerType.Collision,
                IsToggle = true,
                Center = new Vector3Data(10.5, 20.0, 3.25),
                Size = new Vector3Data(8.0, 12.0, 6.0)
            };
            boxTrigger.Events.Add(new WardenEvent
            {
                Type = EventType.SpawnEnemyWave,
                Delay = 1.5f,
                Enabled = true
            });
            gridState.PlacedTriggers.Add(boxTrigger);

            // 2. LookAt Sphere Trigger
            var sphereTrigger = new PlacedTriggerNode
            {
                Name = "Zone0_SphereTrigger",
                Shape = TriggerShape.Sphere,
                Type = TriggerType.LookAt,
                IsToggle = false,
                Center = new Vector3Data(-15.0, 5.5, 1.0),
                Radius = 4.5
            };
            gridState.PlacedTriggers.Add(sphereTrigger);

            // 3. Interaction Capsule Trigger with carry item transform and text
            var capsuleTrigger = new PlacedTriggerNode
            {
                Name = "Zone0_CapsuleTrigger",
                Shape = TriggerShape.Capsule,
                Type = TriggerType.Interaction,
                IsToggle = false,
                Center = new Vector3Data(0.0, -10.0, 2.0),
                Radius = 3.0,
                Height = 8.0,
                InteractionText = "Insert Cell Here",
                CarryItemInsertType = CarryItemInsertType.PowerCell,
                RemoveItemOnInsert = true,
                ItemStateAfterInsert = ItemStateAfterInsert.Inserted_Visible_NotInteractable,
                CarryItemTransform = new CarryItemTransformData
                {
                    Position = new Vector3Data(0.5, 1.2, 0.3),
                    Rotation = new Vector3Data(0.0, 90.0, 0.0),
                    Scale = new Vector3Data(1.0, 1.0, 1.0)
                }
            };
            capsuleTrigger.Events.Add(new WardenEvent
            {
                Type = EventType.OpenSecurityDoor,
                Zone = 1
            });
            gridState.PlacedTriggers.Add(capsuleTrigger);

            // Export ARA
            var exportContext = new ExportContext(mainVm, tempDir);
            exportContext.LayoutIds[level] = level.PersistentId;
            var araJson = AraLevelLayoutService.ExportAraLevelLayout(exportContext, level, exp);

            Assert.NotNull(araJson);
            Assert.Equal(12345u, araJson["MainLevelLayout"]?.GetValue<uint>());
            Assert.NotNull(araJson["Elevator"]);
            Assert.Equal("WE_ARA_{0}_", araJson["AutoWorldEventObjectPrefix"]?.ToString());
            Assert.False(araJson["SetupWorldEventObjectOnAllTerminals"]?.GetValue<bool>());

            var zonesArr = araJson["Zones"] as JsonArray;
            Assert.NotNull(zonesArr);
            Assert.NotEmpty(zonesArr);

            var zone0 = zonesArr.FirstOrDefault(z => z?["LocalIndex"]?.ToString() == "Zone_0") as JsonObject;
            Assert.NotNull(zone0);
            Assert.Equal("Reality", zone0["DimensionIndex"]?.ToString());
            Assert.Equal("MainLayer", zone0["Layer"]?.ToString());

            var weArray = zone0["WorldEventObjects"] as JsonArray;
            Assert.NotNull(weArray);
            Assert.Equal(3, weArray.Count);

            // Verify file exists on disk
            string expectedFile = Path.Combine(tempDir, "Custom", "AdditionalRundownAdvancements", "LevelLayout",
                "A1.json");
            Assert.True(File.Exists(expectedFile));

            // Test EventsOnTrigger export for Zone 0
            var eventsOnTrigger =
                AraLevelLayoutService.ExportEventsOnTriggerForZone(exportContext, gridState, SectorType.Main, 0);
            Assert.Equal(2, eventsOnTrigger.Count);
            Assert.Contains(eventsOnTrigger,
                e => e?["WorldEventTriggerObjectFilter"]?.ToString() == "Zone0_BoxTrigger");
            Assert.Contains(eventsOnTrigger,
                e => e?["WorldEventTriggerObjectFilter"]?.ToString() == "Zone0_CapsuleTrigger");

            // Now Import into a fresh level
            var importVm = new MainWindowViewModel();
            var importedLevel = new LevelLayoutBlock { PersistentId = 12345, Name = "ImportedTest" };
            var importContext = new ImportContext(importVm, tempDir);

            // Prepare zoneEventsByFilter from exported eventsOnTrigger
            var zoneEventsMap = new Dictionary<string, List<WardenEvent>>(StringComparer.OrdinalIgnoreCase);
            var parsedWardenEvents = new ObservableCollection<WardenEvent>();
            LevelLayoutDataBlockImporter.ImportWardenEvents(eventsOnTrigger, parsedWardenEvents, importContext);
            for (int i = 0; i < eventsOnTrigger.Count && i < parsedWardenEvents.Count; i++)
            {
                string f = eventsOnTrigger[i]?["WorldEventTriggerObjectFilter"]?.ToString() ?? string.Empty;
                if (!zoneEventsMap.TryGetValue(f, out var list))
                {
                    list = new List<WardenEvent>();
                    zoneEventsMap[f] = list;
                }

                list.Add(parsedWardenEvents[i]);
            }

            AraLevelLayoutService.ImportAraLevelLayout(importContext, importedLevel, exp, zoneEventsMap);

            Assert.Equal(3, importedLevel.GridState.PlacedTriggers.Count);

            // 1. Box trigger
            var impBox = importedLevel.GridState.PlacedTriggers.FirstOrDefault(t => t.Name == "Zone0_BoxTrigger");
            Assert.NotNull(impBox);
            Assert.Equal(TriggerShape.Box, impBox.Shape);
            Assert.Equal(TriggerType.Collision, impBox.Type);
            Assert.True(impBox.IsToggle);
            Assert.Equal(10.5, impBox.Center.X);
            Assert.Equal(20.0, impBox.Center.Y);
            Assert.Equal(3.25, impBox.Center.Z);
            Assert.Equal(8.0, impBox.Size.X);
            Assert.Equal(12.0, impBox.Size.Y);
            Assert.Equal(6.0, impBox.Size.Z);
            Assert.Single(impBox.Events);
            Assert.Equal(EventType.SpawnEnemyWave, impBox.Events[0].Type);

            // 2. Sphere trigger
            var impSphere = importedLevel.GridState.PlacedTriggers.FirstOrDefault(t => t.Name == "Zone0_SphereTrigger");
            Assert.NotNull(impSphere);
            Assert.Equal(TriggerShape.Sphere, impSphere.Shape);
            Assert.Equal(TriggerType.LookAt, impSphere.Type);
            Assert.False(impSphere.IsToggle);
            Assert.Equal(-15.0, impSphere.Center.X);
            Assert.Equal(5.5, impSphere.Center.Y);
            Assert.Equal(1.0, impSphere.Center.Z);
            Assert.Equal(4.5, impSphere.Radius);

            // 3. Capsule trigger
            var impCapsule =
                importedLevel.GridState.PlacedTriggers.FirstOrDefault(t => t.Name == "Zone0_CapsuleTrigger");
            Assert.NotNull(impCapsule);
            Assert.Equal(TriggerShape.Capsule, impCapsule.Shape);
            Assert.Equal(TriggerType.Interaction, impCapsule.Type);
            Assert.Equal(3.0, impCapsule.Radius);
            Assert.Equal(8.0, impCapsule.Height);
            Assert.Equal(CarryItemInsertType.PowerCell, impCapsule.CarryItemInsertType);
            Assert.True(impCapsule.RemoveItemOnInsert);
            Assert.Equal(ItemStateAfterInsert.Inserted_Visible_NotInteractable, impCapsule.ItemStateAfterInsert);
            Assert.NotNull(impCapsule.CarryItemTransform);
            Assert.Equal(0.5, impCapsule.CarryItemTransform.Position.X);
            Assert.Equal(1.2, impCapsule.CarryItemTransform.Position.Y);
            Assert.Equal(0.3, impCapsule.CarryItemTransform.Position.Z);
            Assert.Equal(90.0, impCapsule.CarryItemTransform.Rotation.Y);
            Assert.Single(impCapsule.Events);
            Assert.Equal(EventType.OpenSecurityDoor, impCapsule.Events[0].Type);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    [Fact]
    public void FullDatablockExportAndImport_WithAraLevelLayout_PreservesTriggersAndEvents()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "FullAraExportImportTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var vm = new MainWindowViewModel();
            vm.Modded.AdditionalRundownAdvancements = true;
            var level = vm.LevelLayout.Levels[0];
            level.PersistentId = 9991;
            level.Name = "AraFullTest";

            var rundown = vm.Rundown.SelectedRundown!;
            rundown.TierA[0].Descriptive.Prefix = "A1";
            rundown.TierA[0].Descriptive.PublicName = level.Name;
            rundown.TierA[0].LevelLayoutData = level.PersistentId;

            // Add triggers
            var trigger = new PlacedTriggerNode
            {
                Name = "EndGame_LookAtTrigger",
                Shape = TriggerShape.Sphere,
                Type = TriggerType.LookAt,
                IsToggle = true,
                Center = new Vector3Data(12.0, -8.0, 4.0),
                Radius = 5.0
            };
            trigger.Events.Add(new WardenEvent
            {
                Type = EventType.SetSuccessScreen,
                Delay = 0.5f
            });
            level.GridState.PlacedTriggers.Add(trigger);

            // Export
            var exportResult = DatablockManager.Instance.ExportDatablocks(vm, tempDir);
            Assert.True(exportResult.Success);

            // Verify ARA file was written
            string araFile = Path.Combine(tempDir, "Custom", "AdditionalRundownAdvancements", "LevelLayout", "A1.json");
            Assert.True(File.Exists(araFile));

            // Import into a fresh VM
            var importedVm = new MainWindowViewModel();
            var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, tempDir);
            Assert.True(importResult.Success);

            Assert.NotEmpty(importedVm.LevelLayout.Levels);
            var impLevel = importedVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 9991);
            Assert.NotNull(impLevel);

            Assert.Single(impLevel.GridState.PlacedTriggers);
            var impTrigger = impLevel.GridState.PlacedTriggers[0];
            Assert.Equal("EndGame_LookAtTrigger", impTrigger.Name);
            Assert.Equal(TriggerShape.Sphere, impTrigger.Shape);
            Assert.Equal(TriggerType.LookAt, impTrigger.Type);
            Assert.True(impTrigger.IsToggle);
            Assert.Equal(12.0, impTrigger.Center.X);
            Assert.Equal(-8.0, impTrigger.Center.Y);
            Assert.Equal(4.0, impTrigger.Center.Z);
            Assert.Equal(5.0, impTrigger.Radius);
            Assert.Single(impTrigger.Events);
            Assert.Equal(EventType.SetSuccessScreen, impTrigger.Events[0].Type);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    [Fact]
    public void GetZoneForPoint_MatchesDirectNavmeshOrClosestRoomByDistance()
    {
        var gridState = new LevelLayoutGridState();

        var room1 = new PlacedRoomNode
        {
            RoomNumber = 1,
            Sector = SectorType.Main,
            Zone = 0,
            AreaIndex = 0,
            BoundsMinX = 0,
            BoundsMaxX = 64,
            BoundsMinY = 0,
            BoundsMaxY = 64
        };
        room1.Triangles2D.Add(new Triangle2D(0, 0, 64, 0, 64, 64, 0, 0, 0));
        room1.Triangles2D.Add(new Triangle2D(0, 0, 64, 64, 0, 64, 0, 0, 0));
        gridState.PlacedRooms.Add(room1);

        var room2 = new PlacedRoomNode
        {
            RoomNumber = 2,
            Sector = SectorType.Secondary,
            Zone = 1,
            AreaIndex = 2,
            BoundsMinX = 100,
            BoundsMaxX = 164,
            BoundsMinY = 0,
            BoundsMaxY = 64
        };
        room2.Triangles2D.Add(new Triangle2D(100, 0, 164, 0, 164, 64, 10, 10, 10));
        room2.Triangles2D.Add(new Triangle2D(100, 0, 164, 64, 100, 64, 10, 10, 10));
        gridState.PlacedRooms.Add(room2);

        // Point inside Room 1 (elev center is (0,0) in item space -> (32, 32) in canvas space)
        var zone1 = gridState.GetZoneForPoint(0, 0);
        Assert.Equal(SectorType.Main, zone1.Sector);
        Assert.Equal(0, zone1.Zone);
        Assert.Equal(0, zone1.AreaIndex);

        // Point inside Room 2 (canvas space X = 132, item space X = 100)
        var zone2 = gridState.GetZoneForPoint(100, 0);
        Assert.Equal(SectorType.Secondary, zone2.Sector);
        Assert.Equal(1, zone2.Zone);
        Assert.Equal(2, zone2.AreaIndex);

        // Point far outside rooms (canvas space X = 200, item space X = 168) -> closest to Room 2
        var zoneClosest = gridState.GetZoneForPoint(168, 0);
        Assert.Equal(SectorType.Secondary, zoneClosest.Sector);
        Assert.Equal(1, zoneClosest.Zone);
    }

    [Fact]
    public void BigPickup_CreationAndPaletteOperations_WorkCorrectly()
    {
        var vm = new LevelLayoutViewModel();
        vm.InitializePaletteCategories();

        var pickupCat = vm.PaletteCategories.FirstOrDefault(c => c.Name == "Pickups");
        Assert.NotNull(pickupCat);

        var bigPickupItem = pickupCat.Items.FirstOrDefault(i => i.ItemType == PaletteItemType.BigPickup);
        Assert.NotNull(bigPickupItem);
        Assert.Equal("Big Pickup", bigPickupItem.DisplayName);

        var placedPickup = vm.AddPaletteItem(bigPickupItem, 15.0, -25.0, 4.5);
        Assert.NotNull(placedPickup);
        Assert.True(placedPickup.IsBigPickup);
        Assert.True(placedPickup.IsWorldEventObject);
        Assert.Equal(ARA_CustomWorldEventObjectOptions.DataSphere, placedPickup.PickupToSpawn);
        Assert.Equal(15.0, placedPickup.Center.X);
        Assert.Equal(-25.0, placedPickup.Center.Y);
        Assert.Equal(4.5, placedPickup.Center.Z);
        Assert.Equal(1.0, placedPickup.Scale.X);
        Assert.Equal("#00E5FF", placedPickup.ShapeColorHex);
        Assert.Contains("DataSphere", placedPickup.TooltipText);

        // Clone
        var cloned = placedPickup.Clone();
        Assert.True(cloned.IsBigPickup);
        Assert.Equal(placedPickup.Name, cloned.Name);
        Assert.Equal(placedPickup.Center.X, cloned.Center.X);

        // ToData and FromData
        var data = placedPickup.ToData();
        Assert.True(data.IsWorldEventObject);
        Assert.Equal(ARA_CustomWorldEventObjectOptions.DataSphere, data.PickupToSpawn);

        var restored = PlacedTriggerNode.FromData(data);
        Assert.True(restored.IsBigPickup);
        Assert.Equal(data.PickupToSpawn, restored.PickupToSpawn);
    }

    [Fact]
    public void AraLevelLayoutService_MultiSectorExport_GeneratesSeparateSectorConfigFilesAndSpecificPickupDatas()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "MultiSectorAraTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var level = mainVm.LevelLayout.Levels[0];
            level.PersistentId = 5001;
            level.Name = "MultiSectorExpedition";

            var exp = mainVm.Rundown.SelectedRundown?.Expeditions.FirstOrDefault();
            Assert.NotNull(exp);
            exp.LevelLayoutData = 5001;
            exp.Descriptive.Prefix = "B2";
            exp.Descriptive.PublicName = "Multi Sector Expedition";
            exp.SecondaryLayerEnabled = true;
            exp.SecondaryLayout = 5002;
            exp.ThirdLayerEnabled = true;
            exp.ThirdLayout = 5003;

            var gridState = level.GridState;
            gridState.PlacedRooms.Clear();
            gridState.PlacedTriggers.Clear();

            // Room in Main Sector Zone 0
            var mainRoom = new PlacedRoomNode
            {
                RoomNumber = 1,
                Sector = SectorType.Main,
                Zone = 0,
                AreaIndex = 0,
                BoundsMinX = 0,
                BoundsMaxX = 64,
                BoundsMinY = 0,
                BoundsMaxY = 64
            };
            mainRoom.Triangles2D.Add(new Triangle2D(0, 0, 64, 0, 64, 64, 0, 0, 0));
            mainRoom.Triangles2D.Add(new Triangle2D(0, 0, 64, 64, 0, 64, 0, 0, 0));
            gridState.PlacedRooms.Add(mainRoom);

            // Room in Secondary Sector Zone 1
            var secRoom = new PlacedRoomNode
            {
                RoomNumber = 2,
                Sector = SectorType.Secondary,
                Zone = 1,
                AreaIndex = 1,
                BoundsMinX = 100,
                BoundsMaxX = 164,
                BoundsMinY = 0,
                BoundsMaxY = 64
            };
            secRoom.Triangles2D.Add(new Triangle2D(100, 0, 164, 0, 164, 64, 0, 0, 0));
            secRoom.Triangles2D.Add(new Triangle2D(100, 0, 164, 64, 100, 64, 0, 0, 0));
            gridState.PlacedRooms.Add(secRoom);

            // Add Big Pickup in Main Sector Zone 0 (Datasphere)
            var mainPickup = new PlacedTriggerNode
            {
                Name = "Datasphere",
                IsWorldEventObject = true,
                ItemType = PaletteItemType.BigPickup,
                PickupToSpawn = ARA_CustomWorldEventObjectOptions.DataSphere,
                Center = new Vector3Data(0.0, 0.0, 0.0) // canvas space (32, 32)
            };
            mainPickup.Events.Add(new WardenEvent { Type = EventType.PlaySound, SoundID = 1234 });
            gridState.PlacedTriggers.Add(mainPickup);

            // Add Big Pickup in Secondary Sector Zone 1 (PowerCell)
            var secPickup = new PlacedTriggerNode
            {
                Name = "Powercell_01",
                IsWorldEventObject = true,
                ItemType = PaletteItemType.BigPickup,
                PickupToSpawn = ARA_CustomWorldEventObjectOptions.PowerCell,
                Center = new Vector3Data(100.0, 0.0, 0.0) // canvas space (132, 32)
            };
            gridState.PlacedTriggers.Add(secPickup);

            // Export
            var exportContext = new ExportContext(mainVm, tempDir);
            exportContext.LayoutIds[level] = 5001;
            exportContext.SecondaryLayoutIds[level] = 5002;
            exportContext.ThirdLayoutIds[level] = 5003;

            AraLevelLayoutService.ExportAraLevelLayout(exportContext, level, exp);

            // Verify files created
            string mainFile = Path.Combine(tempDir, "Custom", "AdditionalRundownAdvancements", "LevelLayout",
                "B2.json");
            string secFile = Path.Combine(tempDir, "Custom", "AdditionalRundownAdvancements", "LevelLayout",
                "B2_Secondary.json");
            string thirdFile = Path.Combine(tempDir, "Custom", "AdditionalRundownAdvancements", "LevelLayout",
                "B2_Third.json");

            Assert.True(File.Exists(mainFile));
            Assert.True(File.Exists(secFile));
            Assert.True(File.Exists(thirdFile));

            // Verify main file content
            var mainJson = JsonNode.Parse(File.ReadAllText(mainFile)) as JsonObject;
            Assert.NotNull(mainJson);
            Assert.Equal(5001u, mainJson["MainLevelLayout"]?.GetValue<uint>());

            var mainZones = mainJson["Zones"] as JsonArray;
            Assert.NotNull(mainZones);
            var mainZone0 = mainZones.FirstOrDefault(z => z?["LocalIndex"]?.ToString() == "Zone_0") as JsonObject;
            Assert.NotNull(mainZone0);
            var mainWeList = mainZone0["WorldEventObjects"] as JsonArray;
            Assert.NotNull(mainWeList);
            Assert.Single(mainWeList);
            Assert.Equal("Datasphere", mainWeList[0]?["WorldEventObjectFilter"]?.ToString());
            Assert.NotNull(mainWeList[0]?["Components"]?["WE_SpecificPickup"]);

            // Verify secondary file content
            var secJson = JsonNode.Parse(File.ReadAllText(secFile)) as JsonObject;
            Assert.NotNull(secJson);
            Assert.Equal(5002u, secJson["MainLevelLayout"]?.GetValue<uint>());

            var secZones = secJson["Zones"] as JsonArray;
            Assert.NotNull(secZones);
            var secZone1 = secZones.FirstOrDefault(z => z?["LocalIndex"]?.ToString() == "Zone_1") as JsonObject;
            Assert.NotNull(secZone1);
            var secWeList = secZone1["WorldEventObjects"] as JsonArray;
            Assert.NotNull(secWeList);
            Assert.Single(secWeList);
            Assert.Equal("Powercell_01", secWeList[0]?["WorldEventObjectFilter"]?.ToString());

            // Test ExportSpecificPickupSpawningDatasForZone
            var mainSpsd =
                AraLevelLayoutService.ExportSpecificPickupSpawningDatasForZone(exportContext, gridState,
                    SectorType.Main, 0);
            Assert.Single(mainSpsd);
            Assert.Equal(151, mainSpsd[0]?["PickupToSpawn"]?.GetValue<int>());
            Assert.Equal("Datasphere", mainSpsd[0]?["WorldEventObjectFilter"]?.ToString());

            var secSpsd =
                AraLevelLayoutService.ExportSpecificPickupSpawningDatasForZone(exportContext, gridState,
                    SectorType.Secondary, 1);
            Assert.Single(secSpsd);
            Assert.Equal(131, secSpsd[0]?["PickupToSpawn"]?.GetValue<int>());
            Assert.Equal("Powercell_01", secSpsd[0]?["WorldEventObjectFilter"]?.ToString());

            // Import back into fresh level
            var importVm = new MainWindowViewModel();
            var importedLevel = new LevelLayoutBlock { PersistentId = 5001, Name = "ImportedExpedition" };
            var importContext = new ImportContext(importVm, tempDir);

            var spsdMap = new Dictionary<string, ARA_CustomWorldEventObjectOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["Datasphere"] = ARA_CustomWorldEventObjectOptions.DataSphere,
                ["Powercell_01"] = ARA_CustomWorldEventObjectOptions.PowerCell
            };

            AraLevelLayoutService.ImportAraLevelLayout(importContext, importedLevel, exp, null, spsdMap);

            Assert.Equal(2, importedLevel.GridState.PlacedTriggers.Count);
            var impDatasphere = importedLevel.GridState.PlacedTriggers.FirstOrDefault(t => t.Name == "Datasphere");
            Assert.NotNull(impDatasphere);
            Assert.True(impDatasphere.IsWorldEventObject);
            Assert.Equal(ARA_CustomWorldEventObjectOptions.DataSphere, impDatasphere.PickupToSpawn);
            Assert.Single(impDatasphere.Events);
            Assert.Equal(EventType.PlaySound, impDatasphere.Events[0].Type);

            var impPowercell = importedLevel.GridState.PlacedTriggers.FirstOrDefault(t => t.Name == "Powercell_01");
            Assert.NotNull(impPowercell);
            Assert.True(impPowercell.IsWorldEventObject);
            Assert.Equal(ARA_CustomWorldEventObjectOptions.PowerCell, impPowercell.PickupToSpawn);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    [Fact]
    public void BigPickup_AutoGeneratedFilterAndDefaults_CorrectlySetAndExported()
    {
        var vm = new LevelLayoutViewModel();
        var pickup1 = vm.AddBigPickup(10.0, 20.0, 5.0, ARA_CustomWorldEventObjectOptions.PowerCell);
        var pickup2 = vm.AddBigPickup(15.0, 25.0, 5.0, ARA_CustomWorldEventObjectOptions.PowerCell);

        // Auto-generated name/filter
        Assert.Equal("BigPickup_PowerCell", pickup1.Name);
        Assert.Equal("BigPickup_PowerCell", pickup2.Name);

        // Changing pickup type updates auto-generated filter name
        pickup2.PickupToSpawn = ARA_CustomWorldEventObjectOptions.FogTurbine;
        Assert.Equal("BigPickup_FogTurbine", pickup2.Name);

        // AreaIndex should be -1, UseExistingFilterInArea should be false
        Assert.Equal(-1, pickup1.AreaIndex);
        Assert.False(pickup1.UseExistingFilterInArea);
        Assert.Equal(-1, pickup2.AreaIndex);
        Assert.False(pickup2.UseRandomPosition);

        // Collider flags should be false for Big Pickups
        Assert.True(pickup1.IsBigPickup);
        Assert.False(pickup1.IsBox);
        Assert.False(pickup1.IsSphere);
        Assert.False(pickup1.IsCapsule);
        Assert.False(pickup1.IsCollision);
        Assert.False(pickup1.IsInteraction);
        Assert.False(pickup1.IsLookAt);
        Assert.False(pickup1.HasCarryItem);

        // Export test to verify AreaIndex: -1, UseExistingFilterInArea: false, InstanceIndex auto-generated
        string tempDir = Path.Combine(Path.GetTempPath(), "BigPickupDefaultsTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var level = mainVm.LevelLayout.Levels[0];
            level.GridState.PlacedRooms.Clear();
            level.GridState.PlacedTriggers.Clear();

            var room = new PlacedRoomNode
            {
                RoomNumber = 1,
                Sector = SectorType.Main,
                Zone = 0,
                AreaIndex = 0,
                BoundsMinX = 0,
                BoundsMaxX = 64,
                BoundsMinY = 0,
                BoundsMaxY = 64
            };
            room.Triangles2D.Add(new Triangle2D(0, 0, 64, 0, 64, 64, 0, 0, 0));
            level.GridState.PlacedRooms.Add(room);

            var pA = level.GridState.PlacedTriggers.Count;
            mainVm.LevelLayout.AddBigPickup(0.0, 0.0, 0.0, ARA_CustomWorldEventObjectOptions.DataSphere);
            mainVm.LevelLayout.AddBigPickup(1.0, 1.0, 0.0, ARA_CustomWorldEventObjectOptions.DataSphere);

            var exportContext = new ExportContext(mainVm, tempDir);
            exportContext.LayoutIds[level] = 6001;

            var araJson = AraLevelLayoutService.ExportAraLevelLayout(exportContext, level, null);
            Assert.NotNull(araJson);

            var zones = araJson["Zones"] as JsonArray;
            Assert.NotNull(zones);
            var zone0 = zones[0] as JsonObject;
            Assert.NotNull(zone0);
            var weObjs = zone0["WorldEventObjects"] as JsonArray;
            Assert.NotNull(weObjs);
            Assert.Equal(2, weObjs.Count);

            var weA = weObjs[0] as JsonObject;
            var weB = weObjs[1] as JsonObject;
            Assert.NotNull(weA);
            Assert.NotNull(weB);

            Assert.Equal("BigPickup_DataSphere", weA["WorldEventObjectFilter"]?.ToString());
            Assert.Equal(0, weA["InstanceIndex"]?.GetValue<int>());
            Assert.Equal(-1, weA["AreaIndex"]?.GetValue<int>());
            Assert.False(weA["UseExistingFilterInArea"]?.GetValue<bool>());

            Assert.Equal("BigPickup_DataSphere", weB["WorldEventObjectFilter"]?.ToString());
            Assert.Equal(1, weB["InstanceIndex"]?.GetValue<int>());
            Assert.Equal(-1, weB["AreaIndex"]?.GetValue<int>());
            Assert.False(weB["UseExistingFilterInArea"]?.GetValue<bool>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    [Fact]
    public void BigPickup_MiddleHitTesting_HasSmallerDragAreaAndLargerClickArea()
    {
        // 100x100 Box / Big Pickup
        // Standard trigger has 0.6 ratio (middle is <= 30 from center (50,50))
        // Big pickup has 0.35 ratio (middle is <= 17.5 from center (50,50))

        var centerPoint = new Avalonia.Point(50, 50);
        var middleClosePoint = new Avalonia.Point(60, 50); // dx = 10, relX = 0.2 -> Middle for both
        var intermediatePoint = new Avalonia.Point(72, 50); // dx = 22, relX = 0.44 -> Edge for BigPickup, Middle for normal Box
        var edgePoint = new Avalonia.Point(85, 50); // dx = 35, relX = 0.7 -> Edge for both

        Assert.True(LevelLayoutView.IsPointerInTriggerMiddle(centerPoint, 100, 100, TriggerShape.Box, false));
        Assert.True(LevelLayoutView.IsPointerInTriggerMiddle(centerPoint, 100, 100, TriggerShape.Box, true));

        Assert.True(LevelLayoutView.IsPointerInTriggerMiddle(middleClosePoint, 100, 100, TriggerShape.Box, false));
        Assert.True(LevelLayoutView.IsPointerInTriggerMiddle(middleClosePoint, 100, 100, TriggerShape.Box, true));

        // Intermediate point is middle for standard trigger, but edge (clickable for config) for Big Pickup
        Assert.True(LevelLayoutView.IsPointerInTriggerMiddle(intermediatePoint, 100, 100, TriggerShape.Box, false));
        Assert.False(LevelLayoutView.IsPointerInTriggerMiddle(intermediatePoint, 100, 100, TriggerShape.Box, true));

        Assert.False(LevelLayoutView.IsPointerInTriggerMiddle(edgePoint, 100, 100, TriggerShape.Box, false));
        Assert.False(LevelLayoutView.IsPointerInTriggerMiddle(edgePoint, 100, 100, TriggerShape.Box, true));
    }

    [Fact]
    public void DragAndDropBigPickup_OnPickupEvents_ExportAndImportCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DragDropBigPickupEventsTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var level = mainVm.LevelLayout.Levels[0];
            level.GridState.PlacedRooms.Clear();
            level.GridState.PlacedTriggers.Clear();

            var room = new PlacedRoomNode
            {
                RoomNumber = 1,
                Sector = SectorType.Main,
                Zone = 0,
                AreaIndex = 0,
                BoundsMinX = 0,
                BoundsMaxX = 64,
                BoundsMinY = 0,
                BoundsMaxY = 64
            };
            room.Triangles2D.Add(new Triangle2D(0, 0, 64, 0, 64, 64, 0, 0, 0));
            level.GridState.PlacedRooms.Add(room);

            // Add standard BigPickupDistributionInZone item (without events)
            var zs = level.GridState.GetOrCreateZoneSettings(SectorType.Main, 0);
            zs.BigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.PowerCell });
            zs.BigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.FogTurbine });

            // Add a drag-and-drop placed Big Pickup with OnPickupEvents
            var placedPickup = new PlacedTriggerNode
            {
                Id = Guid.NewGuid().ToString(),
                Name = "BigPickup_Datasphere_Custom",
                ItemType = PaletteItemType.BigPickup,
                IsWorldEventObject = true,
                PickupToSpawn = ARA_CustomWorldEventObjectOptions.DataSphere,
                Center = new Vector3Data(10, 15, 2),
                AreaIndex = -1,
                UseExistingFilterInArea = false
            };
            placedPickup.Events.Add(new WardenEvent
            {
                Type = EventType.AllLightsOn,
                Delay = 2.0
            });
            placedPickup.Events.Add(new WardenEvent
            {
                Type = EventType.PlaySound,
                SoundID = 12345
            });
            level.GridState.PlacedTriggers.Add(placedPickup);

            var exportContext = new ExportContext(mainVm, tempDir);
            exportContext.LayoutIds[level] = 7001;

            var araJson = AraLevelLayoutService.ExportAraLevelLayout(exportContext, level, null);
            Assert.NotNull(araJson);

            var zones = araJson["Zones"] as JsonArray;
            Assert.NotNull(zones);

            var zone0 = zones.FirstOrDefault(z => z?["LocalIndex"]?.ToString() == "Zone_0" && z?["DimensionIndex"]?.ToString() == "Reality") as JsonObject;
            Assert.NotNull(zone0);
            var weObjs = zone0["WorldEventObjects"] as JsonArray;
            Assert.NotNull(weObjs);
            Assert.Single(weObjs); // Only the drag-and-drop big pickup

            var we1 = weObjs[0] as JsonObject;
            Assert.NotNull(we1);
            Assert.Equal("BigPickup_Datasphere_Custom", we1["WorldEventObjectFilter"]?.ToString());
            Assert.Equal(0, we1["InstanceIndex"]?.GetValue<int>());
            Assert.Equal(-1, we1["AreaIndex"]?.GetValue<int>());
            Assert.False(we1["UseExistingFilterInArea"]?.GetValue<bool>());

            var comp1 = we1["Components"] as JsonObject;
            Assert.NotNull(comp1);
            var sp1 = comp1["WE_SpecificPickup"] as JsonObject;
            Assert.NotNull(sp1);
            var evs1 = sp1["EventsOnPickup"] as JsonArray;
            Assert.NotNull(evs1);
            Assert.Equal(2, evs1.Count);

            // Test Project File Save / Load preservation of grid state and zone settings
            var projData = level.GridState.ExportStateData();
            var restoredGrid = new LevelLayoutGridState();
            restoredGrid.ImportStateData(projData);
            var restoredZs = restoredGrid.GetZoneSettings(SectorType.Main, 0);
            Assert.NotNull(restoredZs);
            Assert.Equal(2, restoredZs.BigPickupDistributionInZone.Count);
            Assert.Equal(RetrieveBigItemType.PowerCell, restoredZs.BigPickupDistributionInZone[0].ItemType);
            Assert.Equal(RetrieveBigItemType.FogTurbine, restoredZs.BigPickupDistributionInZone[1].ItemType);
            Assert.Single(restoredGrid.PlacedTriggers);
            Assert.Equal(2, restoredGrid.PlacedTriggers[0].Events.Count);

            // Test ARA Import back into fresh level
            var importVm = new MainWindowViewModel();
            var importedLevel = new LevelLayoutBlock { PersistentId = 7001, Name = "ImportedExpedition" };
            var importContext = new ImportContext(importVm, tempDir);

            AraLevelLayoutService.ImportAraLevelLayout(importContext, importedLevel, null, null, null);

            Assert.Single(importedLevel.GridState.PlacedTriggers);
            var importedPickup = importedLevel.GridState.PlacedTriggers[0];
            Assert.Equal("BigPickup_Datasphere_Custom", importedPickup.Name);
            Assert.Equal(2, importedPickup.Events.Count);
            Assert.Equal(EventType.AllLightsOn, importedPickup.Events[0].Type);
            Assert.Equal(EventType.PlaySound, importedPickup.Events[1].Type);
            Assert.Equal((uint)12345, importedPickup.Events[1].SoundID);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    [Fact]
    public void EditorDialogs_HaveUniformGlobalDimensions()
    {
        var dialogFiles = new[]
        {
            "EditRoomDialog.axaml",
            "DimensionConfigDialog.axaml",
            "TriggerConfigDialog.axaml",
            "EditDoorDialog.axaml",
            "EditSectorDialog.axaml",
            "PickGeomorphDialog.axaml",
            "LevelConfigDialog.axaml",
            "ExpandRoomDialog.axaml",
            "SettingsDialog.axaml"
        };

        var dialogDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "GTFO_GUIEditor", "Views", "Dialogs");

        foreach (var file in dialogFiles)
        {
            var filePath = Path.Combine(dialogDir, file);
            if (!File.Exists(filePath))
            {
                // Fallback for execution from test directory
                filePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "GTFO_GUIEditor", "Views", "Dialogs", file));
            }

            Assert.True(File.Exists(filePath), $"Dialog file not found: {filePath}");
            var content = File.ReadAllText(filePath);

            Assert.Contains("Width=\"920\"", content);
            Assert.Contains("MaxHeight=\"800\"", content);
        }
    }

    [Fact]
    public void AdditionalRundownAdvancements_ModStateAndFeatureLocking_WorksCorrectly()
    {
        var modVm = new ModdedViewModel();
        modVm.Reset();

        Assert.False(modVm.AdditionalRundownAdvancements);
        Assert.False(ModManager.Instance.AdditionalRundownAdvancements);
        Assert.False(modVm.IsAdditionalRundownAdvancementsInUse);
        Assert.True(modVm.CanDisableAdditionalRundownAdvancements);

        // Enable ARA
        modVm.AdditionalRundownAdvancements = true;
        Assert.True(modVm.AdditionalRundownAdvancements);
        Assert.True(ModManager.Instance.AdditionalRundownAdvancements);

        // Setup LevelLayoutViewModel with placed trigger
        var levelVm = new LevelLayoutViewModel();
        Assert.True(levelVm.IsAraEnabled);

        var level = levelVm.Levels[0];
        var trigger = new PlacedTriggerNode { Name = "TestTrigger", Shape = TriggerShape.Box };
        level.GridState.PlacedTriggers.Add(trigger);

        // Now feature is in use
        Assert.True(levelVm.HasAnyAraFeaturesInUse());
        Assert.True(modVm.IsAdditionalRundownAdvancementsInUse);
        Assert.False(modVm.CanDisableAdditionalRundownAdvancements);

        // Attempting to disable should fail and set error message
        modVm.AdditionalRundownAdvancements = false;
        Assert.True(modVm.AdditionalRundownAdvancements);
        Assert.True(modVm.HasErrorMessage);
        Assert.Contains("Placed Triggers & Objects", modVm.ErrorMessage);

        // Remove trigger
        level.GridState.PlacedTriggers.Clear();
        Assert.False(levelVm.HasAnyAraFeaturesInUse());
        Assert.False(modVm.IsAdditionalRundownAdvancementsInUse);
        Assert.True(modVm.CanDisableAdditionalRundownAdvancements);

        // Now disabling succeeds
        modVm.AdditionalRundownAdvancements = false;
        Assert.False(modVm.AdditionalRundownAdvancements);
        Assert.False(levelVm.IsAraEnabled);
        Assert.False(modVm.HasErrorMessage);

        // Test ModdedSettingsData export and import
        modVm.AdditionalRundownAdvancements = true;
        var exportedData = modVm.ExportData();
        Assert.True(exportedData.AdditionalRundownAdvancements);

        modVm.Reset();
        Assert.False(modVm.AdditionalRundownAdvancements);

        modVm.ApplyData(exportedData);
        Assert.True(modVm.AdditionalRundownAdvancements);
    }
}