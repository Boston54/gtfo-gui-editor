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
    private static readonly (Color Fill, Color Stroke)[] ZoneColorPalette = new[]
    {
        (Color.FromArgb(200, 35, 50, 70), Color.FromArgb(240, 70, 100, 140)),   // Zone 0 - Slate Blue
        (Color.FromArgb(200, 30, 65, 95), Color.FromArgb(240, 60, 130, 190)),   // Zone 1 - Blue
        (Color.FromArgb(200, 65, 35, 85), Color.FromArgb(240, 130, 70, 170)),   // Zone 2 - Purple
        (Color.FromArgb(200, 30, 70, 55), Color.FromArgb(240, 60, 140, 110)),   // Zone 3 - Teal/Green
        (Color.FromArgb(200, 80, 55, 30), Color.FromArgb(240, 160, 110, 60)),   // Zone 4 - Amber/Orange
        (Color.FromArgb(200, 80, 30, 45), Color.FromArgb(240, 160, 60, 90)),    // Zone 5 - Crimson/Rose
        (Color.FromArgb(200, 50, 65, 40), Color.FromArgb(240, 100, 130, 80)),   // Zone 6 - Olive/Forest
        (Color.FromArgb(200, 45, 60, 80), Color.FromArgb(240, 90, 120, 160)),   // Zone 7 - Steel
    };

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

    public static (string SvgContent, string SvgPathData, Geometry? Geometry) LoadRoomSvg(MeshData? mesh, int zone = 0, double rotationAngle = 0.0)
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

        var (fill, stroke) = GetZoneColors(zone);
        string svgContent = GenerateSvgContent(mesh, pathData, fill, stroke: "none", strokeWidth: 0.0, rotationAngle: rotationAngle);
        var geometry = CreateGeometry(mesh, rotationAngle);

        return (svgContent, pathData, geometry);
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
