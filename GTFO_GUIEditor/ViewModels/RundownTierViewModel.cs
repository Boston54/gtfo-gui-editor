using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.ViewModels;

public class RundownTierViewModel : ViewModelBase
{
    private readonly RundownBlock _rundown;
    private readonly string _tierLetter;
    private readonly string _tierName;
    private readonly Action<RundownTierViewModel>? _onOpenSettings;
    private readonly Action<ExpeditionBlock, RundownTierViewModel>? _onOpenExpedition;
    private readonly Action<ExpeditionBlock, RundownTierViewModel>? _onRequestDeleteExpedition;
    private TierVisuals _visuals;
    private TierRequirements _requirements;
    private ObservableCollection<ExpeditionBlock> _expeditions;

    public RundownTierViewModel(
        RundownBlock rundown,
        string tierLetter,
        TierVisuals visuals,
        TierRequirements requirements,
        List<ExpeditionBlock> expeditionsList,
        Action<RundownTierViewModel>? onOpenSettings = null,
        Action<ExpeditionBlock, RundownTierViewModel>? onOpenExpedition = null,
        Action<ExpeditionBlock, RundownTierViewModel>? onRequestDeleteExpedition = null)
    {
        _rundown = rundown;
        _tierLetter = tierLetter.ToUpperInvariant();
        _tierName = $"Tier {_tierLetter}";
        _visuals = visuals;
        _requirements = requirements;
        _expeditions = new ObservableCollection<ExpeditionBlock>(expeditionsList);
        _onOpenSettings = onOpenSettings;
        _onOpenExpedition = onOpenExpedition;
        _onRequestDeleteExpedition = onRequestDeleteExpedition;

        HookEvents();
    }

    private void HookEvents()
    {
        _visuals.PropertyChanged += (s, e) =>
        {
            UpdateExpeditionsColors();
            OnPropertyChanged(nameof(Visuals));
            OnPropertyChanged(nameof(ColorHex));
            OnPropertyChanged(nameof(Scale));
            OnPropertyChanged(nameof(ScaleYModifier));
            OnPropertyChanged(nameof(ScaledHeight));
            OnPropertyChanged(nameof(ScaledWidth));
            OnPropertyChanged(nameof(ExpeditionSpacing));
            OnPropertyChanged(nameof(TierVerticalMargin));
            OnPropertyChanged(nameof(TierMargin));
        };

        _visuals.Color.PropertyChanged += (s, e) =>
        {
            UpdateExpeditionsColors();
            OnPropertyChanged(nameof(ColorHex));
        };

        _requirements.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Requirements));
            OnPropertyChanged(nameof(RequirementsSummary));
        };

        HookExpeditionCollection(_expeditions);
        UpdateExpeditionsIndexAndColor();

        _expeditions.CollectionChanged += (s, e) =>
        {
            if (e.OldItems != null)
            {
                foreach (ExpeditionBlock item in e.OldItems)
                {
                    item.PropertyChanged -= OnExpeditionPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (ExpeditionBlock item in e.NewItems)
                {
                    item.PropertyChanged += OnExpeditionPropertyChanged;
                }
            }

            UpdateExpeditionsIndexAndColor();
            SyncToRundownList();
            OnPropertyChanged(nameof(ExpeditionCount));
            OnPropertyChanged(nameof(VisibleExpeditionCount));
            OnPropertyChanged(nameof(ExpeditionCountText));
            OnPropertyChanged(nameof(HasExpeditions));
            OnPropertyChanged(nameof(HasVisibleExpeditions));
        };
    }

    private void HookExpeditionCollection(IEnumerable<ExpeditionBlock> expeditions)
    {
        foreach (var exp in expeditions)
        {
            exp.PropertyChanged += OnExpeditionPropertyChanged;
        }
    }

    private void OnExpeditionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExpeditionBlock.Enabled))
        {
            OnPropertyChanged(nameof(VisibleExpeditionCount));
            OnPropertyChanged(nameof(ExpeditionCountText));
            OnPropertyChanged(nameof(HasVisibleExpeditions));
        }
    }

    private void UpdateExpeditionsIndexAndColor()
    {
        for (int i = 0; i < _expeditions.Count; i++)
        {
            _expeditions[i].ExpeditionIndexInTier = i + 1;
            _expeditions[i].TierColorHex = ColorHex;
        }
    }

    private void UpdateExpeditionsColors()
    {
        for (int i = 0; i < _expeditions.Count; i++)
        {
            _expeditions[i].TierColorHex = ColorHex;
        }
    }

    public string TierLetter => _tierLetter;
    public string TierName => _tierName;

    public TierVisuals Visuals
    {
        get => _visuals;
        set
        {
            if (SetField(ref _visuals, value))
            {
                OnPropertyChanged(nameof(ColorHex));
                OnPropertyChanged(nameof(Scale));
                OnPropertyChanged(nameof(ScaleYModifier));
            }
        }
    }

    public TierRequirements Requirements
    {
        get => _requirements;
        set => SetField(ref _requirements, value);
    }

    public ObservableCollection<ExpeditionBlock> Expeditions
    {
        get => _expeditions;
        set => SetField(ref _expeditions, value);
    }

    public ColorData TierColor => _visuals.Color;

    public string ColorHex => _visuals.Color?.HexCode ?? "#FFFFFFFF";

    public double Scale
    {
        get => _visuals.Scale;
        set
        {
            if (Math.Abs(_visuals.Scale - value) > 0.0001)
            {
                _visuals.Scale = value;
                OnPropertyChanged(nameof(Scale));
                OnPropertyChanged(nameof(ExpeditionSpacing));
            }
        }
    }

    public double ScaleYModifier
    {
        get => _visuals.ScaleYModifier;
        set
        {
            if (Math.Abs(_visuals.ScaleYModifier - value) > 0.0001)
            {
                _visuals.ScaleYModifier = value;
                OnPropertyChanged(nameof(ScaleYModifier));
            }
        }
    }

    public double ScaledWidth => 180.0;

    public double ScaledHeight => 100.0;

    public double ExpeditionSpacing => Math.Max(16.0, 90.0 * Math.Clamp(_visuals.Scale, 0.2, 3.0));

    public double TierVerticalMargin => 24.0;

    public Thickness TierMargin => new Thickness(0, 0, 0, 24);

    public int ExpeditionCount => _expeditions.Count;

    public int VisibleExpeditionCount => _expeditions.Count(e => e.Enabled);

    public string ExpeditionCountText => $"{VisibleExpeditionCount} Expedition{(VisibleExpeditionCount == 1 ? "" : "s")}";

    public bool HasExpeditions => _expeditions.Count > 0;

    public bool HasVisibleExpeditions => _expeditions.Any(e => e.Enabled);

    public string RequirementsSummary
    {
        get
        {
            if (!_requirements.HasAnyRequirement) return "No sector requirements";
            return $"Requires: Main={_requirements.MainSectors}, Sec={_requirements.SecondarySectors}, Overload={_requirements.ThirdSectors}, All={_requirements.AllClearedSectors}";
        }
    }

    public void OpenSettings()
    {
        _onOpenSettings?.Invoke(this);
    }

    public void OpenExpedition(ExpeditionBlock exp)
    {
        _onOpenExpedition?.Invoke(exp, this);
    }

    public void AddExpedition()
    {
        int nextIndex = _expeditions.Count + 1;
        string defaultPrefix = _expeditions.FirstOrDefault()?.Descriptive?.Prefix ?? _tierLetter;
        var newExp = new ExpeditionBlock
        {
            Enabled = true,
            Accessibility = ExpeditionAccessibility.Normal,
            ExpeditionIndexInTier = nextIndex,
            TierColorHex = ColorHex,
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = defaultPrefix,
                PublicName = $"Level {_tierLetter}{nextIndex}",
                ExpeditionDepth = 500 + nextIndex * 100,
                EstimatedDuration = 900,
                ProgressionVisualStyle = "Normal",
                SkipExpNumberInName = false
            },
            MainLayerData = new ExpeditionLayerData
            {
                ObjectiveData = new ExpeditionObjectiveData
                {
                    WinCondition = "GoToElevator",
                    DataBlockId = 0
                }
            },
            SecondaryLayerEnabled = false,
            ThirdLayerEnabled = false
        };

        _expeditions.Add(newExp);
    }

    public void RequestDeleteExpedition(ExpeditionBlock exp)
    {
        if (_onRequestDeleteExpedition != null)
        {
            _onRequestDeleteExpedition.Invoke(exp, this);
        }
        else
        {
            RemoveExpedition(exp);
        }
    }

    public void RemoveExpedition(ExpeditionBlock exp)
    {
        if (_expeditions.Contains(exp))
        {
            _expeditions.Remove(exp);
        }
    }

    public ExpeditionBlock DuplicateExpedition(ExpeditionBlock exp)
    {
        var clone = exp.Clone();
        int nextIndex = _expeditions.Count + 1;
        clone.Descriptive.Prefix = $"{_tierLetter}{nextIndex}";
        clone.Descriptive.PublicName = $"{exp.Descriptive.PublicName} (Copy)";
        _expeditions.Add(clone);
        return clone;
    }

    private void SyncToRundownList()
    {
        var list = _rundown.GetTierList(_tierLetter);
        list.Clear();
        list.AddRange(_expeditions);
    }
}
