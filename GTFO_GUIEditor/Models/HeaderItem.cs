using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class HeaderItem
{
    [JsonPropertyName("AboveBlockID")]
    public uint AboveBlockID { get; set; }

    [JsonPropertyName("LabelText")]
    public string LabelText { get; set; } = string.Empty;

    public HeaderItem Clone()
    {
        return new HeaderItem
        {
            AboveBlockID = AboveBlockID,
            LabelText = LabelText
        };
    }
}
