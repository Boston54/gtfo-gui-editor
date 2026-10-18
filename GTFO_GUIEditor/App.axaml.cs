using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace GTFO_GUIEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Prevent closed ComboBoxes from cycling selection on mouse wheel hover
        ComboBox.PointerWheelChangedEvent.AddClassHandler<ComboBox>((comboBox, e) =>
        {
            if (!comboBox.IsDropDownOpen)
            {
                e.Handled = true;

                if (comboBox.GetVisualParent() is Control parent && comboBox.GetVisualRoot() is Visual rootVisual)
                {
                    var newArgs = new PointerWheelEventArgs(
                        parent,
                        e.Pointer,
                        rootVisual,
                        e.GetPosition(rootVisual),
                        e.Timestamp,
                        e.GetCurrentPoint(rootVisual).Properties,
                        e.KeyModifiers,
                        e.Delta);

                    parent.RaiseEvent(newArgs);
                }
            }
        }, RoutingStrategies.Tunnel);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}