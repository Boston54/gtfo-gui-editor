namespace GTFO_GUIEditor.Models;

public class TerminalOption
{
    public int TerminalIndex { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public TerminalOption() { }

    public TerminalOption(int terminalIndex, string displayName)
    {
        TerminalIndex = terminalIndex;
        DisplayName = displayName;
    }
}
