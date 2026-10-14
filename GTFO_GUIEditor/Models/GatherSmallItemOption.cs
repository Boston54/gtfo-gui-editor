namespace GTFO_GUIEditor.Models;

public class GatherSmallItemOption
{
    public GatherSmallItemType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int ItemId => (int)Type;

    public GatherSmallItemOption(GatherSmallItemType type, string displayName)
    {
        Type = type;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
