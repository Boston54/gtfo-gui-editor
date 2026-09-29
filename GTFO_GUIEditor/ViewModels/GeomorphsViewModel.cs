using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class GeomorphsViewModel : ViewModelBase
{
    private readonly List<GeomorphEntry> _allGeomorphs;
    private readonly IClipboardService _clipboardService;
    private ObservableCollection<GeomorphComplexViewModel> _complexes;
    private ObservableCollection<GeomorphEntry> _filteredAllGeomorphs;
    private string _searchText = string.Empty;
    private string _statusText = string.Empty;
    private GeomorphEntry? _selectedGeomorph;
    private int _selectedComplexTabIndex;

    public GeomorphsViewModel(IClipboardService? clipboardService = null)
        : this(GeomorphService.LoadGeomorphs(), clipboardService)
    {
    }

    public GeomorphsViewModel(IEnumerable<GeomorphEntry> geomorphs, IClipboardService? clipboardService = null)
    {
        _clipboardService = clipboardService ?? new AvaloniaClipboardService();
        _allGeomorphs = geomorphs.ToList();
        _filteredAllGeomorphs = new ObservableCollection<GeomorphEntry>(_allGeomorphs);

        var hierarchy = GeomorphService.BuildHierarchy(_allGeomorphs);
        _complexes = new ObservableCollection<GeomorphComplexViewModel>(hierarchy);

        UpdateStatusText();
    }

    public List<GeomorphEntry> AllGeomorphs => _allGeomorphs;

    public ObservableCollection<GeomorphComplexViewModel> Complexes
    {
        get => _complexes;
        set => SetField(ref _complexes, value);
    }

    public ObservableCollection<GeomorphEntry> FilteredAllGeomorphs
    {
        get => _filteredAllGeomorphs;
        private set => SetField(ref _filteredAllGeomorphs, value);
    }

    public GeomorphEntry? SelectedGeomorph
    {
        get => _selectedGeomorph;
        set
        {
            if (SetField(ref _selectedGeomorph, value) && value != null)
            {
                _ = CopyPrefabPathAsync(value);
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

    public int SelectedComplexTabIndex
    {
        get => _selectedComplexTabIndex;
        set => SetField(ref _selectedComplexTabIndex, value);
    }

    public async Task<bool> CopyPrefabPathAsync(GeomorphEntry? geomorph)
    {
        if (geomorph == null || string.IsNullOrWhiteSpace(geomorph.PrefabPath))
        {
            return false;
        }

        await _clipboardService.SetTextAsync(geomorph.PrefabPath);
        StatusText = $"Copied prefab path {geomorph.PrefabPath} to clipboard";
        return true;
    }

    public void ApplyFilter()
    {
        var query = _searchText?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            FilteredAllGeomorphs = new ObservableCollection<GeomorphEntry>(_allGeomorphs);
        }
        else
        {
            var filtered = _allGeomorphs.Where(g => MatchesFilter(g, query)).ToList();
            FilteredAllGeomorphs = new ObservableCollection<GeomorphEntry>(filtered);
        }

        foreach (var complex in _complexes)
        {
            complex.ApplyFilter(query ?? string.Empty);
        }

        UpdateStatusText();
    }

    public static bool MatchesFilter(GeomorphEntry entry, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        var comparison = StringComparison.OrdinalIgnoreCase;

        if (!string.IsNullOrEmpty(entry.Name) && entry.Name.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.DisplayName) && entry.DisplayName.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.PrefabName) && entry.PrefabName.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.PrefabPath) && entry.PrefabPath.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.Description) && entry.Description.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.Complex) && entry.Complex.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.SubComplex) && entry.SubComplex.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.Category) && entry.Category.Contains(query, comparison)) return true;
        if (!string.IsNullOrEmpty(entry.ImageName) && entry.ImageName.Contains(query, comparison)) return true;

        return false;
    }

    private void UpdateStatusText()
    {
        StatusText = $"Showing {FilteredAllGeomorphs.Count:N0} of {_allGeomorphs.Count:N0} geomorphs";
    }
}
