using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Views;

public partial class LevelLayoutView : UserControl
{
    private bool _isPanningLevelLayout;
    private bool _isDraggingPan;
    private Point _pointerPressedPosition;
    private Point _lastPointerPosition;
    private bool _hasInitializedViewCenter;
    private LayoutPaletteItem? _draggedPaletteItem;
    private PlacedTriggerNode? _draggedTrigger;
    private Point _triggerDragStartPoint;
    private double _triggerInitialCenterX;
    private double _triggerInitialCenterY;
    private double _triggerStartWorldX;
    private double _triggerStartWorldY;
    private bool _isDraggingTrigger;

    public LevelLayoutView()
    {
        InitializeComponent();

        var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder");
        if (canvasBorder != null)
        {
            canvasBorder.SizeChanged += OnLevelLayoutCanvasBorderSizeChanged;
            DragDrop.SetAllowDrop(canvasBorder, true);
            canvasBorder.AddHandler(DragDrop.DragOverEvent, OnCanvasDragOver);
            canvasBorder.AddHandler(DragDrop.DragLeaveEvent, OnCanvasDragLeave);
            canvasBorder.AddHandler(DragDrop.DropEvent, OnCanvasDrop);
        }
    }

    private LevelLayoutViewModel? GetViewModel()
    {
        if (DataContext is LevelLayoutViewModel layoutVm)
            return layoutVm;
        if (DataContext is MainWindowViewModel mainVm)
            return mainVm.LevelLayout;
        return null;
    }

    private void OnLevelLayoutCanvasBorderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null && e.NewSize.Width > 50 && e.NewSize.Height > 50)
        {
            if (!_hasInitializedViewCenter)
            {
                _hasInitializedViewCenter = true;
                vm.GridState.ResetView(e.NewSize.Width, e.NewSize.Height);
            }
            else
            {
                vm.GridState.ViewportWidth = e.NewSize.Width;
                vm.GridState.ViewportHeight = e.NewSize.Height;
            }
        }
    }

    private void OnLevelLayoutPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(sender as Visual);
        if (currentPoint.Properties.IsLeftButtonPressed)
        {
            _isPanningLevelLayout = true;
            _isDraggingPan = false;
            _pointerPressedPosition = e.GetPosition(sender as Visual);
            _lastPointerPosition = _pointerPressedPosition;
            e.Pointer.Capture(sender as Control);
            e.Handled = true;
        }
    }

    private void OnLevelLayoutPointerMoved(object? sender, PointerEventArgs e)
    {
        var vm = GetViewModel();
        if (vm == null) return;

        var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder") ?? sender as Visual;
        var currentPosition = e.GetPosition(canvasBorder);

        double mouseWorldX = (currentPosition.X - vm.GridState.PanX) / vm.GridState.Zoom;
        double mouseWorldY = (currentPosition.Y - vm.GridState.PanY) / vm.GridState.Zoom;
        double hoverItemX = mouseWorldX - 32.0;
        double hoverItemY = 32.0 - mouseWorldY;
        double hoverItemZ = vm.GridState.GetHeightAt(mouseWorldX, mouseWorldY);

        vm.GridState.UpdateHoverCoordinates(hoverItemX, hoverItemY, hoverItemZ);

        if (_draggedTrigger != null)
        {
            double deltaDragX = currentPosition.X - _triggerDragStartPoint.X;
            double deltaDragY = currentPosition.Y - _triggerDragStartPoint.Y;
            double totalDrag = Math.Sqrt(deltaDragX * deltaDragX + deltaDragY * deltaDragY);
            if (!_isDraggingTrigger && totalDrag > 4.0)
            {
                _isDraggingTrigger = true;
            }

            if (_isDraggingTrigger)
            {
                double curWorldX = (currentPosition.X - vm.GridState.PanX) / vm.GridState.Zoom;
                double curWorldY = (currentPosition.Y - vm.GridState.PanY) / vm.GridState.Zoom;
                double deltaWorldX = curWorldX - _triggerStartWorldX;
                double deltaWorldY = curWorldY - _triggerStartWorldY;

                double newCenterX = _triggerInitialCenterX + deltaWorldX;
                double newCenterY = _triggerInitialCenterY - deltaWorldY;

                double worldPosForHeightX = newCenterX + 32.0;
                double worldPosForHeightY = 32.0 - newCenterY;
                double itemZ = vm.GridState.GetHeightAt(worldPosForHeightX, worldPosForHeightY);

                _draggedTrigger.Center.X = Math.Round(newCenterX, 2);
                _draggedTrigger.Center.Y = Math.Round(newCenterY, 2);
                _draggedTrigger.Center.Z = Math.Round(itemZ, 2);
                _draggedTrigger.NotifyTransformProperties();
            }

            e.Handled = true;
        }
        else if (_isPanningLevelLayout)
        {
            double moveX = currentPosition.X - _pointerPressedPosition.X;
            double moveY = currentPosition.Y - _pointerPressedPosition.Y;
            double totalMove = Math.Sqrt(moveX * moveX + moveY * moveY);
            if (totalMove > 4.0)
            {
                _isDraggingPan = true;
            }

            double deltaX = currentPosition.X - _lastPointerPosition.X;
            double deltaY = currentPosition.Y - _lastPointerPosition.Y;
            _lastPointerPosition = currentPosition;

            vm.Pan(deltaX, deltaY);
            e.Handled = true;
        }
        else
        {
            var room = vm.GridState.GetRoomAt(mouseWorldX, mouseWorldY);
            vm.GridState.HoveredRoom = room;
        }
    }

    private void OnLevelLayoutPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedTrigger != null)
        {
            e.Pointer.Capture(null);
            var trigger = _draggedTrigger;
            bool wasDragging = _isDraggingTrigger;
            _draggedTrigger = null;
            _isDraggingTrigger = false;
            e.Handled = true;

            var vm = GetViewModel();
            if (!wasDragging && vm != null)
            {
                vm.OnTriggerClicked(trigger);
            }
        }
        else if (_isPanningLevelLayout)
        {
            bool wasDragging = _isDraggingPan;
            _isPanningLevelLayout = false;
            _isDraggingPan = false;
            e.Pointer.Capture(null);
            e.Handled = true;

            var vm = GetViewModel();
            if (!wasDragging && vm != null)
            {
                var pos = e.GetPosition(sender as Visual);
                double worldX = (pos.X - vm.GridState.PanX) / vm.GridState.Zoom;
                double worldY = (pos.Y - vm.GridState.PanY) / vm.GridState.Zoom;
                var clickedRoom = vm.GridState.GetRoomAt(worldX, worldY);
                if (clickedRoom != null)
                {
                    vm.OnRoomClicked(clickedRoom);
                }
            }
        }
    }

    private void OnLevelLayoutPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isPanningLevelLayout = false;
        _isDraggingPan = false;
        _draggedTrigger = null;
        _isDraggingTrigger = false;
    }

    private void OnLevelLayoutPointerExited(object? sender, PointerEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null)
        {
            vm.GridState.ClearHoverCoordinates();
            if (!_isPanningLevelLayout)
            {
                vm.GridState.HoveredRoom = null;
                vm.GridState.IsDraggingTriggerPreview = false;
            }
        }
    }

    private void OnLevelLayoutPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null)
        {
            var pos = e.GetPosition(sender as Visual);
            vm.ZoomAt(e.Delta.Y, pos.X, pos.Y);
            e.Handled = true;
        }
    }

    private void OnResetViewClicked(object? sender, RoutedEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null)
        {
            var viewportBorder = this.FindControl<Border>("LevelLayoutCanvasBorder");
            double width = viewportBorder?.Bounds.Width ?? 800;
            double height = viewportBorder?.Bounds.Height ?? 600;
            if (width < 100) width = 800;
            if (height < 100) height = 600;

            vm.GridState.ResetView(width, height);
        }
    }

    private void OnDoorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        var vm = GetViewModel();
        if (sender is Control ctrl && ctrl.DataContext is PlacedDoorNode door && vm != null)
        {
            vm.OnDoorClicked(door);
        }
    }

    public static bool IsPointerInTriggerMiddle(Point localPos, double width, double height, TriggerShape shape, bool isBigPickup = false)
    {
        if (width <= 0 || height <= 0) return true;

        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double dx = Math.Abs(localPos.X - centerX);
        double dy = Math.Abs(localPos.Y - centerY);

        double middleRatio = isBigPickup ? 0.35 : 0.6;

        if (shape == TriggerShape.Sphere || shape == TriggerShape.Capsule)
        {
            double radius = Math.Min(width, height) / 2.0;
            if (radius <= 0) return true;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            // Middle is inner ratio of radius; outer is edge
            return dist <= radius * middleRatio;
        }
        else // Box / BigPickup
        {
            double halfW = width / 2.0;
            double halfH = height / 2.0;
            if (halfW <= 0 || halfH <= 0) return true;
            double relX = dx / halfW;
            double relY = dy / halfH;
            // Middle is inner ratio in both dimensions; outer is edge
            return Math.Max(relX, relY) <= middleRatio;
        }
    }

    private void OnTriggerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(sender as Visual);
        if (currentPoint.Properties.IsLeftButtonPressed && sender is Control ctrl && ctrl.DataContext is PlacedTriggerNode trigger)
        {
            var vm = GetViewModel();
            if (vm == null) return;

            // Double click anywhere on trigger opens config
            if (e.ClickCount >= 2)
            {
                e.Pointer.Capture(null);
                _draggedTrigger = null;
                _isDraggingTrigger = false;
                vm.OnTriggerClicked(trigger);
                e.Handled = true;
                return;
            }

            var localPos = e.GetPosition(ctrl);
            double width = ctrl.Bounds.Width > 0 ? ctrl.Bounds.Width : trigger.CanvasWidth;
            double height = ctrl.Bounds.Height > 0 ? ctrl.Bounds.Height : trigger.CanvasHeight;

            bool isMiddle = IsPointerInTriggerMiddle(localPos, width, height, trigger.Shape, trigger.IsBigPickup);
            if (!isMiddle)
            {
                // Single click on edge opens config directly without starting drag
                e.Pointer.Capture(null);
                _draggedTrigger = null;
                _isDraggingTrigger = false;
                vm.OnTriggerClicked(trigger);
                e.Handled = true;
                return;
            }

            // Clicked in middle: allow dragging or single-click release
            var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder");
            var canvasPos = e.GetPosition(canvasBorder ?? ctrl);

            _draggedTrigger = trigger;
            _isDraggingTrigger = false;
            _triggerDragStartPoint = canvasPos;
            _triggerInitialCenterX = trigger.Center.X;
            _triggerInitialCenterY = trigger.Center.Y;
            _triggerStartWorldX = (canvasPos.X - vm.GridState.PanX) / vm.GridState.Zoom;
            _triggerStartWorldY = (canvasPos.Y - vm.GridState.PanY) / vm.GridState.Zoom;

            e.Pointer.Capture(ctrl);
            e.Handled = true;
        }
    }

    private void OnTriggerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTrigger != null)
        {
            var vm = GetViewModel();
            if (vm == null) return;

            var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder") ?? (sender as Visual);
            var currentPosition = e.GetPosition(canvasBorder);

            double deltaDragX = currentPosition.X - _triggerDragStartPoint.X;
            double deltaDragY = currentPosition.Y - _triggerDragStartPoint.Y;
            double totalDrag = Math.Sqrt(deltaDragX * deltaDragX + deltaDragY * deltaDragY);
            if (!_isDraggingTrigger && totalDrag > 4.0)
            {
                _isDraggingTrigger = true;
            }

            if (_isDraggingTrigger)
            {
                double curWorldX = (currentPosition.X - vm.GridState.PanX) / vm.GridState.Zoom;
                double curWorldY = (currentPosition.Y - vm.GridState.PanY) / vm.GridState.Zoom;
                double deltaWorldX = curWorldX - _triggerStartWorldX;
                double deltaWorldY = curWorldY - _triggerStartWorldY;

                double newCenterX = _triggerInitialCenterX + deltaWorldX;
                double newCenterY = _triggerInitialCenterY - deltaWorldY;

                double worldPosForHeightX = newCenterX + 32.0;
                double worldPosForHeightY = 32.0 - newCenterY;
                double itemZ = vm.GridState.GetHeightAt(worldPosForHeightX, worldPosForHeightY);

                _draggedTrigger.Center.X = Math.Round(newCenterX, 2);
                _draggedTrigger.Center.Y = Math.Round(newCenterY, 2);
                _draggedTrigger.Center.Z = Math.Round(itemZ, 2);
                _draggedTrigger.NotifyTransformProperties();
            }

            double mouseWorldX = (currentPosition.X - vm.GridState.PanX) / vm.GridState.Zoom;
            double mouseWorldY = (currentPosition.Y - vm.GridState.PanY) / vm.GridState.Zoom;
            double hoverItemX = mouseWorldX - 32.0;
            double hoverItemY = 32.0 - mouseWorldY;
            double hoverItemZ = vm.GridState.GetHeightAt(mouseWorldX, mouseWorldY);
            vm.GridState.UpdateHoverCoordinates(hoverItemX, hoverItemY, hoverItemZ);

            e.Handled = true;
        }
        else if (sender is Control ctrl && ctrl.DataContext is PlacedTriggerNode trigger)
        {
            var localPos = e.GetPosition(ctrl);
            double width = ctrl.Bounds.Width > 0 ? ctrl.Bounds.Width : trigger.CanvasWidth;
            double height = ctrl.Bounds.Height > 0 ? ctrl.Bounds.Height : trigger.CanvasHeight;
            bool isMiddle = IsPointerInTriggerMiddle(localPos, width, height, trigger.Shape, trigger.IsBigPickup);
            ctrl.Cursor = isMiddle ? new Cursor(StandardCursorType.SizeAll) : new Cursor(StandardCursorType.Hand);
        }
    }

    private void OnTriggerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedTrigger != null)
        {
            e.Pointer.Capture(null);
            var trigger = _draggedTrigger;
            bool wasDragging = _isDraggingTrigger;
            _draggedTrigger = null;
            _isDraggingTrigger = false;
            e.Handled = true;

            var vm = GetViewModel();
            if (!wasDragging && vm != null)
            {
                vm.OnTriggerClicked(trigger);
            }
        }
    }

    private void OnTriggerPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is PlacedTriggerNode trigger)
        {
            trigger.IsHovered = true;
            var localPos = e.GetPosition(ctrl);
            double width = ctrl.Bounds.Width > 0 ? ctrl.Bounds.Width : trigger.CanvasWidth;
            double height = ctrl.Bounds.Height > 0 ? ctrl.Bounds.Height : trigger.CanvasHeight;
            bool isMiddle = IsPointerInTriggerMiddle(localPos, width, height, trigger.Shape, trigger.IsBigPickup);
            ctrl.Cursor = isMiddle ? new Cursor(StandardCursorType.SizeAll) : new Cursor(StandardCursorType.Hand);
        }
    }

    private void OnTriggerPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is PlacedTriggerNode trigger)
        {
            trigger.IsHovered = false;
        }
    }

    private async void OnPaletteItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(sender as Visual ?? this);
        if (!currentPoint.Properties.IsLeftButtonPressed) return;

        if (sender is Control ctrl && ctrl.DataContext is LayoutPaletteItem item)
        {
            _draggedPaletteItem = item;
            var dataObject = new DataObject();
            dataObject.Set("GTFO_PaletteItem", item);
            await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Copy);
        }
    }

    private void OnCanvasDragOver(object? sender, DragEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null)
        {
            var item = (e.Data.Contains("GTFO_PaletteItem") ? e.Data.Get("GTFO_PaletteItem") as LayoutPaletteItem : null) ?? _draggedPaletteItem;
            if (item != null)
            {
                var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder") ?? (sender as Visual) ?? this;
                var pos = e.GetPosition(canvasBorder);
                double worldX = (pos.X - vm.GridState.PanX) / vm.GridState.Zoom;
                double worldY = (pos.Y - vm.GridState.PanY) / vm.GridState.Zoom;

                double itemX = worldX - 32.0;
                double itemY = 32.0 - worldY;
                double itemZ = vm.GridState.GetHeightAt(worldX, worldY);

                if (item.ItemType == PaletteItemType.BigPickup)
                {
                    vm.GridState.DragPreviewTrigger.IsWorldEventObject = true;
                    vm.GridState.DragPreviewTrigger.ItemType = PaletteItemType.BigPickup;
                    vm.GridState.DragPreviewTrigger.PickupToSpawn = ARA_CustomWorldEventObjectOptions.DataSphere;
                    vm.GridState.DragPreviewTrigger.Center.X = Math.Round(itemX, 2);
                    vm.GridState.DragPreviewTrigger.Center.Y = Math.Round(itemY, 2);
                    vm.GridState.DragPreviewTrigger.Center.Z = Math.Round(itemZ, 2);
                    vm.GridState.DragPreviewTrigger.NotifyTransformProperties();
                    vm.GridState.IsDraggingTriggerPreview = true;
                }
                else if (item.TriggerShape.HasValue)
                {
                    vm.GridState.DragPreviewTrigger.IsWorldEventObject = false;
                    vm.GridState.DragPreviewTrigger.ItemType = item.ItemType;
                    vm.GridState.DragPreviewTrigger.Shape = item.TriggerShape.Value;
                    vm.GridState.DragPreviewTrigger.Center.X = Math.Round(itemX, 2);
                    vm.GridState.DragPreviewTrigger.Center.Y = Math.Round(itemY, 2);
                    vm.GridState.DragPreviewTrigger.Center.Z = Math.Round(itemZ, 2);
                    vm.GridState.DragPreviewTrigger.NotifyTransformProperties();
                    vm.GridState.IsDraggingTriggerPreview = true;
                }
            }
        }

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnCanvasDragLeave(object? sender, DragEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null)
        {
            vm.GridState.IsDraggingTriggerPreview = false;
        }
    }

    private void OnCanvasDrop(object? sender, DragEventArgs e)
    {
        var vm = GetViewModel();
        if (vm != null)
        {
            vm.GridState.IsDraggingTriggerPreview = false;

            LayoutPaletteItem? item = null;
            if (e.Data.Contains("GTFO_PaletteItem"))
            {
                item = e.Data.Get("GTFO_PaletteItem") as LayoutPaletteItem;
            }
            item ??= _draggedPaletteItem;

            if (item != null)
            {
                var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder") ?? (sender as Visual) ?? this;
                var pos = e.GetPosition(canvasBorder);
                double worldX = (pos.X - vm.GridState.PanX) / vm.GridState.Zoom;
                double worldY = (pos.Y - vm.GridState.PanY) / vm.GridState.Zoom;

                // Coordinate system:
                // Centre of elevator tile is (0,0), grid size is 64x64, top right of elevator is (32, 32).
                double itemX = worldX - 32.0;
                double itemY = 32.0 - worldY;
                double itemZ = vm.GridState.GetHeightAt(worldX, worldY);

                vm.AddPaletteItem(item, itemX, itemY, itemZ);
                e.Handled = true;
            }
        }

        _draggedPaletteItem = null;
    }
}
