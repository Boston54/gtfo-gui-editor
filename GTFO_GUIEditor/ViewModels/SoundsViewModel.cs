using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class SoundsViewModel : ViewModelBase
{
    private readonly List<SoundEntry> _allSounds;
    private readonly IClipboardService _clipboardService;
    private string _searchText = string.Empty;
    private ObservableCollection<SoundEntry> _filteredSounds;
    private string _statusText = string.Empty;
    private SoundEntry? _selectedSound;

    public SoundsViewModel(IClipboardService? clipboardService = null)
        : this(SoundService.LoadSounds(), clipboardService)
    {
    }

    public SoundsViewModel(IEnumerable<SoundEntry> sounds, IClipboardService? clipboardService = null)
    {
        _clipboardService = clipboardService ?? new AvaloniaClipboardService();
        _allSounds = sounds.ToList();
        _filteredSounds = new ObservableCollection<SoundEntry>(_allSounds);
        UpdateStatusText();
    }

    public ObservableCollection<SoundEntry> FilteredSounds
    {
        get => _filteredSounds;
        private set => SetField(ref _filteredSounds, value);
    }

    public SoundEntry? SelectedSound
    {
        get => _selectedSound;
        set
        {
            if (SetField(ref _selectedSound, value) && value != null)
            {
                _ = CopySoundIdAsync(value);
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public async Task<bool> CopySoundIdAsync(SoundEntry? sound)
    {
        if (sound == null) return false;

        string idToCopy = !string.IsNullOrEmpty(sound.IdString) ? sound.IdString : sound.Id.ToString();
        await _clipboardService.SetTextAsync(idToCopy);
        StatusText = $"Copied ID {idToCopy} to clipboard";
        return true;
    }

    public void ApplyFilter()
    {
        var query = _searchText?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            FilteredSounds = new ObservableCollection<SoundEntry>(_allSounds);
        }
        else
        {
            var filtered = _allSounds.Where(s => MatchesFilter(s, query)).ToList();
            FilteredSounds = new ObservableCollection<SoundEntry>(filtered);
        }
        UpdateStatusText();
    }

    public static bool MatchesFilter(SoundEntry sound, string query)
    {
        if (string.IsNullOrEmpty(query)) return true;

        var comparison = StringComparison.OrdinalIgnoreCase;

        if (sound.IdString.Contains(query, comparison)) return true;
        if (sound.Id.ToString().Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(sound.Name) && sound.Name.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(sound.WwisePath) && sound.WwisePath.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(sound.Event) && sound.Event.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(sound.Notes) && sound.Notes.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(sound.RawLine) && sound.RawLine.Contains(query, comparison)) return true;

        return false;
    }

    private void UpdateStatusText()
    {
        StatusText = $"Showing {FilteredSounds.Count:N0} of {_allSounds.Count:N0} sounds";
    }
}
