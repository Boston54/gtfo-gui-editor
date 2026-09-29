using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class AlarmsViewModel : ViewModelBase
{
    private readonly List<AlarmBlock> _vanillaTemplates;
    private ObservableCollection<AlarmBlock> _alarms;
    private AlarmBlock? _selectedAlarm;
    private string _statusText = string.Empty;

    // Create alarm modal state
    private bool _isCreatingAlarm;
    private bool _isCreatingFresh = true;
    private bool _isCreatingFromTemplate;
    private AlarmBlock? _selectedTemplate;
    private string _newAlarmName = string.Empty;
    private uint _newAlarmPersistentId;
    private string _creationErrorMessage = string.Empty;

    // Delete alarm confirmation modal state
    private bool _isConfirmingDelete;
    private AlarmBlock? _alarmToDelete;

    public static SurvivalWaveSpawnType[] AvailableWaveSpawnTypes => Enum.GetValues<SurvivalWaveSpawnType>();
    public static WaveFilterType[] AvailableFilterTypes => Enum.GetValues<WaveFilterType>();

    public SurvivalWaveSpawnType[] WaveSpawnTypes => AvailableWaveSpawnTypes;
    public WaveFilterType[] FilterTypes => AvailableFilterTypes;

    public ObservableCollection<EnemyEntry> Enemies { get; }

    public ObservableCollection<AlarmBlock> Alarms
    {
        get => _alarms;
        private set => SetField(ref _alarms, value);
    }

    public ObservableCollection<AlarmBlock> AvailableTemplates { get; }

    public AlarmsViewModel()
        : this(AlarmService.LoadVanillaAlarms(), AlarmService.LoadEnemies())
    {
    }

    public AlarmsViewModel(IEnumerable<AlarmBlock> initialAlarms, IEnumerable<EnemyEntry>? enemies = null)
    {
        var alarmList = initialAlarms.ToList();
        _vanillaTemplates = alarmList.Select(a => a.Clone()).ToList();
        _alarms = new ObservableCollection<AlarmBlock>(alarmList);
        _alarms.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        };

        AvailableTemplates = new ObservableCollection<AlarmBlock>(_vanillaTemplates);

        var enemyList = (enemies ?? AlarmService.LoadEnemies()).ToList();
        Enemies = new ObservableCollection<EnemyEntry>(enemyList);

        if (_alarms.Count > 0)
        {
            SelectedAlarm = _alarms[0];
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
            return _alarms.Count(a => a.PersistentId == _selectedAlarm.PersistentId) > 1;
        }
    }

    public string DuplicatePersistentIdWarning
    {
        get
        {
            if (!HasDuplicatePersistentId || _selectedAlarm == null) return string.Empty;
            int count = _alarms.Count(a => a.PersistentId == _selectedAlarm.PersistentId);
            return $"Warning: Persistent ID {_selectedAlarm.PersistentId} is used by {count} alarms in this project. Persistent IDs should be unique.";
        }
    }

    // --- Create Alarm Workflow ---

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
                IsCreatingFromTemplate = false;
            }
        }
    }

    public bool IsCreatingFromTemplate
    {
        get => _isCreatingFromTemplate;
        set
        {
            if (SetField(ref _isCreatingFromTemplate, value) && value)
            {
                IsCreatingFresh = false;
            }
        }
    }

    public AlarmBlock? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetField(ref _selectedTemplate, value);
    }

    public string NewAlarmName
    {
        get => _newAlarmName;
        set => SetField(ref _newAlarmName, value);
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
        IsCreatingFresh = true;
        IsCreatingFromTemplate = false;
        SelectedTemplate = AvailableTemplates.FirstOrDefault();
        CreationErrorMessage = string.Empty;
        IsCreatingAlarm = true;
    }

    private uint GenerateUniquePersistentId()
    {
        var rng = new Random();
        for (int i = 0; i < 1000; i++)
        {
            uint candidate = (uint)rng.Next(1000, 999999);
            if (!_alarms.Any(a => a.PersistentId == candidate))
            {
                return candidate;
            }
        }
        return (uint)(_alarms.Count > 0 ? _alarms.Max(a => a.PersistentId) + 1 : 1);
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

        if (_alarms.Any(a => a.PersistentId == NewAlarmPersistentId))
        {
            CreationErrorMessage = $"An alarm with Persistent ID {NewAlarmPersistentId} already exists.";
            return false;
        }

        AlarmBlock newAlarm;
        if (IsCreatingFromTemplate)
        {
            if (SelectedTemplate == null)
            {
                CreationErrorMessage = "Please select a template alarm.";
                return false;
            }
            newAlarm = AlarmService.CreateAlarmFromTemplate(SelectedTemplate, NewAlarmPersistentId, NewAlarmName.Trim());
        }
        else
        {
            newAlarm = AlarmService.CreateFreshAlarm(NewAlarmPersistentId, NewAlarmName.Trim());
        }

        _alarms.Add(newAlarm);
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
        int index = _alarms.IndexOf(alarm);
        if (index < 0) return false;

        string deletedName = alarm.Name;
        uint deletedId = alarm.PersistentId;

        _alarms.RemoveAt(index);

        if (_selectedAlarm == alarm)
        {
            if (_alarms.Count > 0)
            {
                int newIndex = Math.Min(index, _alarms.Count - 1);
                SelectedAlarm = _alarms[newIndex];
            }
            else
            {
                SelectedAlarm = null;
            }
        }

        UpdateStatusText();
        StatusText = $"Deleted alarm [{deletedId}] {deletedName}. {_alarms.Count:N0} alarms remaining.";
        return true;
    }

    private void UpdateStatusText()
    {
        StatusText = $"{_alarms.Count:N0} alarms loaded";
    }
}
