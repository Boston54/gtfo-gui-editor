using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class LightCategorySettingItem : INotifyPropertyChanged
{
    private bool _on = true;
    private LightCategory _category = LightCategory.General;
    private double _chance = 1.0;
    private double _chanceBroken = 0.0;
    private LightColor _color = new();
    private double _intensityMul = 1.0;
    private double _weight = 1.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("On")]
    public bool On
    {
        get => _on;
        set => SetField(ref _on, value);
    }

    [JsonPropertyName("Category")]
    public LightCategory Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    [JsonPropertyName("Chance")]
    public double Chance
    {
        get => _chance;
        set => SetField(ref _chance, value);
    }

    [JsonPropertyName("ChanceBroken")]
    public double ChanceBroken
    {
        get => _chanceBroken;
        set => SetField(ref _chanceBroken, value);
    }

    [JsonPropertyName("Color")]
    public LightColor Color
    {
        get => _color;
        set
        {
            LightColor target = value ?? new LightColor();
            _color.PropertyChanged -= Color_PropertyChanged;
            if (SetField(ref _color, target))
            {
                _color.PropertyChanged += Color_PropertyChanged;
            }
        }
    }

    [JsonPropertyName("IntensityMul")]
    public double IntensityMul
    {
        get => _intensityMul;
        set => SetField(ref _intensityMul, value);
    }

    [JsonPropertyName("Weight")]
    public double Weight
    {
        get => _weight;
        set => SetField(ref _weight, value);
    }

    public LightCategorySettingItem()
    {
        _color.PropertyChanged += Color_PropertyChanged;
    }

    private void Color_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Color));
    }

    public LightCategorySettingItem Clone()
    {
        return new LightCategorySettingItem
        {
            On = On,
            Category = Category,
            Chance = Chance,
            ChanceBroken = ChanceBroken,
            Color = Color?.Clone() ?? new LightColor(),
            IntensityMul = IntensityMul,
            Weight = Weight
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
