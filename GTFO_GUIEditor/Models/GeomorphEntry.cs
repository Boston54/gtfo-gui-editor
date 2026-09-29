using System;
using Avalonia.Media.Imaging;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class GeomorphEntry
{
    private Bitmap? _imageBitmap;
    private bool _imageAttempted;

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PrefabName { get; set; } = string.Empty;
    public string PrefabPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string Complex { get; set; } = string.Empty;
    public int ComplexId { get; set; }
    public string SubComplex { get; set; } = string.Empty;
    public string SubComplexKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int VariantIndex { get; set; } = 1;
    public int VariantCount { get; set; } = 1;
    public string? ImagePath { get; set; }

    public Bitmap? ImageBitmap
    {
        get
        {
            if (_imageBitmap == null && !_imageAttempted && !string.IsNullOrWhiteSpace(ImageName))
            {
                _imageAttempted = true;
                _imageBitmap = GeomorphService.LoadImageBitmap(ImageName);
                if (_imageBitmap != null && ImagePath == null)
                {
                    ImagePath = GeomorphService.FindImageFile(ImageName);
                }
            }
            return _imageBitmap;
        }
        set
        {
            _imageBitmap = value;
            _imageAttempted = true;
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageName);
    public bool HasImageBitmap => ImageBitmap != null;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public override string ToString() => $"{Name} ({PrefabName})";
}
