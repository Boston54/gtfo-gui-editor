using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class GeomorphMetadata
{
    [JsonPropertyName("areaDatas")]
    public List<AreaData> AreaDatas { get; set; } = new();

    [JsonPropertyName("geoAssetLink")]
    public string GeoAssetLink { get; set; } = string.Empty;
}

public class AreaData
{
    [JsonPropertyName("expanderDatas")]
    public List<ExpanderData> ExpanderDatas { get; set; } = new();
}

public class ExpanderData
{
    [JsonPropertyName("areaTo")]
    public int AreaTo { get; set; } = -1;

    [JsonPropertyName("y_angle")]
    public double YAngle { get; set; }

    [JsonPropertyName("expanderPosition")]
    public ExpanderPosition ExpanderPosition { get; set; } = new();

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;
}

public class ExpanderPosition
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
}
