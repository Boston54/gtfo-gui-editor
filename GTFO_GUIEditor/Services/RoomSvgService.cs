using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class RoomSvgService
{
    private static readonly (Color Fill, Color Stroke, Color ZoneHighlight, Color RoomHover)[] ZoneColorPalette = new[]
    {
        (Color.FromArgb(200, 35, 50, 70), Color.FromArgb(240, 70, 100, 140), Color.FromArgb(230, 55, 80, 115), Color.FromArgb(255, 100, 160, 240)),   // Zone 0 - Slate Blue
        (Color.FromArgb(200, 30, 65, 95), Color.FromArgb(240, 60, 130, 190), Color.FromArgb(230, 50, 105, 155), Color.FromArgb(255, 80, 180, 255)),   // Zone 1 - Blue
        (Color.FromArgb(200, 65, 35, 85), Color.FromArgb(240, 130, 70, 170), Color.FromArgb(230, 105, 55, 140), Color.FromArgb(255, 180, 100, 240)),  // Zone 2 - Purple
        (Color.FromArgb(200, 30, 70, 55), Color.FromArgb(240, 60, 140, 110), Color.FromArgb(230, 50, 115, 90), Color.FromArgb(255, 80, 210, 160)),   // Zone 3 - Teal/Green
        (Color.FromArgb(200, 80, 55, 30), Color.FromArgb(240, 160, 110, 60), Color.FromArgb(230, 135, 90, 45), Color.FromArgb(255, 240, 170, 80)),   // Zone 4 - Amber/Orange
        (Color.FromArgb(200, 80, 30, 45), Color.FromArgb(240, 160, 60, 90), Color.FromArgb(230, 135, 50, 75), Color.FromArgb(255, 240, 90, 130)),    // Zone 5 - Crimson/Rose
        (Color.FromArgb(200, 50, 65, 40), Color.FromArgb(240, 100, 130, 80), Color.FromArgb(230, 85, 110, 65), Color.FromArgb(255, 150, 200, 110)),  // Zone 6 - Olive/Forest
        (Color.FromArgb(200, 45, 60, 80), Color.FromArgb(240, 90, 120, 160), Color.FromArgb(230, 75, 100, 135), Color.FromArgb(255, 140, 190, 255)),  // Zone 7 - Steel
    };

    public static double CalculateMeshHeight(MeshData? mesh)
    {
        if (mesh?.Vertices == null || mesh.Vertices.Count == 0)
        {
            return 0.0;
        }

        double sumY = 0.0;
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            sumY += mesh.Vertices[i].Y;
        }
        return sumY / mesh.Vertices.Count;
    }

    public static Color GetColorForHeight(double totalHeight, double altitudeOffset = 6.0)
    {
        double offset = altitudeOffset > 0 ? altitudeOffset : 6.0;
        double minH = -offset * 1.5;
        double maxH = offset * 1.5;
        double t = Math.Clamp((totalHeight - minH) / (maxH - minH), 0.0, 1.0);

        byte r = (byte)Math.Clamp(Math.Round(24.0 + t * (130.0 - 24.0)), 0, 255);
        byte g = (byte)Math.Clamp(Math.Round(34.0 + t * (165.0 - 34.0)), 0, 255);
        byte b = (byte)Math.Clamp(Math.Round(48.0 + t * (205.0 - 48.0)), 0, 255);
        byte a = 210;

        return Color.FromArgb(a, r, g, b);
    }

    public static Color GetHoverColorForHeight(double totalHeight, double altitudeOffset = 6.0)
    {
        var baseColor = GetColorForHeight(totalHeight, altitudeOffset);
        byte r = (byte)Math.Min(255, baseColor.R + 70);
        byte g = (byte)Math.Min(255, baseColor.G + 70);
        byte b = (byte)Math.Min(255, baseColor.B + 70);
        return Color.FromArgb(255, r, g, b);
    }

    public static Color GetZoneHighlightColorForHeight(double totalHeight, double altitudeOffset = 6.0)
    {
        var baseColor = GetColorForHeight(totalHeight, altitudeOffset);
        byte r = (byte)Math.Min(255, baseColor.R + 35);
        byte g = (byte)Math.Min(255, baseColor.G + 35);
        byte b = (byte)Math.Min(255, baseColor.B + 35);
        return Color.FromArgb(235, r, g, b);
    }

    public static IBrush GetHeightFillBrush(double totalHeight, double altitudeOffset = 6.0)
    {
        return new SolidColorBrush(GetColorForHeight(totalHeight, altitudeOffset));
    }

    public static IBrush GetHoveredHeightFillBrush(double totalHeight, double altitudeOffset = 6.0)
    {
        return new SolidColorBrush(GetHoverColorForHeight(totalHeight, altitudeOffset));
    }

    public static IBrush GetZoneHighlightHeightFillBrush(double totalHeight, double altitudeOffset = 6.0)
    {
        return new SolidColorBrush(GetZoneHighlightColorForHeight(totalHeight, altitudeOffset));
    }

    public static IBrush GetZoneFillBrush(int zone)
    {
        int idx = Math.Abs(zone) % ZoneColorPalette.Length;
        return new SolidColorBrush(ZoneColorPalette[idx].Fill);
    }

    public static IBrush GetZoneStrokeBrush(int zone)
    {
        int idx = Math.Abs(zone) % ZoneColorPalette.Length;
        return new SolidColorBrush(ZoneColorPalette[idx].Stroke);
    }

    public static IBrush GetZoneHighlightFillBrush(int zone)
    {
        int idx = Math.Abs(zone) % ZoneColorPalette.Length;
        return new SolidColorBrush(ZoneColorPalette[idx].ZoneHighlight);
    }

    public static IBrush GetHoveredRoomFillBrush(int zone)
    {
        int idx = Math.Abs(zone) % ZoneColorPalette.Length;
        return new SolidColorBrush(ZoneColorPalette[idx].RoomHover);
    }

    public static IBrush HoverStrokeBrush { get; } = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

    public static (string SvgContent, string SvgPathData, Geometry? Geometry) LoadRoomSvg(MeshData? mesh, double totalHeight = 0.0, double altitudeOffset = 6.0, double rotationAngle = 0.0)
    {
        if (mesh == null || mesh.Vertices == null || mesh.Indices == null || mesh.Vertices.Count == 0 || mesh.Indices.Count < 3)
        {
            return (string.Empty, string.Empty, null);
        }

        string pathData = GenerateSvgPathData(mesh, rotationAngle);
        if (string.IsNullOrWhiteSpace(pathData))
        {
            return (string.Empty, string.Empty, null);
        }

        var fillColor = GetColorForHeight(totalHeight, altitudeOffset);
        string fillHex = $"#{fillColor.R:X2}{fillColor.G:X2}{fillColor.B:X2}";
        string svgContent = GenerateSvgContent(mesh, pathData, fillHex, stroke: "none", strokeWidth: 0.0, rotationAngle: rotationAngle);
        var geometry = CreateGeometry(mesh, rotationAngle);

        return (svgContent, pathData, geometry);
    }

    public static (string SvgContent, string SvgPathData, Geometry? Geometry) LoadRoomSvg(MeshData? mesh, int zone, double rotationAngle = 0.0)
    {
        return LoadRoomSvg(mesh, totalHeight: 0.0, altitudeOffset: 6.0, rotationAngle: rotationAngle);
    }

    public static string GenerateSvgPathData(MeshData? mesh, double rotationAngle = 0.0)
    {
        if (mesh?.Vertices == null || mesh.Indices == null || mesh.Vertices.Count == 0 || mesh.Indices.Count < 3)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        int validTriangles = 0;

        for (int i = 0; i + 2 < mesh.Indices.Count; i += 3)
        {
            int i0 = mesh.Indices[i];
            int i1 = mesh.Indices[i + 1];
            int i2 = mesh.Indices[i + 2];

            if (i0 < 0 || i0 >= mesh.Vertices.Count ||
                i1 < 0 || i1 >= mesh.Vertices.Count ||
                i2 < 0 || i2 >= mesh.Vertices.Count)
            {
                continue;
            }

            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];

            // 2D projection with rotation
            var (x0, y0) = RotatePoint(v0.X, -v0.Z, rotationAngle);
            var (x1, y1) = RotatePoint(v1.X, -v1.Z, rotationAngle);
            var (x2, y2) = RotatePoint(v2.X, -v2.Z, rotationAngle);

            // Enforce consistent Counter-Clockwise (CCW) winding in 2D
            double cross = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);
            if (Math.Abs(cross) < 1e-9)
            {
                continue; // Degenerate triangle
            }

            if (cross < 0)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            sb.Append(CultureInfo.InvariantCulture, $"M {x0:G9},{y0:G9} L {x1:G9},{y1:G9} L {x2:G9},{y2:G9} Z ");
            validTriangles++;
        }

        return validTriangles > 0 ? sb.ToString().TrimEnd() : string.Empty;
    }

    public static string GenerateSvgContent(MeshData? mesh, string? pathData = null, string fill = "#2D3748", string stroke = "none", double strokeWidth = 0.0, double rotationAngle = 0.0)
    {
        if (mesh?.Vertices == null || mesh.Vertices.Count == 0)
        {
            return string.Empty;
        }

        pathData ??= GenerateSvgPathData(mesh, rotationAngle);
        if (string.IsNullOrWhiteSpace(pathData))
        {
            return string.Empty;
        }

        double minX = double.MaxValue;
        double maxX = double.MinValue;
        double minY = double.MaxValue;
        double maxY = double.MinValue;

        foreach (var v in mesh.Vertices)
        {
            var (x, y) = RotatePoint(v.X, -v.Z, rotationAngle);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        double width = Math.Max(0.1, maxX - minX);
        double height = Math.Max(0.1, maxY - minY);

        if (strokeWidth <= 0 || string.Equals(stroke, "none", StringComparison.OrdinalIgnoreCase))
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{minX:G9} {minY:G9} {width:G9} {height:G9}\"><path d=\"{pathData}\" fill=\"{fill}\" fill-rule=\"nonzero\" stroke=\"none\" stroke-width=\"0\" /></svg>");
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{minX:G9} {minY:G9} {width:G9} {height:G9}\"><path d=\"{pathData}\" fill=\"{fill}\" fill-rule=\"nonzero\" stroke=\"{stroke}\" stroke-width=\"{strokeWidth:G9}\" /></svg>");
    }

    public static Geometry? CreateGeometry(MeshData? mesh, double rotationAngle = 0.0)
    {
        if (mesh?.Vertices == null || mesh.Indices == null || mesh.Vertices.Count == 0 || mesh.Indices.Count < 3)
        {
            return null;
        }

        try
        {
            var geometry = new StreamGeometry();
            int validTriangles = 0;

            using (var ctx = geometry.Open())
            {
                ctx.SetFillRule(FillRule.NonZero);

                for (int i = 0; i + 2 < mesh.Indices.Count; i += 3)
                {
                    int i0 = mesh.Indices[i];
                    int i1 = mesh.Indices[i + 1];
                    int i2 = mesh.Indices[i + 2];

                    if (i0 < 0 || i0 >= mesh.Vertices.Count ||
                        i1 < 0 || i1 >= mesh.Vertices.Count ||
                        i2 < 0 || i2 >= mesh.Vertices.Count)
                    {
                        continue;
                    }

                    var v0 = mesh.Vertices[i0];
                    var v1 = mesh.Vertices[i1];
                    var v2 = mesh.Vertices[i2];

                    var (x0, y0) = RotatePoint(v0.X, -v0.Z, rotationAngle);
                    var (x1, y1) = RotatePoint(v1.X, -v1.Z, rotationAngle);
                    var (x2, y2) = RotatePoint(v2.X, -v2.Z, rotationAngle);

                    // Enforce consistent Counter-Clockwise (CCW) winding in 2D
                    double cross = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);
                    if (Math.Abs(cross) < 1e-9)
                    {
                        continue;
                    }

                    if (cross < 0)
                    {
                        (x1, x2) = (x2, x1);
                        (y1, y2) = (y2, y1);
                    }

                    ctx.BeginFigure(new Point(x0, y0), isFilled: true);
                    ctx.LineTo(new Point(x1, y1));
                    ctx.LineTo(new Point(x2, y2));
                    ctx.EndFigure(isClosed: true);
                    validTriangles++;
                }
            }

            return validTriangles > 0 ? geometry : null;
        }
        catch
        {
            return null;
        }
    }

    public static Geometry? GenerateGeomorphGeometry(string? prefabPath, double rotationAngle = 0.0)
    {
        if (string.IsNullOrWhiteSpace(prefabPath)) return null;
        var meta = GeomorphMetadataService.GetMetadata(prefabPath);
        if (meta == null || meta.AreaDatas == null || meta.AreaDatas.Count == 0) return null;

        return GenerateGeomorphGeometry(meta, rotationAngle);
    }

    public static Geometry? GenerateGeomorphGeometry(GeomorphMetadata? metadata, double rotationAngle = 0.0)
    {
        if (metadata?.AreaDatas == null || metadata.AreaDatas.Count == 0) return null;

        var allTriangles = new List<Triangle2D>();
        foreach (var area in metadata.AreaDatas)
        {
            if (area.Mesh == null) continue;
            var (triangles, _, _) = Extract2DTriangles(area.Mesh, rotationAngle);
            if (triangles.Count > 0)
            {
                allTriangles.AddRange(triangles);
            }
        }

        if (allTriangles.Count == 0) return null;

        try
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                for (int i = 0; i < allTriangles.Count; i++)
                {
                    var tri = allTriangles[i];
                    ctx.BeginFigure(new Point(tri.X0, tri.Y0), isFilled: true);
                    ctx.LineTo(new Point(tri.X1, tri.Y1));
                    ctx.LineTo(new Point(tri.X2, tri.Y2));
                    ctx.EndFigure(isClosed: true);
                }
            }

            return geometry;
        }
        catch
        {
            return null;
        }
    }

    public static (List<Triangle2D> Triangles, Rect2D Bounds, double Area) Extract2DTriangles(MeshData? mesh, double rotationAngle = 0.0)
    {
        var triangles = new List<Triangle2D>();
        if (mesh?.Vertices == null || mesh.Indices == null || mesh.Vertices.Count == 0 || mesh.Indices.Count < 3)
        {
            return (triangles, new Rect2D(0, 0, 0, 0), 0.0);
        }

        double minX = double.MaxValue;
        double maxX = double.MinValue;
        double minY = double.MaxValue;
        double maxY = double.MinValue;
        double totalArea = 0.0;

        for (int i = 0; i + 2 < mesh.Indices.Count; i += 3)
        {
            int i0 = mesh.Indices[i];
            int i1 = mesh.Indices[i + 1];
            int i2 = mesh.Indices[i + 2];

            if (i0 < 0 || i0 >= mesh.Vertices.Count ||
                i1 < 0 || i1 >= mesh.Vertices.Count ||
                i2 < 0 || i2 >= mesh.Vertices.Count)
            {
                continue;
            }

            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];

            var (x0, y0) = RotatePoint(v0.X, -v0.Z, rotationAngle);
            var (x1, y1) = RotatePoint(v1.X, -v1.Z, rotationAngle);
            var (x2, y2) = RotatePoint(v2.X, -v2.Z, rotationAngle);

            // Enforce consistent Counter-Clockwise (CCW) winding in 2D
            double cross = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);
            if (Math.Abs(cross) < 1e-9)
            {
                continue; // Degenerate triangle
            }

            if (cross < 0)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            double triArea = 0.5 * Math.Abs(cross);
            totalArea += triArea;

            minX = Math.Min(minX, Math.Min(x0, Math.Min(x1, x2)));
            maxX = Math.Max(maxX, Math.Max(x0, Math.Max(x1, x2)));
            minY = Math.Min(minY, Math.Min(y0, Math.Min(y1, y2)));
            maxY = Math.Max(maxY, Math.Max(y0, Math.Max(y1, y2)));

            triangles.Add(new Triangle2D(x0, y0, x1, y1, x2, y2));
        }

        var bounds = triangles.Count > 0
            ? new Rect2D(minX, minY, maxX, maxY)
            : new Rect2D(0, 0, 0, 0);

        return (triangles, bounds, totalArea);
    }

    public static bool IsPointInTriangle(double px, double py, double x0, double y0, double x1, double y1, double x2, double y2)
    {
        double c1 = (x1 - x0) * (py - y0) - (y1 - y0) * (px - x0);
        double c2 = (x2 - x1) * (py - y1) - (y2 - y1) * (px - x1);
        double c3 = (x0 - x2) * (py - y2) - (y0 - y2) * (px - x2);

        bool hasNeg = (c1 < -1e-6) || (c2 < -1e-6) || (c3 < -1e-6);
        bool hasPos = (c1 > 1e-6) || (c2 > 1e-6) || (c3 > 1e-6);

        return !(hasNeg && hasPos);
    }

    private static (double X, double Y) RotatePoint(double x, double y, double rotationAngle)
    {
        double normAngle = ((rotationAngle % 360) + 360) % 360;
        if (Math.Abs(normAngle - 0) < 0.1) return (x == 0 ? 0 : x, y == 0 ? 0 : y);
        if (Math.Abs(normAngle - 90) < 0.1) return (-y == 0 ? 0 : -y, x == 0 ? 0 : x);
        if (Math.Abs(normAngle - 180) < 0.1) return (-x == 0 ? 0 : -x, -y == 0 ? 0 : -y);
        if (Math.Abs(normAngle - 270) < 0.1) return (y == 0 ? 0 : y, -x == 0 ? 0 : -x);

        double rad = normAngle * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double rx = x * cos - y * sin;
        double ry = x * sin + y * cos;
        return (rx == 0 ? 0 : rx, ry == 0 ? 0 : ry);
    }

    private static (string Fill, string Stroke) GetZoneColors(int zone)
    {
        int idx = Math.Abs(zone) % ZoneColorPalette.Length;
        var pair = ZoneColorPalette[idx];
        return ($"#{pair.Fill.A:X2}{pair.Fill.R:X2}{pair.Fill.G:X2}{pair.Fill.B:X2}",
                $"#{pair.Stroke.A:X2}{pair.Stroke.R:X2}{pair.Stroke.G:X2}{pair.Stroke.B:X2}");
    }
}

public readonly record struct Triangle2D(double X0, double Y0, double X1, double Y1, double X2, double Y2)
{
    public double Area => 0.5 * Math.Abs((X1 - X0) * (Y2 - Y0) - (Y1 - Y0) * (X2 - X0));
}

public readonly record struct Rect2D(double MinX, double MinY, double MaxX, double MaxY);
