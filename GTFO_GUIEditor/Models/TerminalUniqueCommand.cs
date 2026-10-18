using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GTFO_GUIEditor.Models;

public class TerminalUniqueCommand : INotifyPropertyChanged
{
    private string _command = string.Empty;
    private string _commandDesc = string.Empty;
    private ObservableCollection<TerminalPostCommandOutput> _postCommandOutputs = new();
    private ObservableCollection<WardenEvent> _commandEvents = new();

    public string Command
    {
        get => _command;
        set => SetField(ref _command, value);
    }

    public string CommandDesc
    {
        get => _commandDesc;
        set => SetField(ref _commandDesc, value);
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

    public void AddPostCommandOutput() => PostCommandOutputs.Add(new TerminalPostCommandOutput());
    public void RemovePostCommandOutput(TerminalPostCommandOutput? output) { if (output != null) PostCommandOutputs.Remove(output); }

    public void AddCommandEvent() => CommandEvents.Add(new WardenEvent());
    public void RemoveCommandEvent(WardenEvent? ev) { if (ev != null) CommandEvents.Remove(ev); }

    public TerminalUniqueCommand Clone()
    {
        return new TerminalUniqueCommand
        {
            Command = Command,
            CommandDesc = CommandDesc,
            PostCommandOutputs = new ObservableCollection<TerminalPostCommandOutput>(PostCommandOutputs.Select(p => p.Clone())),
            CommandEvents = new ObservableCollection<WardenEvent>(CommandEvents.Select(e => e.Clone()))
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
