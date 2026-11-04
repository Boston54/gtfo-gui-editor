using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GTFO_GUIEditor.Views.Dialogs;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void Discard_Click(object? sender, RoutedEventArgs e) => Close(false);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
