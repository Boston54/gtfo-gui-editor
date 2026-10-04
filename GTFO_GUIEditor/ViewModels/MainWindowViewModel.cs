namespace GTFO_GUIEditor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public SoundsViewModel Sounds { get; }
    public GeomorphsViewModel Geomorphs { get; }
    public ComplexResourceSetsViewModel ComplexResourceSets { get; }
    public AlarmsViewModel Alarms { get; }
    public LevelLayoutViewModel LevelLayout { get; }
    public RundownViewModel Rundown { get; }

    public MainWindowViewModel()
    {
        Sounds = new SoundsViewModel();
        Geomorphs = new GeomorphsViewModel();
        ComplexResourceSets = new ComplexResourceSetsViewModel();
        Alarms = new AlarmsViewModel();
        LevelLayout = new LevelLayoutViewModel(ComplexResourceSets.Blocks);
        Rundown = new RundownViewModel(Services.RundownService.LoadVanillaBlocks(), LevelLayout.Levels, ComplexResourceSets.Blocks);
    }
}
