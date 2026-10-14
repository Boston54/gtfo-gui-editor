using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class Vector3Data : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _z;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("x")]
    public double X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    [JsonPropertyName("y")]
    public double Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    [JsonPropertyName("z")]
    public double Z
    {
        get => _z;
        set => SetField(ref _z, value);
    }

    public Vector3Data()
    {
    }

    public Vector3Data(double x, double y, double z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    public Vector3Data Clone() => new()
    {
        X = X,
        Y = Y,
        Z = Z
    };

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class FogSettingsBlock : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private uint _persistentId;
    private bool _internalEnabled = true;
    private bool _enabled = true;
    private LightColor _fogColor = new();
    private double _fogDensity;
    private double _fogAmbience;
    private Vector3Data _densityNoiseDirection = new();
    private double _densityNoiseSpeed;
    private double _densityNoiseScale;
    private double _densityHeightAltitude;
    private double _densityHeightRange;
    private double _densityHeightMaxBoost;
    private double _infection;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("persistentID")]
    public uint PersistentId
    {
        get => _persistentId;
        set
        {
            if (SetField(ref _persistentId, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled
    {
        get => _internalEnabled;
        set => SetField(ref _internalEnabled, value);
    }

    [JsonPropertyName("Enabled")]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [JsonPropertyName("FogColor")]
    public LightColor FogColor
    {
        get => _fogColor;
        set => SetField(ref _fogColor, value ?? new LightColor());
    }

    [JsonPropertyName("FogDensity")]
    public double FogDensity
    {
        get => _fogDensity;
        set => SetField(ref _fogDensity, value);
    }

    [JsonPropertyName("FogAmbience")]
    public double FogAmbience
    {
        get => _fogAmbience;
        set => SetField(ref _fogAmbience, value);
    }

    [JsonPropertyName("DensityNoiseDirection")]
    public Vector3Data DensityNoiseDirection
    {
        get => _densityNoiseDirection;
        set => SetField(ref _densityNoiseDirection, value ?? new Vector3Data());
    }

    [JsonPropertyName("DensityNoiseSpeed")]
    public double DensityNoiseSpeed
    {
        get => _densityNoiseSpeed;
        set => SetField(ref _densityNoiseSpeed, value);
    }

    [JsonPropertyName("DensityNoiseScale")]
    public double DensityNoiseScale
    {
        get => _densityNoiseScale;
        set => SetField(ref _densityNoiseScale, value);
    }

    [JsonPropertyName("DensityHeightAltitude")]
    public double DensityHeightAltitude
    {
        get => _densityHeightAltitude;
        set => SetField(ref _densityHeightAltitude, value);
    }

    [JsonPropertyName("DensityHeightRange")]
    public double DensityHeightRange
    {
        get => _densityHeightRange;
        set => SetField(ref _densityHeightRange, value);
    }

    [JsonPropertyName("DensityHeightMaxBoost")]
    public double DensityHeightMaxBoost
    {
        get => _densityHeightMaxBoost;
        set => SetField(ref _densityHeightMaxBoost, value);
    }

    [JsonPropertyName("Infection")]
    public double Infection
    {
        get => _infection;
        set => SetField(ref _infection, value);
    }

    [JsonIgnore]
    public string DisplayText => $"[{PersistentId}] {Name}";

    public FogSettingsBlock Clone() => new()
    {
        Name = Name,
        PersistentId = PersistentId,
        InternalEnabled = InternalEnabled,
        Enabled = Enabled,
        FogColor = FogColor?.Clone() ?? new LightColor(),
        FogDensity = FogDensity,
        FogAmbience = FogAmbience,
        DensityNoiseDirection = DensityNoiseDirection?.Clone() ?? new Vector3Data(),
        DensityNoiseSpeed = DensityNoiseSpeed,
        DensityNoiseScale = DensityNoiseScale,
        DensityHeightAltitude = DensityHeightAltitude,
        DensityHeightRange = DensityHeightRange,
        DensityHeightMaxBoost = DensityHeightMaxBoost,
        Infection = Infection
    };

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class FogSettingsDataBlockFile
{
    [JsonPropertyName("Headers")]
    public object? Headers { get; set; }

    [JsonPropertyName("Blocks")]
    public List<FogSettingsBlock> Blocks { get; set; } = new();
}

public class FogTransitionOption : INotifyPropertyChanged
{
    private uint _persistentId;
    private string _displayText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public uint PersistentId
    {
        get => _persistentId;
        set => SetField(ref _persistentId, value);
    }

    public string DisplayText
    {
        get => _displayText;
        set => SetField(ref _displayText, value);
    }

    public FogTransitionOption()
    {
    }

    public FogTransitionOption(uint persistentId, string displayText)
    {
        _persistentId = persistentId;
        _displayText = displayText;
    }

    public override string ToString() => DisplayText;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
