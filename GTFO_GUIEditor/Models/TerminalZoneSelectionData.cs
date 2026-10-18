using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Models;

public class TerminalZoneSelectionData : INotifyPropertyChanged
{
    private int _zone;
    private int _terminal;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("Zone")]
    public int Zone
    {
        get => _zone;
        set
        {
            if (SetField(ref _zone, value))
            {
                OnPropertyChanged(nameof(AvailableTerminals));
            }
        }
    }

    [JsonPropertyName("Terminal")]
    public int Terminal
    {
        get => _terminal;
        set => SetField(ref _terminal, value);
    }

    [JsonIgnore]
    public IEnumerable<ZoneOption> AvailableZones
    {
        get
        {
            var vm = LevelLayoutViewModel.Instance;
            if (vm?.GridState == null) return Enumerable.Empty<ZoneOption>();

            var list = new List<ZoneOption>();
            var zoneIndices = vm.GridState.PlacedRooms
                .Select(r => r.Zone)
                .Distinct()
                .Where(z => vm.GridState.GetOrCreateZoneSettings(z).TerminalPlacements.Count > 0)
                .OrderBy(z => z)
                .ToList();

            foreach (int z in zoneIndices)
            {
                string name = vm.SelectedLevel != null ? vm.SelectedLevel.GetZoneDisplayName(z) : $"Zone {z}";
                list.Add(new ZoneOption { ZoneIndex = z, DisplayName = name });
            }

            if (list.Count > 0 && !list.Any(zo => zo.ZoneIndex == Zone))
            {
                string name = vm.SelectedLevel != null ? vm.SelectedLevel.GetZoneDisplayName(Zone) : $"Zone {Zone}";
                list.Add(new ZoneOption { ZoneIndex = Zone, DisplayName = name });
                list = list.OrderBy(zo => zo.ZoneIndex).ToList();
            }

            return list;
        }
    }

    [JsonIgnore]
    public IEnumerable<TerminalOption> AvailableTerminals
    {
        get
        {
            var vm = LevelLayoutViewModel.Instance;
            if (vm?.GridState == null) return Enumerable.Empty<TerminalOption>();

            var list = new List<TerminalOption>();
            var zoneSettings = vm.GridState.GetOrCreateZoneSettings(Zone);
            int terminalCount = zoneSettings.TerminalPlacements.Count;
            if (terminalCount > 0)
            {
                for (int i = 0; i < terminalCount; i++)
                {
                    list.Add(new TerminalOption(i, $"Terminal {i + 1}"));
                }
            }
            else
            {
                list.Add(new TerminalOption(0, "Terminal 1"));
            }

            return list;
        }
    }

    public TerminalZoneSelectionData Clone()
    {
        return new TerminalZoneSelectionData
        {
            Zone = Zone,
            Terminal = Terminal
        };
    }

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
