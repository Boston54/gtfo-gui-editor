using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor;

public partial class MainWindow : Window
{
    private bool _isPanningLevelLayout;
    private Point _lastPointerPosition;
    private bool _hasInitializedViewCenter;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        var canvasBorder = this.FindControl<Border>("LevelLayoutCanvasBorder");
        if (canvasBorder != null)
        {
            canvasBorder.SizeChanged += OnLevelLayoutCanvasBorderSizeChanged;
        }
    }

    private void OnLevelLayoutCanvasBorderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainVm && e.NewSize.Width > 50 && e.NewSize.Height > 50)
        {
            if (!_hasInitializedViewCenter)
            {
                _hasInitializedViewCenter = true;
                mainVm.LevelLayout.GridState.ResetView(e.NewSize.Width, e.NewSize.Height);
            }
            else if (e.PreviousSize.Width > 50 && e.PreviousSize.Height > 50)
            {
                mainVm.LevelLayout.GridState.ViewportWidth = e.NewSize.Width;
                mainVm.LevelLayout.GridState.ViewportHeight = e.NewSize.Height;
                double deltaW = e.NewSize.Width - e.PreviousSize.Width;
                double deltaH = e.NewSize.Height - e.PreviousSize.Height;
                mainVm.LevelLayout.GridState.Pan(deltaW / 2.0, deltaH / 2.0);
            }
        }
    }

    private void OnLevelLayoutPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(sender as Visual);
        if (currentPoint.Properties.IsLeftButtonPressed)
        {
            _isPanningLevelLayout = true;
            _lastPointerPosition = e.GetPosition(sender as Visual);
            e.Pointer.Capture(sender as Control);
            e.Handled = true;
        }
    }

    private void OnLevelLayoutPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanningLevelLayout && DataContext is MainWindowViewModel mainVm)
        {
            var currentPosition = e.GetPosition(sender as Visual);
            double deltaX = currentPosition.X - _lastPointerPosition.X;
            double deltaY = currentPosition.Y - _lastPointerPosition.Y;
            _lastPointerPosition = currentPosition;

            mainVm.LevelLayout.Pan(deltaX, deltaY);
            e.Handled = true;
        }
    }

    private void OnLevelLayoutPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanningLevelLayout)
        {
            _isPanningLevelLayout = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnLevelLayoutPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isPanningLevelLayout = false;
    }

    private void OnLevelLayoutPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainVm)
        {
            var pos = e.GetPosition(sender as Visual);
            mainVm.LevelLayout.ZoomAt(e.Delta.Y, pos.X, pos.Y);
            e.Handled = true;
        }
    }

    private void OnResetViewClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainVm)
        {
            var viewportBorder = this.FindControl<Border>("LevelLayoutCanvasBorder");
            double width = viewportBorder?.Bounds.Width ?? 800;
            double height = viewportBorder?.Bounds.Height ?? 600;
            if (width < 100) width = 800;
            if (height < 100) height = 600;

            mainVm.LevelLayout.GridState.ResetView(width, height);
        }
    }

    private void OnDoorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (sender is Control ctrl && ctrl.DataContext is PlacedDoorNode door && DataContext is MainWindowViewModel mainVm)
        {
            mainVm.LevelLayout.OnDoorClicked(door);
        }
    }

    private async void SoundsDataGrid_CellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.Row?.DataContext is SoundEntry sound)
        {
            if (DataContext is MainWindowViewModel mainVm)
            {
                await mainVm.Sounds.CopySoundIdAsync(sound);
            }
        }
    }

    private async void GeomorphsDataGrid_CellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.Column?.Header?.ToString() == "Image")
        {
            return;
        }

        if (e.Row?.DataContext is GeomorphEntry geomorph)
        {
            if (DataContext is MainWindowViewModel mainVm)
            {
                await mainVm.Geomorphs.CopyPrefabPathAsync(geomorph);
            }
        }
    }
}