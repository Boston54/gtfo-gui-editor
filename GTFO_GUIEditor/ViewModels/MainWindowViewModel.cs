namespace GTFO_GUIEditor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public SoundsViewModel Sounds { get; }
    public GeomorphsViewModel Geomorphs { get; }
    public ComplexResourceSetsViewModel ComplexResourceSets { get; }
    public AlarmsViewModel Alarms { get; }

    public MainWindowViewModel()
    {
        Sounds = new SoundsViewModel();
        Geomorphs = new GeomorphsViewModel();
        ComplexResourceSets = new ComplexResourceSetsViewModel();
        Alarms = new AlarmsViewModel();
    }
}
