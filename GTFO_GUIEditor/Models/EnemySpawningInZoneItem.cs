using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class EnemySpawningInZoneItem : INotifyPropertyChanged
{
    private string _groupType = "Hibernate";
    private string _difficulty = "Easy";
    private EnemyZoneDistribution _distribution = EnemyZoneDistribution.Rel_Value;
    private double _distributionValue = 0.0;

    [JsonPropertyName("GroupType")]
    public string GroupType
    {
        get => _groupType;
        set
        {
            if (SetField(ref _groupType, value))
            {
                OnPropertyChanged(nameof(SelectedOption));
                OnPropertyChanged(nameof(DistributionText));
                OnPropertyChanged(nameof(HasDistributionText));
            }
        }
    }

    [JsonPropertyName("Difficulty")]
    public string Difficulty
    {
        get => _difficulty;
        set
        {
            if (SetField(ref _difficulty, value))
            {
                OnPropertyChanged(nameof(SelectedOption));
                OnPropertyChanged(nameof(DistributionText));
                OnPropertyChanged(nameof(HasDistributionText));
            }
        }
    }

    [JsonPropertyName("Distribution")]
    public EnemyZoneDistribution Distribution
    {
        get => _distribution;
        set
        {
            if (SetField(ref _distribution, value))
            {
                OnPropertyChanged(nameof(IsRelValue));
            }
        }
    }

    [JsonPropertyName("DistributionValue")]
    public double DistributionValue
    {
        get => _distributionValue;
        set => SetField(ref _distributionValue, value);
    }

    [JsonIgnore]
    public bool IsRelValue => Distribution == EnemyZoneDistribution.Rel_Value;

    [JsonIgnore]
    public EnemySpawningOption? SelectedOption
    {
        get => EnemySpawningService.GetOption(GroupType, Difficulty);
        set
        {
            if (value != null)
            {
                GroupType = value.GroupType;
                Difficulty = value.Difficulty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DistributionText));
                OnPropertyChanged(nameof(HasDistributionText));
            }
        }
    }

    [JsonIgnore]
    public string DistributionText => SelectedOption?.DistributionText ?? string.Empty;

    [JsonIgnore]
    public bool HasDistributionText => !string.IsNullOrEmpty(DistributionText);

    public EnemySpawningInZoneItem Clone()
    {
        return new EnemySpawningInZoneItem
        {
            GroupType = GroupType,
            Difficulty = Difficulty,
            Distribution = Distribution,
            DistributionValue = DistributionValue
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
