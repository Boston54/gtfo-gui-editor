namespace GTFO_GUIEditor.Models;

public class ElevatorTileOption
{
    public string Prefab { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "Elevator Shaft";

    public string DisplayText => string.IsNullOrWhiteSpace(DisplayName)
        ? Prefab
        : $"{DisplayName} ({Prefab})";

    public override string ToString() => DisplayText;
}
