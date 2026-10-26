using Avalonia.Controls;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Views;

public partial class GeomorphsView : UserControl
{
    public GeomorphsView()
    {
        InitializeComponent();
    }

    private async void GeomorphsDataGrid_CellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.Column?.Header?.ToString() == "Image")
        {
            return;
        }

        if (e.Row?.DataContext is GeomorphEntry geomorph)
        {
            if (DataContext is GeomorphsViewModel vm)
            {
                await vm.CopyPrefabPathAsync(geomorph);
            }
            else if (DataContext is MainWindowViewModel mainVm)
            {
                await mainVm.Geomorphs.CopyPrefabPathAsync(geomorph);
            }
        }
    }
}
