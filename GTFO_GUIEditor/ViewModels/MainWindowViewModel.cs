namespace GTFO_GUIEditor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public SoundsViewModel Sounds { get; }
    public ComplexResourceSetsViewModel ComplexResourceSets { get; }

    public MainWindowViewModel()
    {
        Sounds = new SoundsViewModel();
        ComplexResourceSets = new ComplexResourceSetsViewModel();
    }
}
