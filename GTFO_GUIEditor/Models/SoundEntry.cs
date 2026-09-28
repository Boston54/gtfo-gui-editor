namespace GTFO_GUIEditor.Models;

public class SoundEntry
{
    public string Event { get; set; } = string.Empty;
    public uint Id { get; set; }
    public string IdString { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WwisePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string RawLine { get; set; } = string.Empty;
}
