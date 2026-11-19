namespace GTFO_GUIEditor.Models;

public class ObjectiveWinConditionOption
{
    public WinCondition Value { get; }
    public string DisplayName { get; }

    public ObjectiveWinConditionOption(WinCondition value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}
