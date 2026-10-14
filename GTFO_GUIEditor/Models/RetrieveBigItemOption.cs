namespace GTFO_GUIEditor.Models;

public class RetrieveBigItemOption
{
    public RetrieveBigItemType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int ItemId => (int)Type;

    public RetrieveBigItemOption(RetrieveBigItemType type, string displayName)
    {
        Type = type;
        DisplayName = displayName;
    }
}
