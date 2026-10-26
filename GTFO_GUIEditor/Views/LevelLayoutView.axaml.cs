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

    public LevelLayoutView()
    {
        InitializeComponent();

        var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder");
        if (canvasBorder != null)
        {
            canvasBorder.SizeChanged += OnLevelLayoutCanvasBorderSizeChanged;
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
            else if (e.PreviousSize.Width > 50 && e.PreviousSize.Height > 50)
            {
                vm.GridState.ViewportWidth = e.NewSize.Width;
                vm.GridState.ViewportHeight = e.NewSize.Height;
                double deltaW = e.NewSize.Width - e.PreviousSize.Width;
                double deltaH = e.NewSize.Height - e.PreviousSize.Height;
                vm.GridState.Pan(deltaW / 2.0, deltaH / 2.0);
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

        var currentPosition = e.GetPosition(sender as Visual);

        if (_isPanningLevelLayout)
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
            double worldX = (currentPosition.X - vm.GridState.PanX) / vm.GridState.Zoom;
            double worldY = (currentPosition.Y - vm.GridState.PanY) / vm.GridState.Zoom;
            var room = vm.GridState.GetRoomAt(worldX, worldY);
            vm.GridState.HoveredRoom = room;
        }
    }

    private void OnLevelLayoutPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanningLevelLayout)
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
    }

    private void OnLevelLayoutPointerExited(object? sender, PointerEventArgs e)
    {
        var vm = GetViewModel();
        if (!_isPanningLevelLayout && vm != null)
        {
            vm.GridState.HoveredRoom = null;
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
}
