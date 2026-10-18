using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class TerminalPlacement : INotifyPropertyChanged
{
    private double _placementStartWeight;
    private double _placementMiddleWeight;
    private double _placementEndWeight;
    private int _areaSeedOffset;
    private int _markerSeedOffset;
    private TerminalStartingState _startingState = TerminalStartingState.Sleeping;
    private bool _useCustomInfoText;
    private string _customInfoText = string.Empty;
    private bool _keepShowingLocalLogCount;
    private bool _passwordProtected;
    private string _passwordHintText = "Password Required.";
    private bool _generatePassword;
    private int _passwordPartCount = 1;
    private bool _showPasswordLength = true;
    private bool _showPasswordPartPositions;
    private ObservableCollection<TerminalZoneSelectionData> _terminalZoneSelectionDatas = new();
    private ObservableCollection<TerminalUniqueCommand> _uniqueCommands = new();

    public double PlacementStartWeight
    {
        get => _placementStartWeight;
        set => SetField(ref _placementStartWeight, value);
    }

    public double PlacementMiddleWeight
    {
        get => _placementMiddleWeight;
        set => SetField(ref _placementMiddleWeight, value);
    }

    public double PlacementEndWeight
    {
        get => _placementEndWeight;
        set => SetField(ref _placementEndWeight, value);
    }

    public int AreaSeedOffset
    {
        get => _areaSeedOffset;
        set => SetField(ref _areaSeedOffset, value);
    }

    public int MarkerSeedOffset
    {
        get => _markerSeedOffset;
        set => SetField(ref _markerSeedOffset, value);
    }

    public TerminalStartingState StartingState
    {
        get => _startingState;
        set => SetField(ref _startingState, value);
    }

    public bool UseCustomInfoText
    {
        get => _useCustomInfoText;
        set => SetField(ref _useCustomInfoText, value);
    }

    public string CustomInfoText
    {
        get => _customInfoText;
        set => SetField(ref _customInfoText, value ?? string.Empty);
    }

    public bool KeepShowingLocalLogCount
    {
        get => _keepShowingLocalLogCount;
        set => SetField(ref _keepShowingLocalLogCount, value);
    }

    public bool PasswordProtected
    {
        get => _passwordProtected;
        set
        {
            if (SetField(ref _passwordProtected, value))
            {
                OnPropertyChanged(nameof(IsPasswordGenerationVisible));
            }
        }
    }

    public string PasswordHintText
    {
        get => _passwordHintText;
        set => SetField(ref _passwordHintText, value ?? string.Empty);
    }

    public bool GeneratePassword
    {
        get => _generatePassword;
        set
        {
            if (SetField(ref _generatePassword, value))
            {
                OnPropertyChanged(nameof(IsPasswordGenerationVisible));
            }
        }
    }

    public int PasswordPartCount
    {
        get => _passwordPartCount;
        set => SetField(ref _passwordPartCount, value);
    }

    public bool ShowPasswordLength
    {
        get => _showPasswordLength;
        set => SetField(ref _showPasswordLength, value);
    }

    public bool ShowPasswordPartPositions
    {
        get => _showPasswordPartPositions;
        set => SetField(ref _showPasswordPartPositions, value);
    }

    [JsonIgnore]
    public bool IsPasswordGenerationVisible => PasswordProtected && GeneratePassword;

    public ObservableCollection<TerminalZoneSelectionData> TerminalZoneSelectionDatas
    {
        get => _terminalZoneSelectionDatas;
        set => SetField(ref _terminalZoneSelectionDatas, value ?? new ObservableCollection<TerminalZoneSelectionData>());
    }

    public ObservableCollection<TerminalUniqueCommand> UniqueCommands
    {
        get => _uniqueCommands;
        set => SetField(ref _uniqueCommands, value ?? new ObservableCollection<TerminalUniqueCommand>());
    }

    public void AddTerminalZoneSelectionData()
    {
        var item = new TerminalZoneSelectionData();
        var firstZone = item.AvailableZones.FirstOrDefault();
        if (firstZone != null)
        {
            item.Zone = firstZone.ZoneIndex;
        }
        TerminalZoneSelectionDatas.Add(item);
    }

    public void RemoveTerminalZoneSelectionData(TerminalZoneSelectionData? item)
    {
        if (item != null) TerminalZoneSelectionDatas.Remove(item);
    }

    public void AddUniqueCommand() => UniqueCommands.Add(new TerminalUniqueCommand());
    public void RemoveUniqueCommand(TerminalUniqueCommand? cmd) { if (cmd != null) UniqueCommands.Remove(cmd); }

    public TerminalPlacement Clone()
    {
        return new TerminalPlacement
        {
            PlacementStartWeight = PlacementStartWeight,
            PlacementMiddleWeight = PlacementMiddleWeight,
            PlacementEndWeight = PlacementEndWeight,
            AreaSeedOffset = AreaSeedOffset,
            MarkerSeedOffset = MarkerSeedOffset,
            StartingState = StartingState,
            UseCustomInfoText = UseCustomInfoText,
            CustomInfoText = CustomInfoText,
            KeepShowingLocalLogCount = KeepShowingLocalLogCount,
            PasswordProtected = PasswordProtected,
            PasswordHintText = PasswordHintText,
            GeneratePassword = GeneratePassword,
            PasswordPartCount = PasswordPartCount,
            ShowPasswordLength = ShowPasswordLength,
            ShowPasswordPartPositions = ShowPasswordPartPositions,
            TerminalZoneSelectionDatas = new ObservableCollection<TerminalZoneSelectionData>(TerminalZoneSelectionDatas.Select(t => t.Clone())),
            UniqueCommands = new ObservableCollection<TerminalUniqueCommand>(UniqueCommands.Select(u => u.Clone()))
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
