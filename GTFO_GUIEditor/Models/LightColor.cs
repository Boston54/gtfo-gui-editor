using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class LightColor : INotifyPropertyChanged
{
    private double _r = 1.0;
    private double _g = 1.0;
    private double _b = 1.0;
    private double _a = 0.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("r")]
    public double R
    {
        get => _r;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetField(ref _r, clamped))
            {
                NotifyDerivedProperties();
            }
        }
    }

    [JsonPropertyName("g")]
    public double G
    {
        get => _g;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetField(ref _g, clamped))
            {
                NotifyDerivedProperties();
            }
        }
    }

    [JsonPropertyName("b")]
    public double B
    {
        get => _b;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetField(ref _b, clamped))
            {
                NotifyDerivedProperties();
            }
        }
    }

    [JsonPropertyName("a")]
    public double A
    {
        get => _a;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetField(ref _a, clamped))
            {
                NotifyDerivedProperties();
            }
        }
    }

    [JsonIgnore]
    public byte AByte
    {
        get => (byte)Math.Clamp((int)Math.Round(_a * 255.0), 0, 255);
        set => A = value / 255.0;
    }

    [JsonIgnore]
    public byte RByte
    {
        get => (byte)Math.Clamp((int)Math.Round(_r * 255.0), 0, 255);
        set => R = value / 255.0;
    }

    [JsonIgnore]
    public byte GByte
    {
        get => (byte)Math.Clamp((int)Math.Round(_g * 255.0), 0, 255);
        set => G = value / 255.0;
    }

    [JsonIgnore]
    public byte BByte
    {
        get => (byte)Math.Clamp((int)Math.Round(_b * 255.0), 0, 255);
        set => B = value / 255.0;
    }

    [JsonIgnore]
    public string HexCode
    {
        get => $"#{AByte:X2}{RByte:X2}{GByte:X2}{BByte:X2}";
        set => SetFromHex(value);
    }

    [JsonIgnore]
    public string HexColor => HexRgb;

    [JsonIgnore]
    public string HexRgb => $"#{RByte:X2}{GByte:X2}{BByte:X2}";

    public void SetArgb(double a, double r, double g, double b)
    {
        _a = Math.Clamp(a, 0.0, 1.0);
        _r = Math.Clamp(r, 0.0, 1.0);
        _g = Math.Clamp(g, 0.0, 1.0);
        _b = Math.Clamp(b, 0.0, 1.0);
        OnPropertyChanged(nameof(A));
        OnPropertyChanged(nameof(R));
        OnPropertyChanged(nameof(G));
        OnPropertyChanged(nameof(B));
        NotifyDerivedProperties();
    }

    public bool SetFromHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        string clean = hex.Trim().TrimStart('#');

        if (clean.Length == 6)
        {
            if (byte.TryParse(clean.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
            {
                SetArgb(_a, r / 255.0, g / 255.0, b / 255.0);
                return true;
            }
        }
        else if (clean.Length == 8)
        {
            if (byte.TryParse(clean.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte a) &&
                byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                byte.TryParse(clean.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
            {
                SetArgb(a / 255.0, r / 255.0, g / 255.0, b / 255.0);
                return true;
            }
        }

        return false;
    }

    private void NotifyDerivedProperties()
    {
        OnPropertyChanged(nameof(AByte));
        OnPropertyChanged(nameof(RByte));
        OnPropertyChanged(nameof(GByte));
        OnPropertyChanged(nameof(BByte));
        OnPropertyChanged(nameof(HexCode));
        OnPropertyChanged(nameof(HexRgb));
        OnPropertyChanged(nameof(HexColor));
    }

    public LightColor Clone()
    {
        return new LightColor
        {
            R = R,
            G = G,
            B = B,
            A = A
        };
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
