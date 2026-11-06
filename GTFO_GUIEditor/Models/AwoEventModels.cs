using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Input;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoReactorState
{
    Intro,
    Wave,
    Verify,
    Idle
}

public class AwoReactorData : INotifyPropertyChanged
{
    private AwoReactorState _state = AwoReactorState.Intro;
    private int _wave = 1;
    private double _progress = 0.0;

    public AwoReactorState State
    {
        get => _state;
        set => SetField(ref _state, value);
    }

    public int Wave
    {
        get => _wave;
        set => SetField(ref _wave, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    public AwoReactorData Clone()
    {
        return new AwoReactorData
        {
            State = State,
            Wave = Wave,
            Progress = Progress
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoCountdownProgressEvent : INotifyPropertyChanged
{
    private double _progress = 0.0;
    private ObservableCollection<WardenEvent> _events = new();

    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, Math.Clamp(value, 0.0, 1.0));
    }

    public ObservableCollection<WardenEvent> Events
    {
        get => _events;
        set => SetField(ref _events, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddEvent() => Events.Add(new WardenEvent());
    public void RemoveEvent(WardenEvent? ev) { if (ev != null) Events.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveEventCommand => new RelayCommand<WardenEvent>(RemoveEvent);

    public AwoCountdownProgressEvent Clone()
    {
        return new AwoCountdownProgressEvent
        {
            Progress = Progress,
            Events = new ObservableCollection<WardenEvent>(Events.Select(e => e.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoCountdownData : INotifyPropertyChanged
{
    private bool _canShowHours = true;
    private string _titleText = string.Empty;
    private string _timerColor = "red";
    private ObservableCollection<AwoCountdownProgressEvent> _eventsOnProgress = new();
    private ObservableCollection<WardenEvent> _eventsOnDone = new();

    public bool CanShowHours
    {
        get => _canShowHours;
        set => SetField(ref _canShowHours, value);
    }

    public string TitleText
    {
        get => _titleText;
        set => SetField(ref _titleText, value ?? string.Empty);
    }

    public string TimerColor
    {
        get => _timerColor;
        set => SetField(ref _timerColor, value ?? string.Empty);
    }

    public ObservableCollection<AwoCountdownProgressEvent> EventsOnProgress
    {
        get => _eventsOnProgress;
        set => SetField(ref _eventsOnProgress, value ?? new ObservableCollection<AwoCountdownProgressEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnDone
    {
        get => _eventsOnDone;
        set => SetField(ref _eventsOnDone, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddProgressEvent() => EventsOnProgress.Add(new AwoCountdownProgressEvent());
    public void RemoveProgressEvent(AwoCountdownProgressEvent? item) { if (item != null) EventsOnProgress.Remove(item); }

    public void AddDoneEvent() => EventsOnDone.Add(new WardenEvent());
    public void RemoveDoneEvent(WardenEvent? ev) { if (ev != null) EventsOnDone.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveProgressEventCommand => new RelayCommand<AwoCountdownProgressEvent>(RemoveProgressEvent);
    [JsonIgnore]
    public ICommand RemoveDoneEventCommand => new RelayCommand<WardenEvent>(RemoveDoneEvent);

    public AwoCountdownData Clone()
    {
        return new AwoCountdownData
        {
            CanShowHours = CanShowHours,
            TitleText = TitleText,
            TimerColor = TimerColor,
            EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent>(EventsOnProgress.Select(p => p.Clone())),
            EventsOnDone = new ObservableCollection<WardenEvent>(EventsOnDone.Select(e => e.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoZoneLightType
{
    RevertToOriginal,
    SetZoneLightData,
    TransitionToOriginal
}

public class AwoSetZoneLightData : INotifyPropertyChanged
{
    private AwoZoneLightType _type = AwoZoneLightType.SetZoneLightData;
    private uint _lightDataId = 0;
    private double _transitionDuration = 1.0;
    private int _seed = 0;

    public AwoZoneLightType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public uint LightDataID
    {
        get => _lightDataId;
        set => SetField(ref _lightDataId, value);
    }

    public double TransitionDuration
    {
        get => _transitionDuration;
        set => SetField(ref _transitionDuration, value);
    }

    public int Seed
    {
        get => _seed;
        set => SetField(ref _seed, value);
    }

    public AwoSetZoneLightData Clone()
    {
        return new AwoSetZoneLightData
        {
            Type = Type,
            LightDataID = LightDataID,
            TransitionDuration = TransitionDuration,
            Seed = Seed
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoCleanupType
{
    Despawn,
    Kill
}

public class AwoAreaItem : INotifyPropertyChanged
{
    private int _areaIndex = 0;

    public int AreaIndex
    {
        get => _areaIndex;
        set => SetField(ref _areaIndex, value);
    }

    public AwoAreaItem Clone() => new() { AreaIndex = AreaIndex };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoEnemyIdItem : INotifyPropertyChanged
{
    private uint _enemyId = 0;

    public uint EnemyID
    {
        get => _enemyId;
        set => SetField(ref _enemyId, value);
    }

    public AwoEnemyIdItem Clone() => new() { EnemyID = EnemyID };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoCleanupEnemiesData : INotifyPropertyChanged
{
    private AwoCleanupType _type = AwoCleanupType.Despawn;
    private int _areaIndex = -1;
    private ObservableCollection<AwoAreaItem> _areaBlacklist = new();
    private bool _includeHibernate = true;
    private bool _includeAggressive = true;
    private bool _includeScout = true;
    private ObservableCollection<AwoEnemyIdItem> _excludeEnemyIDs = new();
    private ObservableCollection<AwoEnemyIdItem> _includeOnlyIDs = new();

    public AwoCleanupType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public int AreaIndex
    {
        get => _areaIndex;
        set => SetField(ref _areaIndex, value);
    }

    public ObservableCollection<AwoAreaItem> AreaBlacklist
    {
        get => _areaBlacklist;
        set => SetField(ref _areaBlacklist, value ?? new ObservableCollection<AwoAreaItem>());
    }

    public bool IncludeHibernate
    {
        get => _includeHibernate;
        set => SetField(ref _includeHibernate, value);
    }

    public bool IncludeAggressive
    {
        get => _includeAggressive;
        set => SetField(ref _includeAggressive, value);
    }

    public bool IncludeScout
    {
        get => _includeScout;
        set => SetField(ref _includeScout, value);
    }

    public ObservableCollection<AwoEnemyIdItem> ExcludeEnemyIDs
    {
        get => _excludeEnemyIDs;
        set => SetField(ref _excludeEnemyIDs, value ?? new ObservableCollection<AwoEnemyIdItem>());
    }

    public ObservableCollection<AwoEnemyIdItem> IncludeOnlyIDs
    {
        get => _includeOnlyIDs;
        set => SetField(ref _includeOnlyIDs, value ?? new ObservableCollection<AwoEnemyIdItem>());
    }

    public void AddAreaBlacklist() => AreaBlacklist.Add(new AwoAreaItem());
    public void RemoveAreaBlacklist(AwoAreaItem? item) { if (item != null) AreaBlacklist.Remove(item); }

    public void AddExcludeEnemy() => ExcludeEnemyIDs.Add(new AwoEnemyIdItem());
    public void RemoveExcludeEnemy(AwoEnemyIdItem? item) { if (item != null) ExcludeEnemyIDs.Remove(item); }

    public void AddIncludeOnlyEnemy() => IncludeOnlyIDs.Add(new AwoEnemyIdItem());
    public void RemoveIncludeOnlyEnemy(AwoEnemyIdItem? item) { if (item != null) IncludeOnlyIDs.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveAreaBlacklistCommand => new RelayCommand<AwoAreaItem>(RemoveAreaBlacklist);
    [JsonIgnore]
    public ICommand RemoveExcludeEnemyCommand => new RelayCommand<AwoEnemyIdItem>(RemoveExcludeEnemy);
    [JsonIgnore]
    public ICommand RemoveIncludeOnlyEnemyCommand => new RelayCommand<AwoEnemyIdItem>(RemoveIncludeOnlyEnemy);

    public AwoCleanupEnemiesData Clone()
    {
        return new AwoCleanupEnemiesData
        {
            Type = Type,
            AreaIndex = AreaIndex,
            AreaBlacklist = new ObservableCollection<AwoAreaItem>(AreaBlacklist.Select(a => a.Clone())),
            IncludeHibernate = IncludeHibernate,
            IncludeAggressive = IncludeAggressive,
            IncludeScout = IncludeScout,
            ExcludeEnemyIDs = new ObservableCollection<AwoEnemyIdItem>(ExcludeEnemyIDs.Select(e => e.Clone())),
            IncludeOnlyIDs = new ObservableCollection<AwoEnemyIdItem>(IncludeOnlyIDs.Select(i => i.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoSpawnHibernatesData : INotifyPropertyChanged
{
    private int _areaIndex = -1;
    private ObservableCollection<AwoAreaItem> _areaBlacklist = new();
    private uint _enemyId = 0;
    private double _placementRandomWeight = 0.25;
    private double _placementScore = 3.0;
    private bool _resetPlacementInfo = false;
    private Vector3Data _position = new();
    private Vector3Data _rotation = new();
    private bool _randomizeRotation = false;

    public AwoSpawnHibernatesData()
    {
        _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
        _rotation.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Rotation));
    }

    public int AreaIndex
    {
        get => _areaIndex;
        set => SetField(ref _areaIndex, value);
    }

    public ObservableCollection<AwoAreaItem> AreaBlacklist
    {
        get => _areaBlacklist;
        set => SetField(ref _areaBlacklist, value ?? new ObservableCollection<AwoAreaItem>());
    }

    public uint EnemyID
    {
        get => _enemyId;
        set => SetField(ref _enemyId, value);
    }

    public double PlacementRandomWeight
    {
        get => _placementRandomWeight;
        set => SetField(ref _placementRandomWeight, value);
    }

    public double PlacementScore
    {
        get => _placementScore;
        set => SetField(ref _placementScore, value);
    }

    public bool ResetPlacementInfo
    {
        get => _resetPlacementInfo;
        set => SetField(ref _resetPlacementInfo, value);
    }

    public Vector3Data Position
    {
        get => _position;
        set
        {
            var old = _position;
            if (SetField(ref _position, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Position));
                _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
            }
        }
    }

    public Vector3Data Rotation
    {
        get => _rotation;
        set
        {
            var old = _rotation;
            if (SetField(ref _rotation, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Rotation));
                _rotation.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Rotation));
            }
        }
    }

    public bool RandomizeRotation
    {
        get => _randomizeRotation;
        set => SetField(ref _randomizeRotation, value);
    }

    public void AddAreaBlacklist() => AreaBlacklist.Add(new AwoAreaItem());
    public void RemoveAreaBlacklist(AwoAreaItem? item) { if (item != null) AreaBlacklist.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveAreaBlacklistCommand => new RelayCommand<AwoAreaItem>(RemoveAreaBlacklist);

    public AwoSpawnHibernatesData Clone()
    {
        return new AwoSpawnHibernatesData
        {
            AreaIndex = AreaIndex,
            AreaBlacklist = new ObservableCollection<AwoAreaItem>(AreaBlacklist.Select(a => a.Clone())),
            EnemyID = EnemyID,
            PlacementRandomWeight = PlacementRandomWeight,
            PlacementScore = PlacementScore,
            ResetPlacementInfo = ResetPlacementInfo,
            Position = Position.Clone(),
            Rotation = Rotation.Clone(),
            RandomizeRotation = RandomizeRotation
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoSpawnScoutsData : INotifyPropertyChanged
{
    private int _areaIndex = -1;
    private ObservableCollection<AwoAreaItem> _areaBlacklist = new();
    private ScoutType _scoutType = ScoutType.Scout;
    private int _groupType = 3;
    private int _difficulty = 0;

    public int AreaIndex
    {
        get => _areaIndex;
        set => SetField(ref _areaIndex, value);
    }

    public ObservableCollection<AwoAreaItem> AreaBlacklist
    {
        get => _areaBlacklist;
        set => SetField(ref _areaBlacklist, value ?? new ObservableCollection<AwoAreaItem>());
    }

    public ScoutType ScoutType
    {
        get => _scoutType;
        set
        {
            if (SetField(ref _scoutType, value))
            {
                var (gt, diff) = value.ToGroupTypeAndDifficulty();
                _groupType = gt;
                _difficulty = diff;
                OnPropertyChanged(nameof(GroupType));
                OnPropertyChanged(nameof(Difficulty));
            }
        }
    }

    public int GroupType
    {
        get => _groupType;
        set
        {
            if (SetField(ref _groupType, value))
            {
                var mapped = ScoutTypeExtensions.FromGroupTypeAndDifficulty(_groupType, _difficulty);
                if (_scoutType != mapped)
                {
                    _scoutType = mapped;
                    OnPropertyChanged(nameof(ScoutType));
                }
            }
        }
    }

    public int Difficulty
    {
        get => _difficulty;
        set
        {
            if (SetField(ref _difficulty, value))
            {
                var mapped = ScoutTypeExtensions.FromGroupTypeAndDifficulty(_groupType, _difficulty);
                if (_scoutType != mapped)
                {
                    _scoutType = mapped;
                    OnPropertyChanged(nameof(ScoutType));
                }
            }
        }
    }

    public void AddAreaBlacklist() => AreaBlacklist.Add(new AwoAreaItem());
    public void RemoveAreaBlacklist(AwoAreaItem? item) { if (item != null) AreaBlacklist.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveAreaBlacklistCommand => new RelayCommand<AwoAreaItem>(RemoveAreaBlacklist);

    public AwoSpawnScoutsData Clone()
    {
        return new AwoSpawnScoutsData
        {
            AreaIndex = AreaIndex,
            AreaBlacklist = new ObservableCollection<AwoAreaItem>(AreaBlacklist.Select(a => a.Clone())),
            ScoutType = ScoutType,
            GroupType = GroupType,
            Difficulty = Difficulty
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoAddTerminalCommandData : INotifyPropertyChanged
{
    private int _terminalIndex = 0;
    private int _commandNumber = 0;
    private string _command = string.Empty;
    private string _commandDesc = string.Empty;
    private bool _autoIndentCommandDesc = false;
    private ObservableCollection<TerminalPostCommandOutput> _postCommandOutputs = new();
    private ObservableCollection<WardenEvent> _commandEvents = new();
    private bool _progressWaitBeforeEvents = false;
    private SpecialCommandRule _specialCommandRule = SpecialCommandRule.Normal;

    public int TerminalIndex
    {
        get => _terminalIndex;
        set => SetField(ref _terminalIndex, value);
    }

    public int CommandNumber
    {
        get => _commandNumber;
        set => SetField(ref _commandNumber, value);
    }

    public string Command
    {
        get => _command;
        set => SetField(ref _command, value ?? string.Empty);
    }

    public string CommandDesc
    {
        get => _commandDesc;
        set => SetField(ref _commandDesc, value ?? string.Empty);
    }

    public bool AutoIndentCommandDesc
    {
        get => _autoIndentCommandDesc;
        set => SetField(ref _autoIndentCommandDesc, value);
    }

    public ObservableCollection<TerminalPostCommandOutput> PostCommandOutputs
    {
        get => _postCommandOutputs;
        set => SetField(ref _postCommandOutputs, value ?? new ObservableCollection<TerminalPostCommandOutput>());
    }

    public ObservableCollection<WardenEvent> CommandEvents
    {
        get => _commandEvents;
        set => SetField(ref _commandEvents, value ?? new ObservableCollection<WardenEvent>());
    }

    public bool ProgressWaitBeforeEvents
    {
        get => _progressWaitBeforeEvents;
        set => SetField(ref _progressWaitBeforeEvents, value);
    }

    public SpecialCommandRule SpecialCommandRule
    {
        get => _specialCommandRule;
        set => SetField(ref _specialCommandRule, value);
    }

    public void AddPostCommandOutput() => PostCommandOutputs.Add(new TerminalPostCommandOutput());
    public void RemovePostCommandOutput(TerminalPostCommandOutput? item) { if (item != null) PostCommandOutputs.Remove(item); }

    public void AddCommandEvent() => CommandEvents.Add(new WardenEvent());
    public void RemoveCommandEvent(WardenEvent? ev) { if (ev != null) CommandEvents.Remove(ev); }

    [JsonIgnore]
    public ICommand RemovePostCommandOutputCommand => new RelayCommand<TerminalPostCommandOutput>(RemovePostCommandOutput);
    [JsonIgnore]
    public ICommand RemoveCommandEventCommand => new RelayCommand<WardenEvent>(RemoveCommandEvent);

    public AwoAddTerminalCommandData Clone()
    {
        return new AwoAddTerminalCommandData
        {
            TerminalIndex = TerminalIndex,
            CommandNumber = CommandNumber,
            Command = Command,
            CommandDesc = CommandDesc,
            AutoIndentCommandDesc = AutoIndentCommandDesc,
            PostCommandOutputs = new ObservableCollection<TerminalPostCommandOutput>(PostCommandOutputs.Select(p => p.Clone())),
            CommandEvents = new ObservableCollection<WardenEvent>(CommandEvents.Select(e => e.Clone())),
            ProgressWaitBeforeEvents = ProgressWaitBeforeEvents,
            SpecialCommandRule = SpecialCommandRule
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoHideTerminalCommandData : INotifyPropertyChanged
{
    private int _terminalIndex = 0;
    private TERM_Command _commandEnum = TERM_Command.None;
    private int _commandNumber = 0;
    private bool _deleteCommand = false;
    private bool _isAwoCommand = false;

    public int TerminalIndex
    {
        get => _terminalIndex;
        set => SetField(ref _terminalIndex, value);
    }

    public TERM_Command CommandEnum
    {
        get => _commandEnum;
        set => SetField(ref _commandEnum, value);
    }

    public int CommandNumber
    {
        get => _commandNumber;
        set => SetField(ref _commandNumber, value);
    }

    public bool DeleteCommand
    {
        get => _deleteCommand;
        set => SetField(ref _deleteCommand, value);
    }

    public bool IsAwoCommand
    {
        get => _isAwoCommand;
        set => SetField(ref _isAwoCommand, value);
    }

    public AwoHideTerminalCommandData Clone()
    {
        return new AwoHideTerminalCommandData
        {
            TerminalIndex = TerminalIndex,
            CommandEnum = CommandEnum,
            CommandNumber = CommandNumber,
            DeleteCommand = DeleteCommand,
            IsAwoCommand = IsAwoCommand
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoUnhideTerminalCommandData : INotifyPropertyChanged
{
    private int _terminalIndex = 0;
    private int _commandNumber = 0;
    private TERM_Command _commandEnum = TERM_Command.None;
    private bool _isAwoCommand = false;

    public int TerminalIndex
    {
        get => _terminalIndex;
        set => SetField(ref _terminalIndex, value);
    }

    public int CommandNumber
    {
        get => _commandNumber;
        set => SetField(ref _commandNumber, value);
    }

    public TERM_Command CommandEnum
    {
        get => _commandEnum;
        set => SetField(ref _commandEnum, value);
    }

    public bool IsAwoCommand
    {
        get => _isAwoCommand;
        set => SetField(ref _isAwoCommand, value);
    }

    public AwoUnhideTerminalCommandData Clone()
    {
        return new AwoUnhideTerminalCommandData
        {
            TerminalIndex = TerminalIndex,
            CommandNumber = CommandNumber,
            CommandEnum = CommandEnum,
            IsAwoCommand = IsAwoCommand
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoActiveEnemyWaveData : INotifyPropertyChanged
{
    private bool _hasActiveEnemyWave = false;
    private uint _enemyGroupInfrontOfDoor = 0;
    private uint _enemyGroupInArea = 0;
    private uint _enemyGroupsInArea = 0;

    public bool HasActiveEnemyWave
    {
        get => _hasActiveEnemyWave;
        set => SetField(ref _hasActiveEnemyWave, value);
    }

    public uint EnemyGroupInfrontOfDoor
    {
        get => _enemyGroupInfrontOfDoor;
        set => SetField(ref _enemyGroupInfrontOfDoor, value);
    }

    public uint EnemyGroupInArea
    {
        get => _enemyGroupInArea;
        set => SetField(ref _enemyGroupInArea, value);
    }

    public uint EnemyGroupsInArea
    {
        get => _enemyGroupsInArea;
        set => SetField(ref _enemyGroupsInArea, value);
    }

    public AwoActiveEnemyWaveData Clone()
    {
        return new AwoActiveEnemyWaveData
        {
            HasActiveEnemyWave = HasActiveEnemyWave,
            EnemyGroupInfrontOfDoor = EnemyGroupInfrontOfDoor,
            EnemyGroupInArea = EnemyGroupInArea,
            EnemyGroupsInArea = EnemyGroupsInArea
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoGiveResourceData : INotifyPropertyChanged
{
    private double _health = 0.0;
    private double _mainAmmo = 0.0;
    private double _specialAmmo = 0.0;
    private double _toolAmmo = 0.0;
    private bool _playerWoods = true;   // Slot 0
    private bool _playerDauda = true;   // Slot 1
    private bool _playerHackett = true; // Slot 2
    private bool _playerBishop = true;  // Slot 3
    private bool _fullTeamOverflow = true;
    private bool _useLocation = false;
    private bool _includeSupplyEfficiency = false;

    public double Health
    {
        get => _health;
        set => SetField(ref _health, value);
    }

    public double MainAmmo
    {
        get => _mainAmmo;
        set => SetField(ref _mainAmmo, value);
    }

    public double SpecialAmmo
    {
        get => _specialAmmo;
        set => SetField(ref _specialAmmo, value);
    }

    public double ToolAmmo
    {
        get => _toolAmmo;
        set => SetField(ref _toolAmmo, value);
    }

    public bool PlayerWoods
    {
        get => _playerWoods;
        set => SetField(ref _playerWoods, value);
    }

    public bool PlayerDauda
    {
        get => _playerDauda;
        set => SetField(ref _playerDauda, value);
    }

    public bool PlayerHackett
    {
        get => _playerHackett;
        set => SetField(ref _playerHackett, value);
    }

    public bool PlayerBishop
    {
        get => _playerBishop;
        set => SetField(ref _playerBishop, value);
    }

    public bool FullTeamOverflow
    {
        get => _fullTeamOverflow;
        set => SetField(ref _fullTeamOverflow, value);
    }

    public bool UseLocation
    {
        get => _useLocation;
        set => SetField(ref _useLocation, value);
    }

    public bool IncludeSupplyEfficiency
    {
        get => _includeSupplyEfficiency;
        set => SetField(ref _includeSupplyEfficiency, value);
    }

    public List<int> GetPlayerFilter()
    {
        var list = new List<int>();
        if (PlayerWoods) list.Add(0);
        if (PlayerDauda) list.Add(1);
        if (PlayerHackett) list.Add(2);
        if (PlayerBishop) list.Add(3);
        return list;
    }

    public void SetPlayerFilter(IEnumerable<int>? filter)
    {
        if (filter == null)
        {
            PlayerWoods = true;
            PlayerDauda = true;
            PlayerHackett = true;
            PlayerBishop = true;
            return;
        }

        var set = new HashSet<int>(filter);
        PlayerWoods = set.Contains(0);
        PlayerDauda = set.Contains(1);
        PlayerHackett = set.Contains(2);
        PlayerBishop = set.Contains(3);
    }

    public AwoGiveResourceData Clone()
    {
        return new AwoGiveResourceData
        {
            Health = Health,
            MainAmmo = MainAmmo,
            SpecialAmmo = SpecialAmmo,
            ToolAmmo = ToolAmmo,
            PlayerWoods = PlayerWoods,
            PlayerDauda = PlayerDauda,
            PlayerHackett = PlayerHackett,
            PlayerBishop = PlayerBishop,
            FullTeamOverflow = FullTeamOverflow,
            UseLocation = UseLocation,
            IncludeSupplyEfficiency = IncludeSupplyEfficiency
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

// ==================== 20K EVENTS MODELS ====================

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoSuccessScreenType
{
    SetSuccessScreen,
    FlashFakeScreen
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoPlayerCharacter
{
    Woods = 0,
    Dauda = 1,
    Hackett = 2,
    Bishop = 3
}

public class AwoCameraShakeData : INotifyPropertyChanged
{
    private double _radius = 0.0;
    private double _amplitude = 0.0;
    private double _frequency = 0.0;
    private bool _directional = true;

    public double Radius
    {
        get => _radius;
        set => SetField(ref _radius, value);
    }

    public double Amplitude
    {
        get => _amplitude;
        set => SetField(ref _amplitude, value);
    }

    public double Frequency
    {
        get => _frequency;
        set => SetField(ref _frequency, value);
    }

    public bool Directional
    {
        get => _directional;
        set => SetField(ref _directional, value);
    }

    public AwoCameraShakeData Clone()
    {
        return new AwoCameraShakeData
        {
            Radius = Radius,
            Amplitude = Amplitude,
            Frequency = Frequency,
            Directional = Directional
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoNestedEventType
{
    ActivateAll,
    RandomAny,
    RandomWeighted
}

public class AwoWheelOfEventItem : INotifyPropertyChanged
{
    private string _debugName = string.Empty;
    private double _weight = 0.0;
    private int _repeatCount = 1;
    private bool _isInfinite = false;
    private ObservableCollection<WardenEvent> _events = new();

    public string DebugName
    {
        get => _debugName;
        set => SetField(ref _debugName, value);
    }

    public double Weight
    {
        get => _weight;
        set => SetField(ref _weight, value);
    }

    public int RepeatCount
    {
        get => _repeatCount;
        set => SetField(ref _repeatCount, value);
    }

    public bool IsInfinite
    {
        get => _isInfinite;
        set => SetField(ref _isInfinite, value);
    }

    public ObservableCollection<WardenEvent> Events
    {
        get => _events;
        set => SetField(ref _events, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddEvent() => Events.Add(new WardenEvent());
    public void RemoveEvent(WardenEvent? ev) { if (ev != null) Events.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveEventCommand => new RelayCommand<WardenEvent>(RemoveEvent);

    public AwoWheelOfEventItem Clone()
    {
        return new AwoWheelOfEventItem
        {
            DebugName = DebugName,
            Weight = Weight,
            RepeatCount = RepeatCount,
            IsInfinite = IsInfinite,
            Events = new ObservableCollection<WardenEvent>(Events.Select(e => e.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoNestedEventData : INotifyPropertyChanged
{
    private AwoNestedEventType _type = AwoNestedEventType.ActivateAll;
    private int _maxRandomEvents = 1;
    private bool _allowRepeatsInRandom = false;
    private bool _playerWoods = true;
    private bool _playerDauda = true;
    private bool _playerHackett = true;
    private bool _playerBishop = true;
    private ObservableCollection<WardenEvent> _eventsToActivate = new();
    private ObservableCollection<AwoWheelOfEventItem> _wheelOfEvents = new();

    public AwoNestedEventType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public int MaxRandomEvents
    {
        get => _maxRandomEvents;
        set => SetField(ref _maxRandomEvents, value);
    }

    public bool AllowRepeatsInRandom
    {
        get => _allowRepeatsInRandom;
        set => SetField(ref _allowRepeatsInRandom, value);
    }

    public bool PlayerWoods
    {
        get => _playerWoods;
        set => SetField(ref _playerWoods, value);
    }

    public bool PlayerDauda
    {
        get => _playerDauda;
        set => SetField(ref _playerDauda, value);
    }

    public bool PlayerHackett
    {
        get => _playerHackett;
        set => SetField(ref _playerHackett, value);
    }

    public bool PlayerBishop
    {
        get => _playerBishop;
        set => SetField(ref _playerBishop, value);
    }

    public ObservableCollection<WardenEvent> EventsToActivate
    {
        get => _eventsToActivate;
        set => SetField(ref _eventsToActivate, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<AwoWheelOfEventItem> WheelOfEvents
    {
        get => _wheelOfEvents;
        set => SetField(ref _wheelOfEvents, value ?? new ObservableCollection<AwoWheelOfEventItem>());
    }

    public void AddEventToActivate() => EventsToActivate.Add(new WardenEvent());
    public void RemoveEventToActivate(WardenEvent? ev) { if (ev != null) EventsToActivate.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveEventToActivateCommand => new RelayCommand<WardenEvent>(RemoveEventToActivate);

    public void AddWheelItem() => WheelOfEvents.Add(new AwoWheelOfEventItem());
    public void RemoveWheelItem(AwoWheelOfEventItem? item) { if (item != null) WheelOfEvents.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveWheelItemCommand => new RelayCommand<AwoWheelOfEventItem>(RemoveWheelItem);

    public List<int> GetPlayerFilter()
    {
        var list = new List<int>();
        if (PlayerWoods) list.Add(0);
        if (PlayerDauda) list.Add(1);
        if (PlayerHackett) list.Add(2);
        if (PlayerBishop) list.Add(3);
        return list;
    }

    public void SetPlayerFilter(IEnumerable<int>? filter)
    {
        if (filter == null)
        {
            PlayerWoods = true;
            PlayerDauda = true;
            PlayerHackett = true;
            PlayerBishop = true;
            return;
        }

        var set = new HashSet<int>(filter);
        PlayerWoods = set.Contains(0);
        PlayerDauda = set.Contains(1);
        PlayerHackett = set.Contains(2);
        PlayerBishop = set.Contains(3);
    }

    public AwoNestedEventData Clone()
    {
        var clone = new AwoNestedEventData
        {
            Type = Type,
            MaxRandomEvents = MaxRandomEvents,
            AllowRepeatsInRandom = AllowRepeatsInRandom,
            PlayerWoods = PlayerWoods,
            PlayerDauda = PlayerDauda,
            PlayerHackett = PlayerHackett,
            PlayerBishop = PlayerBishop,
            EventsToActivate = new ObservableCollection<WardenEvent>(EventsToActivate.Select(e => e.Clone())),
            WheelOfEvents = new ObservableCollection<AwoWheelOfEventItem>(WheelOfEvents.Select(w => w.Clone()))
        };
        return clone;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoEventLoopData : INotifyPropertyChanged
{
    private int _loopIndex = 0;
    private double _loopDelay = 1.0;
    private int _loopCount = -1;
    private ObservableCollection<WardenEvent> _eventsToActivate = new();

    public int LoopIndex
    {
        get => _loopIndex;
        set => SetField(ref _loopIndex, value);
    }

    public double LoopDelay
    {
        get => _loopDelay;
        set => SetField(ref _loopDelay, value);
    }

    public int LoopCount
    {
        get => _loopCount;
        set => SetField(ref _loopCount, value);
    }

    public ObservableCollection<WardenEvent> EventsToActivate
    {
        get => _eventsToActivate;
        set => SetField(ref _eventsToActivate, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddEventToActivate() => EventsToActivate.Add(new WardenEvent());
    public void RemoveEventToActivate(WardenEvent? ev) { if (ev != null) EventsToActivate.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveEventToActivateCommand => new RelayCommand<WardenEvent>(RemoveEventToActivate);

    public AwoEventLoopData Clone()
    {
        return new AwoEventLoopData
        {
            LoopIndex = LoopIndex,
            LoopDelay = LoopDelay,
            LoopCount = LoopCount,
            EventsToActivate = new ObservableCollection<WardenEvent>(EventsToActivate.Select(e => e.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public enum AwoTeleportLookDirection
{
    Forward = 0,
    Left = 1,
    Right = 2,
    Backward = 3,
    RetainCurrent = 4
}

public class AwoTeleportFromLocationData : INotifyPropertyChanged
{
    private bool _enabled = false;
    private int _dimensionIndex = 0;
    private SectorType _layer = SectorType.Main;
    private int _localIndex = 0;

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public int DimensionIndex
    {
        get => _dimensionIndex;
        set => SetField(ref _dimensionIndex, value);
    }

    public SectorType Layer
    {
        get => _layer;
        set => SetField(ref _layer, value);
    }

    public int LocalIndex
    {
        get => _localIndex;
        set => SetField(ref _localIndex, value);
    }

    public AwoTeleportFromLocationData Clone()
    {
        return new AwoTeleportFromLocationData
        {
            Enabled = Enabled,
            DimensionIndex = DimensionIndex,
            Layer = Layer,
            LocalIndex = LocalIndex
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoTPDataItem : INotifyPropertyChanged
{
    private int _playerIndex = 0;
    private int _dimensionIndex = 0;
    private Vector3Data _position = new();
    private string _worldEventObjectFilter = string.Empty;
    private bool _useLookDirectionVector = false;
    private AwoTeleportLookDirection _lookDirection = AwoTeleportLookDirection.Forward;
    private Vector3Data _lookDirectionV3 = new();
    private bool _playWarpAnimation = false;
    private double _flashDuration = 0.0;

    public AwoTPDataItem()
    {
        _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
        _lookDirectionV3.PropertyChanged += (s, e) => OnPropertyChanged(nameof(LookDirectionV3));
    }

    [JsonIgnore]
    public AwoPlayerCharacter Player
    {
        get => (AwoPlayerCharacter)Math.Clamp(_playerIndex, 0, 3);
        set => PlayerIndex = (int)value;
    }

    public int PlayerIndex
    {
        get => _playerIndex;
        set
        {
            if (SetField(ref _playerIndex, value))
            {
                OnPropertyChanged(nameof(Player));
            }
        }
    }

    public int DimensionIndex
    {
        get => _dimensionIndex;
        set => SetField(ref _dimensionIndex, value);
    }

    public Vector3Data Position
    {
        get => _position;
        set
        {
            var old = _position;
            if (SetField(ref _position, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Position));
                _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
            }
        }
    }

    public string WorldEventObjectFilter
    {
        get => _worldEventObjectFilter;
        set => SetField(ref _worldEventObjectFilter, value);
    }

    public bool UseLookDirectionVector
    {
        get => _useLookDirectionVector;
        set => SetField(ref _useLookDirectionVector, value);
    }

    public AwoTeleportLookDirection LookDirection
    {
        get => _lookDirection;
        set => SetField(ref _lookDirection, value);
    }

    public Vector3Data LookDirectionV3
    {
        get => _lookDirectionV3;
        set
        {
            var old = _lookDirectionV3;
            if (SetField(ref _lookDirectionV3, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(LookDirectionV3));
                _lookDirectionV3.PropertyChanged += (s, e) => OnPropertyChanged(nameof(LookDirectionV3));
            }
        }
    }

    public bool PlayWarpAnimation
    {
        get => _playWarpAnimation;
        set => SetField(ref _playWarpAnimation, value);
    }

    public double FlashDuration
    {
        get => _flashDuration;
        set => SetField(ref _flashDuration, value);
    }

    public AwoTPDataItem Clone()
    {
        return new AwoTPDataItem
        {
            PlayerIndex = PlayerIndex,
            DimensionIndex = DimensionIndex,
            Position = Position?.Clone() ?? new Vector3Data(),
            WorldEventObjectFilter = WorldEventObjectFilter,
            UseLookDirectionVector = UseLookDirectionVector,
            LookDirection = LookDirection,
            LookDirectionV3 = LookDirectionV3?.Clone() ?? new Vector3Data(),
            PlayWarpAnimation = PlayWarpAnimation,
            FlashDuration = FlashDuration
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoTeleportPlayerData : INotifyPropertyChanged
{
    private bool _flashTeleport = false;
    private bool _warpSentries = true;
    private bool _warpBigPickups = true;
    private bool _sendBigPickupsToHost = false;
    private bool _fullTeamOverflow = false;
    private AwoTeleportFromLocationData _fromLocation = new();
    private ObservableCollection<AwoTPDataItem> _tpData = new();

    public AwoTeleportPlayerData()
    {
        _fromLocation.PropertyChanged += (s, e) => OnPropertyChanged(nameof(FromLocation));
    }

    public bool FlashTeleport
    {
        get => _flashTeleport;
        set => SetField(ref _flashTeleport, value);
    }

    public bool WarpSentries
    {
        get => _warpSentries;
        set => SetField(ref _warpSentries, value);
    }

    public bool WarpBigPickups
    {
        get => _warpBigPickups;
        set => SetField(ref _warpBigPickups, value);
    }

    public bool SendBigPickupsToHost
    {
        get => _sendBigPickupsToHost;
        set => SetField(ref _sendBigPickupsToHost, value);
    }

    public bool FullTeamOverflow
    {
        get => _fullTeamOverflow;
        set => SetField(ref _fullTeamOverflow, value);
    }

    public AwoTeleportFromLocationData FromLocation
    {
        get => _fromLocation;
        set
        {
            var old = _fromLocation;
            if (SetField(ref _fromLocation, value ?? new AwoTeleportFromLocationData()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(FromLocation));
                _fromLocation.PropertyChanged += (s, e) => OnPropertyChanged(nameof(FromLocation));
            }
        }
    }

    public ObservableCollection<AwoTPDataItem> TPData
    {
        get => _tpData;
        set => SetField(ref _tpData, value ?? new ObservableCollection<AwoTPDataItem>());
    }

    public void AddTPData() => TPData.Add(new AwoTPDataItem());
    public void RemoveTPData(AwoTPDataItem? item) { if (item != null) TPData.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveTPDataCommand => new RelayCommand<AwoTPDataItem>(RemoveTPData);

    public AwoTeleportPlayerData Clone()
    {
        return new AwoTeleportPlayerData
        {
            FlashTeleport = FlashTeleport,
            WarpSentries = WarpSentries,
            WarpBigPickups = WarpBigPickups,
            SendBigPickupsToHost = SendBigPickupsToHost,
            FullTeamOverflow = FullTeamOverflow,
            FromLocation = FromLocation?.Clone() ?? new AwoTeleportFromLocationData(),
            TPData = new ObservableCollection<AwoTPDataItem>(TPData.Select(t => t.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoInfectPlayerData : INotifyPropertyChanged
{
    private bool _playerWoods = true;
    private bool _playerDauda = true;
    private bool _playerHackett = true;
    private bool _playerBishop = true;
    private bool _fullTeamOverflow = true;
    private double _infectionAmount = 0.0;
    private bool _infectOverTime = false;
    private double _interval = 1.0;
    private bool _useZone = false;

    public bool PlayerWoods
    {
        get => _playerWoods;
        set => SetField(ref _playerWoods, value);
    }

    public bool PlayerDauda
    {
        get => _playerDauda;
        set => SetField(ref _playerDauda, value);
    }

    public bool PlayerHackett
    {
        get => _playerHackett;
        set => SetField(ref _playerHackett, value);
    }

    public bool PlayerBishop
    {
        get => _playerBishop;
        set => SetField(ref _playerBishop, value);
    }

    public bool FullTeamOverflow
    {
        get => _fullTeamOverflow;
        set => SetField(ref _fullTeamOverflow, value);
    }

    public double InfectionAmount
    {
        get => _infectionAmount;
        set => SetField(ref _infectionAmount, value);
    }

    public bool InfectOverTime
    {
        get => _infectOverTime;
        set => SetField(ref _infectOverTime, value);
    }

    public double Interval
    {
        get => _interval;
        set => SetField(ref _interval, value);
    }

    public bool UseZone
    {
        get => _useZone;
        set => SetField(ref _useZone, value);
    }

    public List<int> GetPlayerFilter()
    {
        var list = new List<int>();
        if (PlayerWoods) list.Add(0);
        if (PlayerDauda) list.Add(1);
        if (PlayerHackett) list.Add(2);
        if (PlayerBishop) list.Add(3);
        return list;
    }

    public void SetPlayerFilter(IEnumerable<int>? filter)
    {
        if (filter == null)
        {
            PlayerWoods = true;
            PlayerDauda = true;
            PlayerHackett = true;
            PlayerBishop = true;
            return;
        }

        var set = new HashSet<int>(filter);
        PlayerWoods = set.Contains(0);
        PlayerDauda = set.Contains(1);
        PlayerHackett = set.Contains(2);
        PlayerBishop = set.Contains(3);
    }

    public AwoInfectPlayerData Clone()
    {
        return new AwoInfectPlayerData
        {
            PlayerWoods = PlayerWoods,
            PlayerDauda = PlayerDauda,
            PlayerHackett = PlayerHackett,
            PlayerBishop = PlayerBishop,
            FullTeamOverflow = FullTeamOverflow,
            InfectionAmount = InfectionAmount,
            InfectOverTime = InfectOverTime,
            Interval = Interval,
            UseZone = UseZone
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoDamagePlayerData : INotifyPropertyChanged
{
    private bool _playerWoods = true;
    private bool _playerDauda = true;
    private bool _playerHackett = true;
    private bool _playerBishop = true;
    private bool _fullTeamOverflow = true;
    private double _damageAmount = 0.0;
    private bool _dealPercentageDamage = false;
    private bool _damageOverTime = false;
    private double _interval = 1.0;
    private bool _useZone = false;

    public bool PlayerWoods
    {
        get => _playerWoods;
        set => SetField(ref _playerWoods, value);
    }

    public bool PlayerDauda
    {
        get => _playerDauda;
        set => SetField(ref _playerDauda, value);
    }

    public bool PlayerHackett
    {
        get => _playerHackett;
        set => SetField(ref _playerHackett, value);
    }

    public bool PlayerBishop
    {
        get => _playerBishop;
        set => SetField(ref _playerBishop, value);
    }

    public bool FullTeamOverflow
    {
        get => _fullTeamOverflow;
        set => SetField(ref _fullTeamOverflow, value);
    }

    public double DamageAmount
    {
        get => _damageAmount;
        set => SetField(ref _damageAmount, value);
    }

    public bool DealPercentageDamage
    {
        get => _dealPercentageDamage;
        set => SetField(ref _dealPercentageDamage, value);
    }

    public bool DamageOverTime
    {
        get => _damageOverTime;
        set => SetField(ref _damageOverTime, value);
    }

    public double Interval
    {
        get => _interval;
        set => SetField(ref _interval, value);
    }

    public bool UseZone
    {
        get => _useZone;
        set => SetField(ref _useZone, value);
    }

    public List<int> GetPlayerFilter()
    {
        var list = new List<int>();
        if (PlayerWoods) list.Add(0);
        if (PlayerDauda) list.Add(1);
        if (PlayerHackett) list.Add(2);
        if (PlayerBishop) list.Add(3);
        return list;
    }

    public void SetPlayerFilter(IEnumerable<int>? filter)
    {
        if (filter == null)
        {
            PlayerWoods = true;
            PlayerDauda = true;
            PlayerHackett = true;
            PlayerBishop = true;
            return;
        }

        var set = new HashSet<int>(filter);
        PlayerWoods = set.Contains(0);
        PlayerDauda = set.Contains(1);
        PlayerHackett = set.Contains(2);
        PlayerBishop = set.Contains(3);
    }

    public AwoDamagePlayerData Clone()
    {
        return new AwoDamagePlayerData
        {
            PlayerWoods = PlayerWoods,
            PlayerDauda = PlayerDauda,
            PlayerHackett = PlayerHackett,
            PlayerBishop = PlayerBishop,
            FullTeamOverflow = FullTeamOverflow,
            DamageAmount = DamageAmount,
            DealPercentageDamage = DealPercentageDamage,
            DamageOverTime = DamageOverTime,
            Interval = Interval,
            UseZone = UseZone
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoRevivePlayerData : INotifyPropertyChanged
{
    private bool _playerWoods = true;
    private bool _playerDauda = true;
    private bool _playerHackett = true;
    private bool _playerBishop = true;
    private bool _fullTeamOverflow = true;

    public bool PlayerWoods
    {
        get => _playerWoods;
        set => SetField(ref _playerWoods, value);
    }

    public bool PlayerDauda
    {
        get => _playerDauda;
        set => SetField(ref _playerDauda, value);
    }

    public bool PlayerHackett
    {
        get => _playerHackett;
        set => SetField(ref _playerHackett, value);
    }

    public bool PlayerBishop
    {
        get => _playerBishop;
        set => SetField(ref _playerBishop, value);
    }

    public bool FullTeamOverflow
    {
        get => _fullTeamOverflow;
        set => SetField(ref _fullTeamOverflow, value);
    }

    public List<int> GetPlayerFilter()
    {
        var list = new List<int>();
        if (PlayerWoods) list.Add(0);
        if (PlayerDauda) list.Add(1);
        if (PlayerHackett) list.Add(2);
        if (PlayerBishop) list.Add(3);
        return list;
    }

    public void SetPlayerFilter(IEnumerable<int>? filter)
    {
        if (filter == null)
        {
            PlayerWoods = true;
            PlayerDauda = true;
            PlayerHackett = true;
            PlayerBishop = true;
            return;
        }

        var set = new HashSet<int>(filter);
        PlayerWoods = set.Contains(0);
        PlayerDauda = set.Contains(1);
        PlayerHackett = set.Contains(2);
        PlayerBishop = set.Contains(3);
    }

    public AwoRevivePlayerData Clone()
    {
        return new AwoRevivePlayerData
        {
            PlayerWoods = PlayerWoods,
            PlayerDauda = PlayerDauda,
            PlayerHackett = PlayerHackett,
            PlayerBishop = PlayerBishop,
            FullTeamOverflow = FullTeamOverflow
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoAdjustTimerData : INotifyPropertyChanged
{
    private double _duration = 0.0;
    private double _speed = 0.0;
    private bool _updateTitleText = false;
    private string _titleText = string.Empty;
    private bool _updateBodyText = false;
    private string _bodyText = string.Empty;
    private bool _updateColor = false;
    private string _timerColor = "red";
    private bool _updateSpecialHud = false;
    private int _specialHudIndex = 0;
    private double _specialHudTimeModifier = 0.0;

    public double Duration
    {
        get => _duration;
        set => SetField(ref _duration, value);
    }

    public double Speed
    {
        get => _speed;
        set => SetField(ref _speed, value);
    }

    public bool UpdateTitleText
    {
        get => _updateTitleText;
        set => SetField(ref _updateTitleText, value);
    }

    public string TitleText
    {
        get => _titleText;
        set => SetField(ref _titleText, value);
    }

    public bool UpdateBodyText
    {
        get => _updateBodyText;
        set => SetField(ref _updateBodyText, value);
    }

    public string BodyText
    {
        get => _bodyText;
        set => SetField(ref _bodyText, value);
    }

    public bool UpdateColor
    {
        get => _updateColor;
        set => SetField(ref _updateColor, value);
    }

    public string TimerColor
    {
        get => _timerColor;
        set => SetField(ref _timerColor, value);
    }

    public bool UpdateSpecialHud
    {
        get => _updateSpecialHud;
        set => SetField(ref _updateSpecialHud, value);
    }

    public int SpecialHudIndex
    {
        get => _specialHudIndex;
        set => SetField(ref _specialHudIndex, value);
    }

    public double SpecialHudTimeModifier
    {
        get => _specialHudTimeModifier;
        set => SetField(ref _specialHudTimeModifier, value);
    }

    public AwoAdjustTimerData Clone()
    {
        return new AwoAdjustTimerData
        {
            Duration = Duration,
            Speed = Speed,
            UpdateTitleText = UpdateTitleText,
            TitleText = TitleText,
            UpdateBodyText = UpdateBodyText,
            BodyText = BodyText,
            UpdateColor = UpdateColor,
            TimerColor = TimerColor,
            UpdateSpecialHud = UpdateSpecialHud,
            SpecialHudIndex = SpecialHudIndex,
            SpecialHudTimeModifier = SpecialHudTimeModifier
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoCountupData : INotifyPropertyChanged
{
    private double _startValue = 0.0;
    private double _speed = 1.0;
    private string _titleText = string.Empty;
    private string _bodyText = "[COUNTUP]";
    private string _timerColor = "red";
    private int _decimalPoints = 0;
    private ObservableCollection<AwoCountdownProgressEvent> _eventsOnProgress = new();
    private ObservableCollection<WardenEvent> _eventsOnDone = new();

    public double StartValue
    {
        get => _startValue;
        set => SetField(ref _startValue, value);
    }

    public double Speed
    {
        get => _speed;
        set => SetField(ref _speed, value);
    }

    public string TitleText
    {
        get => _titleText;
        set => SetField(ref _titleText, value);
    }

    public string BodyText
    {
        get => _bodyText;
        set => SetField(ref _bodyText, value);
    }

    public string TimerColor
    {
        get => _timerColor;
        set => SetField(ref _timerColor, value);
    }

    public int DecimalPoints
    {
        get => _decimalPoints;
        set => SetField(ref _decimalPoints, value);
    }

    public ObservableCollection<AwoCountdownProgressEvent> EventsOnProgress
    {
        get => _eventsOnProgress;
        set => SetField(ref _eventsOnProgress, value ?? new ObservableCollection<AwoCountdownProgressEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnDone
    {
        get => _eventsOnDone;
        set => SetField(ref _eventsOnDone, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddProgressEvent() => EventsOnProgress.Add(new AwoCountdownProgressEvent());
    public void RemoveProgressEvent(AwoCountdownProgressEvent? ev) { if (ev != null) EventsOnProgress.Remove(ev); }

    public void AddDoneEvent() => EventsOnDone.Add(new WardenEvent());
    public void RemoveDoneEvent(WardenEvent? ev) { if (ev != null) EventsOnDone.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveProgressEventCommand => new RelayCommand<AwoCountdownProgressEvent>(RemoveProgressEvent);
    [JsonIgnore]
    public ICommand RemoveDoneEventCommand => new RelayCommand<WardenEvent>(RemoveDoneEvent);

    public AwoCountupData Clone()
    {
        return new AwoCountupData
        {
            StartValue = StartValue,
            Speed = Speed,
            TitleText = TitleText,
            BodyText = BodyText,
            TimerColor = TimerColor,
            DecimalPoints = DecimalPoints,
            EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent>(EventsOnProgress.Select(p => p.Clone())),
            EventsOnDone = new ObservableCollection<WardenEvent>(EventsOnDone.Select(e => e.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoNavMarkerData : INotifyPropertyChanged
{
    private int _index = 0;
    private int _style = 10;
    private string _title = string.Empty;
    private string _color = "#B36FF6";
    private bool _usePin = true;

    public int Index
    {
        get => _index;
        set => SetField(ref _index, value);
    }

    public int Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    public bool UsePin
    {
        get => _usePin;
        set => SetField(ref _usePin, value);
    }

    public AwoNavMarkerData Clone()
    {
        return new AwoNavMarkerData
        {
            Index = Index,
            Style = Style,
            Title = Title,
            Color = Color,
            UsePin = UsePin
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoPortalData : INotifyPropertyChanged
{
    private int _targetDimension = 1;
    private double _teleportDelay = 5.0;
    private bool _preventPortalWarpTeamEvent = false;

    public int TargetDimension
    {
        get => _targetDimension;
        set => SetField(ref _targetDimension, value);
    }

    public double TeleportDelay
    {
        get => _teleportDelay;
        set => SetField(ref _teleportDelay, value);
    }

    public bool PreventPortalWarpTeamEvent
    {
        get => _preventPortalWarpTeamEvent;
        set => SetField(ref _preventPortalWarpTeamEvent, value);
    }

    public AwoPortalData Clone()
    {
        return new AwoPortalData
        {
            TargetDimension = TargetDimension,
            TeleportDelay = TeleportDelay,
            PreventPortalWarpTeamEvent = PreventPortalWarpTeamEvent
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoSuccessScreenData : INotifyPropertyChanged
{
    private bool _hasSuccessScreen = false;
    private AwoSuccessScreenType _type = AwoSuccessScreenType.SetSuccessScreen;
    private uint _customSuccessScreen = 0;
    private uint _overrideMusic = 0;

    public bool HasSuccessScreen
    {
        get => _hasSuccessScreen;
        set => SetField(ref _hasSuccessScreen, value);
    }

    public AwoSuccessScreenType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public uint CustomSuccessScreen
    {
        get => _customSuccessScreen;
        set => SetField(ref _customSuccessScreen, value);
    }

    public uint OverrideMusic
    {
        get => _overrideMusic;
        set => SetField(ref _overrideMusic, value);
    }

    public AwoSuccessScreenData Clone()
    {
        return new AwoSuccessScreenData
        {
            HasSuccessScreen = HasSuccessScreen,
            Type = Type,
            CustomSuccessScreen = CustomSuccessScreen,
            OverrideMusic = OverrideMusic
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoMultiProgressionItem : INotifyPropertyChanged
{
    private string _customSubObjectiveHeader = string.Empty;
    private string _customSubObjective = string.Empty;
    private int _index = 0;
    private int _priority = 1;
    private SectorType _layer = SectorType.Main;
    private bool _isLayerIndependent = true;
    private uint _overrideTag = 0;

    public string CustomSubObjectiveHeader
    {
        get => _customSubObjectiveHeader;
        set => SetField(ref _customSubObjectiveHeader, value);
    }

    public string CustomSubObjective
    {
        get => _customSubObjective;
        set => SetField(ref _customSubObjective, value);
    }

    public int Index
    {
        get => _index;
        set => SetField(ref _index, value);
    }

    public int Priority
    {
        get => _priority;
        set => SetField(ref _priority, value);
    }

    public SectorType Layer
    {
        get => _layer;
        set => SetField(ref _layer, value);
    }

    public bool IsLayerIndependent
    {
        get => _isLayerIndependent;
        set => SetField(ref _isLayerIndependent, value);
    }

    public uint OverrideTag
    {
        get => _overrideTag;
        set => SetField(ref _overrideTag, value);
    }

    public AwoMultiProgressionItem Clone()
    {
        return new AwoMultiProgressionItem
        {
            CustomSubObjectiveHeader = CustomSubObjectiveHeader,
            CustomSubObjective = CustomSubObjective,
            Index = Index,
            Priority = Priority,
            Layer = Layer,
            IsLayerIndependent = IsLayerIndependent,
            OverrideTag = OverrideTag
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoMultiProgressionData : INotifyPropertyChanged
{
    private ObservableCollection<AwoMultiProgressionItem> _items = new();

    public ObservableCollection<AwoMultiProgressionItem> Items
    {
        get => _items;
        set => SetField(ref _items, value ?? new ObservableCollection<AwoMultiProgressionItem>());
    }

    public void AddItem() => Items.Add(new AwoMultiProgressionItem());
    public void RemoveItem(AwoMultiProgressionItem? item) { if (item != null) Items.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveItemCommand => new RelayCommand<AwoMultiProgressionItem>(RemoveItem);

    public AwoMultiProgressionData Clone()
    {
        return new AwoMultiProgressionData
        {
            Items = new ObservableCollection<AwoMultiProgressionItem>(Items.Select(i => i.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoRoarSound
{
    Striker,
    Shooter,
    Birther,
    Shadow,
    Tank,
    Flyer,
    Immortal,
    Bullrush,
    Pouncer,
    Striker_Berserk,
    Shooter_Spread
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoRoarSize
{
    Small,
    Medium,
    Big
}

public class AwoWaveRoarSoundData : INotifyPropertyChanged
{
    private AwoRoarSound _roarSound = AwoRoarSound.Striker;
    private AwoRoarSize _roarSize = AwoRoarSize.Small;
    private bool _isOutside = false;

    public AwoRoarSound RoarSound
    {
        get => _roarSound;
        set => SetField(ref _roarSound, value);
    }

    public AwoRoarSize RoarSize
    {
        get => _roarSize;
        set => SetField(ref _roarSize, value);
    }

    public bool IsOutside
    {
        get => _isOutside;
        set => SetField(ref _isOutside, value);
    }

    public AwoWaveRoarSoundData Clone()
    {
        return new AwoWaveRoarSoundData
        {
            RoarSound = RoarSound,
            RoarSize = RoarSize,
            IsOutside = IsOutside
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoCustomHudData : INotifyPropertyChanged
{
    private string _titleText = string.Empty;
    private string _bodyText = string.Empty;

    public string TitleText
    {
        get => _titleText;
        set => SetField(ref _titleText, value);
    }

    public string BodyText
    {
        get => _bodyText;
        set => SetField(ref _bodyText, value);
    }

    public AwoCustomHudData Clone()
    {
        return new AwoCustomHudData
        {
            TitleText = TitleText,
            BodyText = BodyText
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoSpecialHudType
{
    StartTimer,
    StartIndexTimer,
    StartPersistent,
    StopIndex,
    StopAll
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoSpecialHudStyle
{
    Default,
    Message,
    Bioscan,
    Warning,
    BioscanAlarm
}

public class AwoSpecialHudData : INotifyPropertyChanged
{
    private AwoSpecialHudType _type = AwoSpecialHudType.StartTimer;
    private int _index = 0;
    private string _message = string.Empty;
    private AwoSpecialHudStyle _style = AwoSpecialHudStyle.Default;
    private int _priority = -2;
    private bool _showTimeInProgressBar = true;
    private bool _invertProgress = false;
    private ObservableCollection<AwoCountdownProgressEvent> _eventsOnProgress = new();
    private ObservableCollection<WardenEvent> _eventsOnDone = new();

    public AwoSpecialHudType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public int Index
    {
        get => _index;
        set => SetField(ref _index, value);
    }

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public AwoSpecialHudStyle Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }

    public int Priority
    {
        get => _priority;
        set => SetField(ref _priority, value);
    }

    public bool ShowTimeInProgressBar
    {
        get => _showTimeInProgressBar;
        set => SetField(ref _showTimeInProgressBar, value);
    }

    public bool InvertProgress
    {
        get => _invertProgress;
        set => SetField(ref _invertProgress, value);
    }

    public ObservableCollection<AwoCountdownProgressEvent> EventsOnProgress
    {
        get => _eventsOnProgress;
        set => SetField(ref _eventsOnProgress, value ?? new ObservableCollection<AwoCountdownProgressEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnDone
    {
        get => _eventsOnDone;
        set => SetField(ref _eventsOnDone, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddProgressEvent() => EventsOnProgress.Add(new AwoCountdownProgressEvent());
    public void RemoveProgressEvent(AwoCountdownProgressEvent? ev) { if (ev != null) EventsOnProgress.Remove(ev); }

    public void AddDoneEvent() => EventsOnDone.Add(new WardenEvent());
    public void RemoveDoneEvent(WardenEvent? ev) { if (ev != null) EventsOnDone.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveProgressEventCommand => new RelayCommand<AwoCountdownProgressEvent>(RemoveProgressEvent);
    [JsonIgnore]
    public ICommand RemoveDoneEventCommand => new RelayCommand<WardenEvent>(RemoveDoneEvent);

    public AwoSpecialHudData Clone()
    {
        return new AwoSpecialHudData
        {
            Type = Type,
            Index = Index,
            Message = Message,
            Style = Style,
            Priority = Priority,
            ShowTimeInProgressBar = ShowTimeInProgressBar,
            InvertProgress = InvertProgress,
            EventsOnProgress = new ObservableCollection<AwoCountdownProgressEvent>(EventsOnProgress.Select(p => p.Clone())),
            EventsOnDone = new ObservableCollection<WardenEvent>(EventsOnDone.Select(e => e.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoPlayerDialogueType
{
    Closest,
    Specific,
    Random
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoDialogueIntensityState
{
    Exploration,
    Stealth,
    Encounter,
    Combat
}

public class AwoPlayerDialogueData : INotifyPropertyChanged
{
    private AwoPlayerDialogueType _type = AwoPlayerDialogueType.Closest;
    private int _characterID = 0;
    private AwoDialogueIntensityState _intensityState = AwoDialogueIntensityState.Exploration;

    public AwoPlayerDialogueType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    [JsonIgnore]
    public AwoPlayerCharacter Character
    {
        get => (AwoPlayerCharacter)Math.Clamp(_characterID, 0, 3);
        set => CharacterID = (int)value;
    }

    public int CharacterID
    {
        get => _characterID;
        set
        {
            if (SetField(ref _characterID, value))
            {
                OnPropertyChanged(nameof(Character));
            }
        }
    }

    public AwoDialogueIntensityState IntensityState
    {
        get => _intensityState;
        set => SetField(ref _intensityState, value);
    }

    public AwoPlayerDialogueData Clone()
    {
        return new AwoPlayerDialogueData
        {
            Type = Type,
            CharacterID = CharacterID,
            IntensityState = IntensityState
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoTerminalLogType
{
    Add,
    Remove,
    Move
}

public class AwoSetTerminalLogData : INotifyPropertyChanged
{
    private int _terminalIndex = 0;
    private AwoTerminalLogType _type = AwoTerminalLogType.Add;
    private string _fileName = string.Empty;
    private string _fileContent = string.Empty;
    private int _fileContentOriginalLanguage = 1;
    private uint _attachedAudioFile = 0;
    private uint _attachedAudioByteSize = 0;
    private uint _playerDialogToTriggerAfterAudio = 0;
    private ObservableCollection<WardenEvent> _eventsOnFileRead = new();
    private int _targetDimensionIndex = 0;
    private SectorType _targetLayer = SectorType.Main;
    private int _targetLocalIndex = 0;
    private int _targetTerminalIndex = 0;

    [JsonIgnore]
    public bool IsMoveType => Type == AwoTerminalLogType.Move;

    public int TerminalIndex
    {
        get => _terminalIndex;
        set => SetField(ref _terminalIndex, value);
    }

    public AwoTerminalLogType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
            {
                OnPropertyChanged(nameof(IsMoveType));
            }
        }
    }

    public string FileName
    {
        get => _fileName;
        set => SetField(ref _fileName, value);
    }

    public string FileContent
    {
        get => _fileContent;
        set => SetField(ref _fileContent, value);
    }

    public int FileContentOriginalLanguage
    {
        get => _fileContentOriginalLanguage;
        set => SetField(ref _fileContentOriginalLanguage, value);
    }

    public uint AttachedAudioFile
    {
        get => _attachedAudioFile;
        set => SetField(ref _attachedAudioFile, value);
    }

    public uint AttachedAudioByteSize
    {
        get => _attachedAudioByteSize;
        set => SetField(ref _attachedAudioByteSize, value);
    }

    public uint PlayerDialogToTriggerAfterAudio
    {
        get => _playerDialogToTriggerAfterAudio;
        set => SetField(ref _playerDialogToTriggerAfterAudio, value);
    }

    public ObservableCollection<WardenEvent> EventsOnFileRead
    {
        get => _eventsOnFileRead;
        set => SetField(ref _eventsOnFileRead, value ?? new ObservableCollection<WardenEvent>());
    }

    public int TargetDimensionIndex
    {
        get => _targetDimensionIndex;
        set
        {
            if (SetField(ref _targetDimensionIndex, value))
            {
                OnPropertyChanged(nameof(AvailableTargetZones));
                OnPropertyChanged(nameof(AvailableTargetTerminals));
            }
        }
    }

    public SectorType TargetLayer
    {
        get => _targetLayer;
        set
        {
            if (SetField(ref _targetLayer, value))
            {
                OnPropertyChanged(nameof(AvailableTargetZones));
                OnPropertyChanged(nameof(AvailableTargetTerminals));
            }
        }
    }

    public int TargetLocalIndex
    {
        get => _targetLocalIndex;
        set
        {
            if (SetField(ref _targetLocalIndex, value))
            {
                OnPropertyChanged(nameof(AvailableTargetTerminals));
            }
        }
    }

    public int TargetTerminalIndex
    {
        get => _targetTerminalIndex;
        set => SetField(ref _targetTerminalIndex, value);
    }

    [JsonIgnore]
    public IEnumerable<ZoneOption> AvailableTargetZones
    {
        get
        {
            var level = LevelLayoutViewModel.Instance?.SelectedLevel;
            var list = new List<ZoneOption>();
            bool isReality = TargetDimensionIndex == 0;

            if (level != null && isReality)
            {
                var sectorZones = level.GridState.GetZonesForSector(TargetLayer);
                if (sectorZones.Count == 0)
                {
                    sectorZones.Add(0);
                }

                var sectorSettings = level.GetSectorSettings(TargetLayer);
                foreach (int z in sectorZones)
                {
                    list.Add(new ZoneOption
                    {
                        ZoneIndex = z,
                        DisplayName = sectorSettings.GetZoneDisplayName(z)
                    });
                }
            }
            else
            {
                list.Add(new ZoneOption
                {
                    ZoneIndex = 0,
                    DisplayName = "Zone 0"
                });
            }

            if (!list.Any(zo => zo.ZoneIndex == TargetLocalIndex))
            {
                string disp = level != null && isReality
                    ? level.GetSectorSettings(TargetLayer).GetZoneDisplayName(TargetLocalIndex)
                    : $"Zone {TargetLocalIndex}";
                list.Add(new ZoneOption
                {
                    ZoneIndex = TargetLocalIndex,
                    DisplayName = disp
                });
                list = list.OrderBy(zo => zo.ZoneIndex).ToList();
            }

            return list;
        }
    }

    [JsonIgnore]
    public IEnumerable<TerminalOption> AvailableTargetTerminals
    {
        get
        {
            var vm = LevelLayoutViewModel.Instance;
            if (vm?.GridState == null) return new[] { new TerminalOption(0, "Terminal 1") };
            var zoneSettings = vm.GridState.GetOrCreateZoneSettings(TargetLocalIndex);
            int count = zoneSettings.TerminalPlacements.Count;
            var list = new List<TerminalOption>();
            for (int i = 0; i < Math.Max(1, count); i++)
            {
                list.Add(new TerminalOption(i, $"Terminal {i + 1}"));
            }
            return list;
        }
    }

    public void AddEventOnFileRead() => EventsOnFileRead.Add(new WardenEvent());
    public void RemoveEventOnFileRead(WardenEvent? ev) { if (ev != null) EventsOnFileRead.Remove(ev); }

    [JsonIgnore]
    public ICommand RemoveEventOnFileReadCommand => new RelayCommand<WardenEvent>(RemoveEventOnFileRead);

    public AwoSetTerminalLogData Clone()
    {
        return new AwoSetTerminalLogData
        {
            TerminalIndex = TerminalIndex,
            Type = Type,
            FileName = FileName,
            FileContent = FileContent,
            FileContentOriginalLanguage = FileContentOriginalLanguage,
            AttachedAudioFile = AttachedAudioFile,
            AttachedAudioByteSize = AttachedAudioByteSize,
            PlayerDialogToTriggerAfterAudio = PlayerDialogToTriggerAfterAudio,
            EventsOnFileRead = new ObservableCollection<WardenEvent>(EventsOnFileRead.Select(e => e.Clone())),
            TargetDimensionIndex = TargetDimensionIndex,
            TargetLayer = TargetLayer,
            TargetLocalIndex = TargetLocalIndex,
            TargetTerminalIndex = TargetTerminalIndex
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwoPocketItemTagType
{
    Custom,
    Specific,
    Random,
    Closest
}

public class AwoPocketItem : INotifyPropertyChanged
{
    private int _index = 0;
    private int _count = 1;
    private bool _isOnTop = true;
    private string _itemName = string.Empty;
    private AwoPocketItemTagType _tagType = AwoPocketItemTagType.Custom;
    private int _playerIndex = 0;
    private string _customTag = string.Empty;

    public int Index
    {
        get => _index;
        set => SetField(ref _index, value);
    }

    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    public bool IsOnTop
    {
        get => _isOnTop;
        set => SetField(ref _isOnTop, value);
    }

    public string ItemName
    {
        get => _itemName;
        set => SetField(ref _itemName, value);
    }

    public AwoPocketItemTagType TagType
    {
        get => _tagType;
        set => SetField(ref _tagType, value);
    }

    [JsonIgnore]
    public AwoPlayerCharacter Player
    {
        get => (AwoPlayerCharacter)Math.Clamp(_playerIndex, 0, 3);
        set => PlayerIndex = (int)value;
    }

    public int PlayerIndex
    {
        get => _playerIndex;
        set
        {
            if (SetField(ref _playerIndex, value))
            {
                OnPropertyChanged(nameof(Player));
            }
        }
    }

    public string CustomTag
    {
        get => _customTag;
        set => SetField(ref _customTag, value);
    }

    public AwoPocketItem Clone()
    {
        return new AwoPocketItem
        {
            Index = Index,
            Count = Count,
            IsOnTop = IsOnTop,
            ItemName = ItemName,
            TagType = TagType,
            PlayerIndex = PlayerIndex,
            CustomTag = CustomTag
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoPocketItemsData : INotifyPropertyChanged
{
    private ObservableCollection<AwoPocketItem> _items = new();

    public ObservableCollection<AwoPocketItem> Items
    {
        get => _items;
        set => SetField(ref _items, value ?? new ObservableCollection<AwoPocketItem>());
    }

    public void AddItem() => Items.Add(new AwoPocketItem());
    public void RemoveItem(AwoPocketItem? item) { if (item != null) Items.Remove(item); }

    [JsonIgnore]
    public ICommand RemoveItemCommand => new RelayCommand<AwoPocketItem>(RemoveItem);

    public AwoPocketItemsData Clone()
    {
        return new AwoPocketItemsData
        {
            Items = new ObservableCollection<AwoPocketItem>(Items.Select(i => i.Clone()))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoEnvironmentData : INotifyPropertyChanged
{
    private string _environmentWetness = "Unchanged";
    private bool _updateColor = false;
    private string _dustColor = "white";
    private string _dustAlphaBoost = "Unchanged";
    private string _dustTurbulence = "Unchanged";

    public string EnvironmentWetness
    {
        get => _environmentWetness;
        set => SetField(ref _environmentWetness, value);
    }

    public bool UpdateColor
    {
        get => _updateColor;
        set => SetField(ref _updateColor, value);
    }

    public string DustColor
    {
        get => _dustColor;
        set => SetField(ref _dustColor, value);
    }

    public string DustAlphaBoost
    {
        get => _dustAlphaBoost;
        set => SetField(ref _dustAlphaBoost, value);
    }

    public string DustTurbulence
    {
        get => _dustTurbulence;
        set => SetField(ref _dustTurbulence, value);
    }

    public AwoEnvironmentData Clone()
    {
        return new AwoEnvironmentData
        {
            EnvironmentWetness = EnvironmentWetness,
            UpdateColor = UpdateColor,
            DustColor = DustColor,
            DustAlphaBoost = DustAlphaBoost,
            DustTurbulence = DustTurbulence
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AwoFadeScreenData : INotifyPropertyChanged
{
    private string _fadeColor = "black";
    private double _curveTimeStart = 0.0;
    private double _curveValueStart = 0.0;
    private double _curveTimeEnd = 1.0;
    private double _curveValueEnd = 1.0;
    private double _fadeSpeed = 1.0;

    public string FadeColor
    {
        get => _fadeColor;
        set => SetField(ref _fadeColor, value);
    }

    public double CurveTimeStart
    {
        get => _curveTimeStart;
        set => SetField(ref _curveTimeStart, value);
    }

    public double CurveValueStart
    {
        get => _curveValueStart;
        set => SetField(ref _curveValueStart, value);
    }

    public double CurveTimeEnd
    {
        get => _curveTimeEnd;
        set => SetField(ref _curveTimeEnd, value);
    }

    public double CurveValueEnd
    {
        get => _curveValueEnd;
        set => SetField(ref _curveValueEnd, value);
    }

    public double FadeSpeed
    {
        get => _fadeSpeed;
        set => SetField(ref _fadeSpeed, value);
    }

    public AwoFadeScreenData Clone()
    {
        return new AwoFadeScreenData
        {
            FadeColor = FadeColor,
            CurveTimeStart = CurveTimeStart,
            CurveValueStart = CurveValueStart,
            CurveTimeEnd = CurveTimeEnd,
            CurveValueEnd = CurveValueEnd,
            FadeSpeed = FadeSpeed
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
