namespace GTFO_GUIEditor.Models;

public class ObjectiveDimensionOption
{
    public int DimensionIndex { get; }
    public string DisplayName { get; }

    public ObjectiveDimensionOption(int dimensionIndex, string displayName)
    {
        DimensionIndex = dimensionIndex;
        DisplayName = displayName;
    }
}
