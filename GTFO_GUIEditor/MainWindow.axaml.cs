using Avalonia.Controls;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(autoReopenLastRundown: true);
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyWindowDisplayMode(this);
        }
    }
}
