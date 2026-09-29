using System.Collections.ObjectModel;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.ViewModels;

public class GeomorphSubComplexViewModel : ViewModelBase
{
    private string _name;
    private string _key;
    private ObservableCollection<GeomorphCategoryViewModel> _categories;

    public GeomorphSubComplexViewModel(string name, string key)
    {
        _name = name;
        _key = key;
        _categories = new ObservableCollection<GeomorphCategoryViewModel>();
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    public ObservableCollection<GeomorphCategoryViewModel> Categories
    {
        get => _categories;
        set => SetField(ref _categories, value);
    }

    public void ApplyFilter(string query)
    {
        foreach (var category in _categories)
        {
            category.ApplyFilter(query);
        }
    }

    public override string ToString() => Name;
}
