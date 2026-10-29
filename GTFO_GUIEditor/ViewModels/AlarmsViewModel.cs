using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class AlarmsViewModel : ViewModelBase
{
    private readonly List<AlarmBlock> _vanillaTemplates;
    private AlarmBlock? _selectedAlarm;
    private string _statusText = string.Empty;

    // Create alarm modal state
    private bool _isCreatingAlarm;
    private bool _isCreatingFresh = true;
    private bool _isCreatingFromExisting;
    private bool _isCreatingFromVanilla;
    private AlarmBlock? _selectedExistingAlarm;
    private AlarmBlock? _selectedVanillaTemplate;
    private string _newAlarmName = string.Empty;
    private string _newAlarmPublicAlarmName = string.Empty;
    private uint _newAlarmPersistentId;
    private string _creationErrorMessage = string.Empty;

    // Delete alarm confirmation modal state
    private bool _isConfirmingDelete;
    private AlarmBlock? _alarmToDelete;

    public static SurvivalWaveSpawnType[] AvailableWaveSpawnTypes => Enum.GetValues<SurvivalWaveSpawnType>();
    public static WaveFilterType[] AvailableFilterTypes => Enum.GetValues<WaveFilterType>();

    public SurvivalWaveSpawnType[] WaveSpawnTypes => AvailableWaveSpawnTypes;
    public WaveFilterType[] FilterTypes => AvailableFilterTypes;

    public ObservableCollection<EnemyEntry> Enemies
    {
        get => DatablockManager.Instance.Get<EnemyEntry>();
    }
    public ObservableCollection<ChainedPuzzleTypeEntry> ChainedPuzzleTypes
    {
        get => DatablockManager.Instance.Get<ChainedPuzzleTypeEntry>();
    }
    public ICommand RemoveChainedPuzzleItemCommand { get; }
    public ICommand AddChainedPuzzleItemCommand { get; }

    public ObservableCollection<AlarmBlock> Alarms
    {
        get => DatablockManager.Instance.Get<AlarmBlock>();
    }

    public ObservableCollection<AlarmBlock> AvailableTemplates { get; }

    public AlarmsViewModel()
        : this(DatablockManager.Instance.Get<AlarmBlock>(), DatablockManager.Instance.Get<EnemyEntry>(), DatablockManager.Instance.Get<ChainedPuzzleTypeEntry>())
    {
    }

    public AlarmsViewModel(IEnumerable<AlarmBlock> initialAlarms, IEnumerable<EnemyEntry>? enemies = null, IEnumerable<ChainedPuzzleTypeEntry>? chainedPuzzleTypes = null)
    {
        var alarmList = initialAlarms.ToList();
        _vanillaTemplates = alarmList.Select(a => a.Clone()).ToList();
        var alarms = Alarms;
        alarms.Clear();
        foreach (var alarm in alarmList)
        {
            alarms.Add(alarm);
        }
        alarms.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasExistingAlarms));
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        };

        AvailableTemplates = new ObservableCollection<AlarmBlock>(_vanillaTemplates);

        if (enemies != null && !ReferenceEquals(Enemies, enemies))
        {
            var enemyList = enemies.ToList();
            Enemies.Clear();
            foreach (var enemy in enemyList) Enemies.Add(enemy);
        }

        if (chainedPuzzleTypes != null && !ReferenceEquals(ChainedPuzzleTypes, chainedPuzzleTypes))
        {
            var puzzleTypesList = chainedPuzzleTypes.ToList();
            ChainedPuzzleTypes.Clear();
            foreach (var puzzleType in puzzleTypesList) ChainedPuzzleTypes.Add(puzzleType);
        }

        RemoveChainedPuzzleItemCommand = new RelayCommand<ChainedPuzzleItem>(RemoveChainedPuzzleItem);
        AddChainedPuzzleItemCommand = new RelayCommand(AddChainedPuzzleItem);

        if (alarms.Count > 0)
        {
            SelectedAlarm = alarms[0];
        }

        UpdateStatusText();
    }

    public AlarmBlock? SelectedAlarm
    {
        get => _selectedAlarm;
        set
        {
            if (_selectedAlarm != null)
            {
                _selectedAlarm.PropertyChanged -= SelectedAlarm_PropertyChanged;
            }

            if (SetField(ref _selectedAlarm, value))
            {
                if (_selectedAlarm != null)
                {
                    _selectedAlarm.PropertyChanged += SelectedAlarm_PropertyChanged;
                }

                UpdateStatusText();
                OnPropertyChanged(nameof(HasSelectedAlarm));
                OnPropertyChanged(nameof(HasDuplicatePersistentId));
                OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            }
        }
    }

    private void SelectedAlarm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AlarmBlock.PersistentId))
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        }
    }

    public bool HasSelectedAlarm => _selectedAlarm != null;

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public bool HasDuplicatePersistentId
    {
        get
        {
            if (_selectedAlarm == null) return false;
            return Alarms.Count(a => a.PersistentId == _selectedAlarm.PersistentId) > 1;
        }
    }

    public string DuplicatePersistentIdWarning
    {
        get
        {
            if (!HasDuplicatePersistentId || _selectedAlarm == null) return string.Empty;
            int count = Alarms.Count(a => a.PersistentId == _selectedAlarm.PersistentId);
            return $"Warning: Persistent ID {_selectedAlarm.PersistentId} is used by {count} alarms in this project. Persistent IDs should be unique.";
        }
    }

    // --- Create Alarm Workflow ---

    public bool HasExistingAlarms => Alarms.Count > 0;

    public bool IsCreatingAlarm
    {
        get => _isCreatingAlarm;
        set => SetField(ref _isCreatingAlarm, value);
    }

    public bool IsCreatingFresh
    {
        get => _isCreatingFresh;
        set
        {
            if (SetField(ref _isCreatingFresh, value) && value)
            {
                _isCreatingFromExisting = false;
                _isCreatingFromVanilla = false;
                OnPropertyChanged(nameof(IsCreatingFromExisting));
                OnPropertyChanged(nameof(IsCreatingFromVanilla));
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
            }
        }
    }

    public bool IsCreatingFromExisting
    {
        get => _isCreatingFromExisting;
        set
        {
            if (SetField(ref _isCreatingFromExisting, value) && value)
            {
                _isCreatingFresh = false;
                _isCreatingFromVanilla = false;
                OnPropertyChanged(nameof(IsCreatingFresh));
                OnPropertyChanged(nameof(IsCreatingFromVanilla));
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
                if (SelectedExistingAlarm != null)
                {
                    NewAlarmPublicAlarmName = SelectedExistingAlarm.PublicAlarmName;
                    if (!string.IsNullOrWhiteSpace(SelectedExistingAlarm.Name))
                    {
                        NewAlarmName = $"{SelectedExistingAlarm.Name}_Copy";
                    }
                }
            }
        }
    }

    public bool IsCreatingFromVanilla
    {
        get => _isCreatingFromVanilla;
        set
        {
            if (SetField(ref _isCreatingFromVanilla, value) && value)
            {
                _isCreatingFresh = false;
                _isCreatingFromExisting = false;
                OnPropertyChanged(nameof(IsCreatingFresh));
                OnPropertyChanged(nameof(IsCreatingFromExisting));
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
                if (SelectedVanillaTemplate != null)
                {
                    NewAlarmPublicAlarmName = SelectedVanillaTemplate.PublicAlarmName;
                    if (!string.IsNullOrWhiteSpace(SelectedVanillaTemplate.Name))
                    {
                        NewAlarmName = SelectedVanillaTemplate.Name;
                    }
                }
            }
        }
    }

    public bool IsCreatingFromTemplate
    {
        get => _isCreatingFromVanilla || _isCreatingFromExisting;
        set
        {
            if (value)
            {
                IsCreatingFromVanilla = true;
            }
            else
            {
                IsCreatingFresh = true;
            }
        }
    }

    public AlarmBlock? SelectedExistingAlarm
    {
        get => _selectedExistingAlarm;
        set
        {
            if (SetField(ref _selectedExistingAlarm, value) && value != null)
            {
                if (IsCreatingFromExisting)
                {
                    NewAlarmPublicAlarmName = value.PublicAlarmName;
                    if (!string.IsNullOrWhiteSpace(value.Name))
                    {
                        NewAlarmName = $"{value.Name}_Copy";
                    }
                }
            }
        }
    }

    public AlarmBlock? SelectedVanillaTemplate
    {
        get => _selectedVanillaTemplate;
        set
        {
            if (SetField(ref _selectedVanillaTemplate, value) && value != null)
            {
                if (IsCreatingFromVanilla || IsCreatingFromTemplate)
                {
                    NewAlarmPublicAlarmName = value.PublicAlarmName;
                    if (!string.IsNullOrWhiteSpace(value.Name))
                    {
                        NewAlarmName = value.Name;
                    }
                }
            }
        }
    }

    public AlarmBlock? SelectedTemplate
    {
        get => _isCreatingFromExisting ? _selectedExistingAlarm : _selectedVanillaTemplate;
        set
        {
            if (_isCreatingFromExisting)
            {
                SelectedExistingAlarm = value;
            }
            else
            {
                SelectedVanillaTemplate = value;
            }
        }
    }

    public string NewAlarmName
    {
        get => _newAlarmName;
        set => SetField(ref _newAlarmName, value);
    }

    public string NewAlarmPublicAlarmName
    {
        get => _newAlarmPublicAlarmName;
        set => SetField(ref _newAlarmPublicAlarmName, value);
    }

    public uint NewAlarmPersistentId
    {
        get => _newAlarmPersistentId;
        set => SetField(ref _newAlarmPersistentId, value);
    }

    public string CreationErrorMessage
    {
        get => _creationErrorMessage;
        set
        {
            if (SetField(ref _creationErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasCreationErrorMessage));
            }
        }
    }

    public bool HasCreationErrorMessage => !string.IsNullOrEmpty(_creationErrorMessage);

    public void OpenCreateAlarmDialog()
    {
        uint randomId = GenerateUniquePersistentId();
        NewAlarmPersistentId = randomId;
        NewAlarmName = $"Alarm_Custom_{randomId}";
        NewAlarmPublicAlarmName = string.Empty;
        IsCreatingFresh = true;
        _isCreatingFromExisting = false;
        _isCreatingFromVanilla = false;
        OnPropertyChanged(nameof(IsCreatingFresh));
        OnPropertyChanged(nameof(IsCreatingFromExisting));
        OnPropertyChanged(nameof(IsCreatingFromVanilla));
        OnPropertyChanged(nameof(IsCreatingFromTemplate));

        SelectedExistingAlarm = _selectedAlarm ?? Alarms.FirstOrDefault();
        SelectedVanillaTemplate = AvailableTemplates.FirstOrDefault();
        CreationErrorMessage = string.Empty;
        IsCreatingAlarm = true;
    }

    public void OpenDuplicateAlarmDialog(AlarmBlock? alarm = null)
    {
        var target = alarm ?? _selectedAlarm ?? Alarms.FirstOrDefault();
        OpenCreateAlarmDialog();
        if (target != null)
        {
            SelectedExistingAlarm = target;
            IsCreatingFromExisting = true;
        }
    }

    private uint GenerateUniquePersistentId()
    {
        var rng = new Random();
        for (int i = 0; i < 1000; i++)
        {
            uint candidate = (uint)rng.Next(1000, 999999);
            if (!Alarms.Any(a => a.PersistentId == candidate))
            {
                return candidate;
            }
        }
        return (uint)(Alarms.Count > 0 ? Alarms.Max(a => a.PersistentId) + 1 : 1);
    }

    public bool ConfirmCreateAlarm()
    {
        if (string.IsNullOrWhiteSpace(NewAlarmName))
        {
            CreationErrorMessage = "Alarm Name cannot be empty.";
            return false;
        }

        if (NewAlarmPersistentId == 0)
        {
            CreationErrorMessage = "Persistent ID must be greater than 0.";
            return false;
        }

        if (Alarms.Any(a => a.PersistentId == NewAlarmPersistentId))
        {
            CreationErrorMessage = $"An alarm with Persistent ID {NewAlarmPersistentId} already exists.";
            return false;
        }

        AlarmBlock newAlarm;
        if (IsCreatingFromExisting)
        {
            if (SelectedExistingAlarm == null)
            {
                CreationErrorMessage = "Please select an existing alarm to duplicate.";
                return false;
            }
            newAlarm = AlarmService.CreateAlarmFromTemplate(SelectedExistingAlarm, NewAlarmPersistentId, NewAlarmName.Trim());
        }
        else if (IsCreatingFromVanilla)
        {
            if (SelectedVanillaTemplate == null)
            {
                CreationErrorMessage = "Please select a vanilla alarm to import.";
                return false;
            }
            newAlarm = AlarmService.CreateAlarmFromTemplate(SelectedVanillaTemplate, NewAlarmPersistentId, NewAlarmName.Trim());
        }
        else
        {
            newAlarm = AlarmService.CreateFreshAlarm(NewAlarmPersistentId, NewAlarmName.Trim());
        }

        newAlarm.PublicAlarmName = NewAlarmPublicAlarmName?.Trim() ?? string.Empty;

        Alarms.Add(newAlarm);
        SelectedAlarm = newAlarm;

        IsCreatingAlarm = false;
        CreationErrorMessage = string.Empty;
        UpdateStatusText();
        StatusText = $"Created alarm [{newAlarm.PersistentId}] {newAlarm.Name}";
        return true;
    }

    public void CancelCreateAlarm()
    {
        IsCreatingAlarm = false;
        CreationErrorMessage = string.Empty;
    }

    // --- Delete Alarm Workflow ---

    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        set => SetField(ref _isConfirmingDelete, value);
    }

    public AlarmBlock? AlarmToDelete
    {
        get => _alarmToDelete;
        set
        {
            if (SetField(ref _alarmToDelete, value))
            {
                OnPropertyChanged(nameof(DeleteConfirmationMessage));
            }
        }
    }

    public string DeleteConfirmationMessage
    {
        get
        {
            var target = _alarmToDelete ?? _selectedAlarm;
            if (target == null) return "Are you sure you want to delete this alarm?";
            return $"Are you sure you want to delete alarm [{target.PersistentId}] \"{target.Name}\"?";
        }
    }

    public void RequestDeleteSelectedAlarm()
    {
        if (_selectedAlarm == null) return;
        RequestDeleteAlarm(_selectedAlarm);
    }

    public void RequestDeleteAlarm(AlarmBlock? alarm)
    {
        if (alarm == null) return;
        AlarmToDelete = alarm;
        IsConfirmingDelete = true;
    }

    public bool ConfirmDeleteAlarm()
    {
        var target = AlarmToDelete ?? _selectedAlarm;
        IsConfirmingDelete = false;
        AlarmToDelete = null;

        if (target == null) return false;
        return DeleteAlarm(target);
    }

    public void CancelDeleteAlarm()
    {
        IsConfirmingDelete = false;
        AlarmToDelete = null;
    }

    public bool DeleteSelectedAlarm()
    {
        if (_selectedAlarm == null) return false;
        return DeleteAlarm(_selectedAlarm);
    }

    public bool DeleteAlarm(AlarmBlock? alarm)
    {
        if (alarm == null) return false;
        int index = Alarms.IndexOf(alarm);
        if (index < 0) return false;

        string deletedName = alarm.Name;
        uint deletedId = alarm.PersistentId;

        Alarms.RemoveAt(index);

        if (_selectedAlarm == alarm)
        {
            if (Alarms.Count > 0)
            {
                int newIndex = Math.Min(index, Alarms.Count - 1);
                SelectedAlarm = Alarms[newIndex];
            }
            else
            {
                SelectedAlarm = null;
            }
        }

        UpdateStatusText();
        StatusText = $"Deleted alarm [{deletedId}] {deletedName}. {Alarms.Count:N0} alarms remaining.";
        return true;
    }

    public void AddChainedPuzzleItem()
    {
        if (SelectedAlarm == null) return;
        if (SelectedAlarm.ChainedPuzzle == null)
        {
            SelectedAlarm.ChainedPuzzle = new ObservableCollection<ChainedPuzzleItem>();
        }
        uint defaultType = ChainedPuzzleTypes.FirstOrDefault()?.PersistentId ?? 6;
        SelectedAlarm.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = defaultType });
    }

    public void RemoveChainedPuzzleItem(ChainedPuzzleItem? item)
    {
        if (SelectedAlarm == null || item == null) return;
        SelectedAlarm.ChainedPuzzle?.Remove(item);
    }

    private void UpdateStatusText()
    {
        StatusText = $"{Alarms.Count:N0} alarms loaded";
    }
}
