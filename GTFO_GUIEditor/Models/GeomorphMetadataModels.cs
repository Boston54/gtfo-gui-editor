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
    [JsonPropertyName("mesh")]
    public MeshData? Mesh { get; set; }

    [JsonPropertyName("expanderDatas")]
    public List<ExpanderData> ExpanderDatas { get; set; } = new();
}

public class MeshData
{
    [JsonPropertyName("vertexCount")]
    public int VertexCount { get; set; }

    [JsonPropertyName("vertices")]
    public List<VertexPosition> Vertices { get; set; } = new();

    [JsonPropertyName("edgeCount")]
    public int EdgeCount { get; set; }

    [JsonPropertyName("edges")]
    public List<int> Edges { get; set; } = new();

    [JsonPropertyName("indices")]
    public List<int> Indices { get; set; } = new();
}

public class VertexPosition
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
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
