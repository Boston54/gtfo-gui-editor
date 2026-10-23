using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class LightSettingsBlock : INotifyPropertyChanged
{
    private uint _persistentId;
    private string _name = string.Empty;
    private bool _internalEnabled = true;
    private ObservableCollection<LightCategorySettingItem> _lightCategorySettings = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LightSettingsBlock()
    {
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

    [JsonPropertyName("LightCategorySettings")]
    public ObservableCollection<LightCategorySettingItem> LightCategorySettings
    {
        get => _lightCategorySettings;
        set => SetField(ref _lightCategorySettings, value);
    }

    [JsonIgnore]
    public string DisplayText => $"[{PersistentId}] {Name}";

    public LightSettingsBlock Clone()
    {
        return new LightSettingsBlock
        {
            PersistentId = PersistentId,
            Name = Name,
            InternalEnabled = InternalEnabled,
            LightCategorySettings = new ObservableCollection<LightCategorySettingItem>(
                LightCategorySettings.Select(c => c.Clone()))
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
