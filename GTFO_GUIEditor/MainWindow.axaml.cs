using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GTFO_GUIEditor.Views.Dialogs;
using GTFO_GUIEditor.ViewModels;
using System.Threading.Tasks;

namespace GTFO_GUIEditor;

public partial class MainWindow : Window
{
    private bool _allowClose;
    private bool _isPromptingForUnsavedChanges;
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(autoReopenLastRundown: true);
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyWindowDisplayMode(this);
        }
        Closing += OnWindowClosing;
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (_allowClose || DataContext is not MainWindowViewModel viewModel || !viewModel.IsDirty)
        {
            e.Cancel = false;
            return;
        }

        if (_isPromptingForUnsavedChanges)
        {
            return;
        }

        _isPromptingForUnsavedChanges = true;
        var dialog = new UnsavedChangesDialog();
        bool? result = await dialog.ShowDialog<bool?>(this);
        _isPromptingForUnsavedChanges = false;

        if (result == null)
        {
            return;
        }

        if (result == true)
        {
            await viewModel.SaveRundownAsync();
            if (viewModel.IsDirty)
            {
                return;
            }
        }

        _allowClose = true;
        Close();
    }
}
