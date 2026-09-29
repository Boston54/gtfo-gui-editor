using Avalonia.Controls;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
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