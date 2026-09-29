using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.ViewModels;

public class GeomorphCategoryViewModel : ViewModelBase
{
    private readonly List<GeomorphEntry> _allGeomorphs;
    private ObservableCollection<GeomorphEntry> _filteredGeomorphs;
    private string _name;

    public GeomorphCategoryViewModel(string name, IEnumerable<GeomorphEntry> geomorphs)
    {
        _name = name;
        _allGeomorphs = geomorphs.ToList();
        _filteredGeomorphs = new ObservableCollection<GeomorphEntry>(_allGeomorphs);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public List<GeomorphEntry> AllGeomorphs => _allGeomorphs;

    public ObservableCollection<GeomorphEntry> FilteredGeomorphs
    {
        get => _filteredGeomorphs;
        private set => SetField(ref _filteredGeomorphs, value);
    }

    public void ApplyFilter(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            FilteredGeomorphs = new ObservableCollection<GeomorphEntry>(_allGeomorphs);
        }
        else
        {
            var filtered = _allGeomorphs.Where(g => GeomorphsViewModel.MatchesFilter(g, query)).ToList();
            FilteredGeomorphs = new ObservableCollection<GeomorphEntry>(filtered);
        }
    }

    public override string ToString() => $"{Name} ({_allGeomorphs.Count})";
}
