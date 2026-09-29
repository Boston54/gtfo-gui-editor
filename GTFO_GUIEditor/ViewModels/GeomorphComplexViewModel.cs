using System.Collections.ObjectModel;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.ViewModels;

public class GeomorphComplexViewModel : ViewModelBase
{
    private string _name;
    private int _complexId;
    private ObservableCollection<GeomorphSubComplexViewModel> _subComplexes;

    public GeomorphComplexViewModel(string name, int complexId = 0)
    {
        _name = name;
        _complexId = complexId;
        _subComplexes = new ObservableCollection<GeomorphSubComplexViewModel>();
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public int ComplexId
    {
        get => _complexId;
        set => SetField(ref _complexId, value);
    }

    public ObservableCollection<GeomorphSubComplexViewModel> SubComplexes
    {
        get => _subComplexes;
        set => SetField(ref _subComplexes, value);
    }

    public void ApplyFilter(string query)
    {
        foreach (var subComplex in _subComplexes)
        {
            subComplex.ApplyFilter(query);
        }
    }

    public override string ToString() => Name;
}
