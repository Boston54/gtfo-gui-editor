using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class ColorData : INotifyPropertyChanged
{
    private double _a = 1.0;
    private double _r = 1.0;
    private double _g = 1.0;
    private double _b = 1.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ColorData()
    {
    }

    public ColorData(double a, double r, double g, double b)
    {
        _a = Math.Clamp(a, 0.0, 1.0);
        _r = Math.Clamp(r, 0.0, 1.0);
        _g = Math.Clamp(g, 0.0, 1.0);
        _b = Math.Clamp(b, 0.0, 1.0);
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
            if (byte.TryParse(clean.Substring(0, 2), NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, null, out byte b))
            {
                SetArgb(1.0, r / 255.0, g / 255.0, b / 255.0);
                return true;
            }
        }
        else if (clean.Length == 8)
        {
            if (byte.TryParse(clean.Substring(0, 2), NumberStyles.HexNumber, null, out byte a) &&
                byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(clean.Substring(6, 2), NumberStyles.HexNumber, null, out byte b))
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
    }

    public ColorData Clone()
    {
        return new ColorData
        {
            _a = _a,
            _r = _r,
            _g = _g,
            _b = _b
        };
    }

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

public class TierVisuals : INotifyPropertyChanged
{
    private ColorData _color = new();
    private double _scale = 0.65;
    private double _scaleYModifier = 0.3;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TierVisuals()
    {
        _color.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(ColorHex));
        };
    }

    [JsonPropertyName("Color")]
    public ColorData Color
    {
        get => _color;
        set
        {
            var val = value ?? new ColorData();
            if (_color != null)
            {
                _color.PropertyChanged -= Color_PropertyChanged;
            }
            _color = val;
            _color.PropertyChanged += Color_PropertyChanged;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorHex));
        }
    }

    [JsonPropertyName("Scale")]
    public double Scale
    {
        get => _scale;
        set => SetField(ref _scale, value);
    }

    [JsonPropertyName("ScaleYModifier")]
    public double ScaleYModifier
    {
        get => _scaleYModifier;
        set => SetField(ref _scaleYModifier, value);
    }

    [JsonIgnore]
    public string ColorHex => _color?.HexCode ?? "#FFFFFFFF";

    private void Color_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(ColorHex));
    }

    public TierVisuals Clone()
    {
        return new TierVisuals
        {
            Color = Color.Clone(),
            Scale = Scale,
            ScaleYModifier = ScaleYModifier
        };
    }

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

public class RundownVisuals : INotifyPropertyChanged
{
    private ColorData _colorBackground = new(1.0, 0.0, 0.0, 0.0);
    private TierVisuals _tierAVisuals = new() { Color = new ColorData(1.0, 0.97, 0.88, 0.79), Scale = 0.65, ScaleYModifier = 0.3 };
    private TierVisuals _tierBVisuals = new() { Color = new ColorData(1.0, 0.91, 0.77, 0.66), Scale = 0.65, ScaleYModifier = 0.3 };
    private TierVisuals _tierCVisuals = new() { Color = new ColorData(1.0, 0.81, 0.66, 0.54), Scale = 0.65, ScaleYModifier = 0.3 };
    private TierVisuals _tierDVisuals = new() { Color = new ColorData(1.0, 0.68, 0.49, 0.32), Scale = 0.65, ScaleYModifier = 0.3 };
    private TierVisuals _tierEVisuals = new() { Color = new ColorData(1.0, 0.68, 0.41, 0.18), Scale = 0.65, ScaleYModifier = 0.3 };

    public event PropertyChangedEventHandler? PropertyChanged;

    public RundownVisuals()
    {
        HookVisualEvents();
    }

    private void HookVisualEvents()
    {
        _colorBackground.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ColorBackground));
        _tierAVisuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TierAVisuals));
        _tierBVisuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TierBVisuals));
        _tierCVisuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TierCVisuals));
        _tierDVisuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TierDVisuals));
        _tierEVisuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TierEVisuals));
    }

    [JsonPropertyName("ColorBackground")]
    public ColorData ColorBackground
    {
        get => _colorBackground;
        set
        {
            var val = value ?? new ColorData(1.0, 0, 0, 0);
            if (_colorBackground != null)
            {
                _colorBackground.PropertyChanged -= ColorBackground_PropertyChanged;
            }
            _colorBackground = val;
            _colorBackground.PropertyChanged += ColorBackground_PropertyChanged;
            OnPropertyChanged();
        }
    }

    private void ColorBackground_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ColorBackground));
    }

    [JsonPropertyName("TierAVisuals")]
    public TierVisuals TierAVisuals
    {
        get => _tierAVisuals;
        set
        {
            var val = value ?? new TierVisuals();
            if (_tierAVisuals != null)
            {
                _tierAVisuals.PropertyChanged -= TierAVisuals_PropertyChanged;
            }
            _tierAVisuals = val;
            _tierAVisuals.PropertyChanged += TierAVisuals_PropertyChanged;
            OnPropertyChanged();
        }
    }

    private void TierAVisuals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TierAVisuals));
    }

    [JsonPropertyName("TierBVisuals")]
    public TierVisuals TierBVisuals
    {
        get => _tierBVisuals;
        set
        {
            var val = value ?? new TierVisuals();
            if (_tierBVisuals != null)
            {
                _tierBVisuals.PropertyChanged -= TierBVisuals_PropertyChanged;
            }
            _tierBVisuals = val;
            _tierBVisuals.PropertyChanged += TierBVisuals_PropertyChanged;
            OnPropertyChanged();
        }
    }

    private void TierBVisuals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TierBVisuals));
    }

    [JsonPropertyName("TierCVisuals")]
    public TierVisuals TierCVisuals
    {
        get => _tierCVisuals;
        set
        {
            var val = value ?? new TierVisuals();
            if (_tierCVisuals != null)
            {
                _tierCVisuals.PropertyChanged -= TierCVisuals_PropertyChanged;
            }
            _tierCVisuals = val;
            _tierCVisuals.PropertyChanged += TierCVisuals_PropertyChanged;
            OnPropertyChanged();
        }
    }

    private void TierCVisuals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TierCVisuals));
    }

    [JsonPropertyName("TierDVisuals")]
    public TierVisuals TierDVisuals
    {
        get => _tierDVisuals;
        set
        {
            var val = value ?? new TierVisuals();
            if (_tierDVisuals != null)
            {
                _tierDVisuals.PropertyChanged -= TierDVisuals_PropertyChanged;
            }
            _tierDVisuals = val;
            _tierDVisuals.PropertyChanged += TierDVisuals_PropertyChanged;
            OnPropertyChanged();
        }
    }

    private void TierDVisuals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TierDVisuals));
    }

    [JsonPropertyName("TierEVisuals")]
    public TierVisuals TierEVisuals
    {
        get => _tierEVisuals;
        set
        {
            var val = value ?? new TierVisuals();
            if (_tierEVisuals != null)
            {
                _tierEVisuals.PropertyChanged -= TierEVisuals_PropertyChanged;
            }
            _tierEVisuals = val;
            _tierEVisuals.PropertyChanged += TierEVisuals_PropertyChanged;
            OnPropertyChanged();
        }
    }

    private void TierEVisuals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TierEVisuals));
    }

    public TierVisuals GetTierVisuals(string tier)
    {
        return tier.ToUpperInvariant() switch
        {
            "A" or "TIERA" => TierAVisuals,
            "B" or "TIERB" => TierBVisuals,
            "C" or "TIERC" => TierCVisuals,
            "D" or "TIERD" => TierDVisuals,
            "E" or "TIERE" => TierEVisuals,
            _ => TierAVisuals
        };
    }

    public RundownVisuals Clone()
    {
        return new RundownVisuals
        {
            ColorBackground = ColorBackground.Clone(),
            TierAVisuals = TierAVisuals.Clone(),
            TierBVisuals = TierBVisuals.Clone(),
            TierCVisuals = TierCVisuals.Clone(),
            TierDVisuals = TierDVisuals.Clone(),
            TierEVisuals = TierEVisuals.Clone()
        };
    }

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

public class Vector2Data : INotifyPropertyChanged
{
    private double _x;
    private double _y;

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

    public Vector2Data Clone()
    {
        return new Vector2Data { X = X, Y = Y };
    }

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

public class TierRequirements : INotifyPropertyChanged
{
    private int _mainSectors;
    private int _secondarySectors;
    private int _thirdSectors;
    private int _allClearedSectors;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("MainSectors")]
    public int MainSectors
    {
        get => _mainSectors;
        set
        {
            if (SetField(ref _mainSectors, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("SecondarySectors")]
    public int SecondarySectors
    {
        get => _secondarySectors;
        set
        {
            if (SetField(ref _secondarySectors, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("ThirdSectors")]
    public int ThirdSectors
    {
        get => _thirdSectors;
        set
        {
            if (SetField(ref _thirdSectors, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("AllClearedSectors")]
    public int AllClearedSectors
    {
        get => _allClearedSectors;
        set
        {
            if (SetField(ref _allClearedSectors, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonIgnore]
    public string DisplayText => $"Main: {MainSectors}, Sec: {SecondarySectors}, Overload: {ThirdSectors}, All: {AllClearedSectors}";

    [JsonIgnore]
    public bool HasAnyRequirement => MainSectors > 0 || SecondarySectors > 0 || ThirdSectors > 0 || AllClearedSectors > 0;

    public TierRequirements Clone()
    {
        return new TierRequirements
        {
            MainSectors = MainSectors,
            SecondarySectors = SecondarySectors,
            ThirdSectors = ThirdSectors,
            AllClearedSectors = AllClearedSectors
        };
    }

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

public class StorytellingData : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private uint _textLog;
    private Vector2Data _textLogPos = new();
    private RundownVisuals _visuals = new();
    private Vector2Data _surfaceIconPosition = new();
    private string _surfaceDescription = string.Empty;
    private uint _externalExpTitle;

    public event PropertyChangedEventHandler? PropertyChanged;

    public StorytellingData()
    {
        _visuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Visuals));
    }

    [JsonPropertyName("Title")]
    [JsonConverter(typeof(FlexibleTextConverter))]
    public string Title
    {
        get => _title;
        set => SetField(ref _title, value ?? string.Empty);
    }

    [JsonPropertyName("TextLog")]
    public uint TextLog
    {
        get => _textLog;
        set => SetField(ref _textLog, value);
    }

    [JsonPropertyName("TextLogPos")]
    public Vector2Data TextLogPos
    {
        get => _textLogPos;
        set => SetField(ref _textLogPos, value ?? new Vector2Data());
    }

    [JsonPropertyName("Visuals")]
    public RundownVisuals Visuals
    {
        get => _visuals;
        set
        {
            if (SetField(ref _visuals, value ?? new RundownVisuals()))
            {
                _visuals.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Visuals));
            }
        }
    }

    [JsonPropertyName("SurfaceIconPosition")]
    public Vector2Data SurfaceIconPosition
    {
        get => _surfaceIconPosition;
        set => SetField(ref _surfaceIconPosition, value ?? new Vector2Data());
    }

    [JsonPropertyName("SurfaceDescription")]
    [JsonConverter(typeof(FlexibleTextConverter))]
    public string SurfaceDescription
    {
        get => _surfaceDescription;
        set => SetField(ref _surfaceDescription, value ?? string.Empty);
    }

    [JsonPropertyName("ExternalExpTitle")]
    public uint ExternalExpTitle
    {
        get => _externalExpTitle;
        set => SetField(ref _externalExpTitle, value);
    }

    public StorytellingData Clone()
    {
        return new StorytellingData
        {
            Title = Title,
            TextLog = TextLog,
            TextLogPos = TextLogPos.Clone(),
            Visuals = Visuals.Clone(),
            SurfaceIconPosition = SurfaceIconPosition.Clone(),
            SurfaceDescription = SurfaceDescription,
            ExternalExpTitle = ExternalExpTitle
        };
    }

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

public class UnlockedByExpeditionData : INotifyPropertyChanged
{
    private string _tier = "TierA";
    private string _exp = "Exp1";

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("Tier")]
    public string Tier
    {
        get => _tier;
        set => SetField(ref _tier, value);
    }

    [JsonPropertyName("Exp")]
    public string Exp
    {
        get => _exp;
        set => SetField(ref _exp, value);
    }

    public UnlockedByExpeditionData Clone() => new() { Tier = Tier, Exp = Exp };

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

public class ExpeditionDescriptiveData : INotifyPropertyChanged
{
    private string _prefix = "A";
    private string _publicName = "Level";
    private bool _isExtraExpedition;
    private bool _skipExpNumberInName;
    private bool _useCustomMatchmakingTier;
    private string _customMatchmakingTier = "TierA";
    private string _progressionVisualStyle = "Normal";
    private int _expeditionDepth;
    private int _estimatedDuration = 900;
    private string _expeditionDescription = string.Empty;
    private string _roleplayedWardenIntel = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("Prefix")]
    public string Prefix
    {
        get => _prefix;
        set
        {
            if (SetField(ref _prefix, value))
            {
                OnPropertyChanged(nameof(FullDisplayName));
            }
        }
    }

    [JsonPropertyName("PublicName")]
    public string PublicName
    {
        get => _publicName;
        set
        {
            if (SetField(ref _publicName, value))
            {
                OnPropertyChanged(nameof(FullDisplayName));
            }
        }
    }

    [JsonPropertyName("IsExtraExpedition")]
    public bool IsExtraExpedition
    {
        get => _isExtraExpedition;
        set => SetField(ref _isExtraExpedition, value);
    }

    [JsonPropertyName("SkipExpNumberInName")]
    public bool SkipExpNumberInName
    {
        get => _skipExpNumberInName;
        set => SetField(ref _skipExpNumberInName, value);
    }

    [JsonPropertyName("UseCustomMatchmakingTier")]
    public bool UseCustomMatchmakingTier
    {
        get => _useCustomMatchmakingTier;
        set => SetField(ref _useCustomMatchmakingTier, value);
    }

    [JsonPropertyName("CustomMatchmakingTier")]
    public string CustomMatchmakingTier
    {
        get => _customMatchmakingTier;
        set => SetField(ref _customMatchmakingTier, value);
    }

    [JsonPropertyName("ProgressionVisualStyle")]
    public string ProgressionVisualStyle
    {
        get => _progressionVisualStyle;
        set
        {
            if (SetField(ref _progressionVisualStyle, value))
            {
                OnPropertyChanged(nameof(IsStory));
            }
        }
    }

    [JsonIgnore]
    public bool IsStory => string.Equals(ProgressionVisualStyle, "Story", StringComparison.OrdinalIgnoreCase) || ProgressionVisualStyle == "1";

    [JsonPropertyName("ExpeditionDepth")]
    public int ExpeditionDepth
    {
        get => _expeditionDepth;
        set => SetField(ref _expeditionDepth, value);
    }

    [JsonPropertyName("EstimatedDuration")]
    public int EstimatedDuration
    {
        get => _estimatedDuration;
        set => SetField(ref _estimatedDuration, value);
    }

    [JsonPropertyName("ExpeditionDescription")]
    [JsonConverter(typeof(FlexibleTextConverter))]
    public string ExpeditionDescription
    {
        get => _expeditionDescription;
        set => SetField(ref _expeditionDescription, value ?? string.Empty);
    }

    [JsonPropertyName("ExpeditionDescriptive")]
    [JsonConverter(typeof(FlexibleTextConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpeditionDescriptiveAlias
    {
        get => null;
        set
        {
            if (!string.IsNullOrEmpty(value) && string.IsNullOrEmpty(_expeditionDescription))
            {
                ExpeditionDescription = value;
            }
        }
    }

    [JsonPropertyName("RoleplayedWardenIntel")]
    [JsonConverter(typeof(FlexibleTextConverter))]
    public string RoleplayedWardenIntel
    {
        get => _roleplayedWardenIntel;
        set => SetField(ref _roleplayedWardenIntel, value ?? string.Empty);
    }

    [JsonIgnore]
    public string FullDisplayName => string.IsNullOrWhiteSpace(PublicName) ? Prefix : $"{Prefix} - {PublicName}";

    public ExpeditionDescriptiveData Clone()
    {
        return new ExpeditionDescriptiveData
        {
            Prefix = Prefix,
            PublicName = PublicName,
            IsExtraExpedition = IsExtraExpedition,
            SkipExpNumberInName = SkipExpNumberInName,
            UseCustomMatchmakingTier = UseCustomMatchmakingTier,
            CustomMatchmakingTier = CustomMatchmakingTier,
            ProgressionVisualStyle = ProgressionVisualStyle,
            ExpeditionDepth = ExpeditionDepth,
            EstimatedDuration = EstimatedDuration,
            ExpeditionDescription = ExpeditionDescription,
            RoleplayedWardenIntel = RoleplayedWardenIntel
        };
    }

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

public class ExpeditionObjectiveData : INotifyPropertyChanged
{
    private uint _dataBlockId;
    private string _winCondition = "GoToElevator";

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("DataBlockId")]
    public uint DataBlockId
    {
        get => _dataBlockId;
        set => SetField(ref _dataBlockId, value);
    }

    [JsonPropertyName("WinCondition")]
    public string WinCondition
    {
        get => _winCondition;
        set => SetField(ref _winCondition, value);
    }

    [JsonPropertyName("ZonePlacementDatas")]
    public JsonElement? ZonePlacementDatas { get; set; }

    public ExpeditionObjectiveData Clone()
    {
        return new ExpeditionObjectiveData
        {
            DataBlockId = DataBlockId,
            WinCondition = WinCondition,
            ZonePlacementDatas = ZonePlacementDatas
        };
    }

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

public class ExpeditionLayerData : INotifyPropertyChanged
{
    private ExpeditionObjectiveData _objectiveData = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ExpeditionLayerData()
    {
        _objectiveData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ObjectiveData));
    }

    [JsonPropertyName("ZonesWithBulkheadEntrance")]
    public JsonElement? ZonesWithBulkheadEntrance { get; set; }

    [JsonPropertyName("BulkheadDoorControllerPlacements")]
    public JsonElement? BulkheadDoorControllerPlacements { get; set; }

    [JsonPropertyName("BulkheadKeyPlacements")]
    public JsonElement? BulkheadKeyPlacements { get; set; }

    [JsonPropertyName("ObjectiveData")]
    public ExpeditionObjectiveData ObjectiveData
    {
        get => _objectiveData;
        set
        {
            if (SetField(ref _objectiveData, value ?? new ExpeditionObjectiveData()))
            {
                _objectiveData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ObjectiveData));
            }
        }
    }

    [JsonPropertyName("ChainedObjectiveData")]
    public JsonElement? ChainedObjectiveData { get; set; }

    [JsonPropertyName("ArtifactData")]
    public JsonElement? ArtifactData { get; set; }

    public ExpeditionLayerData Clone()
    {
        return new ExpeditionLayerData
        {
            ZonesWithBulkheadEntrance = ZonesWithBulkheadEntrance,
            BulkheadDoorControllerPlacements = BulkheadDoorControllerPlacements,
            BulkheadKeyPlacements = BulkheadKeyPlacements,
            ObjectiveData = ObjectiveData.Clone(),
            ChainedObjectiveData = ChainedObjectiveData,
            ArtifactData = ArtifactData
        };
    }

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

public class BuildFromData
{
    [JsonPropertyName("LayerType")]
    public string LayerType { get; set; } = "MainLayer";

    [JsonPropertyName("Zone")]
    public string Zone { get; set; } = "Zone_0";

    public BuildFromData Clone() => new() { LayerType = LayerType, Zone = Zone };
}

public class ExpeditionSeeds
{
    [JsonPropertyName("BuildSeed")]
    public int BuildSeed { get; set; }

    [JsonPropertyName("FunctionMarkerOffset")]
    public int FunctionMarkerOffset { get; set; }

    [JsonPropertyName("StandardMarkerOffset")]
    public int StandardMarkerOffset { get; set; }

    [JsonPropertyName("LightJobSeedOffset")]
    public int LightJobSeedOffset { get; set; }

    public ExpeditionSeeds Clone() => new()
    {
        BuildSeed = BuildSeed,
        FunctionMarkerOffset = FunctionMarkerOffset,
        StandardMarkerOffset = StandardMarkerOffset,
        LightJobSeedOffset = LightJobSeedOffset
    };
}

public class ExpeditionSettings
{
    [JsonPropertyName("ComplexResourceData")]
    public uint ComplexResourceData { get; set; }

    [JsonPropertyName("MLSLevelKit")]
    public uint MLSLevelKit { get; set; }

    [JsonPropertyName("LightSettings")]
    public uint LightSettings { get; set; }

    [JsonPropertyName("FogSettings")]
    public uint FogSettings { get; set; }

    [JsonPropertyName("EnemyPopulation")]
    public uint EnemyPopulation { get; set; }

    [JsonPropertyName("ExpeditionBalance")]
    public uint ExpeditionBalance { get; set; }

    [JsonPropertyName("ScoutWaveSettings")]
    public uint ScoutWaveSettings { get; set; }

    [JsonPropertyName("ScoutWavePopulation")]
    public uint ScoutWavePopulation { get; set; }

    [JsonPropertyName("EnvironmentWetness")]
    public double EnvironmentWetness { get; set; }

    [JsonPropertyName("DustColor")]
    public ColorData DustColor { get; set; } = new(1.0, 0.5, 0.5, 0.5);

    [JsonPropertyName("DustTurbulence")]
    public double DustTurbulence { get; set; } = 1.0;

    public ExpeditionSettings Clone() => new()
    {
        ComplexResourceData = ComplexResourceData,
        MLSLevelKit = MLSLevelKit,
        LightSettings = LightSettings,
        FogSettings = FogSettings,
        EnemyPopulation = EnemyPopulation,
        ExpeditionBalance = ExpeditionBalance,
        ScoutWaveSettings = ScoutWaveSettings,
        ScoutWavePopulation = ScoutWavePopulation,
        EnvironmentWetness = EnvironmentWetness,
        DustColor = DustColor?.Clone() ?? new ColorData(1.0, 0.5, 0.5, 0.5),
        DustTurbulence = DustTurbulence
    };
}

public class SpecialOverrideData
{
    [JsonPropertyName("WeakResourceContainerWithPackChanceForLocked")]
    public double WeakResourceContainerWithPackChanceForLocked { get; set; } = -1.0;

    [JsonPropertyName("InfectionLevelAtExpeditionStart")]
    public double InfectionLevelAtExpeditionStart { get; set; }

    [JsonPropertyName("HealthLevelAtExpeditionStart")]
    public double HealthLevelAtExpeditionStart { get; set; } = 1.0;

    [JsonPropertyName("StandardAmmoAtExpeditionStart")]
    public double StandardAmmoAtExpeditionStart { get; set; } = 1.0;

    [JsonPropertyName("SpecialAmmoAtExpeditionStart")]
    public double SpecialAmmoAtExpeditionStart { get; set; } = 1.0;

    [JsonPropertyName("ToolAmmoAtExpeditionStart")]
    public double ToolAmmoAtExpeditionStart { get; set; } = 1.0;

    [JsonPropertyName("ShowCreditsAfterSuccess")]
    public bool ShowCreditsAfterSuccess { get; set; }

    public SpecialOverrideData Clone() => new()
    {
        WeakResourceContainerWithPackChanceForLocked = WeakResourceContainerWithPackChanceForLocked,
        InfectionLevelAtExpeditionStart = InfectionLevelAtExpeditionStart,
        HealthLevelAtExpeditionStart = HealthLevelAtExpeditionStart,
        StandardAmmoAtExpeditionStart = StandardAmmoAtExpeditionStart,
        SpecialAmmoAtExpeditionStart = SpecialAmmoAtExpeditionStart,
        ToolAmmoAtExpeditionStart = ToolAmmoAtExpeditionStart,
        ShowCreditsAfterSuccess = ShowCreditsAfterSuccess
    };
}

public class ExpeditionBlock : INotifyPropertyChanged
{
    private bool _enabled = true;
    private bool _excludeFromMatchmaking;
    private bool _isSinglePlayer;
    private bool _skipLobby;
    private bool _putIconAboveTier;
    private bool _disablePlayerVoicelines;
    private bool _excludeFromProgression;
    private bool _hideOnLocked;
    private bool _hideRolePlayDataOnLocked;
    private ExpeditionAccessibility _accessibility = ExpeditionAccessibility.Normal;
    private UnlockedByExpeditionData _unlockedByExpedition = new();
    private TierRequirements _customProgressionLock = new();
    private ExpeditionDescriptiveData _descriptive = new();
    private ExpeditionSeeds _seeds = new();
    private ExpeditionSettings _expedition = new();
    private bool _useGearPicker;
    private uint _levelLayoutData;
    private ExpeditionLayerData _mainLayerData = new();
    private bool _secondaryLayerEnabled;
    private uint _secondaryLayout;
    private BuildFromData _buildSecondaryFrom = new();
    private ExpeditionLayerData _secondaryLayerData = new();
    private bool _thirdLayerEnabled;
    private uint _thirdLayout;
    private BuildFromData _buildThirdFrom = new();
    private ExpeditionLayerData _thirdLayerData = new();
    private uint _soundEventOnWarpToReality;
    private SpecialOverrideData _specialOverrideData = new();
    private bool _hasExternalStyle;
    private bool _hasStoryStyle;
    private int _expeditionIndexInTier = 1;
    private string? _tierColorHex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ExpeditionBlock()
    {
        _descriptive.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Descriptive));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(DisplayPrefix));
            OnPropertyChanged(nameof(IsStory));
            OnPropertyChanged(nameof(BorderBrushHex));
        };
        _unlockedByExpedition.PropertyChanged += (s, e) => OnPropertyChanged(nameof(UnlockedByExpedition));
        _mainLayerData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(MainLayerData));
        _secondaryLayerData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SecondaryLayerData));
        _thirdLayerData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ThirdLayerData));
    }

    [JsonPropertyName("Enabled")]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [JsonPropertyName("ExcludeFromMatchmaking")]
    public bool ExcludeFromMatchmaking
    {
        get => _excludeFromMatchmaking;
        set => SetField(ref _excludeFromMatchmaking, value);
    }

    [JsonPropertyName("IsSinglePlayer")]
    public bool IsSinglePlayer
    {
        get => _isSinglePlayer;
        set => SetField(ref _isSinglePlayer, value);
    }

    [JsonPropertyName("SkipLobby")]
    public bool SkipLobby
    {
        get => _skipLobby;
        set => SetField(ref _skipLobby, value);
    }

    [JsonPropertyName("PutIconAboveTier")]
    public bool PutIconAboveTier
    {
        get => _putIconAboveTier;
        set => SetField(ref _putIconAboveTier, value);
    }

    [JsonPropertyName("DisablePlayerVoicelines")]
    public bool DisablePlayerVoicelines
    {
        get => _disablePlayerVoicelines;
        set => SetField(ref _disablePlayerVoicelines, value);
    }

    [JsonPropertyName("ExcludeFromProgression")]
    public bool ExcludeFromProgression
    {
        get => _excludeFromProgression;
        set => SetField(ref _excludeFromProgression, value);
    }

    [JsonPropertyName("HideOnLocked")]
    public bool HideOnLocked
    {
        get => _hideOnLocked;
        set => SetField(ref _hideOnLocked, value);
    }

    [JsonPropertyName("HideRolePlayDataOnLocked")]
    public bool HideRolePlayDataOnLocked
    {
        get => _hideRolePlayDataOnLocked;
        set => SetField(ref _hideRolePlayDataOnLocked, value);
    }

    [JsonPropertyName("Accessibility")]
    public ExpeditionAccessibility Accessibility
    {
        get => _accessibility;
        set
        {
            if (SetField(ref _accessibility, value))
            {
                OnPropertyChanged(nameof(IsUnlockedByExpedition));
            }
        }
    }

    [JsonIgnore]
    public bool IsUnlockedByExpedition => Accessibility == ExpeditionAccessibility.UnlockedByExpedition;

    [JsonPropertyName("UnlockedByExpedition")]
    public UnlockedByExpeditionData UnlockedByExpedition
    {
        get => _unlockedByExpedition;
        set
        {
            if (SetField(ref _unlockedByExpedition, value ?? new UnlockedByExpeditionData()))
            {
                _unlockedByExpedition.PropertyChanged += (s, e) => OnPropertyChanged(nameof(UnlockedByExpedition));
                OnPropertyChanged(nameof(UnlockedByExpedition));
            }
        }
    }

    [JsonPropertyName("CustomProgressionLock")]
    public TierRequirements CustomProgressionLock
    {
        get => _customProgressionLock;
        set => SetField(ref _customProgressionLock, value ?? new TierRequirements());
    }

    [JsonPropertyName("Descriptive")]
    public ExpeditionDescriptiveData Descriptive
    {
        get => _descriptive;
        set
        {
            if (SetField(ref _descriptive, value ?? new ExpeditionDescriptiveData()))
            {
                _descriptive.PropertyChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(Descriptive));
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(DisplayPrefix));
                    OnPropertyChanged(nameof(IsStory));
                    OnPropertyChanged(nameof(BorderBrushHex));
                };
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayPrefix));
                OnPropertyChanged(nameof(IsStory));
                OnPropertyChanged(nameof(BorderBrushHex));
            }
        }
    }

    [JsonPropertyName("Seeds")]
    public ExpeditionSeeds Seeds
    {
        get => _seeds;
        set => SetField(ref _seeds, value ?? new ExpeditionSeeds());
    }

    [JsonPropertyName("Expedition")]
    public ExpeditionSettings Expedition
    {
        get => _expedition;
        set => SetField(ref _expedition, value ?? new ExpeditionSettings());
    }

    [JsonPropertyName("VanityItemsDropData")]
    public JsonElement? VanityItemsDropData { get; set; }

    [JsonPropertyName("UseGearPicker")]
    public bool UseGearPicker
    {
        get => _useGearPicker;
        set => SetField(ref _useGearPicker, value);
    }

    [JsonPropertyName("GearPicker")]
    public JsonElement? GearPicker { get; set; }

    [JsonPropertyName("LevelLayoutData")]
    public uint LevelLayoutData
    {
        get => _levelLayoutData;
        set => SetField(ref _levelLayoutData, value);
    }

    [JsonPropertyName("MainLayerData")]
    public ExpeditionLayerData MainLayerData
    {
        get => _mainLayerData;
        set
        {
            if (SetField(ref _mainLayerData, value ?? new ExpeditionLayerData()))
            {
                _mainLayerData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(MainLayerData));
            }
        }
    }

    [JsonPropertyName("SecondaryLayerEnabled")]
    public bool SecondaryLayerEnabled
    {
        get => _secondaryLayerEnabled;
        set
        {
            if (SetField(ref _secondaryLayerEnabled, value))
            {
                OnPropertyChanged(nameof(HasSecondaryLayer));
            }
        }
    }

    [JsonPropertyName("SecondaryLayout")]
    public uint SecondaryLayout
    {
        get => _secondaryLayout;
        set => SetField(ref _secondaryLayout, value);
    }

    [JsonPropertyName("BuildSecondaryFrom")]
    public BuildFromData BuildSecondaryFrom
    {
        get => _buildSecondaryFrom;
        set => SetField(ref _buildSecondaryFrom, value ?? new BuildFromData());
    }

    [JsonPropertyName("SecondaryLayerData")]
    public ExpeditionLayerData SecondaryLayerData
    {
        get => _secondaryLayerData;
        set
        {
            if (SetField(ref _secondaryLayerData, value ?? new ExpeditionLayerData()))
            {
                _secondaryLayerData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SecondaryLayerData));
            }
        }
    }

    [JsonPropertyName("ThirdLayerEnabled")]
    public bool ThirdLayerEnabled
    {
        get => _thirdLayerEnabled;
        set
        {
            if (SetField(ref _thirdLayerEnabled, value))
            {
                OnPropertyChanged(nameof(HasThirdLayer));
            }
        }
    }

    [JsonPropertyName("ThirdLayout")]
    public uint ThirdLayout
    {
        get => _thirdLayout;
        set => SetField(ref _thirdLayout, value);
    }

    [JsonPropertyName("BuildThirdFrom")]
    public BuildFromData BuildThirdFrom
    {
        get => _buildThirdFrom;
        set => SetField(ref _buildThirdFrom, value ?? new BuildFromData());
    }

    [JsonPropertyName("ThirdLayerData")]
    public ExpeditionLayerData ThirdLayerData
    {
        get => _thirdLayerData;
        set
        {
            if (SetField(ref _thirdLayerData, value ?? new ExpeditionLayerData()))
            {
                _thirdLayerData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(ThirdLayerData));
            }
        }
    }

    [JsonPropertyName("DimensionDatas")]
    public JsonElement? DimensionDatas { get; set; }

    [JsonPropertyName("SoundEventOnWarpToReality")]
    public uint SoundEventOnWarpToReality
    {
        get => _soundEventOnWarpToReality;
        set => SetField(ref _soundEventOnWarpToReality, value);
    }

    [JsonPropertyName("SpecialOverrideData")]
    public SpecialOverrideData SpecialOverrideData
    {
        get => _specialOverrideData;
        set => SetField(ref _specialOverrideData, value ?? new SpecialOverrideData());
    }

    [JsonPropertyName("HasExternalStyle")]
    public bool HasExternalStyle
    {
        get => _hasExternalStyle;
        set => SetField(ref _hasExternalStyle, value);
    }

    [JsonPropertyName("HasStoryStyle")]
    public bool HasStoryStyle
    {
        get => _hasStoryStyle;
        set
        {
            if (SetField(ref _hasStoryStyle, value))
            {
                OnPropertyChanged(nameof(IsStory));
                OnPropertyChanged(nameof(BorderBrushHex));
            }
        }
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    [JsonIgnore]
    public int ExpeditionIndexInTier
    {
        get => _expeditionIndexInTier;
        set
        {
            if (SetField(ref _expeditionIndexInTier, value))
            {
                OnPropertyChanged(nameof(DisplayPrefix));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonIgnore]
    public string? TierColorHex
    {
        get => _tierColorHex;
        set
        {
            if (SetField(ref _tierColorHex, value))
            {
                OnPropertyChanged(nameof(BorderBrushHex));
            }
        }
    }

    [JsonIgnore]
    public bool IsStory => string.Equals(Descriptive?.ProgressionVisualStyle, "Story", StringComparison.OrdinalIgnoreCase)
                           || Descriptive?.ProgressionVisualStyle == "1"
                           || HasStoryStyle;

    [JsonIgnore]
    public string BorderBrushHex => IsStory ? "#E5A93C" : "#303030";

    [JsonIgnore]
    public string DisplayPrefix
    {
        get
        {
            if (Descriptive == null) return string.Empty;
            if (Descriptive.SkipExpNumberInName)
            {
                return Descriptive.Prefix ?? string.Empty;
            }
            int num = ExpeditionIndexInTier > 0 ? ExpeditionIndexInTier : 1;
            return $"{Descriptive.Prefix}{num}";
        }
    }

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Descriptive?.PublicName) ? DisplayPrefix : $"{DisplayPrefix} - \"{Descriptive.PublicName}\"";

    [JsonIgnore]
    public bool HasSecondaryLayer => SecondaryLayerEnabled;

    [JsonIgnore]
    public bool HasThirdLayer => ThirdLayerEnabled;

    public ExpeditionBlock Clone()
    {
        return new ExpeditionBlock
        {
            Enabled = Enabled,
            ExcludeFromMatchmaking = ExcludeFromMatchmaking,
            IsSinglePlayer = IsSinglePlayer,
            SkipLobby = SkipLobby,
            PutIconAboveTier = PutIconAboveTier,
            DisablePlayerVoicelines = DisablePlayerVoicelines,
            ExcludeFromProgression = ExcludeFromProgression,
            HideOnLocked = HideOnLocked,
            HideRolePlayDataOnLocked = HideRolePlayDataOnLocked,
            Accessibility = Accessibility,
            UnlockedByExpedition = UnlockedByExpedition.Clone(),
            CustomProgressionLock = CustomProgressionLock.Clone(),
            Descriptive = Descriptive.Clone(),
            Seeds = Seeds.Clone(),
            Expedition = Expedition.Clone(),
            VanityItemsDropData = VanityItemsDropData,
            UseGearPicker = UseGearPicker,
            GearPicker = GearPicker,
            LevelLayoutData = LevelLayoutData,
            MainLayerData = MainLayerData.Clone(),
            SecondaryLayerEnabled = SecondaryLayerEnabled,
            SecondaryLayout = SecondaryLayout,
            BuildSecondaryFrom = BuildSecondaryFrom.Clone(),
            SecondaryLayerData = SecondaryLayerData.Clone(),
            ThirdLayerEnabled = ThirdLayerEnabled,
            ThirdLayout = ThirdLayout,
            BuildThirdFrom = BuildThirdFrom.Clone(),
            ThirdLayerData = ThirdLayerData.Clone(),
            DimensionDatas = DimensionDatas,
            SoundEventOnWarpToReality = SoundEventOnWarpToReality,
            SpecialOverrideData = SpecialOverrideData.Clone(),
            HasExternalStyle = HasExternalStyle,
            HasStoryStyle = HasStoryStyle,
            ExtensionData = ExtensionData != null ? new Dictionary<string, JsonElement>(ExtensionData) : null
        };
    }

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

public class RundownBlock : INotifyPropertyChanged
{
    private string _name = "New Rundown";
    private bool _internalEnabled = true;
    private uint _persistentId;
    private bool _neverShowRundownTree;
    private bool _useTierUnlockRequirements;
    private TierRequirements _reqToReachTierA = new();
    private TierRequirements _reqToReachTierB = new();
    private TierRequirements _reqToReachTierC = new();
    private TierRequirements _reqToReachTierD = new();
    private TierRequirements _reqToReachTierE = new();
    private StorytellingData _storytellingData = new();
    private uint _vanityItemLayerDropDataBlock;
    private List<ExpeditionBlock> _tierA = new();
    private List<ExpeditionBlock> _tierB = new();
    private List<ExpeditionBlock> _tierC = new();
    private List<ExpeditionBlock> _tierD = new();
    private List<ExpeditionBlock> _tierE = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public RundownBlock()
    {
        _storytellingData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(StorytellingData));
    }

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

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled
    {
        get => _internalEnabled;
        set => SetField(ref _internalEnabled, value);
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

    [JsonPropertyName("NeverShowRundownTree")]
    public bool NeverShowRundownTree
    {
        get => _neverShowRundownTree;
        set => SetField(ref _neverShowRundownTree, value);
    }

    [JsonPropertyName("UseTierUnlockRequirements")]
    public bool UseTierUnlockRequirements
    {
        get => _useTierUnlockRequirements;
        set => SetField(ref _useTierUnlockRequirements, value);
    }

    [JsonPropertyName("ReqToReachTierA")]
    public TierRequirements ReqToReachTierA
    {
        get => _reqToReachTierA;
        set => SetField(ref _reqToReachTierA, value ?? new TierRequirements());
    }

    [JsonPropertyName("ReqToReachTierB")]
    public TierRequirements ReqToReachTierB
    {
        get => _reqToReachTierB;
        set => SetField(ref _reqToReachTierB, value ?? new TierRequirements());
    }

    [JsonPropertyName("ReqToReachTierC")]
    public TierRequirements ReqToReachTierC
    {
        get => _reqToReachTierC;
        set => SetField(ref _reqToReachTierC, value ?? new TierRequirements());
    }

    [JsonPropertyName("ReqToReachTierD")]
    public TierRequirements ReqToReachTierD
    {
        get => _reqToReachTierD;
        set => SetField(ref _reqToReachTierD, value ?? new TierRequirements());
    }

    [JsonPropertyName("ReqToReachTierE")]
    public TierRequirements ReqToReachTierE
    {
        get => _reqToReachTierE;
        set => SetField(ref _reqToReachTierE, value ?? new TierRequirements());
    }

    [JsonPropertyName("StorytellingData")]
    public StorytellingData StorytellingData
    {
        get => _storytellingData;
        set
        {
            if (SetField(ref _storytellingData, value ?? new StorytellingData()))
            {
                _storytellingData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(StorytellingData));
            }
        }
    }

    [JsonPropertyName("VanityItemLayerDropDataBlock")]
    public uint VanityItemLayerDropDataBlock
    {
        get => _vanityItemLayerDropDataBlock;
        set => SetField(ref _vanityItemLayerDropDataBlock, value);
    }

    [JsonPropertyName("TierA")]
    public List<ExpeditionBlock> TierA
    {
        get => _tierA;
        set => SetField(ref _tierA, value ?? new List<ExpeditionBlock>());
    }

    [JsonPropertyName("TierB")]
    public List<ExpeditionBlock> TierB
    {
        get => _tierB;
        set => SetField(ref _tierB, value ?? new List<ExpeditionBlock>());
    }

    [JsonPropertyName("TierC")]
    public List<ExpeditionBlock> TierC
    {
        get => _tierC;
        set => SetField(ref _tierC, value ?? new List<ExpeditionBlock>());
    }

    [JsonPropertyName("TierD")]
    public List<ExpeditionBlock> TierD
    {
        get => _tierD;
        set => SetField(ref _tierD, value ?? new List<ExpeditionBlock>());
    }

    [JsonPropertyName("TierE")]
    public List<ExpeditionBlock> TierE
    {
        get => _tierE;
        set => SetField(ref _tierE, value ?? new List<ExpeditionBlock>());
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    [JsonIgnore]
    public string DisplayText => string.IsNullOrWhiteSpace(Name)
        ? $"[{PersistentId}]"
        : $"{Name} [{PersistentId}]";

    public List<ExpeditionBlock> GetTierList(string tier)
    {
        return tier.ToUpperInvariant() switch
        {
            "A" or "TIERA" => TierA,
            "B" or "TIERB" => TierB,
            "C" or "TIERC" => TierC,
            "D" or "TIERD" => TierD,
            "E" or "TIERE" => TierE,
            _ => TierA
        };
    }

    public TierRequirements GetTierRequirements(string tier)
    {
        return tier.ToUpperInvariant() switch
        {
            "A" or "TIERA" => ReqToReachTierA,
            "B" or "TIERB" => ReqToReachTierB,
            "C" or "TIERC" => ReqToReachTierC,
            "D" or "TIERD" => ReqToReachTierD,
            "E" or "TIERE" => ReqToReachTierE,
            _ => ReqToReachTierA
        };
    }

    public RundownBlock Clone()
    {
        return new RundownBlock
        {
            Name = Name,
            InternalEnabled = InternalEnabled,
            PersistentId = PersistentId,
            NeverShowRundownTree = NeverShowRundownTree,
            UseTierUnlockRequirements = UseTierUnlockRequirements,
            ReqToReachTierA = ReqToReachTierA.Clone(),
            ReqToReachTierB = ReqToReachTierB.Clone(),
            ReqToReachTierC = ReqToReachTierC.Clone(),
            ReqToReachTierD = ReqToReachTierD.Clone(),
            ReqToReachTierE = ReqToReachTierE.Clone(),
            StorytellingData = StorytellingData.Clone(),
            VanityItemLayerDropDataBlock = VanityItemLayerDropDataBlock,
            TierA = TierA.Select(e => e.Clone()).ToList(),
            TierB = TierB.Select(e => e.Clone()).ToList(),
            TierC = TierC.Select(e => e.Clone()).ToList(),
            TierD = TierD.Select(e => e.Clone()).ToList(),
            TierE = TierE.Select(e => e.Clone()).ToList(),
            ExtensionData = ExtensionData != null ? new Dictionary<string, JsonElement>(ExtensionData) : null
        };
    }

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

public class RundownDataBlockFile
{
    [JsonPropertyName("Headers")]
    public List<HeaderItem> Headers { get; set; } = new();

    [JsonPropertyName("Blocks")]
    public List<RundownBlock> Blocks { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public class ExpeditionLookupItem : INotifyPropertyChanged
{
    private string _tier = "TierA";
    private string _exp = "Exp1";
    private string _tierLetter = "A";
    private int _expeditionNumber = 1;
    private string _displayText = string.Empty;
    private ExpeditionBlock? _expedition;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Tier
    {
        get => _tier;
        set => SetField(ref _tier, value);
    }

    public string Exp
    {
        get => _exp;
        set => SetField(ref _exp, value);
    }

    public string TierLetter
    {
        get => _tierLetter;
        set => SetField(ref _tierLetter, value);
    }

    public int ExpeditionNumber
    {
        get => _expeditionNumber;
        set => SetField(ref _expeditionNumber, value);
    }

    public string DisplayText
    {
        get => _displayText;
        set => SetField(ref _displayText, value);
    }

    public ExpeditionBlock? Expedition
    {
        get => _expedition;
        set => SetField(ref _expedition, value);
    }

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
