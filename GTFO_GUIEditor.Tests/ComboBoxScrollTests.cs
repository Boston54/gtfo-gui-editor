using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ComboBoxScrollTests
{
    static ComboBoxScrollTests()
    {
        // Ensure App initialization logic has run
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

    [Fact]
    public void ClosedComboBox_PointerWheel_DoesNotChangeSelection()
    {
        var comboBox = new ComboBox
        {
            ItemsSource = new List<string> { "Option 1", "Option 2", "Option 3" },
            SelectedIndex = 0
        };

        var pointer = new Pointer(1, PointerType.Mouse, true);
        var pointerProperties = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other);
        var delta = new Vector(0, -1);

        var eventArgs = new PointerWheelEventArgs(
            comboBox,
            pointer,
            comboBox,
            new Point(0, 0),
            0,
            pointerProperties,
            KeyModifiers.None,
            delta);

        comboBox.RaiseEvent(eventArgs);

        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.True(eventArgs.Handled);
    }
}
