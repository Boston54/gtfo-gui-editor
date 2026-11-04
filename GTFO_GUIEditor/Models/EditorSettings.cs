using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Models;

public class EditorSettings : ViewModelBase
{
    private AppearanceSettings _appearance = new();
    private GeneralSettings _general = new();

    public AppearanceSettings Appearance
    {
        get => _appearance;
        set => SetField(ref _appearance, value ?? new AppearanceSettings());
    }

    public GeneralSettings General
    {
        get => _general;
        set => SetField(ref _general, value ?? new GeneralSettings());
    }
}

public enum StartupProjectMode
{
    NewProject,
    PreviousOpenedProject,
    SetProject
}

public enum DataFlowMode
{
    RundownFile,
    Datablocks
}

public enum WindowDisplayMode
{
    Windowed,
    WindowedFullscreen,
    Fullscreen
}

public class GeneralSettings : ViewModelBase
{
    private StartupProjectMode _startupProject = StartupProjectMode.PreviousOpenedProject;
    private string? _startupProjectPath;
    private DataFlowMode _dataFlow = DataFlowMode.RundownFile;
    private WindowDisplayMode _windowDisplayMode = WindowDisplayMode.Windowed;
    private int _autosaveIntervalMinutes = 2;
    private string? _lastOpenedRundownPath;
    private string? _lastOpenedDatablockPath;

    public int AutosaveIntervalMinutes
    {
        get => _autosaveIntervalMinutes;
        set => SetField(ref _autosaveIntervalMinutes, Math.Max(0, value));
    }

    public StartupProjectMode StartupProject
    {
        get => _startupProject;
        set
        {
            if (SetField(ref _startupProject, value))
            {
                OnPropertyChanged(nameof(IsNewProjectStartup));
                OnPropertyChanged(nameof(IsPreviousOpenedProjectStartup));
                OnPropertyChanged(nameof(IsSetProjectStartup));
            }
        }
    }

    public string? StartupProjectPath
    {
        get => _startupProjectPath;
        set => SetField(ref _startupProjectPath, value);
    }

    public DataFlowMode DataFlow
    {
        get => _dataFlow;
        set
        {
            if (SetField(ref _dataFlow, value))
            {
                StartupProjectPath = null;
                OnPropertyChanged(nameof(IsRundownDataFlow));
                OnPropertyChanged(nameof(IsDatablocksDataFlow));
            }
        }
    }

    public string? LastOpenedRundownPath
    {
        get => _lastOpenedRundownPath;
        set => SetField(ref _lastOpenedRundownPath, value);
    }

    public string? LastOpenedDatablockPath
    {
        get => _lastOpenedDatablockPath;
        set => SetField(ref _lastOpenedDatablockPath, value);
    }

    public WindowDisplayMode WindowDisplayMode
    {
        get => _windowDisplayMode;
        set
        {
            if (SetField(ref _windowDisplayMode, value))
            {
                OnPropertyChanged(nameof(IsWindowed));
                OnPropertyChanged(nameof(IsWindowedFullscreen));
                OnPropertyChanged(nameof(IsFullscreen));
            }
        }
    }

    public bool IsNewProjectStartup
    {
        get => StartupProject == StartupProjectMode.NewProject;
        set { if (value) StartupProject = StartupProjectMode.NewProject; }
    }

    public bool IsPreviousOpenedProjectStartup
    {
        get => StartupProject == StartupProjectMode.PreviousOpenedProject;
        set { if (value) StartupProject = StartupProjectMode.PreviousOpenedProject; }
    }

    public bool IsSetProjectStartup
    {
        get => StartupProject == StartupProjectMode.SetProject;
        set { if (value) StartupProject = StartupProjectMode.SetProject; }
    }

    public bool IsRundownDataFlow
    {
        get => DataFlow == DataFlowMode.RundownFile;
        set { if (value) DataFlow = DataFlowMode.RundownFile; }
    }

    public bool IsDatablocksDataFlow
    {
        get => DataFlow == DataFlowMode.Datablocks;
        set { if (value) DataFlow = DataFlowMode.Datablocks; }
    }

    public bool IsWindowed
    {
        get => WindowDisplayMode == WindowDisplayMode.Windowed;
        set { if (value) WindowDisplayMode = WindowDisplayMode.Windowed; }
    }

    public bool IsWindowedFullscreen
    {
        get => WindowDisplayMode == WindowDisplayMode.WindowedFullscreen;
        set { if (value) WindowDisplayMode = WindowDisplayMode.WindowedFullscreen; }
    }

    public bool IsFullscreen
    {
        get => WindowDisplayMode == WindowDisplayMode.Fullscreen;
        set { if (value) WindowDisplayMode = WindowDisplayMode.Fullscreen; }
    }

    public void ResetToDefaults()
    {
        StartupProject = StartupProjectMode.PreviousOpenedProject;
        StartupProjectPath = null;
        DataFlow = DataFlowMode.RundownFile;
        AutosaveIntervalMinutes = 2;
        LastOpenedRundownPath = null;
        LastOpenedDatablockPath = null;
        WindowDisplayMode = WindowDisplayMode.Windowed;
    }
}

public class GradientKeyframe : ViewModelBase
{
    private double _position;
    private string _color = "#182230";
    private bool _isSelected;

    public GradientKeyframe()
    {
    }

    public GradientKeyframe(double position, string color)
    {
        _position = Math.Clamp(position, 0.0, 1.0);
        _color = AppearanceSettings.NormalizeHex(color, "#182230");
    }

    [JsonPropertyName("Position")]
    public double Position
    {
        get => _position;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            SetField(ref _position, clamped);
        }
    }

    [JsonPropertyName("Color")]
    public string ColorHex
    {
        get => _color;
        set
        {
            if (SetField(ref _color, AppearanceSettings.NormalizeHex(value, "#182230")))
            {
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    [JsonIgnore]
    public Color Color
    {
        get => AppearanceSettings.SafeParseColor(ColorHex, Color.FromRgb(24, 34, 48));
        set => ColorHex = AppearanceSettings.ColorToHex(value);
    }

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }
}

public class AppearanceSettings : ViewModelBase
{
    public const string DefaultUnexpandedDoorColor = "#FFFF00";
    public const string DefaultConnectedDoorColor = "#9D00FF";
    public const string DefaultSecurityDoorColor = "#FF0000";
    public const string DefaultMainDoorColor = "#CCCCCC";
    public const string DefaultSecondaryDoorColor = "#FFA200";
    public const string DefaultOverloadDoorColor = "#00F2FF";
    public const string DefaultBlockedDoorColor = "#757575";

    public const string DefaultHeightmapLowColor = "#182230";
    public const string DefaultHeightmapHighColor = "#82A5CD";

    private string _unexpandedDoorColor = DefaultUnexpandedDoorColor;
    private string _connectedDoorColor = DefaultConnectedDoorColor;
    private string _securityDoorColor = DefaultSecurityDoorColor;
    private string _mainDoorColor = DefaultMainDoorColor;
    private string _secondaryDoorColor = DefaultSecondaryDoorColor;
    private string _overloadDoorColor = DefaultOverloadDoorColor;
    private string _blockedDoorColor = DefaultBlockedDoorColor;

    private ObservableCollection<GradientKeyframe> _heightmapGradient = new();
    private GradientKeyframe? _selectedKeyframe;

    public AppearanceSettings()
    {
        InitializeDefaultGradient();
    }

    [JsonIgnore]
    public GradientKeyframe? SelectedKeyframe
    {
        get => _selectedKeyframe;
        set
        {
            if (SetField(ref _selectedKeyframe, value))
            {
                OnPropertyChanged(nameof(HasSelectedKeyframe));
                UpdateKeyframeSelectionStates();
            }
        }
    }

    [JsonIgnore]
    public bool HasSelectedKeyframe => _selectedKeyframe != null;

    private void UpdateKeyframeSelectionStates()
    {
        if (_heightmapGradient == null) return;
        foreach (var kf in _heightmapGradient)
        {
            kf.IsSelected = (kf == _selectedKeyframe);
        }
    }

    public ObservableCollection<GradientKeyframe> HeightmapGradient
    {
        get => _heightmapGradient;
        set
        {
            if (_heightmapGradient != null)
            {
                _heightmapGradient.CollectionChanged -= OnGradientCollectionChanged;
                foreach (var kf in _heightmapGradient)
                {
                    kf.PropertyChanged -= OnKeyframePropertyChanged;
                }
            }

            _heightmapGradient = value ?? new ObservableCollection<GradientKeyframe>();

            if (_heightmapGradient.Count == 0)
            {
                InitializeDefaultGradient();
                return;
            }

            _heightmapGradient.CollectionChanged += OnGradientCollectionChanged;
            foreach (var kf in _heightmapGradient)
            {
                kf.PropertyChanged += OnKeyframePropertyChanged;
            }

            if (_selectedKeyframe == null || !_heightmapGradient.Contains(_selectedKeyframe))
            {
                SelectedKeyframe = _heightmapGradient.FirstOrDefault();
            }
            else
            {
                UpdateKeyframeSelectionStates();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(HeightmapGradientBrush));
            OnPropertyChanged(nameof(CanRemoveKeyframes));
        }
    }

    [JsonIgnore]
    public bool CanRemoveKeyframes => _heightmapGradient != null && _heightmapGradient.Count > 2;

    [JsonIgnore]
    public IBrush HeightmapGradientBrush
    {
        get
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative)
            };

            if (_heightmapGradient == null || _heightmapGradient.Count == 0)
            {
                brush.GradientStops.Add(new GradientStop(SafeParseColor(DefaultHeightmapLowColor, Color.FromRgb(24, 34, 48)), 0.0));
                brush.GradientStops.Add(new GradientStop(SafeParseColor(DefaultHeightmapHighColor, Color.FromRgb(130, 165, 205)), 1.0));
                return brush;
            }

            var sorted = _heightmapGradient.OrderBy(k => k.Position).ToList();
            if (sorted.Count == 1)
            {
                brush.GradientStops.Add(new GradientStop(sorted[0].Color, 0.0));
                brush.GradientStops.Add(new GradientStop(sorted[0].Color, 1.0));
                return brush;
            }

            foreach (var kf in sorted)
            {
                brush.GradientStops.Add(new GradientStop(kf.Color, Math.Clamp(kf.Position, 0.0, 1.0)));
            }

            return brush;
        }
    }

    public Color EvaluateHeightmapColor(double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        if (_heightmapGradient == null || _heightmapGradient.Count == 0)
        {
            byte r = (byte)Math.Clamp(Math.Round(24.0 + t * (130.0 - 24.0)), 0, 255);
            byte g = (byte)Math.Clamp(Math.Round(34.0 + t * (165.0 - 34.0)), 0, 255);
            byte b = (byte)Math.Clamp(Math.Round(48.0 + t * (205.0 - 48.0)), 0, 255);
            return Color.FromArgb(210, r, g, b);
        }

        var sorted = _heightmapGradient.OrderBy(k => k.Position).ToList();
        if (sorted.Count == 1)
        {
            var c = sorted[0].Color;
            return Color.FromArgb(210, c.R, c.G, c.B);
        }

        if (t <= sorted[0].Position)
        {
            var c = sorted[0].Color;
            return Color.FromArgb(210, c.R, c.G, c.B);
        }

        if (t >= sorted[^1].Position)
        {
            var c = sorted[^1].Color;
            return Color.FromArgb(210, c.R, c.G, c.B);
        }

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var k0 = sorted[i];
            var k1 = sorted[i + 1];

            if (t >= k0.Position && t <= k1.Position)
            {
                double range = k1.Position - k0.Position;
                double factor = range > 1e-6 ? (t - k0.Position) / range : 0.0;

                var c0 = k0.Color;
                var c1 = k1.Color;

                byte r = (byte)Math.Clamp(Math.Round(c0.R + factor * (c1.R - c0.R)), 0, 255);
                byte g = (byte)Math.Clamp(Math.Round(c0.G + factor * (c1.G - c0.G)), 0, 255);
                byte b = (byte)Math.Clamp(Math.Round(c0.B + factor * (c1.B - c0.B)), 0, 255);

                return Color.FromArgb(210, r, g, b);
            }
        }

        var last = sorted[^1].Color;
        return Color.FromArgb(210, last.R, last.G, last.B);
    }

    public void AddKeyframe()
    {
        if (_heightmapGradient == null)
        {
            _heightmapGradient = new ObservableCollection<GradientKeyframe>();
        }

        if (_heightmapGradient.Count == 0)
        {
            var kf1 = new GradientKeyframe(0.0, DefaultHeightmapLowColor);
            var kf2 = new GradientKeyframe(1.0, DefaultHeightmapHighColor);
            _heightmapGradient.Add(kf1);
            _heightmapGradient.Add(kf2);
            SelectedKeyframe = kf2;
            return;
        }

        if (_heightmapGradient.Count == 1)
        {
            double pos = _heightmapGradient[0].Position < 0.5 ? 1.0 : 0.0;
            var newKf = new GradientKeyframe(pos, DefaultHeightmapHighColor);
            _heightmapGradient.Add(newKf);
            SelectedKeyframe = newKf;
            return;
        }

        var sorted = _heightmapGradient.OrderBy(k => k.Position).ToList();
        double maxGap = -1.0;
        double newPos = 0.5;
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            double gap = sorted[i + 1].Position - sorted[i].Position;
            if (gap > maxGap)
            {
                maxGap = gap;
                newPos = sorted[i].Position + gap / 2.0;
            }
        }

        var evaluated = EvaluateHeightmapColor(newPos);
        string hex = $"#{evaluated.R:X2}{evaluated.G:X2}{evaluated.B:X2}";

        var addedKf = new GradientKeyframe(Math.Round(newPos, 2), hex);
        _heightmapGradient.Add(addedKf);
        SelectedKeyframe = addedKf;
    }

    public void RemoveKeyframe(GradientKeyframe? keyframe)
    {
        if (keyframe == null || _heightmapGradient == null) return;
        if (_heightmapGradient.Count <= 2) return;

        bool wasSelected = (_selectedKeyframe == keyframe);
        _heightmapGradient.Remove(keyframe);

        if (wasSelected)
        {
            SelectedKeyframe = _heightmapGradient.FirstOrDefault();
        }
    }

    public void RemoveSelectedKeyframe()
    {
        if (_selectedKeyframe != null)
        {
            RemoveKeyframe(_selectedKeyframe);
        }
    }

    private void InitializeDefaultGradient()
    {
        if (_heightmapGradient != null)
        {
            _heightmapGradient.CollectionChanged -= OnGradientCollectionChanged;
            foreach (var kf in _heightmapGradient)
            {
                kf.PropertyChanged -= OnKeyframePropertyChanged;
            }
            _heightmapGradient.Clear();
        }
        else
        {
            _heightmapGradient = new ObservableCollection<GradientKeyframe>();
        }

        var defaultKf1 = new GradientKeyframe(0.0, DefaultHeightmapLowColor);
        var defaultKf2 = new GradientKeyframe(1.0, DefaultHeightmapHighColor);
        _heightmapGradient.Add(defaultKf1);
        _heightmapGradient.Add(defaultKf2);

        _heightmapGradient.CollectionChanged += OnGradientCollectionChanged;
        foreach (var kf in _heightmapGradient)
        {
            kf.PropertyChanged += OnKeyframePropertyChanged;
        }

        SelectedKeyframe = defaultKf1;

        OnPropertyChanged(nameof(HeightmapGradient));
        OnPropertyChanged(nameof(HeightmapGradientBrush));
        OnPropertyChanged(nameof(CanRemoveKeyframes));
    }

    private void OnGradientCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GradientKeyframe kf in e.NewItems)
            {
                kf.PropertyChanged += OnKeyframePropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (GradientKeyframe kf in e.OldItems)
            {
                kf.PropertyChanged -= OnKeyframePropertyChanged;
            }
        }
        OnPropertyChanged(nameof(HeightmapGradient));
        OnPropertyChanged(nameof(HeightmapGradientBrush));
        OnPropertyChanged(nameof(CanRemoveKeyframes));
    }

    private void OnKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeightmapGradient));
        OnPropertyChanged(nameof(HeightmapGradientBrush));
    }

    public string UnexpandedDoorColor
    {
        get => _unexpandedDoorColor;
        set
        {
            if (SetField(ref _unexpandedDoorColor, NormalizeHex(value, DefaultUnexpandedDoorColor)))
            {
                OnPropertyChanged(nameof(UnexpandedColor));
            }
        }
    }

    public string ConnectedDoorColor
    {
        get => _connectedDoorColor;
        set
        {
            if (SetField(ref _connectedDoorColor, NormalizeHex(value, DefaultConnectedDoorColor)))
            {
                OnPropertyChanged(nameof(ConnectedColor));
            }
        }
    }

    public string SecurityDoorColor
    {
        get => _securityDoorColor;
        set
        {
            if (SetField(ref _securityDoorColor, NormalizeHex(value, DefaultSecurityDoorColor)))
            {
                OnPropertyChanged(nameof(SecurityColor));
            }
        }
    }

    public string MainDoorColor
    {
        get => _mainDoorColor;
        set
        {
            if (SetField(ref _mainDoorColor, NormalizeHex(value, DefaultMainDoorColor)))
            {
                OnPropertyChanged(nameof(MainColor));
            }
        }
    }

    public string SecondaryDoorColor
    {
        get => _secondaryDoorColor;
        set
        {
            if (SetField(ref _secondaryDoorColor, NormalizeHex(value, DefaultSecondaryDoorColor)))
            {
                OnPropertyChanged(nameof(SecondaryColor));
            }
        }
    }

    public string OverloadDoorColor
    {
        get => _overloadDoorColor;
        set
        {
            if (SetField(ref _overloadDoorColor, NormalizeHex(value, DefaultOverloadDoorColor)))
            {
                OnPropertyChanged(nameof(OverloadColor));
            }
        }
    }

    public string BlockedDoorColor
    {
        get => _blockedDoorColor;
        set
        {
            if (SetField(ref _blockedDoorColor, NormalizeHex(value, DefaultBlockedDoorColor)))
            {
                OnPropertyChanged(nameof(BlockedColor));
            }
        }
    }

    [JsonIgnore]
    public Color UnexpandedColor
    {
        get => SafeParseColor(UnexpandedDoorColor, Color.Parse(DefaultUnexpandedDoorColor));
        set => UnexpandedDoorColor = ColorToHex(value);
    }

    [JsonIgnore]
    public Color ConnectedColor
    {
        get => SafeParseColor(ConnectedDoorColor, Color.Parse(DefaultConnectedDoorColor));
        set => ConnectedDoorColor = ColorToHex(value);
    }

    [JsonIgnore]
    public Color SecurityColor
    {
        get => SafeParseColor(SecurityDoorColor, Color.Parse(DefaultSecurityDoorColor));
        set => SecurityDoorColor = ColorToHex(value);
    }

    [JsonIgnore]
    public Color MainColor
    {
        get => SafeParseColor(MainDoorColor, Color.Parse(DefaultMainDoorColor));
        set => MainDoorColor = ColorToHex(value);
    }

    [JsonIgnore]
    public Color SecondaryColor
    {
        get => SafeParseColor(SecondaryDoorColor, Color.Parse(DefaultSecondaryDoorColor));
        set => SecondaryDoorColor = ColorToHex(value);
    }

    [JsonIgnore]
    public Color OverloadColor
    {
        get => SafeParseColor(OverloadDoorColor, Color.Parse(DefaultOverloadDoorColor));
        set => OverloadDoorColor = ColorToHex(value);
    }

    [JsonIgnore]
    public Color BlockedColor
    {
        get => SafeParseColor(BlockedDoorColor, Color.Parse(DefaultBlockedDoorColor));
        set => BlockedDoorColor = ColorToHex(value);
    }

    public void ResetUnexpandedDoorColor() => UnexpandedDoorColor = DefaultUnexpandedDoorColor;
    public void ResetConnectedDoorColor() => ConnectedDoorColor = DefaultConnectedDoorColor;
    public void ResetSecurityDoorColor() => SecurityDoorColor = DefaultSecurityDoorColor;
    public void ResetMainDoorColor() => MainDoorColor = DefaultMainDoorColor;
    public void ResetSecondaryDoorColor() => SecondaryDoorColor = DefaultSecondaryDoorColor;
    public void ResetOverloadDoorColor() => OverloadDoorColor = DefaultOverloadDoorColor;
    public void ResetBlockedDoorColor() => BlockedDoorColor = DefaultBlockedDoorColor;
    public void ResetHeightmapGradient() => InitializeDefaultGradient();

    public void ResetToDefaults()
    {
        ResetUnexpandedDoorColor();
        ResetConnectedDoorColor();
        ResetSecurityDoorColor();
        ResetMainDoorColor();
        ResetSecondaryDoorColor();
        ResetOverloadDoorColor();
        ResetBlockedDoorColor();
        ResetHeightmapGradient();
    }

    public static string NormalizeHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        string trimmed = hex.Trim();
        if (!trimmed.StartsWith("#"))
        {
            trimmed = "#" + trimmed;
        }
        return trimmed;
    }

    public static Color SafeParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            return Color.Parse(hex);
        }
        catch
        {
            return fallback;
        }
    }

    public static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
