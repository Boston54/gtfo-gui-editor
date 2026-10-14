using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class RundownViewModel : ViewModelBase
{
    public static readonly List<string> AvailableProgressionStyles = new() { "Normal", "Story" };
    public static ExpeditionAccessibility[] AvailableAccessibilities => Enum.GetValues<ExpeditionAccessibility>();

    public ExpeditionAccessibility[] Accessibilities => AvailableAccessibilities;

    private RundownBlock? _selectedRundown;
    private ObservableCollection<RundownTierViewModel> _tiers = new();

    // Available Level Layouts for Expedition linking
    private ObservableCollection<LevelLayoutBlock> _availableLevelLayouts = new();
    private ObservableCollection<ComplexResourceSetBlock> _availableComplexResourceSets = new();

    // Active editing tier modal
    private RundownTierViewModel? _activeEditingTier;
    private bool _isEditingTierSettings;

    // Active editing expedition modal
    private ExpeditionBlock? _activeEditingExpedition;
    private RundownTierViewModel? _activeEditingExpeditionTier;
    private bool _isEditingExpedition;
    private ObservableCollection<ExpeditionLookupItem> _availableOtherExpeditions = new();
    private ExpeditionLookupItem? _selectedOtherExpedition;

    // Global Rundown settings modal
    private bool _isEditingRundownSettings;

    // Intel modal
    private bool _isShowingIntel;

    // Delete Expedition modal
    private bool _isConfirmingDeleteExpedition;
    private ExpeditionBlock? _expeditionToDelete;
    private RundownTierViewModel? _expeditionTierToDelete;

    public RundownViewModel()
        : this(RundownService.LoadVanillaBlocks().FirstOrDefault() ?? RundownService.CreateFreshBlock(1, "Rundown"))
    {
    }

    public RundownViewModel(
        RundownBlock? rundown,
        ObservableCollection<LevelLayoutBlock>? levelLayouts = null,
        ObservableCollection<ComplexResourceSetBlock>? complexSets = null)
    {
        _selectedRundown = rundown ?? RundownService.LoadVanillaBlocks().FirstOrDefault() ?? RundownService.CreateFreshBlock(1, "Rundown");
        _selectedRundown.PropertyChanged += SelectedRundown_PropertyChanged;

        if (levelLayouts != null)
        {
            _availableLevelLayouts = levelLayouts;
            _availableLevelLayouts.CollectionChanged += AvailableLevelLayouts_CollectionChanged;
            foreach (var level in _availableLevelLayouts)
            {
                level.PropertyChanged += Level_PropertyChanged;
            }
        }

        if (complexSets != null)
        {
            _availableComplexResourceSets = complexSets;
        }

        RebuildTiers();
        SyncAllExpeditionsWithLevelLayouts();
    }

    public RundownViewModel(
        IEnumerable<RundownBlock>? initialRundowns,
        ObservableCollection<LevelLayoutBlock>? levelLayouts = null,
        ObservableCollection<ComplexResourceSetBlock>? complexSets = null)
        : this(initialRundowns?.FirstOrDefault(), levelLayouts, complexSets)
    {
    }

    public ObservableCollection<LevelLayoutBlock> AvailableLevelLayouts
    {
        get => _availableLevelLayouts;
        set
        {
            if (_availableLevelLayouts != null)
            {
                _availableLevelLayouts.CollectionChanged -= AvailableLevelLayouts_CollectionChanged;
                foreach (var level in _availableLevelLayouts)
                {
                    level.PropertyChanged -= Level_PropertyChanged;
                }
            }
            _availableLevelLayouts = value ?? new ObservableCollection<LevelLayoutBlock>();
            _availableLevelLayouts.CollectionChanged += AvailableLevelLayouts_CollectionChanged;
            foreach (var level in _availableLevelLayouts)
            {
                level.PropertyChanged += Level_PropertyChanged;
            }
            OnPropertyChanged();
            SyncAllExpeditionsWithLevelLayouts();
            OnPropertyChanged(nameof(SelectedEditingExpeditionLevel));
        }
    }

    private void AvailableLevelLayouts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (LevelLayoutBlock level in e.NewItems)
            {
                level.PropertyChanged += Level_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (LevelLayoutBlock level in e.OldItems)
            {
                level.PropertyChanged -= Level_PropertyChanged;
            }
        }
        SyncAllExpeditionsWithLevelLayouts();
    }

    private void Level_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LevelLayoutBlock.HasSecondaryDoor) ||
            e.PropertyName == nameof(LevelLayoutBlock.HasOverloadDoor) ||
            e.PropertyName == nameof(LevelLayoutBlock.PersistentId))
        {
            SyncAllExpeditionsWithLevelLayouts();
        }
    }

    public void SyncExpeditionSectorsWithLevel(ExpeditionBlock? expedition)
    {
        if (expedition == null) return;
        var level = _availableLevelLayouts.FirstOrDefault(l => l.PersistentId == expedition.LevelLayoutData);
        if (level != null)
        {
            expedition.SecondaryLayerEnabled = level.HasSecondaryDoor;
            expedition.ThirdLayerEnabled = level.HasOverloadDoor;
        }
    }

    public void SyncAllExpeditionsWithLevelLayouts()
    {
        if (_selectedRundown == null) return;
        foreach (var exp in _selectedRundown.TierA) SyncExpeditionSectorsWithLevel(exp);
        foreach (var exp in _selectedRundown.TierB) SyncExpeditionSectorsWithLevel(exp);
        foreach (var exp in _selectedRundown.TierC) SyncExpeditionSectorsWithLevel(exp);
        foreach (var exp in _selectedRundown.TierD) SyncExpeditionSectorsWithLevel(exp);
        foreach (var exp in _selectedRundown.TierE) SyncExpeditionSectorsWithLevel(exp);
    }

    public ObservableCollection<ComplexResourceSetBlock> AvailableComplexResourceSets
    {
        get => _availableComplexResourceSets;
        set => SetField(ref _availableComplexResourceSets, value);
    }

    public ObservableCollection<RundownTierViewModel> Tiers
    {
        get => _tiers;
        private set => SetField(ref _tiers, value);
    }

    public RundownBlock? SelectedRundown
    {
        get => _selectedRundown;
        set
        {
            if (_selectedRundown != null)
            {
                _selectedRundown.PropertyChanged -= SelectedRundown_PropertyChanged;
            }

            if (SetField(ref _selectedRundown, value))
            {
                if (_selectedRundown != null)
                {
                    _selectedRundown.PropertyChanged += SelectedRundown_PropertyChanged;
                }

                RebuildTiers();
                SyncAllExpeditionsWithLevelLayouts();
                OnPropertyChanged(nameof(HasSelectedRundown));
                OnPropertyChanged(nameof(BackgroundColorHex));
            }
        }
    }

    public bool HasSelectedRundown => _selectedRundown != null;

    public string BackgroundColorHex => _selectedRundown?.StorytellingData?.Visuals?.ColorBackground?.HexCode ?? "#FF000000";

    // Modals state
    public RundownTierViewModel? ActiveEditingTier
    {
        get => _activeEditingTier;
        set
        {
            if (SetField(ref _activeEditingTier, value))
            {
                IsEditingTierSettings = _activeEditingTier != null;
                OnPropertyChanged(nameof(IsEditingTierSettings));
            }
        }
    }

    public bool IsEditingTierSettings
    {
        get => _isEditingTierSettings;
        set => SetField(ref _isEditingTierSettings, value);
    }

    public ExpeditionBlock? ActiveEditingExpedition
    {
        get => _activeEditingExpedition;
        set
        {
            if (_activeEditingExpedition != null)
            {
                _activeEditingExpedition.PropertyChanged -= ActiveEditingExpedition_PropertyChanged;
            }

            if (SetField(ref _activeEditingExpedition, value))
            {
                if (_activeEditingExpedition != null)
                {
                    _activeEditingExpedition.PropertyChanged += ActiveEditingExpedition_PropertyChanged;
                }

                IsEditingExpedition = _activeEditingExpedition != null;
                OnPropertyChanged(nameof(IsEditingExpedition));
                OnPropertyChanged(nameof(SelectedEditingExpeditionLevel));
                OnPropertyChanged(nameof(IsUnlockedByExpeditionSelected));
                RefreshAvailableOtherExpeditions();
            }
        }
    }

    private void ActiveEditingExpedition_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExpeditionBlock.Accessibility))
        {
            OnPropertyChanged(nameof(IsUnlockedByExpeditionSelected));
            if (ActiveEditingExpedition?.Accessibility == ExpeditionAccessibility.UnlockedByExpedition)
            {
                RefreshAvailableOtherExpeditions();
            }
        }
        else if (e.PropertyName == nameof(ExpeditionBlock.LevelLayoutData))
        {
            OnPropertyChanged(nameof(SelectedEditingExpeditionLevel));
        }
    }

    public LevelLayoutBlock? SelectedEditingExpeditionLevel
    {
        get => _availableLevelLayouts.FirstOrDefault(l => l.PersistentId == ActiveEditingExpedition?.LevelLayoutData);
        set
        {
            if (ActiveEditingExpedition != null && value != null)
            {
                ActiveEditingExpedition.LevelLayoutData = value.PersistentId;
                SyncExpeditionSectorsWithLevel(ActiveEditingExpedition);
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ExpeditionLookupItem> AvailableOtherExpeditions
    {
        get => _availableOtherExpeditions;
        private set => SetField(ref _availableOtherExpeditions, value);
    }

    public ExpeditionLookupItem? SelectedOtherExpedition
    {
        get => _selectedOtherExpedition;
        set
        {
            if (SetField(ref _selectedOtherExpedition, value))
            {
                if (_selectedOtherExpedition != null && ActiveEditingExpedition != null)
                {
                    ActiveEditingExpedition.UnlockedByExpedition.Tier = _selectedOtherExpedition.Tier;
                    ActiveEditingExpedition.UnlockedByExpedition.Exp = _selectedOtherExpedition.Exp;
                }
            }
        }
    }

    public bool IsUnlockedByExpeditionSelected => ActiveEditingExpedition?.Accessibility == ExpeditionAccessibility.UnlockedByExpedition;

    public void RefreshAvailableOtherExpeditions()
    {
        var list = new List<ExpeditionLookupItem>();

        if (_selectedRundown != null)
        {
            var tiers = new (string Letter, string TierKey, List<ExpeditionBlock> Expeditions)[]
            {
                ("A", "TierA", _selectedRundown.TierA),
                ("B", "TierB", _selectedRundown.TierB),
                ("C", "TierC", _selectedRundown.TierC),
                ("D", "TierD", _selectedRundown.TierD),
                ("E", "TierE", _selectedRundown.TierE),
            };

            foreach (var (letter, tierKey, expList) in tiers)
            {
                for (int i = 0; i < expList.Count; i++)
                {
                    var exp = expList[i];
                    if (exp == ActiveEditingExpedition)
                    {
                        continue;
                    }

                    int expNumber = i + 1;
                    string expKey = $"Exp{expNumber}";
                    string displayText = !string.IsNullOrWhiteSpace(exp.Descriptive?.PublicName)
                        ? $"{exp.DisplayPrefix}: {exp.Descriptive.PublicName}"
                        : $"{exp.DisplayPrefix}";

                    list.Add(new ExpeditionLookupItem
                    {
                        Tier = tierKey,
                        TierLetter = letter,
                        Exp = expKey,
                        ExpeditionNumber = expNumber,
                        Expedition = exp,
                        DisplayText = displayText
                    });
                }
            }
        }

        AvailableOtherExpeditions = new ObservableCollection<ExpeditionLookupItem>(list);

        if (ActiveEditingExpedition != null)
        {
            var currentTier = ActiveEditingExpedition.UnlockedByExpedition.Tier;
            var currentExp = ActiveEditingExpedition.UnlockedByExpedition.Exp;

            var match = AvailableOtherExpeditions.FirstOrDefault(item =>
                string.Equals(item.Tier, currentTier, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Exp, currentExp, StringComparison.OrdinalIgnoreCase));

            _selectedOtherExpedition = match ?? AvailableOtherExpeditions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedOtherExpedition));

            if (_selectedOtherExpedition != null)
            {
                ActiveEditingExpedition.UnlockedByExpedition.Tier = _selectedOtherExpedition.Tier;
                ActiveEditingExpedition.UnlockedByExpedition.Exp = _selectedOtherExpedition.Exp;
            }
        }
        else
        {
            _selectedOtherExpedition = null;
            OnPropertyChanged(nameof(SelectedOtherExpedition));
        }
    }

    public RundownTierViewModel? ActiveEditingExpeditionTier
    {
        get => _activeEditingExpeditionTier;
        set => SetField(ref _activeEditingExpeditionTier, value);
    }

    public bool IsEditingExpedition
    {
        get => _isEditingExpedition;
        set => SetField(ref _isEditingExpedition, value);
    }

    public bool IsEditingRundownSettings
    {
        get => _isEditingRundownSettings;
        set => SetField(ref _isEditingRundownSettings, value);
    }

    public bool IsShowingIntel
    {
        get => _isShowingIntel;
        set => SetField(ref _isShowingIntel, value);
    }

    // Delete Expedition state
    public bool IsConfirmingDeleteExpedition
    {
        get => _isConfirmingDeleteExpedition;
        set => SetField(ref _isConfirmingDeleteExpedition, value);
    }

    public ExpeditionBlock? ExpeditionToDelete
    {
        get => _expeditionToDelete;
        set
        {
            if (SetField(ref _expeditionToDelete, value))
            {
                OnPropertyChanged(nameof(DeleteExpeditionConfirmationMessage));
            }
        }
    }

    public RundownTierViewModel? ExpeditionTierToDelete
    {
        get => _expeditionTierToDelete;
        set => SetField(ref _expeditionTierToDelete, value);
    }

    public string DeleteExpeditionConfirmationMessage
    {
        get
        {
            if (_expeditionToDelete == null) return string.Empty;
            string prefix = _expeditionToDelete.DisplayPrefix;
            string name = _expeditionToDelete.Descriptive?.PublicName ?? string.Empty;
            string label = !string.IsNullOrWhiteSpace(name) ? $"{prefix} \"{name}\"" : prefix;
            return $"Are you sure you want to delete expedition {label}? This action cannot be undone.";
        }
    }

    private void SelectedRundown_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RundownBlock.StorytellingData))
        {
            OnPropertyChanged(nameof(BackgroundColorHex));
        }
        else if (e.PropertyName == nameof(RundownBlock.Name) || e.PropertyName == nameof(RundownBlock.PersistentId))
        {
            OnPropertyChanged(nameof(SelectedRundown));
        }
    }

    public void RebuildTiers()
    {
        if (_selectedRundown == null)
        {
            Tiers = new ObservableCollection<RundownTierViewModel>();
            return;
        }

        var visuals = _selectedRundown.StorytellingData.Visuals;
        var tierA = new RundownTierViewModel(_selectedRundown, "A", visuals.TierAVisuals, _selectedRundown.ReqToReachTierA, _selectedRundown.TierA, OpenTierSettings, OpenExpeditionEditor, RequestDeleteExpedition);
        var tierB = new RundownTierViewModel(_selectedRundown, "B", visuals.TierBVisuals, _selectedRundown.ReqToReachTierB, _selectedRundown.TierB, OpenTierSettings, OpenExpeditionEditor, RequestDeleteExpedition);
        var tierC = new RundownTierViewModel(_selectedRundown, "C", visuals.TierCVisuals, _selectedRundown.ReqToReachTierC, _selectedRundown.TierC, OpenTierSettings, OpenExpeditionEditor, RequestDeleteExpedition);
        var tierD = new RundownTierViewModel(_selectedRundown, "D", visuals.TierDVisuals, _selectedRundown.ReqToReachTierD, _selectedRundown.TierD, OpenTierSettings, OpenExpeditionEditor, RequestDeleteExpedition);
        var tierE = new RundownTierViewModel(_selectedRundown, "E", visuals.TierEVisuals, _selectedRundown.ReqToReachTierE, _selectedRundown.TierE, OpenTierSettings, OpenExpeditionEditor, RequestDeleteExpedition);

        Tiers = new ObservableCollection<RundownTierViewModel> { tierA, tierB, tierC, tierD, tierE };
    }

    public void OpenTierSettings(RundownTierViewModel tier)
    {
        ActiveEditingTier = tier;
    }

    public void CloseTierSettings()
    {
        ActiveEditingTier = null;
    }

    public void OpenExpeditionEditor(ExpeditionBlock exp, RundownTierViewModel? tier = null)
    {
        SyncExpeditionSectorsWithLevel(exp);
        ActiveEditingExpedition = exp;
        ActiveEditingExpeditionTier = tier;
    }

    public void CloseExpeditionEditor()
    {
        ActiveEditingExpedition = null;
        ActiveEditingExpeditionTier = null;
    }

    public void OpenRundownSettings()
    {
        IsEditingRundownSettings = true;
    }

    public void CloseRundownSettings()
    {
        IsEditingRundownSettings = false;
        OnPropertyChanged(nameof(BackgroundColorHex));
    }

    public void OpenIntelDialog()
    {
        IsShowingIntel = true;
    }

    public void CloseIntelDialog()
    {
        IsShowingIntel = false;
    }

    public void ApplyColorPreset(string hex)
    {
        if (ActiveEditingTier?.Visuals?.Color != null)
        {
            ActiveEditingTier.Visuals.Color.SetFromHex(hex);
        }
    }

    public void SetBackgroundColor(string hex)
    {
        if (SelectedRundown?.StorytellingData?.Visuals?.ColorBackground != null)
        {
            SelectedRundown.StorytellingData.Visuals.ColorBackground.SetFromHex(hex);
            OnPropertyChanged(nameof(BackgroundColorHex));
        }
    }

    public void RequestDeleteExpedition(ExpeditionBlock exp, RundownTierViewModel tier)
    {
        ExpeditionToDelete = exp;
        ExpeditionTierToDelete = tier;
        IsConfirmingDeleteExpedition = true;
    }

    public void ConfirmDeleteExpedition()
    {
        if (ExpeditionToDelete != null && ExpeditionTierToDelete != null)
        {
            if (ActiveEditingExpedition == ExpeditionToDelete)
            {
                CloseExpeditionEditor();
            }
            ExpeditionTierToDelete.RemoveExpedition(ExpeditionToDelete);
        }

        IsConfirmingDeleteExpedition = false;
        ExpeditionToDelete = null;
        ExpeditionTierToDelete = null;
    }

    public void CancelDeleteExpedition()
    {
        IsConfirmingDeleteExpedition = false;
        ExpeditionToDelete = null;
        ExpeditionTierToDelete = null;
    }
}
