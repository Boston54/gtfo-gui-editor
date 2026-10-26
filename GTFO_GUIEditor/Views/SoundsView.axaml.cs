using Avalonia.Controls;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Views;

public partial class SoundsView : UserControl
{
    public SoundsView()
    {
        InitializeComponent();
    }

    private async void SoundsDataGrid_CellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.Row?.DataContext is SoundEntry sound)
        {
            if (DataContext is SoundsViewModel vm)
            {
                await vm.CopySoundIdAsync(sound);
            }
            else if (DataContext is MainWindowViewModel mainVm)
            {
                await mainVm.Sounds.CopySoundIdAsync(sound);
            }
        }
    }
}
